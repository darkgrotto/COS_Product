# Setup Guide

This guide covers running the first-run wizard, post-setup configuration, and verifying that the instance is working.

---

## 1. Prerequisites

Install the required tools before running the wizard. The wizard detects missing prerequisites and provides installation instructions, but it cannot proceed until all requirements are met.

### All deployments

| Requirement | Purpose | Installation |
|-------------|---------|--------------|
| .NET 8 SDK | Runs the wizard | https://dotnet.microsoft.com/download |

### Docker deployments

| Requirement | Purpose | Installation |
|-------------|---------|--------------|
| Docker Desktop (Windows/Mac) or Docker Engine (Linux) | Runs containers | https://docs.docker.com/get-docker/ |
| Docker Compose v2 | Orchestrates services | Included with Docker Desktop; standalone Linux: https://docs.docker.com/compose/install/ |

Docker Desktop must be installed by the user - the wizard cannot auto-install it. The wizard will detect whether `docker` and `docker compose` are available and block progress until they are.

### Cloud deployments (Azure, AWS, GCP)

| Requirement | Provider | Installation |
|-------------|----------|--------------|
| Terraform >= 1.5.0 | All cloud | https://developer.hashicorp.com/terraform/install |
| Azure CLI (`az`) | Azure only | https://learn.microsoft.com/en-us/cli/azure/install-azure-cli |
| AWS CLI (`aws`) | AWS only | https://docs.aws.amazon.com/cli/latest/userguide/getting-started-install.html |
| Google Cloud CLI (`gcloud`) | GCP only | https://cloud.google.com/sdk/docs/install |

---

## 2. First-Run Wizard

Start the wizard:

```
dotnet run --project src/CountOrSell.Wizard
```

The wizard runs 17 steps in sequence. It collects all required configuration before executing deployment, and will not proceed past any step that has unmet requirements.

### Step 1 - Deployment type selection

Choose one of:
- `1` - Azure (App Service)
- `2` - AWS (App Runner)
- `3` - GCP (Cloud Run)
- `4` - Docker Compose

### Step 2 - Prerequisite detection

The wizard checks for required tools based on the deployment type selected in Step 1. For each missing tool it prints the name and install instructions. The wizard re-checks after you confirm installation and will not proceed until all prerequisites are present.

Docker deployments require: `docker`, `docker compose`
Cloud deployments require: `terraform`, plus the provider CLI (`az`, `aws`, or `gcloud`)

### Step 3 - Docker image registry (Docker deployments only)

Provide the registry host where the CountOrSell Docker image is hosted. This is the registry you specified when setting up CI/CD. The value is written to the `.env` file as `REGISTRY`.

Cloud deployments skip this step.

### Step 4 - Environment-specific configuration

**Cloud deployments:** Enter cloud credentials and region. The wizard collects what Terraform needs to authenticate - refer to your provider documentation for how to create a service principal or IAM credentials.

**Docker deployments:** No additional environment configuration is collected at this step.

### Step 5 - Hosting preferences

- **Subdomain / hostname:** The public hostname the instance will be reachable at.
- **Port:** The port the reverse proxy will listen on (Docker deployments). Defaults are provided.

### Step 6 - SSL certificate generation

For Docker deployments, a self-signed certificate is generated and placed in `docker/certs/`. The reverse proxy (nginx) serves HTTPS using this certificate.

Cloud deployments use provider-native SSL (App Service, App Runner, Cloud Run all terminate TLS natively) and do not generate a self-signed certificate.

### Step 7 - Instance branding

Enter the instance name (text only). This appears in the page title, application header, and browser tab. It is stored as the `INSTANCE_NAME` environment variable and displayed in the About view.

### Step 8 - Database admin account

Create the PostgreSQL database administrator account.

- Username: any non-empty string
- Password: **minimum 15 characters**, enforced by the wizard

This account is the PostgreSQL superuser for the instance database. It is separate from the CountOrSell application admin account created in Step 9.

### Step 9 - Product admin account

Create the built-in local admin account for the CountOrSell application.

- Always a local account - never OAuth
- Username: any non-empty string
- Password: **minimum 15 characters**, enforced by the wizard

This account cannot be removed, disabled, demoted, or converted to OAuth. It provides guaranteed emergency access to the admin panel. See [docs/user-management.md](user-management.md) for details.

### Step 10 - General user account

Create exactly one general user account during the wizard.

- Local account
- Username: any non-empty string
- Password: **minimum 15 characters**, enforced by the wizard

Additional users (local or OAuth) are created post-setup from the admin panel. Self-enrollment is off by default.

### Step 11 - Backup destination configuration

Select and configure the primary backup destination. One destination is required during the wizard; additional destinations can be added post-setup from the admin panel.

| Option | Configuration collected |
|--------|------------------------|
| Azure Blob Storage | Connection string |
| AWS S3 | Bucket name, AWS region |
| GCP Storage | GCS bucket name |
| Local file export | Local directory path (default: `./backups`) |

Note: Azure Blob, AWS S3, and GCP Storage backup destination implementations are stubs pending SDK integration. Local file export is fully implemented.

### Step 12 - Backup schedule

Set how often scheduled backups run. Accepted values:

