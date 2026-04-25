# CountOrSell

CountOrSell is a self-hostable web application for tracking Magic: The Gathering collections, sealed product inventory, serialized cards, and graded/slabbed cards. It provides market value tracking, profit/loss calculations, set completion metrics, and wishlist management. All canonical card and set data is delivered through update packages from countorsell.com - the application holds no data authority of its own.

## Features

- **Standard card collection** - track cards by identifier, treatment, condition, quantity, acquisition price, and notes
- **Serialized card tracker** - track individual serialized cards with serial number and print run fields
- **Slab tracker** - track graded cards with grading agency, grade, certificate number, and optional serial fields
- **Sealed product inventory** - track sealed product quantities and acquisition cost
- **Market value and profit/loss** - per-entry, per-set, per-content-type, and whole-collection calculations
- **Set completion tracking** - raw count and percentage, with optional regular-treatment-only mode per user
- **Wishlist management** - per-user wishlists with current market values and total
- **Universal filtering** - filter by set, color, condition, card type, treatment, autographed, serialized, slabbed, sealed product, and grading agency across all views
- **TCGPlayer direct price refresh** - single-card price queries using a user or admin-supplied API key
- **Content update system** - automatic daily content updates from countorsell.com, admin-approved schema updates
- **Backup and restore** - scheduled and pre-update backups to local, Azure Blob, AWS S3, or GCP Storage destinations
- **Multi-provider deployment** - Azure App Service, AWS App Runner, GCP Cloud Run, or Docker Compose
- **Local and OAuth authentication** - local accounts plus Google, Microsoft, and GitHub OAuth (configured post-setup)
- **Two-role model** - Admin and GeneralUser, with a protected built-in local admin account
- **Self-enrollment** - configurable post-setup, off by default

## Deployment Options

| Provider | Compute | Database | Storage | Secrets |
|----------|---------|----------|---------|---------|
| Azure | App Service | Azure Database for PostgreSQL | Azure Blob Storage | Azure Key Vault |
| AWS | App Runner | Amazon RDS for PostgreSQL | AWS S3 | AWS Secrets Manager |
| GCP | Cloud Run | Cloud SQL for PostgreSQL | Google Cloud Storage | Google Secret Manager |
| Docker | Docker Compose | PostgreSQL container | Local file or any configured destination | Environment variables in managed .env file |

## Quick Start

See [docs/setup.md](docs/setup.md) for the full setup guide.

**Prerequisites summary:**
- .NET 8 SDK (to run the wizard)
- Docker Desktop with Docker Compose v2 (Docker deployments)
- Terraform >= 1.5.0 (cloud deployments)
- Azure CLI / AWS CLI / Google Cloud CLI (cloud deployments, provider-specific)

Run the first-run wizard to configure and deploy:

```
dotnet run --project src/CountOrSell.Wizard
```

## Documentation

| Document | Description |
|----------|-------------|
| [docs/setup.md](docs/setup.md) | First-run wizard walkthrough, post-setup configuration, verification |
| [docs/configuration.md](docs/configuration.md) | Environment variables, application settings, backup destinations, OAuth, TCGPlayer |
| [docs/user-management.md](docs/user-management.md) | Roles, account states, removal export, OAuth, self-enrollment |
| [docs/collection-management.md](docs/collection-management.md) | All four collection types, identifiers, treatments, conditions, filters |
| [docs/metrics-and-values.md](docs/metrics-and-values.md) | Collection value, profit/loss, set completion, wishlist value |
| [docs/updates.md](docs/updates.md) | Content updates, schema updates, application version updates, manifest format |
| [docs/backup-restore.md](docs/backup-restore.md) | Backup scope, types, format, destinations, restore workflow |
| [docs/deployment/docker.md](docs/deployment/docker.md) | Docker Compose deployment, generated files, update procedure |
| [docs/deployment/cloud.md](docs/deployment/cloud.md) | Azure, AWS, and GCP deployment via Terraform and GitHub Actions |
| [docs/api-reference.md](docs/api-reference.md) | All API endpoints grouped by controller |

## Development

**Stack:** .NET 8 (ASP.NET Core API), React 18 + TypeScript (Vite), PostgreSQL

**Build the API:**
```
dotnet restore src/CountOrSell.sln
dotnet build src/CountOrSell.sln --configuration Release
```

**Run API tests:**
```
dotnet test src/CountOrSell.sln --configuration Release
```

**Build the client:**
```
cd src/CountOrSell.Api/Client
npm install
npm run build
```

A local PostgreSQL instance is required to run the API locally. Set the `POSTGRES_CONNECTION` environment variable or configure it in `appsettings.Development.json`.

## License

CountOrSell is licensed under [CC BY-NC-SA 4.0](https://creativecommons.org/licenses/by-nc-sa/4.0/).

You may use, share, and adapt it for non-commercial purposes provided you give attribution and distribute any adaptations under the same license. See [LICENSE](LICENSE) for the full terms.
