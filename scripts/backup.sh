#!/bin/sh
# Scheduled pg_dump backup for CountOrSell.
# Runs inside a postgres:16-alpine container on the Compose network.
# Intended to be called by the sleep-loop entrypoint in docker-compose.prod.yml.
#
# Environment variables (same names as the app service):
#   DB_HOST      Postgres hostname (Compose service name)
#   DB_PORT      Postgres port (default: 5432)
#   DB_NAME      Database name (default: countorsell)
#   DB_USER      Postgres username (required)
#   DB_PASSWORD  Postgres password (required, passed via PGPASSWORD)
#
# Backup files:
#   /backups/countorsell-backup-YYYY-MM-DDTHHMM.dump  (Postgres custom format)
#   Retention: controlled by BACKUP_RETENTION env var (default: 4).

set -e

BACKUP_DIR="/backups"
RETENTION="${BACKUP_RETENTION:-4}"

DB_HOST="${DB_HOST:?DB_HOST is required}"
DB_PORT="${DB_PORT:-5432}"
DB_NAME="${DB_NAME:-countorsell}"
DB_USER="${DB_USER:?DB_USER is required}"

export PGPASSWORD="${DB_PASSWORD:?DB_PASSWORD is required}"

timestamp=$(date -u +"%Y-%m-%dT%H%M")
filename="countorsell-backup-${timestamp}.dump"
filepath="${BACKUP_DIR}/${filename}"

log() {
    echo "$(date -u +"%Y-%m-%dT%H:%M:%SZ") [backup] $1"
}

log "Starting backup to ${filename}"

if pg_dump \
    -h "$DB_HOST" \
    -p "$DB_PORT" \
    -U "$DB_USER" \
    -d "$DB_NAME" \
    -Fc \
    -f "$filepath"; then
    size=$(du -h "$filepath" | cut -f1)
    log "Backup complete: ${filename} (${size})"
else
    log "ERROR: pg_dump failed with exit code $?"
    exit 1
fi

# Prune old backups, keeping the most recent $RETENTION files.
# ls -t sorts by modification time, newest first. tail outputs everything
# after the first $RETENTION lines, which are the oldest files to remove.
removed=0
for old in $(ls -t "${BACKUP_DIR}"/countorsell-backup-*.dump 2>/dev/null | tail -n +$((RETENTION + 1))); do
    rm -f "$old"
    removed=$((removed + 1))
    log "Pruned old backup: $(basename "$old")"
done

if [ "$removed" -gt 0 ]; then
    log "Pruned ${removed} old backup(s), ${RETENTION} retained"
fi
