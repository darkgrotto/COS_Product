# CLAUDE.md - Product (CountOrSell)

## Critical Code Standards
- Use standard hyphens (-) only in all code, comments, configuration, and content. Em-dashes, en-dashes, and similar Unicode dash characters are never acceptable and will cause errors. Applies to every file in this repo without exception.
- Use the most specific and strongly-typed approach for all data fields, structures, and formats - prefer explicit types and constrained formats over loose strings wherever possible.
- Use secure programming methods and create tests that are security-specific for all user input paths.

## Identifier Formats and Validation
These patterns apply everywhere - validation, storage, display, documentation, and examples.

**Set code:** `^[a-z0-9]{3,4}$` - stored lowercase (e.g. "eoe", "3ed"), displayed uppercase in all UI (e.g. "EOE"). Never accept or store a set code as a display name.

**Card identifier:** `^[a-z0-9]{3,4}[0-9]{3,4}[a-z]?$`
- Numeric suffix: zero-padded to 3 digits for 001-999; 4 digits unpadded for 1000-9999. A 4-digit suffix must be >= 1000 ("0123" is never valid).
- Optional single trailing lowercase letter for letter-suffixed collector numbers (e.g. "pala001a", "pala001b"). Cards differing only by trailing letter are distinct.
- The letter "x" is permanently reserved as the synthetic mapping for Scryfall collector numbers ending in dagger (†). e.g. DRK/77† -> "drk077x". "x" will never appear as a real collector number letter.
- Stored lowercase always (e.g. "eoe019", "eoe1234", "pala001a", "drk077x"); displayed uppercase in all UI.
- Valid examples: "eoe019", "eoe999", "eoe1234", "3ed019", "pala001a", "pala001b", "drk077x", "arn002x"

## What This Repo Is
The self-hostable CountOrSell product - a web application for tracking collectible card game collections, sealed product inventory, serialized cards, and graded/slabbed cards. Includes market value tracking, collection metrics, and wishlist functionality.

Purposes: (1) Collection tracking (cards, sealed product, serialized cards, slabs), (2) Market value and profit/loss tracking, (3) Collection metrics and set completion, (4) Wishlist management, (5) Receiving and applying content updates from countorsell.com.

## What This Repo Is NOT
- Not the Admin Backend - no canonical data authoring, submission review, or publishing
- Not the Website - does not serve public content or handle community submissions
- Not a data authority - all canonical content comes from update packages published by the Admin Backend
- Not responsible for layer resolution - always receives fully resolved flat data, never applies layer logic
- Not connected to the Admin Backend directly - update source is countorsell.com only, no custom sources

## Related Repos
- Website: https://www.github.com/darkgrotto/COS_Website - source of update manifests and packages; destination for community submissions
- Admin Backend: https://www.github.com/darkgrotto/COS_Backend - authors and publishes all canonical content; not directly accessible to Product instance users

## Do Not
- Do not hardcode treatment values - always reference the treatments table received in update packages
- Do not add layer resolution logic - the Product always receives a fully resolved flat view
- Do not add a custom update source option - countorsell.com is the only valid update source
- Do not expose Admin Backend access to Product users
- Do not introduce iOS/Android or React Native dependencies
- Do not use SQLite - PostgreSQL only
- Do not use em-dashes or special dash characters anywhere
- Do not hardcode sealed product category or sub-type slugs or display names; always reference the taxonomy reference tables received in update packages
- Do not display taxonomy slugs in the UI; always use display_name values from the reference tables
- Do not add an is_active or current flag to taxonomy entries; current vs. legacy product type distinction is not modeled

---

## Tech Stack
- Runtime: dotnet with React
- Database: PostgreSQL
- Containerization: Docker Compose (managed, no fallback to single-container SQLite)
- Authentication: local accounts plus OAuth via Google, Microsoft (Live and Entra ID), and GitHub
- TCGPlayer pricing: direct API (user or admin-supplied key, CountOrSell does not proxy this connection)
- CI/CD: GitHub Actions
- IaC: Terraform with provider-native state storage

---

## Deployment Types

Supported targets: Azure (App Service, Azure Database for PostgreSQL, Azure Blob Storage, Azure Key Vault), AWS (App Runner, Amazon RDS for PostgreSQL, AWS S3, AWS Secrets Manager), GCP (Cloud Run, Cloud SQL for PostgreSQL, Google Cloud Storage, Google Secret Manager), Docker Compose.