| Input | Cron expression | Meaning |
|-------|----------------|---------|
| `weekly` (default) | `0 2 * * 0` | Every Sunday at 02:00 UTC |
| `daily` | `0 2 * * *` | Every day at 02:00 UTC |
| `monthly` | `0 2 1 * *` | First day of each month at 02:00 UTC |
| Any cron expression | Used as-is | Custom schedule |

Press Enter to accept the default (weekly).

### Step 13 - Backup retention

Set how many backups of each type to retain. Scheduled backups and pre-update backups are counted independently. Default is 4 of each. Enter a positive integer or press Enter for the default.

### Step 14 - Initial update download

Choose whether to download and apply the initial content update from countorsell.com immediately after deployment. Default is yes (`Y`). If you skip this, you can trigger the initial update manually from the admin panel.

### Step 15 - Generate files (Docker deployments only)

For Docker deployments, the wizard generates:
- `docker/compose/docker-compose.yml` - the managed Compose file
- `docker/scripts/update.sh` - the application update script
- `.env` - environment variables for the Compose file

Do not edit these files manually. Re-run the wizard to regenerate them if changes are needed.

Cloud deployments already have Terraform files in `infrastructure/`. The wizard records configuration needed for Terraform apply.

### Step 16 - Deployment execution

The wizard runs the deployment:
- **Docker:** `docker compose up -d` using the generated Compose file
- **Cloud:** `terraform init` and `terraform apply` in the appropriate `infrastructure/<provider>/` directory

### Step 17 - Random daily update check time generation

The wizard generates a random time (HH:MM format) for the daily content update check. This randomization spreads load across all instances. The value is stored as `UPDATE_CHECK_TIME` in the `.env` file.

---

## 3. Post-Setup Configuration

The following are configured after the wizard completes, from the admin panel.

### OAuth providers

OAuth is not configured during the wizard. To enable OAuth login, configure one or more providers in admin settings. Supported providers:

- Google
- Microsoft (supports both personal Live accounts and Entra ID organizational accounts)
- GitHub

For each provider you need a client ID and client secret obtained from the provider's developer console. See [docs/configuration.md](configuration.md) for the configuration keys.

### Additional backup destinations

The wizard configures one backup destination. To add more, use the admin panel (Settings > Backup > Add Destination). All active destinations receive every backup. The `POST /api/backup/destinations` endpoint accepts a `destinationType` of `local`, `azure-blob`, `aws-s3`, or `gcp-storage`.

### Self-enrollment

Self-enrollment is off by default. When enabled, new users who sign up (locally or via OAuth) receive immediate general user access with no admin approval step. Admins can enable or disable this at any time from the admin panel.

### TCGPlayer API key

A TCGPlayer API key enables per-card price refresh from TCGPlayer directly. The key can be supplied by any admin from the admin settings. Without a key, the refresh-price feature is unavailable. See [docs/configuration.md](configuration.md) for details.

---

## 4. Verifying the Installation

**Health endpoint:**

```
GET /health
```

Returns `{"status":"healthy","database":"reachable"}` with HTTP 200 when the application and database are up. Returns HTTP 503 when unhealthy.

**Login:**

Navigate to the instance URL and log in with the product admin account created in Step 9. The admin panel should be accessible. The general user account created in Step 10 should be able to log in and see the collection dashboard.

**About view:**

Any authenticated user can access the About view (`/api/about`), which shows the current application version, latest released version, update pending status, last content update, and instance name.

---

## 5. Docker-Specific Notes

### Generated files

The wizard generates three files for Docker deployments:

- `docker/compose/docker-compose.yml` - defines four services: `app`, `postgres`, `reverse-proxy` (nginx), `backup`
- `docker/scripts/update.sh` - wraps `docker compose pull` and `docker compose up -d`
- `.env` - environment variables read by the Compose file

These files are managed by the wizard. Do not edit them directly. If configuration changes are needed, re-run the wizard.

### Services

| Service | Image | Purpose |
|---------|-------|---------|
| `app` | `${REGISTRY}/countorsell:latest` | CountOrSell API and client |
| `postgres` | `postgres:16-alpine` | PostgreSQL database |
| `reverse-proxy` | `nginx:alpine` | SSL termination, port exposure |
| `backup` | `${REGISTRY}/countorsell-backup:latest` | Scheduled backup container |

PostgreSQL data is stored in the named volume `countorsell_postgres_data`. The `app` service waits for PostgreSQL to pass its healthcheck before starting.

No container has access to the Docker socket.

### Updating the application

When a new application version is available, the admin panel displays a notification with the exact command to run:

```
./docker/scripts/update.sh
```

This script runs `docker compose pull` to fetch updated images and `docker compose up -d` to recreate containers with the new versions. Schema migrations run automatically on startup if the new version includes them.

### Startup schema migration

On every container start, the application checks for pending schema migrations. If migrations are needed:
1. A pre-update backup is taken silently.
2. If the backup succeeds, migrations run.
3. If the backup fails, startup is aborted and the admin is notified.
4. If migration fails after a successful backup, the backup is restored automatically and startup is aborted.

See [docs/backup-restore.md](backup-restore.md) for the full migration and restore behavior.
