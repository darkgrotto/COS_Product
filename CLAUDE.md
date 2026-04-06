# CLAUDE.md - Product (CountOrSell)

## Critical Code Standards
- Use standard hyphens (-) only in all code, comments,
  configuration, and content
- Em-dashes, en-dashes, and similar Unicode dash characters
  are never acceptable and will cause errors
- This applies to every file in this repo without exception
- Use the most specific and strongly-typed approach for all
  data fields, structures, and formats - prefer explicit
  types and constrained formats over loose strings wherever
  possible

## Identifier Formats and Validation
These patterns apply everywhere in this repo - validation,
storage, display, documentation, and examples.

Set code:
- Pattern: ^[a-z0-9]{3,4}$
- Stored: lowercase always (e.g. "eoe", "3ed", "mmh2")
- Displayed: uppercase by convention in all UI (e.g. "EOE")
- Never accept or store a set code as a display name
  (e.g. "Edge of Eternities" is never a valid set code)

Card identifier:
- Pattern: ^[a-z0-9]{3,4}[0-9]{3,4}[a-z]?$
- Set code portion follows set code pattern above
- Numeric suffix: zero-padded to three digits for values
  001 through 999, expands to four digits unpadded for
  values 1000 through 9999
- A four-digit numeric suffix must be >= 1000 - zero-padded
  four-digit suffixes (e.g. "0123") are never valid
- Optional single trailing lowercase letter for cards with
  letter-suffixed collector numbers (e.g. "1a", "1b")
- Cards differing only by trailing letter are distinct
  (e.g. "eoe001a" and "eoe001b" are different cards)
- The letter "x" is permanently reserved as the synthetic
  mapping for Scryfall collector numbers ending in dagger
  (†), e.g. DRK/77† maps to "drk077x". "x" will never
  appear as a real collector number letter.
- Stored: lowercase always (e.g. "eoe019", "eoe1234", "pala001a")
- Displayed: uppercase by convention in all UI (e.g. "EOE019")
- Valid examples: "eoe019", "eoe999", "eoe1234", "3ed019",
  "pala001a", "pala001b", "drk077x", "arn002x"

## What This Repo Is
The self-hostable CountOrSell product. A web application
for tracking collectible card game collections, sealed
product inventory, serialized cards, and graded/slabbed
cards. Includes market value tracking, collection metrics,
and wishlist functionality.

It serves the following purposes:
1. Collection tracking (cards, sealed product, serialized
   cards, slabs)
2. Market value and profit/loss tracking
3. Collection metrics and set completion
4. Wishlist management
5. Receiving and applying content updates from
   countorsell.com

## What This Repo Is NOT
- Not the Admin Backend - has no access to canonical data
  authoring, submission review, or publishing
- Not the Website - does not serve public content or
  handle community submissions
- Not a data authority - all canonical content comes from
  update packages published by the Admin Backend
- Not responsible for layer resolution - always receives
  fully resolved flat data, never applies layer logic
- Not connected to the Admin Backend directly - update
  source is countorsell.com only, no custom sources

## Related Repos
- Website: https://www.github.com/darkgrotto/COS_Website
  Source of update manifests and packages.
  Destination for community submissions.
- Admin Backend: https://www.github.com/darkgrotto/COS_Backend
  Authors and publishes all canonical content.
  Not directly accessible to Product instance users.

## Do Not
- Do not hardcode treatment values - always reference
  the treatments table received in update packages
- Do not add layer resolution logic - the Product always
  receives a fully resolved flat view
- Do not add a custom update source option - countorsell.com
  is the only valid update source
- Do not expose Admin Backend access to Product users
- Do not introduce iOS/Android or React Native dependencies
- Do not use SQLite - PostgreSQL only
- Do not use em-dashes or special dash characters anywhere
- Do not hardcode sealed product category or sub-type
  slugs or display names; always reference the
  taxonomy reference tables received in update packages
- Do not display taxonomy slugs in the UI; always
  use display_name values from the reference tables
- Do not add an is_active or current flag to taxonomy
  entries; current vs. legacy product type distinction
  is not modeled

---

## Tech Stack

- Runtime: dotnet with React
- Database: PostgreSQL
- Containerization: Docker Compose (managed, no fallback
  to single-container SQLite)
- Authentication: local accounts plus OAuth via
  Google, Microsoft (Live and Entra ID), and GitHub
- TCGPlayer pricing: direct API (user or admin-supplied
  key, CountOrSell does not proxy this connection)
- CI/CD: GitHub Actions
- IaC: Terraform with provider-native state storage

---

## Deployment Types

Supported deployment targets:
- Azure (App Service, Azure Database for PostgreSQL,
  Azure Blob Storage, Azure Key Vault)
- AWS (App Runner, Amazon RDS for PostgreSQL,
  AWS S3, AWS Secrets Manager)
- GCP (Cloud Run, Cloud SQL for PostgreSQL,
  Google Cloud Storage, Google Secret Manager)
- Docker Compose

Linode is not a supported deployment target.
Do not prevent adding it later architecturally.

### Compute Per Provider
```
Azure:   App Service
AWS:     App Runner
GCP:     Cloud Run (uses same Docker image as
         Docker Compose deployment)
Docker:  Docker Compose
```

### SSL Per Provider
```
Azure:   App Service native SSL
AWS:     App Runner native SSL via ACM
GCP:     Cloud Run native HTTPS
Docker:  Self-signed certificate generated by wizard
```