Linode is not a supported deployment target. Do not prevent adding it later architecturally.

| | Azure | AWS | GCP | Docker |
|---|---|---|---|---|
| Compute | App Service | App Runner | Cloud Run | Docker Compose |
| SSL | App Service native | App Runner via ACM | Cloud Run native HTTPS | Self-signed (wizard) |
| Secrets | Key Vault | Secrets Manager | Secret Manager | Env vars in .env |
| Backup default | Azure Blob | S3 | GCS | Local or any configured |
| TF state | Azure Blob | S3 | GCS | - |

User can configure any supported backup destination regardless of deployment provider. Wizard defaults to provider-native but does not require it. No cross-provider Terraform state dependency.

### Docker Compose Services
Generated Compose file includes: Product application container, PostgreSQL container with named volume, Nginx reverse proxy for SSL termination, backup container with scheduled task. The Compose file is managed and regenerated by wizard if changes are needed. Users do not edit it directly. A generated update.sh script is provided for application version updates. No Docker socket access from within any container.

### Instance Branding
Text only (instance name). Appears in: page title, header, browser tab. Configured during first-run wizard. No logo or image branding in initial implementation.

---

## First-Run Wizard

The wizard collects all configuration needed for deployment and executes with minimal further interaction. For cloud deployments, it checks for a valid cloud login then provisions and builds everything automatically.

### Wizard Sequence
1. Deployment type selection (Azure, AWS, GCP, Docker)
2. Prerequisite detection: install automatically where possible; error with detailed instructions if required dependency cannot be auto-installed (e.g. Docker Desktop); do not proceed until all prerequisites are met
3. Docker image registry prompt (Docker deployments only, default: ghcr.io/darkgrotto)
4. Environment-specific configuration (cloud): verify active cloud login; auto-detect account details (subscription, tenant, project); collect resource naming choices; Terraform state storage is created automatically by wizard during deployment
5. Hosting preferences: subdomain configuration, port selection
6. SSL certificate generation (self-signed)
7. Instance branding: instance name (text only)
8. Database admin account creation: username and password, minimum 15 character password enforced
9. Product admin account creation: always a local account (never OAuth), minimum 15 character password enforced
10. Product general user account creation: exactly one general user created during wizard, minimum 15 character password enforced, local account
11. Backup destination configuration: one destination configured during wizard; supported: Azure Blob, AWS S3, GCP Storage, local file export; additional destinations configured post-setup
12. Backup schedule configuration (default: weekly)
13. Backup retention configuration (default: four)
14. Initial update download prompt (yes or no)
15. Compose file and update.sh generation (Docker only)
16. Deployment execution: cloud deployments provision Terraform state storage then run terraform init and apply; Docker deployments run docker compose up -d
17. Random daily update check time generation

### Wizard Constraints
- OAuth is configured post-setup, not during wizard
- Self-enrollment is not prompted - off by default, changed post-setup by admin
- Update source is always countorsell.com - not configurable

---

## Authentication and Roles

### Built-in Local Admin
- Created during first-run wizard; always a local account, never OAuth
- Cannot be removed, disabled, demoted, or converted
- No collection of its own; emergency access guarantee
- These constraints are enforced by the application, not convention

### Admin Role
- Multiple admin accounts supported; local or OAuth (except built-in which is always local)
- No collection of their own; can view (never modify) all general user collections
- Account management: add users (admin or general), remove users (triggers export workflow, never built-in local admin), disable accounts (reversible, never built-in local admin), re-enable disabled accounts, block and unblock self-enrollment
- Can trigger database and schema updates (admin approval required for schema updates)
- Can manage: instance branding, TCGPlayer API key, backup destinations and schedule, OAuth configuration (post-setup), can view and delete removed user export files

### General User Role
- Local or OAuth; has their own collection (cards, sealed product, serialized cards, slabs) and wishlist
- Can modify only their own collection and wishlist; cannot see other users' data
- Cannot perform administrative actions; can see update status in About view

### Self-Enrollment
- Off by default on new instances
- When enabled: new users get immediate general user access, no approval step required
- Admin can enable or disable at any time post-setup

### OAuth Providers (configured post-setup): Google, Microsoft (Live and Entra ID), GitHub

### Account States
- Active
- Disabled (cannot log in, data retained, reversible, never applied to built-in local admin)
- Removed (permanent, triggers data export before deletion)

