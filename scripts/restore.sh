#!/bin/sh
# Restore a CountOrSell database from a backup file.
# Runs on the host and uses docker exec to reach the Postgres container,
# so psql and pg_restore do not need to be installed locally.
#
# Backups are stored by the cos-backup container in a named Docker volume
# (cos_backup_data, mounted at /backups). This script reads the volume
# through the cos-backup container, copies the selected file into the
# cos-postgres container, and runs pg_restore there.
#
# Usage:
#   bash scripts/restore.sh
#
# Override defaults with environment variables:
#   POSTGRES_CONTAINER=cos-postgres BACKUP_CONTAINER=cos-backup \
#     DB_USER=admin DB_NAME=countorsell bash scripts/restore.sh
#
# What happens during a restore:
#   1. Existing tables in the target database are dropped (--clean)
#   2. Schema and data are restored from the selected backup
#   3. The application should be restarted after a successful restore
#      so it picks up the restored state

set -e

POSTGRES_CONTAINER="${POSTGRES_CONTAINER:-cos-postgres}"
BACKUP_CONTAINER="${BACKUP_CONTAINER:-cos-backup}"
DB_USER="${DB_USER:-admin}"
DB_NAME="${DB_NAME:-countorsell}"

# --- Preflight checks -------------------------------------------------------

if ! command -v docker >/dev/null 2>&1; then
    echo "Error: docker is not installed or not in PATH."
    exit 1
fi

for ctr in "$POSTGRES_CONTAINER" "$BACKUP_CONTAINER"; do
    if ! docker ps --format '{{.Names}}' | grep -q "^${ctr}$"; then
        echo "Error: container '${ctr}' is not running."
        echo "Start your deployment first:  docker compose -f docker-compose.prod.yml up -d"
        exit 1
    fi
done

# --- List available backups --------------------------------------------------

echo ""
echo "CountOrSell Database Restore"
echo "============================"
echo ""
echo "Scanning for backups..."
echo ""

# List backup files inside the backup container (where the volume is mounted).
# Sort newest-first so the most recent backup is option 1.
BACKUP_LIST=$(docker exec "$BACKUP_CONTAINER" \
    sh -c 'ls -1t /backups/countorsell-backup-*.dump 2>/dev/null' || true)

if [ -z "$BACKUP_LIST" ]; then
    echo "No backup files found in the ${BACKUP_CONTAINER} container."
    echo "Backups are created automatically every 6 hours."
    echo "If the system was just started, wait for the first backup cycle."
    exit 1
fi

# Number each file and print a human-readable line.
# Filename format: countorsell-backup-YYYY-MM-DDTHHMM.dump
echo "Available backups (newest first):"
echo ""

i=1
for filepath in $BACKUP_LIST; do
    filename=$(basename "$filepath")

    # Extract the timestamp portion: YYYY-MM-DDTHHMM
    ts=$(echo "$filename" | sed 's/countorsell-backup-//;s/\.dump//')
    # Reformat to readable: "2026-07-13 at 04:00 UTC"
    date_part=$(echo "$ts" | cut -c1-10)
    time_part=$(echo "$ts" | cut -c12-13):$(echo "$ts" | cut -c14-15)

    # Get file size via the backup container
    size=$(docker exec "$BACKUP_CONTAINER" du -h "$filepath" | cut -f1)

    printf "  %2d)  %s at %s UTC  (%s)\n" "$i" "$date_part" "$time_part" "$size"
    i=$((i + 1))
done

total=$((i - 1))
echo ""

# --- Select a backup --------------------------------------------------------

printf "Enter backup number [1-%d]: " "$total"
read -r selection

# Validate the input is a number in range
case "$selection" in
    ''|*[!0-9]*)
        echo "Error: '${selection}' is not a valid number."
        exit 1
        ;;
esac

if [ "$selection" -lt 1 ] || [ "$selection" -gt "$total" ]; then
    echo "Error: selection must be between 1 and ${total}."
    exit 1
fi

# Retrieve the selected filepath
chosen=$(echo "$BACKUP_LIST" | sed -n "${selection}p")
chosen_name=$(basename "$chosen")
echo ""
echo "Selected: ${chosen_name}"

# --- Confirm -----------------------------------------------------------------

echo ""
echo "WARNING: This will drop all existing data in the '${DB_NAME}' database"
echo "         and replace it with the contents of this backup."
echo ""
echo "         The application container should be restarted after restore."
echo ""
printf "Type 'yes' to proceed: "
read -r confirm

if [ "$confirm" != "yes" ]; then
    echo ""
    echo "Restore cancelled."
    exit 0
fi

echo ""

# --- Restore -----------------------------------------------------------------

# Copy the backup file from the backup container to the postgres container.
# We pipe through docker exec stdin rather than using a shared volume, so
# this works even if the two containers mount different volumes.
echo "Copying backup into postgres container..."
docker exec "$BACKUP_CONTAINER" cat "$chosen" \
    | docker exec -i "$POSTGRES_CONTAINER" sh -c 'cat > /tmp/restore.dump'

echo "Restoring database (this may take a moment)..."

# pg_restore flags:
#   --clean        Drop existing objects before restoring
#   --if-exists    Don't error on DROP for objects that don't exist yet
#   --no-owner     Skip ownership commands (container user owns everything)
#   --no-privileges Skip GRANT/REVOKE (single-user container setup)
#   -d             Target database
#   -U             Database user
#
# pg_restore exits 1 for warnings (e.g. "table does not exist" on --clean
# when restoring to an empty database). Only exit codes >1 are true failures.
# The "|| true" prevents set -e from aborting on warnings; we capture the
# real exit code and check it ourselves.
restore_exit=0
docker exec "$POSTGRES_CONTAINER" \
    pg_restore \
        --clean \
        --if-exists \
        --no-owner \
        --no-privileges \
        -U "$DB_USER" \
        -d "$DB_NAME" \
        /tmp/restore.dump \
    || restore_exit=$?

# Clean up the temporary file
docker exec "$POSTGRES_CONTAINER" rm -f /tmp/restore.dump

echo ""

if [ "$restore_exit" -le 1 ]; then
    echo "Restore completed successfully from: ${chosen_name}"
    echo ""
    echo "Next steps:"
    echo "  1. Restart the application:  docker compose -f docker-compose.prod.yml restart app"
    echo "  2. Verify the application is healthy:  curl http://localhost:${PORT:-3000}/health"
else
    echo "ERROR: Restore failed. The database may be in an inconsistent state."
    echo ""
    echo "Recovery options:"
    echo "  1. Run this script again with a different backup"
    echo "  2. Check postgres logs:  docker logs ${POSTGRES_CONTAINER}"
    exit 1
fi