### Secrets Per Provider
```
Azure:   Azure Key Vault
AWS:     AWS Secrets Manager
GCP:     Google Secret Manager
Docker:  Environment variables in managed .env file
```

### Backup Storage Defaults Per Provider
```
Azure:   Azure Blob Storage (default)
AWS:     AWS S3 (default)
GCP:     Google Cloud Storage (default)
Docker:  Local file or any configured destination
```
User can configure any supported destination regardless
of deployment provider. Wizard defaults to provider-
native storage but does not require it.

### Terraform State Storage Per Provider
```
Azure:   Azure Blob Storage
AWS:     AWS S3
GCP:     Google Cloud Storage
```
No cross-provider state dependency. Each deployment
manages its own Terraform state in its own provider
storage.

### Docker Compose Services
Generated Compose file includes:
- Product application container
- PostgreSQL container with named volume for data
  persistence
- Reverse proxy container for SSL termination
- Backup container with scheduled task

The Compose file is managed - regenerated by the wizard
if changes are needed. Users do not edit it directly.

A generated update.sh script is provided alongside the
Compose file for application version updates.

No Docker socket access from within any container.

### Instance Branding
- Text only (instance name)
- Appears in: page title, header, browser tab
- Configured during first-run wizard
- No logo or image branding in initial implementation

---

## First-Run Wizard

The wizard collects all configuration needed for
deployment and executes with minimal further interaction
once information is gathered. For cloud deployments,
the wizard checks for a valid cloud login, then
provisions and builds everything automatically. Users
only need to install prerequisites and log in to their
cloud provider before starting.

### Wizard Sequence
1. Deployment type selection (Azure, AWS, GCP, Docker)
2. Prerequisite detection:
   - Install automatically where possible
   - Error with detailed instructions if a required
     dependency cannot be auto-installed (e.g. Docker
     Desktop must be installed by the user)
   - Do not proceed until all prerequisites are met
3. Docker image registry prompt (Docker deployments only,
   default: ghcr.io/darkgrotto)
4. Environment-specific configuration (cloud deployments):
   - Verify active cloud login; prompt user to log in
     if not already authenticated
   - Auto-detect account details (subscription, tenant,
     project) from the active login session
   - Collect resource naming choices (resource groups,
     region, state storage names)
   - Terraform state storage is created automatically
     by the wizard during deployment - no pre-creation
     required
5. Hosting preferences:
   - Subdomain configuration
   - Port selection
6. SSL certificate generation (self-signed)
7. Instance branding: instance name (text only)
8. Database admin account creation:
   - Username and password
   - Minimum 15 character password enforced
9. Product admin account creation:
   - Always a local account (never OAuth)
   - Minimum 15 character password enforced
10. Product general user account creation:
    - Exactly one general user created during wizard
    - Minimum 15 character password enforced
    - Local account
11. Backup destination configuration:
    - One destination configured during wizard
    - Supported: Azure Blob, AWS S3, GCP Storage,
      local file export
    - Additional destinations configured post-setup
12. Backup schedule configuration (default: weekly)
13. Backup retention configuration (default: four)
14. Initial update download prompt (yes or no)
15. Compose file and update.sh generation (Docker only)
16. Deployment execution:
    - Cloud deployments: provision Terraform state
      storage, then run terraform init and apply
    - Docker deployments: docker compose up -d
17. Random daily update check time generation

### Wizard Constraints
- OAuth is configured post-setup, not during wizard
- Self-enrollment is not prompted - off by default,
  changed post-setup by admin
- Update source is always countorsell.com - not
  configurable

---

## Authentication and Roles

### Built-in Local Admin
- Created during first-run wizard
- Always a local account, never OAuth
- Cannot be removed, disabled, demoted, or converted
- No collection of its own
- Emergency access guarantee
- These constraints are enforced by the application,
  not convention

### Admin Role
- Multiple admin accounts supported
- Local or OAuth (except built-in which is always local)
- No collection of their own
- Can view (never modify) all general user collections
- Account management actions:
  - Add users (admin or general)
  - Remove users (triggers export workflow, never
    built-in local admin)
  - Disable accounts (reversible, never built-in
    local admin)
  - Re-enable disabled accounts
  - Block and unblock self-enrollment
- Can trigger database and schema updates (with admin
  approval required for schema updates)
- Can manage instance branding and configuration
- Can manage TCGPlayer API key
- Can manage backup destinations and schedule
- Can manage OAuth configuration (post-setup)
- Can view and delete removed user export files

### General User Role
- Local or OAuth
- Has their own collection (cards, sealed product,
  serialized cards, slabs)
- Has their own wishlist
- Can modify only their own collection and wishlist
- Cannot see other users' collections or wishlists
- Cannot perform administrative actions
- Can see update status in About view

### Self-Enrollment
- Off by default on new instances
- When enabled: new users get immediate general user
  access, no approval step required
- Admin can enable or disable at any time post-setup

### OAuth Providers (configured post-setup)
- Google
- Microsoft (Live and Entra ID)
- GitHub

### Account States
- Active
- Disabled (cannot log in, data retained, reversible,
  never applied to built-in local admin)
- Removed (permanent, triggers data export before
  deletion)

### Account Removal Export
- On removal: collection data exported to Product-specific
  backup format before deletion
- Export labeled with username and removal timestamp
- Export stored in admin panel, available for download
- Live collection data deleted only after successful
  export
