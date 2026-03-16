# Update System

---

## 1. Overview

All canonical content (cards, sets, treatments, sealed products, images) comes from countorsell.com. The Product instance polls a manifest once daily at a randomly generated time, checks for new content, and applies updates automatically when available.

The update source is always `countorsell.com`. This is not configurable.

| Update type | Applies automatically | Requires admin approval |
|------------|----------------------|------------------------|
| Content update | Yes | No |
| Schema update | No | Yes |
| Application version update | No (notified only) | N/A - requires manual action |

---

## 2. Content Updates

Content updates deliver new and updated canonical reference data. The Product never applies layer logic - it always receives fully resolved flat data.

**Contents of a content update package (ZIP archive):**
- `treatments.json` - treatment reference table (key, display name, sort order)
- `sets.json` - set reference data (code, name, total card count, release date)
- `cards.json` - card reference data (identifier, set code, name, color, card type, current market value)
- `sealed_products.json` - sealed product reference data (identifier, set code, name)
- `images/` directory - card and sealed product images (best-effort; image save failures are logged but do not fail the update)

**Application behavior:**
1. Download the package ZIP from the URL in the manifest
2. Verify the SHA-256 checksum (see Section 7)
3. Apply all data changes in a single database transaction (treatments, sets, cards, sealed products)
4. Record the new content version in the `update_versions` table
5. Save images outside the transaction (best-effort, failures logged and skipped)

If the database transaction fails, it is rolled back entirely. No partial updates are committed.

Content updates are applied in the background with no user interaction required and no service interruption.

---

## 3. Schema Updates

Schema updates modify the database structure and require explicit admin approval before they are applied.

**Detection:** When the manifest contains a `schema` entry, and the Product's current schema version is below the `minimumProductSchemaVersion` of the content entry, a `PendingSchemaUpdate` record is created.

**Approval workflow:**
1. Admin sees a notification in the admin panel (via `GET /api/updates/notifications` or `GET /api/updates/status`)
2. Admin reviews the pending schema update description
3. Admin approves via `POST /api/updates/schema/{id}/approve`
4. The `SchemaUpdateCoordinator` executes the update:
   a. Takes a pre-update backup (silently, no user confirmation)
   b. If backup fails: blocks the update, notifies the admin with instructions to check backup destination configuration
   c. If backup succeeds: runs EF Core migrations
   d. If migration fails: attempts automatic restore from the pre-update backup, notifies admin
   e. If migration succeeds: marks the pending update as approved and notifies the admin

**Failure notifications** appear in the admin notifications panel (`GET /api/updates/notifications`) and include actionable instructions.

---

## 4. Application Version Updates

The manifest includes an `application` entry with the latest released application version. This is compared against `ProductVersion.Current` (currently `1.0.0`).

### Docker deployments

When a new version is detected, the admin panel displays a notification with the location of the update script:

```
./docker/scripts/update.sh
```

This script runs:
```bash
docker compose -f docker/compose/docker-compose.yml pull
docker compose -f docker/compose/docker-compose.yml up -d
```

No Docker socket access occurs from within any container. The update script is run manually by the operator.

### Cloud deployments (Azure, AWS, GCP)

Application version updates on cloud deployments are performed through the deployment infrastructure (Terraform + GitHub Actions). The UI notifies the admin that an update is available; the update itself is triggered through the cloud provider's native mechanism by pushing a new image and triggering a redeploy.

---

## 5. Manual Update Trigger

Product instance admins can trigger an immediate update check at any time:

```
POST /api/updates/check
```

Requires Admin authentication. This triggers the same check that runs on the daily schedule and applies the same logic (content updates applied automatically, schema updates queued for approval).

---

## 6. Update Notifications

**Admin in-app notifications:** Schema update detections, approval confirmations, failures, and backup/rollback events are stored as `AdminNotification` records and surfaced via `GET /api/updates/notifications`. Admins can mark notifications as read via `POST /api/updates/notifications/{id}/read`.

**Email notifications:** The email notification service is a stub. `EmailNotificationService.SendUpdateNotificationAsync` logs a message but does not send email. Email notification is planned but not yet implemented.

**General users:** Can see the current application version, latest released version, and whether an update is pending via `GET /api/about`. They cannot see update package contents, notification details, or any administrative controls.

---

## 7. Checksum Verification

All downloaded packages are verified with SHA-256 before being applied. The checksum is provided in the manifest as `zipSha256`.

The `PackageVerifier` computes a SHA-256 hash of the downloaded package stream and compares it case-insensitively against the expected value from the manifest. If the checksum does not match, the package is rejected and not applied. The admin is notified of the mismatch.

---

## 8. Manifest Format

The manifest is fetched from `https://countorsell.com/updates/manifest.json`. The update source is not configurable.

```json
{
  "content": {
    "version": "2026-03-08",
    "downloadUrl": "https://countorsell.com/updates/content-2026-03-08.zip",
    "zipSha256": "abc123...",
    "minimumProductSchemaVersion": 1
  },
  "schema": {
    "version": "2",
    "description": "Adds grading agency table",
    "downloadUrl": "https://countorsell.com/updates/schema-v2.zip",
    "zipSha256": "def456..."
  },
  "application": {
    "version": "1.1.0"
  }
}
```

| Field | Type | Description |
|-------|------|-------------|
| `content.version` | string | Content package version identifier |
| `content.downloadUrl` | string | URL to download the content ZIP |
| `content.zipSha256` | string | Expected SHA-256 hex digest of the content ZIP |
| `content.minimumProductSchemaVersion` | integer | Minimum schema version required to apply this content package |
| `schema` | object | Present only when a schema update is available; absent otherwise |
| `schema.version` | string | Target schema version |
| `schema.description` | string | Human-readable description shown to admin before approval |
| `schema.downloadUrl` | string | URL to download the schema migration ZIP |
| `schema.zipSha256` | string | Expected SHA-256 hex digest of the schema ZIP |
| `application` | object | Present only when a new application version is available; absent otherwise |
| `application.version` | string | Latest released application version |

If `schema` is absent, no schema update is pending. If `application` is absent, no new application version is available.