### Account Removal Export
- On removal: collection data exported to Product-specific backup format before deletion
- Export labeled with username and removal timestamp; stored in admin panel, available for download
- Live collection data deleted only after successful export
- Export failure blocks removal - data safety takes precedence
- Export files retained until admin explicitly deletes them - no automatic expiry
- Removal blocked if export fails - admin notified with clear recovery instructions

### Last Local Admin Protection
- System prevents removal, disabling, or demotion of the last remaining local admin account
- Built-in local admin is always protected regardless of how many other local admins exist

---

## Data Model

### Content Types Received via Update Packages
All canonical data arrives as fully resolved flat view. No layer logic exists in the Product.

| Content type | Notes |
|---|---|
| Cards | Fully resolved flat view; includes rarity, set_type, treatments array |
| Card images | Bundled in update packages |
| Card pricing | Per-set pricing.json in packages; may be refreshed per card via TCGPlayer |
| Sets | Fully resolved flat view; includes set_type |
| Treatments | Reference table, never hardcoded |
| Set symbols | Keyrune TTF font, bundled; updated with app version updates; never fetched at runtime |
| Sealed product | Fully resolved flat view; category_slug and sub_type_slug shipped directly |
| Sealed product images | Bundled in update packages |
| Oracle ruling URLs | Stored as card field, linked only |
| Slabs | Product-managed only, NOT in update packages; slabs content_version key always 0.0.0/0 in manifests |
| Serialized cards | Product-managed only, not in update packages |
| Grading agencies | Product-managed only, NOT in update packages; canonical agencies seeded at first run |

### Treatments Reference Table
Received via update packages, versioned independently. Never hardcoded in application code or UI. All treatment display logic references this table.

Current treatments (as of 2026-03-08): regular->Regular, foil->Foil, surge-foil->Surge Foil, fracture-foil->Fracture Foil, etched-foil->Etched Foil, textured-foil->Textured Foil, galaxy-foil->Galaxy Foil, serialized->Serialized, gilded-foil->Gilded Foil, artist-proof->Artist Proof, promo->Promo.

Storage: normalized lowercase with hyphens always. Display: as listed above. New treatments arrive via update packages without application code changes.

### Card Condition
Stored as enum (fixed, not a reference table): NM (Near Mint), LP (Lightly Played), MP (Moderately Played), HP (Heavily Played), DMG (Damaged).

Autographed is a separate boolean field (default false). When autographed is true, condition grade is preserved and displayed alongside. Display example: "NM - Autographed"

### Collection Entry Fields (Standard)
- Card identifier (required, `^[a-z0-9]{3,4}[0-9]{3,4}[a-z]?$`), Treatment (required, from treatments table), Quantity (required, integer), Condition (required, enum), Autographed (boolean, default false)
- Acquisition date (required, defaults to current date), Acquisition price (required, defaults to current market value at time of entry), Notes (optional)
- Both acquisition fields editable after entry

### Serialized Card Tracker Fields
Same as standard collection entry except: Serial number (required, integer), Print run total (required, integer); acquisition date and price are required with no defaults (must be entered at time of addition).

### Slab Tracker Fields
- Card identifier, Treatment, Grading agency (required, from grading agency table), Grade (required, string), Certificate number (required, string)
- Serial number (optional, integer - only if slabbed card is also serialized), Print run total (optional, integer - required if serial number present)
- Acquisition date and price required with no defaults; Notes (optional); both acquisition fields editable after entry

### Sealed Product Inventory Fields
- Product identifier (required), Quantity (required, integer), Condition (required, enum)
- Acquisition date (required, defaults to current date), Acquisition price (required, defaults to current market value), Notes (optional)
- Cannot be marked as opened; both acquisition fields editable after entry

### Grading Agency Reference Table
Canonical agencies are NOT received via update packages - they are seeded into the Product at first run and are fixed. Product instance admins can add local agencies.

Fields: agency code (required, unique across canonical and local, e.g. "PSA"), agency full name (required), validation URL template (required; configurable by admin for local agencies, not modifiable for canonical), supports direct certificate lookup (boolean, default true), source (canonical or local, system-assigned), active flag (boolean, default true).