- Export failure blocks removal - data safety takes
  precedence
- Export files retained until admin explicitly deletes
  them - no automatic expiry
- Removal blocked if export fails - admin notified
  with clear recovery instructions

### Last Local Admin Protection
- System prevents removal, disabling, or demotion of
  the last remaining local admin account
- Built-in local admin is always protected regardless
  of how many other local admins exist

---

## Data Model

### Content Types Received via Update Packages
All canonical data arrives as fully resolved flat view.
No layer logic exists in the Product.
```
Content type         Notes
------------         -----
Cards                Fully resolved flat view
Card images          Bundled in update packages,
                     fallback fetch from third party
Card pricing         Scryfall-cached, may be
                     refreshed per card via TCGPlayer
Sets                 Fully resolved flat view
Treatments           Reference table, never hardcoded
Set symbols          Keyrune TTF font, bundled
Sealed product       Fully resolved flat view
Sealed product       Bundled in update packages
  images
Oracle ruling URLs   Stored as card field, linked only
Slabs                Product-managed only, not in
                     update packages
Serialized cards     Product-managed only, not in
                     update packages
```

### Keyrune Font
- TTF font bundled into the Product
- Updated with application version updates
- Never fetched at runtime from external source

### Treatments Reference Table
- Received via update packages, versioned independently
- Never hardcoded in application code or UI
- All treatment display logic references this table
- Current treatments (as of 2026-03-08):
  - regular -> Regular
  - foil -> Foil
  - surge-foil -> Surge Foil
  - fracture-foil -> Fracture Foil
  - etched-foil -> Etched Foil
  - textured-foil -> Textured Foil
  - galaxy-foil -> Galaxy Foil
  - serialized -> Serialized
  - gilded-foil -> Gilded Foil
  - artist-proof -> Artist Proof
  - promo -> Promo
- Storage: normalized lowercase with hyphens always
- Display: as listed above in all UI
- New treatments arrive via update packages without
  application code changes

### Card Condition
Stored as enum (fixed, not a reference table):
- NM (Near Mint)
- LP (Lightly Played)
- MP (Moderately Played)
- HP (Heavily Played)
- DMG (Damaged)

Autographed is a separate boolean field (default false).
When autographed is true, condition grade is preserved
and displayed alongside the autograph indicator.
Display example: "NM - Autographed"

### Standard Collection Entry Fields
- Card identifier (required, ^[a-z0-9]{3,4}[0-9]{3,4}[a-z]?$)
- Treatment (required, from treatments reference table)
- Quantity (required, integer, multiple copies per record)
- Condition (required, enum: NM, LP, MP, HP, DMG)
- Autographed (boolean, default false)
- Acquisition date (required, defaults to current date)
- Acquisition price (required, defaults to current
  market value from pricing data at time of entry)
- Notes (optional, string)
- Both acquisition fields editable after entry

### Serialized Card Tracker Fields
- Card identifier (required, ^[a-z0-9]{3,4}[0-9]{3,4}[a-z]?$)
- Treatment (required, from treatments reference table)
- Serial number (required, integer)
- Print run total (required, integer)
- Condition (required, enum: NM, LP, MP, HP, DMG)
- Autographed (boolean, default false)
- Acquisition date (required, must be entered at time
  of addition - no default)
- Acquisition price (required, must be entered at time
  of addition - no default)
- Notes (optional, string)
- Both acquisition fields editable after entry

### Slab Tracker Fields
- Card identifier (required, ^[a-z0-9]{3,4}[0-9]{3,4}[a-z]?$)
- Treatment (required, from treatments reference table)
- Grading agency (required, from grading agency
  reference table)
- Grade (required, string)
- Certificate number (required, string)
- Serial number (optional, integer - present only if
  slabbed card is also serialized)
- Print run total (optional, integer - required if
  serial number is present, absent if serial number
  is absent)
- Acquisition date (required, must be entered at time
  of addition - no default)
- Acquisition price (required, must be entered at time
  of addition - no default)
- Notes (optional, string)
- Both acquisition fields editable after entry

### Sealed Product Inventory Fields
- Product identifier (required)
- Quantity (required, integer)
- Condition (required, enum: NM, LP, MP, HP, DMG)
- Acquisition date (required, defaults to current date)
- Acquisition price (required, defaults to current
  market value at time of entry)
- Notes (optional, string)
- Cannot be marked as opened
- Both acquisition fields editable after entry

### Grading Agency Reference Table
Received via update packages for canonical agencies.
Product instance admins can add local agencies.

Fields per agency:
- Agency code (required, unique across canonical and
  local, e.g. "PSA")
- Agency full name (required)
- Validation URL template (required, configurable by
  Product instance admin for local agencies, not
  modifiable for canonical agencies)
- Supports direct certificate lookup (boolean,
  default true)
- Source (canonical or local, system-assigned,
  not editable)
- Active flag (boolean, default true)

Initial canonical agencies:
- BGS (Beckett Grading Services)
- PSA (Professional Sports Authenticator)
- SGC (Sportscard Guaranty)
- CGC (Certified Guaranty Company)
- CCC (Certificateur de Cartes de Collection,
  https://cccgrading.com/en/ccc-card-verification,
  supports-direct-lookup: false)
- ISA (International Sports Authentication)

Certificate validation display:
- Supports direct lookup (true): "Verify Certificate"
  link opens directly to certificate record with
  certificate number interpolated into URL template
