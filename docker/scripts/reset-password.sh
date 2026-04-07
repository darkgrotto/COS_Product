#!/bin/sh
# Reset a local account password for a CountOrSell Docker deployment.
# Uses pgcrypto inside the Postgres container - no external tools required.
#
# Usage:
#   ./reset-password.sh <username>
#
# Override defaults with environment variables:
#   POSTGRES_CONTAINER=cos-postgres ./reset-password.sh <username>
#   DB_USER=admin DB_NAME=countorsell ./reset-password.sh <username>

set -e

POSTGRES_CONTAINER="${POSTGRES_CONTAINER:-cos-postgres}"
DB_USER="${DB_USER:-admin}"
DB_NAME="${DB_NAME:-countorsell}"

if [ -z "$1" ]; then
  echo "Usage: $0 <username>"
  exit 1
fi

USERNAME="$1"

echo "CountOrSell password reset"
echo "  Container : $POSTGRES_CONTAINER"
echo "  Database  : $DB_NAME"
echo "  Username  : $USERNAME"
echo ""

# Verify the container is running
if ! docker ps --format '{{.Names}}' | grep -q "^${POSTGRES_CONTAINER}$"; then
  echo "Error: container '$POSTGRES_CONTAINER' is not running."
  echo "Start your deployment first, then run this script."
  exit 1
fi

# Verify the user exists and is a local account
USER_CHECK=$(docker exec "$POSTGRES_CONTAINER" psql -U "$DB_USER" -d "$DB_NAME" -tA -c \
  "SELECT auth_type FROM users WHERE username = '$USERNAME' AND state != 'Removed';")

if [ -z "$USER_CHECK" ]; then
  echo "Error: user '$USERNAME' not found or has been removed."
  exit 1
fi

if [ "$USER_CHECK" != "Local" ]; then
  echo "Error: '$USERNAME' is an OAuth account. Password reset only applies to local accounts."
  exit 1
fi

# Read new password without echoing (POSIX compatible)
printf "New password (min 15 characters): "
stty -echo
read -r NEW_PASSWORD
stty echo
printf "\n"

printf "Confirm new password: "
stty -echo
read -r CONFIRM_PASSWORD
stty echo
printf "\n"

if [ "$NEW_PASSWORD" != "$CONFIRM_PASSWORD" ]; then
  echo "Error: passwords do not match."
  exit 1
fi

if [ "${#NEW_PASSWORD}" -lt 15 ]; then
  echo "Error: password must be at least 15 characters (got ${#NEW_PASSWORD})."
  exit 1
fi

# Ensure pgcrypto is available (NOTICE output discarded)
docker exec "$POSTGRES_CONTAINER" psql -U "$DB_USER" -d "$DB_NAME" -q -c \
  "CREATE EXTENSION IF NOT EXISTS pgcrypto;" > /dev/null

# Update the password hash using BCrypt (bf = Blowfish)
RESULT=$(docker exec "$POSTGRES_CONTAINER" psql -U "$DB_USER" -d "$DB_NAME" -tA -c \
  "UPDATE users
   SET password_hash = crypt('$NEW_PASSWORD', gen_salt('bf', 12)),
       updated_at    = now()
   WHERE username = '$USERNAME'
     AND auth_type  = 'Local'
     AND state     != 'Removed'
   RETURNING username;")

if [ "$RESULT" = "$USERNAME" ]; then
  echo "Password reset successfully for '$USERNAME'."
else
  echo "Error: password was not updated. Check the container logs for details."
  exit 1
fi