Initial canonical agencies: BGS (Beckett Grading Services), PSA (Professional Sports Authenticator), SGC (Sportscard Guaranty), CGC (Certified Guaranty Company), CCC (Certificateur de Cartes de Collection, https://cccgrading.com/en/ccc-card-verification, supports-direct-lookup: false), ISA (International Sports Authentication).

Certificate validation: supports-direct-lookup true -> "Verify Certificate" link opens directly to record with cert number interpolated into URL template. supports-direct-lookup false -> link opens landing page, certificate number displayed prominently alongside link for manual entry.

Agency collision handling (identical codes): admin notified before update is applied; all records referencing local agency remapped to canonical version automatically; admin must acknowledge before update proceeds.

Local agency deletion with existing records: UI prompts admin to select replacement agency; all impacted records remapped; deletion cannot proceed without replacement if records exist; if no records reference the agency, deletion proceeds without prompt.

---

## Values and Metrics

### Universal Filters
Applied across all views where contextually appropriate. Filters that add no value in a given context are hidden (e.g. set filter suppressed when viewing a single set).

Set (`^[a-z0-9]{3,4}$`), Color (White/Blue/Black/Red/Green/Colorless/Multicolor), Condition (NM/LP/MP/HP/DMG), Card type (top-level MTG types only: Creature, Instant, Sorcery, Land, Enchantment, Artifact, Planeswalker, Battle, etc. - subtypes deferred, do not prevent architecturally), Treatment (from treatments table), Autographed (boolean), Serialized (boolean), Slabbed (boolean), Sealed product (boolean), Sealed product category (from taxonomy table; suppressed when table is empty), Sealed product sub-type (from taxonomy table; suppressed when empty or no category selected), Grading agency (where applicable).

### Collection Value
Calculation: current market value x quantity. Breakdown by content type: cards, sealed product, serialized cards, slabs. Viewable at: per card, per set, per content type, per collection. Universal filters applicable at all levels.

### Profit/Loss
Calculation: (current market value x quantity) - (acquisition price x quantity). Available at all same levels as collection value. Universal filters applicable.

### Historical Values
Two price points only: original acquisition price and current market value. No intermediate price history stored.

### Pricing Data
Current market value from update packages (authoritative). Direct TCGPlayer query updates stored value immediately (requires user or admin-supplied API key). TCGPlayer-sourced price always overwritten by next update package regardless of which is newer. No UI distinction between TCGPlayer-refreshed price and update package price. UI displays date of last overall card data update (not per-card).

### TCGPlayer Direct Query
Requires API key supplied by user or Product instance admin. Configured in Product settings post-setup. CountOrSell does not proxy this connection. Single card query only (set code plus card number). If no API key configured, feature is not available. Key stored securely, never in plain text.

### Set Completion Tracking
Displays raw count (e.g. 110 of 115) and percentage (e.g. 95.7%). Default: one copy of each card counts regardless of treatment. User setting toggle: count only non-foil/regular treatment variants. Per-user setting stored with user preferences.

### Total Card Count
Cards: total count with slices by all universal filters. Serialized cards: separate count, own slice view. Slabs: separate count, own slice view. Sealed product: separate count with total value.

### Collection Sorted by Value
Cards sortable by current market value across all sets. Universal filters applicable. Profit/loss column available alongside current value.

### Wishlist
Per user. Current market value displayed per card. Total wishlist value calculation. Universal filters applicable. Wishlist is a separate feature from the collection.

---

## Collection Overview Dashboard
- Default landing page after login for general users
- User can change default to any other tab or page; default page preference stored per user
- Surfaces all metrics with universal filtering
- Admins see read-only view of all user collections, no own collection view

---

## Update System

### Content Updates
- Check frequency: once daily at a random time generated during wizard
- Manual check trigger available to Product instance admins at any time
- Content-only updates applied automatically in background; no user interaction required
- Daily update check service checks countorsell.com/updates/manifest.json; applies content updates automatically; surfaces schema updates to admin for approval

### Schema Updates
- Require explicit Product instance admin approval before proceeding
- Pre-update backup is mandatory and automatic before any schema update; failure blocks schema update
- Admin notified if pre-update backup fails
- If schema update fails after successful backup: automatic restore from pre-update backup; admin notified with clear recovery instructions

### Schema Migration on Container Startup
1. Detect schema version mismatch
2. Take pre-update backup silently
3. If backup succeeds: run migrations
4. If backup fails: abort startup, notify admin, do not migrate
5. If migration fails: restore from backup automatically, abort startup, notify admin

### Update Check Manifest
Polls `www.countorsell.com/updates/manifest.json`. Update source is not configurable. Checksums verified per-file before applying. Two-level manifest: website manifest and per-package manifest.

Website manifest format:
```json
{
  "schema_version": "1.0.0",
  "generated_at": "<ISO 8601>",
  "minimum_product_version": "1.0.0",
  "content_versions": {
    "cards": { "version": "1.2.0" },
    "sets": { "version": "1.1.0" },
    "sealed_products": { "version": "1.0.0" },
    "treatments": { "version": "1.0.0" },
    "images": { "version": "1.1.0" },
    "taxonomy": { "version": "1.0.0" }
  },
  "packages": [
    {
      "package_id": "<id>",
      "package_type": "full|delta",
      "download_url": "<url>/package.zip",
      "manifest_url": "<url>/manifest.json",
      "base_full_version": "1.0.0 or null for full",
      "generated_at": "<ISO 8601>"
    }
  ]
}
```

Per-package manifest format:
```json
{
  "package_type": "full|delta",
  "generated_at": "<ISO 8601>",
  "base_full_version": "1.0.0 or null",
  "schema_version": "1.0.0",
  "content_versions": {
    "cards":           { "version": "...", "record_count": 123 },
    "sets":            { "version": "...", "record_count": 42 },
    "sealed_products": { "version": "...", "record_count": 18 },
    "treatments":      { "version": "...", "record_count": 11 },
    "taxonomy":        { "version": "...", "record_count": 5 },
    "prices":          { "version": "...", "record_count": null },
    "images":          { "version": "...", "record_count": null },
    "slabs":           { "version": "0.0.0", "record_count": 0 }
  },
  "retained_full_versions": ["1.0.0"],
  "checksums": {
    "metadata/treatments.json": "sha256:<hex_lowercase>",
    "metadata/sets/eoe/set.json": "sha256:<hex_lowercase>",
    "metadata/sets/eoe/cards.json": "sha256:<hex_lowercase>",
    "metadata/sets/eoe/pricing.json": "sha256:<hex_lowercase>",
    "metadata/sealed/<id>.json": "sha256:<hex_lowercase>",
    "images/sets/eoe/<card_id>.jpg": "sha256:<hex_lowercase>",
    "images/sealed/<id>.jpg": "sha256:<hex_lowercase>"
  }
}
```

ZIP file structure (package.zip):
```
manifest.json
metadata/
  treatments.json          - array of {treatment_id, normalized_name, display_name, sort_order}
  taxonomy.json            - {version, categories:[{slug, display_name, sort_order, sub_types:[{slug, display_name, sort_order},...]}]}
  sets/{set_code}/
    set.json               - {set_code, name, card_count, released_at, set_type, scryfall_id}
    cards.json             - array of {card_id, set_code, collector_number, name, oracle_id,
                             mana_cost, cmc, type_line, oracle_text, colors, color_identity,
                             keywords, layout, rarity, scryfall_id, oracle_ruling_uri,
                             is_reserved, treatments, image_path}
                             Note: image_path is nullable; present only when image exists in package.
                             treatments is an array of normalized_name strings.
    pricing.json           - array of {card_id, treatment, price_usd, captured_at}
                             treatment is normalized_name; price_usd nullable; captured_at ISO 8601 UTC.
  sealed/{product_id}.json - {product_id, set_code, name, category_slug, sub_type_slug,
                             front_image_blob_name, supplemental_image_blob_name}
                             set_code, category_slug, sub_type_slug are all nullable.
images/
  sets/{set_code}/{card_id}.jpg
  sealed/{product_id}.jpg
  sealed/{product_id}_s.jpg
```

Package types: "full" (complete snapshot, base_full_version is null), "delta" (incremental from base_full_version, contains only changed files). Deltas are cumulative from their base_full_version; sequential application not required. Backend retains 3 full versions at all times. No separate schema packages - schema version is a metadata field only; schema migrations run on startup. slabs content type is always present in manifests with version 0.0.0 and record_count 0 (placeholder only - no slab data is ever shipped).

### Application Version Updates
Docker: UI notifies admin when new version is available; update performed via generated update.sh script (wraps docker compose pull and docker compose up -d); UI displays exact script location and commands; no Docker socket access from within containers.

Cloud (Azure, AWS, GCP): UI-triggered update through deployment infrastructure; native update mechanism per cloud provider.

Both: identical schema migration and pre-update backup behavior; admin notified of pending updates in-app (default); email notification optional, configurable in settings.

Update notifications: admin gets in-app notification (default) and optional email. General users see current version, latest released version, and update pending status in About view only.

Restore validation: block with clear error if attempting to restore a backup with a newer schema version than the current deployment; error message includes instructions to update the deployment first.

---

## Backup and Restore

### Backup Scope
Includes: user accounts and preferences, collections (cards, sealed product, serialized cards, slabs), wishlist data, application configuration (branding, settings, OAuth configuration), removed user export files, database schema version.

Excludes: canonical data (cards, sets, treatments, sealed product reference data, images) - re-downloaded from update packages after restore.

### Backup Types
Two distinct types, separately identified and tracked, stored in the same destination:
- Scheduled backups: labeled with instance name, timestamp, type. Default retention: four most recent, configurable.
- Pre-update backups: labeled with instance name, timestamp, schema version, type. Default retention: four most recent, configurable, independently of scheduled backups. Triggered automatically before any schema update; failure blocks schema update from proceeding.

### Backup Schedule and Destinations
- Default: weekly; configurable by admin; both scheduled and manual triggers supported
- One destination configured during wizard; additional destinations configurable post-setup; all configured destinations receive every backup
- Supported: Azure Blob Storage, AWS S3, GCP Storage, local file export (always available)

### Restore Scenarios
All three scenarios use identical restore logic: same instance recovery, migration to new instance or deployment type, first-run wizard restoration. After restore, canonical data update required if instance is behind current versions.

Partial restore: not implemented in initial release; do not prevent architecturally.

Restore validation: block with clear error if backup schema version is newer than current deployment; provide clear instructions to update deployment first.

---

## About View
Available to all users. Displays: current application version, latest released application version, whether an update is pending, date of last content update, instance name, demo environment notice (isDemo, demoSets) when in demo mode. Does not display update package contents or any administrative controls.

---

## Demo Mode

Demo mode is a runtime-only configuration state. Not a user setting, not a database flag, not configurable by any user or admin through the UI.

**Activation:** Set `DEMO_MODE=true`. Detected at startup and never changes at runtime.

**Demo sets (fixed, stored lowercase):** lea, 2ed, vis, eoe, fdn, ecl, tla, fin, dsk, usg, ulg, uns, p23, tdm

**Environment variables:**
```
DEMO_MODE=true           # Activates demo mode
DEMO_EXPIRES_AT=         # ISO 8601 datetime for countdown clock (optional)
```

**Behavior:**
- Instance name overridden to "CountOrSell Demo" in all API responses (INSTANCE_NAME env var ignored)
- Persistent non-dismissible banner shown to all users; countdown clock shown when DEMO_EXPIRES_AT is set
- Filter scope indicator on all collection views noting results are limited to demo sets
- About view includes a demo environment section
- `GET /api/demo/status` returns 200 with demo state (isDemo, expiresAt, secondsRemaining, visitorId, demoSets); returns 404 when not in demo mode
- Visitor tracking uses per-session UUID stored in ASP.NET Core session (key: "visitor_id")

**Locked endpoints (return 403, message: "This action is not available in demo mode."):**
- POST /api/collection/refresh-price/{cardIdentifier}
- GET /api/wishlist/export/tcgplayer
- POST /api/backup/trigger
- POST /api/restore, POST /api/restore/{backupId}
- POST /api/backup/destinations, DELETE /api/backup/destinations/{id}
- PATCH /api/settings/instance
- PATCH /api/settings/oauth/{provider}, DELETE /api/settings/oauth/{provider}
- PATCH /api/settings/self-enrollment
- POST /api/updates/check, POST /api/updates/schema/{id}/approve
- POST /api/users/{id}/remove

**Allowed in demo mode:** All read operations unrestricted. Users can add, modify, and remove their own collection and wishlist entries (affecting shared demo data) - intentionally permitted for full product exploration.

**Seed script:** `docker/scripts/demo-seed.sql` populates a freshly migrated database with demo accounts and sample data. Usage: `psql "$POSTGRES_CONNECTION" -f docker/scripts/demo-seed.sql`. SQL file includes placeholder bcrypt hashes that must be replaced with real hashes before use.

**Frontend:** DemoProvider wraps the app at root level. `useDemo()` returns `{ isDemo, demoSets, secondsRemaining }`. DemoBanner: persistent notice. DemoLock: wrapper that disables child elements (semi-transparent, pointer events disabled, tooltip explains restriction). FilterScopeIndicator: note near filter panel listing demo sets.

---

## Environment Variables
All sensitive configuration stored securely. Never hardcode credentials or connection strings.
```
POSTGRES_CONNECTION=          # PostgreSQL connection string
UPDATE_CHECK_TIME=            # HH:MM format, random time generated by wizard
INSTANCE_NAME=                # Branding text
BACKUP_SCHEDULE=              # Cron expression for backup
BACKUP_RETENTION=             # Number of backups to retain
BLOB_BACKUP_CONNECTION=       # Primary backup destination
OAUTH_GOOGLE_CLIENT_ID=       # Retrieved from secure store
OAUTH_GOOGLE_CLIENT_SECRET=
OAUTH_MICROSOFT_CLIENT_ID=
OAUTH_MICROSOFT_CLIENT_SECRET=
OAUTH_GITHUB_CLIENT_ID=
OAUTH_GITHUB_CLIENT_SECRET=
TCGPLAYER_API_KEY=            # Optional, user/admin supplied
DEMO_MODE=                    # "true" to activate demo mode
DEMO_EXPIRES_AT=              # Optional ISO 8601 expiry for demo countdown clock
CLOUD_PROVIDER=               # Set by Terraform: "azure", "aws", "gcp", or absent for Docker
AZURE_SUBSCRIPTION_ID=        # Azure only - set by Terraform
AZURE_RESOURCE_GROUP=
AZURE_APP_NAME=
CLOUD_APP_RUNNER_SERVICE_NAME=# AWS only - set by Terraform
CLOUD_REGION=
CLOUD_ECR_REGISTRY=           # {account_id}.dkr.ecr.{region}.amazonaws.com
GCP_PROJECT_ID=               # GCP only - set by Terraform
GCP_REGION=
GCP_SERVICE_NAME=
```

---

## File Structure
```
/src
  /Api                  # API controllers
  /Background           # Background services (Updates, Backup, AppVersion)
  /Data                 # Repository layer (Canonical, Collection, Slabs, Serialized,
                        #   SealedInventory, Wishlist, Users, GradingAgencies)
  /Domain               # Business logic (Updates, Collection, Metrics, Backup, Auth)
  /Infrastructure       # External service clients
  /Client               # React frontend (Dashboard, Collection, Serialized, Slabs,
                        #   SealedProduct, Wishlist, Metrics, Admin, About, Setup)
/docker
  /compose              # Generated Compose files
  /scripts              # update.sh and helpers
/infrastructure         # Terraform configuration
  /azure, /aws, /gcp    # Each: main.tf, variables.tf, outputs.tf, /modules/
```

---

## Deployment

- Platform: Azure App Service, AWS App Runner, GCP Cloud Run, or Docker Compose
- Database: PostgreSQL (all deployment types)
- CI/CD: GitHub Actions; pipeline definition: /.github/workflows/
- IaC: Terraform with provider-native state storage

### Docker Image
- Registry: ghcr.io/darkgrotto/countorsell
- Tags: `:latest` (most recent stable), `:X.Y.Z` (specific version), `:X.Y` (latest patch for minor), `:X` (latest minor for major), `:dev` (built from every push to main, unstable, not for production)
- Architectures: linux/amd64, linux/arm64
- Version tags created manually by pushing a git tag in format vX.Y.Z (e.g. v1.2.3)
- **Claude Code must never create git tags**

### Build Commands
```bash
# Local multi-arch build (requires Docker Desktop with Buildx and QEMU):
docker buildx build --platform linux/amd64,linux/arm64 \
  -t ghcr.io/darkgrotto/countorsell:dev --push .

# Health check test:
docker compose -f docker/test/docker-compose.test.yml up --abort-on-container-exit
```

---

## Open Decisions
- [ ] Card subtype filtering (deferred enhancement, do not prevent architecturally)
- [ ] Partial restore (deferred, do not prevent architecturally)
- [ ] Email notification service for admin update alerts (implementation detail per provider)
- [ ] CCC certificate validation URL template (landing page only, confirm exact cert lookup path at implementation)
- [ ] Validation URL templates for BGS, SGC, CGC, ISA (confirm exact patterns at implementation)
- [ ] OAuth configuration UI design (post-setup admin settings)
- [ ] Application version check source URL - no app-version.json endpoint exists on countorsell.com currently; version check is a no-op pending a defined endpoint
- [ ] Additional backup destinations beyond initial three (e.g. Dropbox, Google Drive)
- [ ] pricing.json per-set file in update packages - format confirmed, but Product currently does not apply per-treatment pricing from packages; all market values currently come as a single CurrentMarketValue per card. Decide whether per-treatment pricing should be stored and surfaced.

## Decision Log
Decisions are recorded here when they resolve open questions or override defaults. Implementation details are in the sections above.

- 2026-03-08 - Standard hyphens only; em-dashes cause errors. PostgreSQL only, no SQLite. Docker Compose only, no single-container fallback. countorsell.com is the only valid update source. No Admin Backend access for Product users. Treatments never hardcoded. Card condition as enum; autographed as separate boolean. Two-tier role model; built-in admin cannot be removed/disabled/demoted/converted. Self-enrollment off by default. OAuth post-setup only (Google, Microsoft, GitHub). Account removal triggers export; export failure blocks removal. Content updates automatic; schema updates require admin approval with mandatory pre-update backup. Daily update check at random wizard-generated time. Docker app updates via update.sh, no Docker socket from containers. Backup excludes canonical data; includes schema version; 4 retained per type independently; restore blocks if backup schema version newer than deployment. Grading agencies BGS/PSA/SGC/CGC/CCC/ISA canonical; CCC is landing-page-only; admins can add local agencies; collision requires admin acknowledgment. Set completion raw count + percentage, user toggle for regular-only. Historical values: acquisition price and current market value only. Collection overview is default landing page; user can change. Card type filtering top-level MTG types only, subtypes deferred. Wishlist per user, separate from collection. iOS/Android/React Native dropped. Linode removed, don't prevent adding later. GitHub Actions CI/CD; Terraform IaC with provider-native state.
- 2026-03-18 - Docker Compose reverse proxy: Nginx (resolved). Sealed product taxonomy reference tables added (sealed_product_categories, sealed_product_sub_types); received via update packages, versioned independently; slugs are PKs (no integer IDs); taxonomy replacement on update nulls orphaned inventory references; taxonomy filters suppressed when tables are empty; current vs. legacy product type distinction not modeled.
- 2026-03-19 - Docker images published to ghcr.io/darkgrotto/countorsell; linux/amd64 and linux/arm64; :dev from main push; release builds from manual vX.Y.Z tag push. Card identifier pattern extended with optional trailing letter (e.g. "pala001a") for letter-suffixed collector numbers; cards differing only by trailing letter are distinct. Demo mode is runtime-only (DEMO_MODE=true env var); not a DB flag or user setting; demo sets fixed; expiry via DEMO_EXPIRES_AT; visitor tracking via per-session UUID; locked endpoints return 403; instance name overridden to "CountOrSell Demo"; seed script at docker/scripts/demo-seed.sql.
- 2026-03-31 - Trailing letter "x" permanently reserved as synthetic mapping for Scryfall dagger (†) collector numbers (e.g. DRK/77† -> drk077x). "x" will never appear as a real collector number letter.
- 2026-04-05 - Update package format confirmed from COS_Backend analysis. Website manifest at www.countorsell.com/updates/manifest.json uses packages array with package_id, package_type (full or delta), download_url, manifest_url, base_full_version, generated_at. No separate schema packages - schema migration runs on startup only. Per-package manifest has per-file checksums in format sha256:<hex_lowercase>. ZIP uses metadata/ prefix for all data files. Images are in ZIP at images/ paths (NOT fetched separately). No app-version.json endpoint on countorsell.com currently. Card colors published as array of symbols. Treatment key is normalized_name field. Set total cards is card_count field. Set code is set_code field.
- 2026-04-08 - COS_Backend deep review. Sealed product package format corrected: backend ships category_slug and sub_type_slug as two separate nullable fields (NOT a single product_type field); SealedProductDto and ContentUpdateApplicator updated accordingly. Confirmed: grading agencies are NOT in update packages - canonical agencies are Product-seeded only; slabs content_version key always 0.0.0/record_count 0 in manifests (placeholder). Confirmed: cards.json includes rarity (Scryfall values: common/uncommon/rare/mythic/special), image_path (nullable, only present if image in package), and treatments array (normalized_name strings). set.json confirmed to include set_type (Scryfall values: core/expansion/masters/alchemy/promo/box/duel-deck/token/memorabilia/vanguard/funny/starter/commander/planechase/archenemy/scheme/masterpiece/digital and others). Per-package manifest content_versions keys: cards, sets, sealed_products, treatments, taxonomy, prices, images, slabs. pricing.json confirmed as per-set file {card_id, treatment, price_usd, captured_at} - Product does not yet apply per-treatment pricing from packages. Backend retains 3 full versions; deltas are cumulative from base_full_version.