- Supports direct lookup (false): "Verify Certificate"
  link opens landing page, certificate number displayed
  prominently alongside link for manual entry by user

Agency collision handling (identical agency codes):
- Admin notified before update is applied
- All records referencing local agency remapped to
  canonical version automatically
- Admin must acknowledge before update proceeds

Local agency deletion with existing records:
- UI prompts admin to select replacement agency
- All impacted records remapped to selected replacement
- Deletion cannot proceed without replacement selection
  if records exist
- If no records reference the agency, deletion proceeds
  without prompt

---

## Values and Metrics

### Universal Filters
Applied across all views where contextually appropriate.
Filters that add no value in a given context are hidden
(e.g. set filter suppressed when viewing a single set).

- Set (^[a-z0-9]{3,4}$)
- Color (White, Blue, Black, Red, Green, Colorless,
  Multicolor)
- Condition (NM, LP, MP, HP, DMG)
- Card type (top-level MTG types only: Creature,
  Instant, Sorcery, Land, Enchantment, Artifact,
  Planeswalker, Battle, etc. - subtypes deferred)
- Treatment (from treatments reference table)
- Autographed (boolean)
- Serialized (boolean)
- Slabbed (boolean)
- Sealed product (boolean)
- Sealed product category (from taxonomy reference table; 
  suppressed when table is empty)
- Sealed product sub-type (from taxonomy reference table; 
  suppressed when table is empty or no category
  is selected)
- Grading agency (where applicable)

### Collection Value
- Calculation: current market value x quantity
- Breakdown by content type: cards, sealed product,
  serialized cards, slabs
- Viewable at: per card, per set, per content type,
  per collection
- Universal filters applicable at all levels

### Profit/Loss
- Calculation: (current market value x quantity) -
  (acquisition price x quantity)
- Available at all same levels as collection value
- Universal filters applicable
- Displays net gain or loss to current point in time

### Historical Values
- Two price points only: original acquisition price
  and current market value
- No intermediate price history stored
- Net gain/loss calculated from these two points

### Pricing Data
- Current market value from update packages
  (authoritative)
- Direct TCGPlayer query updates stored value
  immediately (requires user or admin-supplied API key)
- TCGPlayer-sourced price always overwritten by next
  update package regardless of which is newer
- No UI distinction between TCGPlayer-refreshed price
  and update package price
- UI displays date of last overall card data update
  (not per-card)

### TCGPlayer Direct Query
- Requires API key supplied by user or Product instance
  admin
- Configured in Product settings post-setup
- CountOrSell does not proxy this connection
- Single card query only (set code plus card number)
- If no API key configured, feature is not available
- Key stored securely, never in plain text

### Set Completion Tracking
- Displays raw count (e.g. 110 of 115) and percentage
  (e.g. 95.7%)
- Default: one copy of each card counts regardless of
  treatment
- User setting toggle: count only non-foil/regular
  treatment variants
- Per-user setting, stored with user preferences
- Completion is per-user against their own collection

### Total Card Count
- Cards: total count with slices by all universal
  filters
- Serialized cards: separate count, own slice view
- Slabs: separate count, own slice view
- Sealed product: separate count with total value

### Collection Sorted by Value
- Cards sortable by current market value across all
  sets
- Universal filters applicable
- Profit/loss column available alongside current value

### Sealed Product Value
- Total value: current market value x quantity
- Profit/loss: (current market value x quantity) -
  (acquisition price x quantity)
- Universal filters applicable where relevant

### Card Type Filtering
- Top-level MTG card types only in initial
  implementation
- Subtypes (creature subtypes, land types, etc.)
  are a deferred enhancement
- Do not prevent subtypes architecturally

### Wishlist
- Per user, each general user has their own wishlist
- Current market value displayed per card
- Total wishlist value calculation
- Universal filters applicable
- Wishlist is a separate feature from the collection

---

## Collection Overview Dashboard

- Default landing page after login for general users
- User can change default to any other tab or page
- Default page preference stored per user
- Surfaces all metrics with universal filtering
- Admins see read-only view of all user collections,
  no own collection view

---

## Update System

### Content Updates
- Check frequency: once daily at a random time
  generated during wizard
- Manual check trigger available to Product instance
  admins at any time
- Content-only updates applied automatically in
  background
- No user interaction required for content updates

### Schema Updates
- Require explicit Product instance admin approval
  before proceeding
- Pre-update backup is mandatory and automatic before
  any schema update
- Schema update does not proceed if backup fails
- Admin notified if pre-update backup fails
- If schema update fails after successful backup:
  automatic restore from pre-update backup
- Admin notified on automatic restore after failed
  schema update, with clear recovery instructions

### Update Check Manifest
- Polls www.countorsell.com/updates/manifest.json
- Update source is not configurable - countorsell.com
  only
- Checksums verified per-file before applying
- Manifest has two levels: website manifest and
  per-package manifest

Website manifest format (updates/manifest.json):
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

