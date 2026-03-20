# Demo Snapshot Deployment

A demo snapshot is a Docker-based deployment of CountOrSell with demo mode active. It uses the standard application image - no separate demo image exists. The demo is configured entirely through environment variables and a seed script run against the database after first startup.

---

## 1. Overview

| Aspect | Details |
|--------|---------|
| Image | `ghcr.io/darkgrotto/countorsell:latest` (or a specific version tag) |
| Demo activation | `DEMO_MODE=true` environment variable |
| Countdown clock | `DEMO_EXPIRES_AT` environment variable (optional) |
| Seed data | `docker/scripts/demo-seed.sql` applied after first startup |
| Demo sets | lea, 2ed, vis, eoe, fdn, ecl, tla, fin, dsk, usg, ulg, uns, p23, tdm (fixed) |

Demo mode is detected at application startup and never changes at runtime. The instance name is overridden to `CountOrSell Demo` in all API responses regardless of the `INSTANCE_NAME` variable.

---

## 2. Prerequisites

- Docker Engine with Compose plugin, or Docker Desktop
- `psql` available on the host (for running the seed script)
- A real bcrypt hash for each demo account password (see Section 4)

---

## 3. Compose File

Create `docker-compose.demo.yml` in the repository root (or any working directory):

```yaml
services:
  postgres:
    image: postgres:16-alpine
    environment:
      POSTGRES_DB: countorsell
      POSTGRES_USER: countorsell
      POSTGRES_PASSWORD: countorsell
    volumes:
      - demo_postgres_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U countorsell"]
      interval: 5s
      timeout: 5s
      retries: 10

  app:
    image: ghcr.io/darkgrotto/countorsell:latest
    environment:
      POSTGRES_CONNECTION: "Host=postgres;Database=countorsell;Username=countorsell;Password=countorsell"
      INSTANCE_NAME: "CountOrSell Demo"
      DEMO_MODE: "true"
      DEMO_EXPIRES_AT: "2026-04-01T18:00:00Z"
      UPDATE_CHECK_TIME: "03:00"
    ports:
      - "8080:8080"
    depends_on:
      postgres:
        condition: service_healthy

volumes:
  demo_postgres_data:
```

Adjust `DEMO_EXPIRES_AT` to the desired expiry time, or omit it entirely if no countdown clock is needed.

The demo does not require the nginx reverse proxy or the backup service - HTTP on port 8080 is sufficient for a demo environment.

---

## 4. Prepare the Seed Script

The seed script at `docker/scripts/demo-seed.sql` contains placeholder bcrypt hashes that **must be replaced** before use. The placeholders are:

```
$2a$12$placeholder_admin_hash_replace_me_before_use
$2a$12$placeholder_user_hash_replace_me_before_use
```

Generate real bcrypt hashes for the demo passwords. The application enforces a minimum 15-character password, but the seed script bypasses validation since it inserts directly into the database - use any passwords you choose for demo accounts.

Example using the `bcrypt` Python library:
```bash
python3 -c "import bcrypt; print(bcrypt.hashpw(b'demoAdminPass1!', bcrypt.gensalt(12)).decode())"
python3 -c "import bcrypt; print(bcrypt.hashpw(b'demoUserPass1!', bcrypt.gensalt(12)).decode())"
```

Or using the `htpasswd` tool (bcrypt mode):
```bash
htpasswd -bnBC 12 "" demoAdminPass1! | tr -d ':\n'
```

Replace both placeholder strings in `docker/scripts/demo-seed.sql` with the generated hashes before proceeding.

---

## 5. First-Time Setup

**Step 1: Start the database and application**

```bash
docker compose -f docker-compose.demo.yml up -d
```

The application's `StartupMigrationService` runs all EF Core migrations automatically on first start. Wait until the `app` container is healthy before proceeding.

Check readiness:
```bash
curl http://localhost:8080/health
# Expected: {"status":"healthy","database":"reachable"}
```

**Step 2: Apply the seed script**

With the database running and migrations applied:

```bash
psql "Host=localhost;Database=countorsell;Username=countorsell;Password=countorsell" \
  -f docker/scripts/demo-seed.sql
```

Or using a standard connection string:
```bash
PGPASSWORD=countorsell psql \
  -h localhost -U countorsell -d countorsell \
  -f docker/scripts/demo-seed.sql
```

The seed script is idempotent (`ON CONFLICT DO NOTHING`) and can be run multiple times safely.

**Step 3: Verify demo mode**

```bash
curl http://localhost:8080/api/demo/status
```

Expected response:
```json
{
  "isDemo": true,
  "expiresAt": "2026-04-01T18:00:00+00:00",
  "secondsRemaining": 12345,
  "visitorId": "...",
  "demoSets": ["lea", "2ed", "vis", "eoe", "fdn", "ecl", "tla", "fin", "dsk", "usg", "ulg", "uns", "p23", "tdm"]
}
```

---

## 6. Demo Accounts

After seeding, two accounts are available:

| Account | Username | Role | Default Password |
|---------|----------|------|-----------------|
| Demo Admin | `demo-admin` | Admin | `demoAdminPass1!` (or whatever you hashed) |
| Demo User | `demo-user` | GeneralUser | `demoUserPass1!` (or whatever you hashed) |

The demo user has pre-seeded collection and wishlist entries covering the demo sets. The demo admin account has no collection.

---

## 7. Demo Behavior Reference

When `DEMO_MODE=true`:

- A persistent non-dismissible banner is shown to all users on every page
- The instance name is displayed as `CountOrSell Demo` everywhere
- A countdown clock is shown in the banner if `DEMO_EXPIRES_AT` is set
- Collection views show a filter scope indicator noting results are limited to demo sets
- The About view includes a demo environment section listing the active demo sets
- All read operations are unrestricted; visitors can fully explore the collection
- Visitors can add, modify, and remove their own collection and wishlist entries
- The following write operations are blocked and return HTTP 403:
  - `POST /api/collection/refresh-price/{cardIdentifier}`
  - `GET /api/wishlist/export/tcgplayer`
  - `POST /api/backup/trigger`
  - `POST /api/restore`
  - `POST /api/restore/{backupId}`
  - `POST /api/backup/destinations`
  - `DELETE /api/backup/destinations/{id}`
  - `PATCH /api/settings/instance`
  - `PATCH /api/settings/oauth/{provider}`
  - `DELETE /api/settings/oauth/{provider}`
  - `PATCH /api/settings/self-enrollment`
  - `POST /api/updates/check`
  - `POST /api/updates/schema/{id}/approve`
  - `POST /api/users/{id}/remove`

See [docs/api-reference.md](../api-reference.md) for the full endpoint listing.

---

## 8. Resetting Demo Data

The seed script is idempotent, but visitor-added collection entries are not removed by re-running it. To fully reset the demo database:

```bash
docker compose -f docker-compose.demo.yml down -v
docker compose -f docker-compose.demo.yml up -d
# Wait for health check, then:
PGPASSWORD=countorsell psql -h localhost -U countorsell -d countorsell \
  -f docker/scripts/demo-seed.sql
```

The `-v` flag removes the named volume, wiping all data including migrations. The app re-migrates on next startup.

---

## 9. Refreshing the Demo Image

To pull a newer application image:

```bash
docker compose -f docker-compose.demo.yml pull app
docker compose -f docker-compose.demo.yml up -d app
```

Schema migrations run automatically on startup if the new image includes them.