Per-package manifest format (fetched from manifest_url
or extracted from ZIP as manifest.json):
```json
{
  "package_type": "full|delta",
  "generated_at": "<ISO 8601>",
  "base_full_version": "1.0.0 or null",
  "schema_version": "1.0.0",
  "content_versions": { "cards": { "version": "...", "record_count": 123 }, ... },
  "retained_full_versions": ["1.0.0"],
  "checksums": {
    "metadata/treatments.json": "sha256:<hex_lowercase>",
    "metadata/sets/eoe/set.json": "sha256:<hex_lowercase>",
    "metadata/sets/eoe/cards.json": "sha256:<hex_lowercase>",
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
  taxonomy.json            - {version, categories:[{slug, display_name, sort_order, sub_types:[...]}]}
  sets/{set_code}/
    set.json               - {set_code, name, card_count, released_at, set_type, scryfall_id}
    cards.json             - array of {card_id, set_code, collector_number, name, oracle_id,
                             mana_cost, cmc, type_line, oracle_text, colors, color_identity,
                             keywords, layout, rarity, scryfall_id, oracle_ruling_uri,
                             is_reserved, treatments}
  sealed/{product_id}.json - {product_id, set_code, name, product_type,
                             front_image_blob_name, supplemental_image_blob_name}
images/
  sets/{set_code}/{card_id}.jpg
  sealed/{product_id}.jpg
  sealed/{product_id}_s.jpg
```

Package types:
- "full" - complete snapshot, base_full_version is null
- "delta" - incremental from base_full_version, contains
  only changed files

No separate schema packages exist - schema version is
a metadata field only. Schema migrations run on startup.

### Application Version Updates

Docker deployments:
- UI notifies Product instance admin when new
  application version is available
- Update performed via generated update.sh script
  (wraps docker compose pull and docker compose up -d)
- UI displays exact script location and commands
- No Docker socket access from within containers

Cloud deployments (Azure, AWS, GCP):
- UI-triggered update through deployment infrastructure
- Native update mechanism per cloud provider
- Implementation detail per provider

Both deployment types:
- Identical schema migration and pre-update backup
  behavior
- Admin notified of pending application updates in-app
  (default)
- Email notification optional, configurable in settings

### Update Notifications
- Product instance admin: in-app notification (default),
  email notification (optional, configurable)
- General users: current version, latest released
  version, and update pending status visible in
  About view only - not prominently featured

### Schema Migration on Container Startup (Docker)
1. Detect schema version mismatch
2. Take pre-update backup silently
3. If backup succeeds: run migrations
4. If backup fails: abort startup, notify admin,
   do not migrate
5. If migration fails: restore from backup
   automatically, abort startup, notify admin

### Restore Validation
- Block with clear error if attempting to restore
  a backup with a newer schema version than the
  current deployment
- Error message includes instructions to update
  the deployment first before restoring

---

## Backup and Restore

### Backup Scope
Includes everything needed for complete instance
restoration except canonical data:
- User accounts and preferences
- Collections (cards, sealed product, serialized
  cards, slabs)
- Wishlist data
- Application configuration (branding, settings,
  OAuth configuration)
- Removed user export files
- Database schema version (included explicitly to
  support restoration after significant version gaps)

Excludes:
- Canonical data (cards, sets, treatments, sealed
  product reference data, images) - re-downloaded
  from update packages after restore
- This exclusion keeps backups lean while restore
  remains fully functional

### Backup Types
Two distinct types, separately identified and tracked,
stored in the same destination:

Scheduled backups:
- Labeled with instance name, timestamp, type
- Default retention: four most recent
- Retention configurable by Product instance admin

Pre-update backups:
- Labeled with instance name, timestamp, schema
  version, type
- Default retention: four most recent
- Retention configurable by Product instance admin
- Triggered automatically before any schema update
- Failure blocks schema update from proceeding

### Backup Schedule
- Default: weekly
- Configurable by Product instance admin
- Both scheduled and manual triggers supported
- Schedule configured during first-run wizard

### Backup Destinations
- Configured during wizard: one destination
- Additional destinations configurable post-setup
- All configured destinations receive every backup
- Supported destinations:
  - Azure Blob Storage
  - AWS S3
  - GCP Storage
  - Local file export (always available)
- Product instance admin can configure as many
  destinations as desired post-setup

### Restore Scenarios
All three scenarios use identical restore logic:
- Same instance recovery (data corruption, accidental
  deletion)
- Migration to new instance or deployment type
- First-run wizard restoration (fresh deployment with
  existing data)

After restore, canonical data update required if
instance is behind current versions.

### Partial Restore
- Not implemented in initial release
- Do not prevent architecturally
- Noted in Open Decisions

### Restore Validation
- Block with clear error if backup schema version
  is newer than current deployment
- Provide clear instructions to update deployment
  first

---

## Background Services

### Daily Update Check Service
- Runs once daily at random time generated by wizard
- Checks countorsell.com/updates/manifest.json
- Applies content updates automatically if available
- Surfaces schema updates to admin for approval
- Manual trigger available to Product instance admins

### Scheduled Backup Service
- Runs on admin-configured schedule (default weekly)
- Writes to all configured backup destinations
- Retains configured number of backups (default four)
- Prunes older backups after successful new backup

### Pre-Update Backup Service
- Triggered automatically before any schema update
- Silent, no user confirmation required
- Failure blocks schema update from proceeding
- Stored alongside scheduled backups with distinct
  type label
- Retains configured number of pre-update backups
  (default four, independently of scheduled backups)

### Application Version Check Service
- Checks for new Product application versions
- Separate from content update checks
- Surfaces notification to admin only
- General users see status in About view only

---

## About View

Available to all users. Displays:
- Current application version
- Latest released application version
- Whether an update is pending
- Date of last content update
- Instance name
- Demo environment notice (isDemo, demoSets) when in demo mode

Does not display:
- Update package contents or change details (admin only)
- Any administrative controls

---

## Demo Mode

Demo mode is a runtime-only configuration state for
hosting public demonstrations. It is not a user setting,
not a database flag, and not configurable by any user
or admin through the UI.

### Activation
Set environment variable `DEMO_MODE=true`.
Demo mode is detected at startup and never changes
at runtime.

### Additional Environment Variables
```
DEMO_MODE=true                # Activates demo mode
DEMO_EXPIRES_AT=              # ISO 8601 datetime for
                              # countdown clock (optional)
```

### Demo Sets (fixed, stored lowercase)
lea, 2ed, vis, eoe, fdn, ecl, tla, fin, dsk,
usg, ulg, uns, p23, tdm

### Demo Mode Behavior
- Instance name overridden to "CountOrSell Demo" in
  all API responses (INSTANCE_NAME env var ignored)
- Persistent non-dismissible banner shown to all users
- Countdown clock shown when DEMO_EXPIRES_AT is set
- Filter scope indicator shown on all collection views
  noting that results are limited to demo sets
- About view includes a demo environment section
- GET /api/demo/status returns 200 with demo state
  (isDemo, expiresAt, secondsRemaining, visitorId,
  demoSets); returns 404 when not in demo mode
- Visitor tracking uses per-session UUID stored in
  ASP.NET Core session (key: "visitor_id")

### Locked Endpoints (return 403 in demo mode)
These endpoints return 403 with error message
"This action is not available in demo mode."
- POST /api/collection/refresh-price/{cardIdentifier}
- GET /api/wishlist/export/tcgplayer
- POST /api/backup/trigger
- POST /api/restore
- POST /api/restore/{backupId}
- POST /api/backup/destinations
- DELETE /api/backup/destinations/{id}
- PATCH /api/settings/instance
- PATCH /api/settings/oauth/{provider}
- DELETE /api/settings/oauth/{provider}
- PATCH /api/settings/self-enrollment
- POST /api/updates/check
- POST /api/updates/schema/{id}/approve
- POST /api/users/{id}/remove

### Allowed in Demo Mode
All read operations are unrestricted. Users can add,
modify, and remove their own collection and wishlist
entries (affecting shared demo data). These writes
are intentionally permitted so visitors can explore
the product fully.

### Seed Script
docker/scripts/demo-seed.sql populates a freshly
migrated database with demo accounts and sample data.
Usage:
```
psql "$POSTGRES_CONNECTION" -f docker/scripts/demo-seed.sql
```
The SQL file includes placeholder bcrypt hashes that
must be replaced with real hashes before use.

### Frontend Implementation
- DemoProvider wraps the app at root level
- useDemo() hook returns { isDemo, demoSets,
  secondsRemaining }
- DemoBanner: persistent notice at top of every page
- DemoLock: wrapper that disables child elements in
  demo mode (rendered as semi-transparent, pointer
  events disabled, tooltip explains restriction)
- FilterScopeIndicator: note near filter panel listing
  demo sets

---

## Environment Variables
All sensitive configuration stored securely.
Never hardcode credentials or connection strings.
```
POSTGRES_CONNECTION=          # PostgreSQL connection string
UPDATE_CHECK_TIME=            # Random time generated by
                              # wizard (HH:MM format)
INSTANCE_NAME=                # Branding text
BACKUP_SCHEDULE=              # Cron expression for backup
BACKUP_RETENTION=             # Number of backups to retain
BLOB_BACKUP_CONNECTION=       # Primary backup destination
OAUTH_GOOGLE_CLIENT_ID=       # Retrieved from secure store
OAUTH_GOOGLE_CLIENT_SECRET=   # Retrieved from secure store
OAUTH_MICROSOFT_CLIENT_ID=    # Retrieved from secure store
OAUTH_MICROSOFT_CLIENT_SECRET=# Retrieved from secure store
OAUTH_GITHUB_CLIENT_ID=       # Retrieved from secure store
OAUTH_GITHUB_CLIENT_SECRET=   # Retrieved from secure store
TCGPLAYER_API_KEY=            # Optional, user/admin supplied
DEMO_MODE=                    # "true" to activate demo mode
DEMO_EXPIRES_AT=              # Optional ISO 8601 expiry for
                              # demo countdown clock
CLOUD_PROVIDER=               # Set by Terraform: "azure",
                              # "aws", "gcp", or absent for
                              # Docker deployments
AZURE_SUBSCRIPTION_ID=        # Azure only - set by Terraform
AZURE_RESOURCE_GROUP=         # Azure only - set by Terraform
AZURE_APP_NAME=               # Azure only - set by Terraform
CLOUD_APP_RUNNER_SERVICE_NAME=# AWS only - set by Terraform
CLOUD_REGION=                 # AWS only - set by Terraform
CLOUD_ECR_REGISTRY=           # AWS only - set by Terraform
                              # {account_id}.dkr.ecr.{region}.amazonaws.com
GCP_PROJECT_ID=               # GCP only - set by Terraform
GCP_REGION=                   # GCP only - set by Terraform
GCP_SERVICE_NAME=             # GCP only - set by Terraform
```

---

## File Structure
```
/src
  /Api                        # API controllers
  /Background                 # Background services
    /Updates                  # Content update service
    /Backup                   # Backup services
    /AppVersion               # Application version check
  /Data                       # Repository layer
    /Canonical                # Cards, sets, treatments,
                              # sealed product reference
    /Collection               # User collection data
    /Slabs                    # Slab tracker
    /Serialized               # Serialized card tracker
    /SealedInventory          # Sealed product inventory
    /Wishlist                 # Wishlist data
    /Users                    # User accounts and prefs
    /GradingAgencies          # Agency reference table
  /Domain                     # Business logic
    /Updates                  # Update application logic
    /Collection               # Collection management
    /Metrics                  # Value and metric calcs
    /Backup                   # Backup and restore logic
    /Auth                     # Authentication logic
  /Infrastructure             # External service clients
  /Client                     # React frontend
    /Dashboard                # Collection overview
    /Collection               # Collection management
    /Serialized               # Serialized card tracker
    /Slabs                    # Slab tracker
    /SealedProduct            # Sealed product inventory
    /Wishlist                 # Wishlist
    /Metrics                  # Value and metrics views
    /Admin                    # Admin panel
    /About                    # About view
    /Setup                    # First-run wizard
/docker
  /compose                    # Generated Compose files
  /scripts                    # update.sh and helpers
/infrastructure               # Terraform configuration
  /azure/
    /main.tf
    /variables.tf
    /outputs.tf
    /modules/
      /app-service
      /postgresql
      /key-vault
      /storage
  /aws/
    /main.tf
    /variables.tf
    /outputs.tf
    /modules/
      /app-runner
      /rds
      /secrets-manager
      /s3
  /gcp/
    /main.tf
    /variables.tf
    /outputs.tf
    /modules/
      /cloud-run
      /cloud-sql
      /secret-manager
      /cloud-storage
```

---

## Deployment
- Platform: Azure App Service, AWS App Runner,
  GCP Cloud Run, or Docker Compose
- Database: PostgreSQL (all deployment types)
- CI/CD: GitHub Actions
- IaC: Terraform with provider-native state storage
- Pipeline definition: /.github/workflows/

### Docker Image
- Registry: ghcr.io/darkgrotto/countorsell
- Tags:
  - :latest - most recent stable release
  - :X.Y.Z - specific version (e.g. :1.2.3)
  - :X.Y - tracks latest patch for minor version
  - :X - tracks latest minor for major version
  - :dev - built from every push to main,
           unstable, not for production use
- Architectures: linux/amd64, linux/arm64
- Version tags are created manually by pushing
  a git tag in format vX.Y.Z (e.g. v1.2.3)
- Claude Code must never create git tags

### Build Commands
Local multi-arch build (requires Docker Desktop
with Buildx and QEMU configured):
```
docker buildx build \
  --platform linux/amd64,linux/arm64 \
  -t ghcr.io/darkgrotto/countorsell:dev \
  --push .
```

Health check test (verifies image starts and
/health returns 200 with a local Postgres):
```
docker compose -f docker/test/docker-compose.test.yml \
  up --abort-on-container-exit
```

---

## Open Decisions
- [ ] Card subtype filtering (deferred enhancement,
      do not prevent architecturally)
- [ ] Partial restore (deferred, do not prevent
      architecturally)
- [ ] Email notification service for admin update
      alerts (implementation detail per provider)
- [ ] CCC certificate validation URL template
      (landing page only, confirm exact cert lookup
      path at implementation)
- [ ] Validation URL templates for BGS, SGC, CGC,
      ISA (confirm exact patterns at implementation)
- [ ] OAuth configuration UI design (post-setup
      admin settings)
- [ ] Application version check source URL - no
      app-version.json endpoint exists on countorsell.com
      currently; version check is a no-op pending a
      defined endpoint
- [ ] product_type field in sealed product packages -
      whether it maps to category_slug or sub_type_slug
      must be confirmed at first real package ingestion
- [ ] Reverse proxy choice for Docker Compose
      (Nginx or Traefik - implementation detail)
- [ ] Additional backup destinations beyond initial
      three (e.g. Dropbox, Google Drive)

## Decision Log
2026-03-08 - Standard hyphens only throughout all code
and content. Em-dashes cause errors and are never
acceptable.
2026-03-08 - No SQLite. PostgreSQL only across all
deployment types.
2026-03-08 - Docker Compose only, no single-container
fallback. Compose file is managed and regenerated by
wizard if changes needed.
2026-03-08 - No custom update source. countorsell.com
is the only valid update source. This is not
configurable by users or Product instance admins.
2026-03-08 - No Admin Backend access exposed to
Product instance users under any circumstances.
2026-03-08 - Treatments reference table received via
update packages. Never hardcoded. All treatment display
logic references this table.
2026-03-08 - Card condition stored as enum (NM, LP,
MP, HP, DMG). Autographed is a separate boolean field.
Condition preserved when autographed is true.
2026-03-08 - Two-tier role model: admin and general
user. Built-in local admin cannot be removed, disabled,
demoted, or converted. Admins have no collections.
Admins can view but not modify other users collections.
2026-03-08 - Self-enrollment off by default. Not
prompted in wizard. Configured post-setup by admin.
2026-03-08 - OAuth configured post-setup only.
Supported providers: Google, Microsoft (Live and
Entra ID), GitHub.
2026-03-08 - Account removal triggers export to
Product-specific backup format before deletion. Export
retained until admin explicitly deletes. Removal blocked
if export fails.
2026-03-08 - Content updates applied automatically.
Schema updates require admin approval. Pre-update backup
mandatory and silent for schema updates.
2026-03-08 - Daily update check at random time
generated by wizard. Manual trigger available to admins.
2026-03-08 - Docker application updates via generated
update.sh script. No Docker socket access from
containers. Cloud updates via native deployment
infrastructure.
2026-03-08 - Backup excludes canonical data. Includes
schema version. Four retained per type by default
(scheduled and pre-update independently). Restore
blocks if backup schema version newer than deployment.
2026-03-08 - Grading agencies: BGS, PSA, SGC, CGC,
CCC, ISA canonical. CCC is landing-page-only
verification (no direct cert lookup). Product instance
admins can add local agencies. Canonical agencies
authoritative and not modifiable locally. Agency code
collision triggers admin notification and confirmation.
2026-03-08 - Serial number and print run total stored
as separate integer fields. Four-digit suffix only
valid when >= 1000. Applies to serialized card tracker
and slab tracker.
2026-03-08 - Slab tracker includes optional serial
number and print run total for slabbed serialized cards.
2026-03-08 - Set completion: raw count and percentage.
Default counts one copy regardless of treatment. User
toggle for regular/non-foil only. Stored per user.
2026-03-08 - Historical values: acquisition price and
current market value only. No intermediate history.
TCGPlayer direct query always overwritten by next
update package.
2026-03-08 - Collection overview is default landing
page for general users. User can change default,
stored per user.
2026-03-08 - Card type filtering: top-level MTG types
only. Subtypes deferred. Do not prevent architecturally.
2026-03-08 - Wishlist is per user. Shows current market
value per card and total wishlist value. Separate
feature from collection.
2026-03-08 - Set codes: ^[a-z0-9]{3,4}$, stored
lowercase, displayed uppercase.
2026-03-08 - Card identifiers: ^[a-z0-9]{3,4}[0-9]{3,4}[a-z]?$
four-digit numeric suffix only valid when >= 1000,
stored lowercase, displayed uppercase.
2026-03-19 - Card identifier pattern extended with optional
trailing letter (e.g. "pala001a", "pala001b") to support
cards with letter-suffixed collector numbers. Cards
differing only by trailing letter are distinct cards.
2026-03-31 - Trailing letter "x" permanently reserved as
synthetic mapping for Scryfall dagger (†) collector numbers
(e.g. DRK/77† maps to drk077x, ARN variants like arn002x).
"x" will never appear as a real collector number letter.
2026-03-08 - iOS/Android and React Native dropped.
Responsive web only. Do not reintroduce.
2026-03-08 - Linode removed as supported deployment
target. Do not prevent adding later architecturally.
2026-03-08 - CI/CD: GitHub Actions throughout all
repos. IaC: Terraform throughout with provider-native
state storage (Azure Blob for Azure, AWS S3 for AWS,
GCP Cloud Storage for GCP).
2026-03-08 - Deployment targets: Azure (App Service),
AWS (App Runner), GCP (Cloud Run), Docker Compose.
2026-03-18 - Docker Compose reverse proxy: Nginx.
2026-03-18 - Sealed product taxonomy reference tables
added (sealed_product_categories and
sealed_product_sub_types). Received via update packages,
versioned independently. Slugs are primary keys in the
Product schema (no integer IDs). Taxonomy replacement
on update nulls orphaned inventory references rather
than deleting inventory records. Taxonomy filters
suppressed in UI when tables are empty. Current vs.
legacy product type distinction is not modeled.
2026-03-19 - Docker images published to
ghcr.io/darkgrotto/countorsell. Architectures:
linux/amd64 and linux/arm64. Dev builds from
main push (:dev tag). Release builds from manual
git tag push (vX.Y.Z produces :X.Y.Z, :X.Y,
:X, :latest). Version tags created manually,
never by automated workflow.
2026-03-19 - Demo mode is a runtime-only configuration
state activated by DEMO_MODE=true environment variable.
Not a DB flag, not a user setting, not configurable
post-launch. Demo sets are fixed (lea, 2ed, vis, eoe,
fdn, ecl, tla, fin, dsk, usg, ulg, uns, p23, tdm).
Expiry tracked via DEMO_EXPIRES_AT env var. Visitor
tracking uses per-session UUID (ASP.NET Core session).
Locked endpoints return 403 with consistent error
message. Instance name overridden to "CountOrSell Demo"
in demo mode. About view and banner surface demo state
to all users. Filter scope indicator appears on all
pages with universal filter. Seed script at
docker/scripts/demo-seed.sql populates demo accounts
and sample data against a freshly migrated database.
2026-04-05 - Update package format confirmed from
COS_Backend analysis. Website manifest at
www.countorsell.com/updates/manifest.json uses a
packages array with package_id, package_type (full or
delta), download_url, manifest_url, base_full_version,
generated_at. No separate schema packages - schema
migration runs on startup only. Per-package manifest
has per-file checksums in format sha256:<hex_lowercase>.
ZIP structure uses metadata/ prefix for all data files
(metadata/treatments.json, metadata/taxonomy.json,
metadata/sets/{code}/set.json,
metadata/sets/{code}/cards.json,
metadata/sealed/{id}.json). Images are in ZIP at
images/ paths (NOT fetched separately). No
app-version.json endpoint exists on countorsell.com
currently. Card colors published as array of symbols.
Treatment key is normalized_name field. Set total cards
is card_count field. Set code is set_code field.