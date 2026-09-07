# Update System

---

## 1. Overview

All canonical content (cards, sets, treatments, sealed products, images) comes from countorsell.com. The Product instance polls a manifest once daily at a randomly generated time, checks for new content, and applies updates automatically when available.

The update source is always `countorsell.com`. This is not configurable. The website manifest is
served from the `www` host (`https://www.countorsell.com/updates/manifest.json`) and the packages it
links are served from the Backend's package storage origin. See Section 8 for both hosts.

| Update type | Applies automatically | Requires admin approval |
|------------|----------------------|------------------------|
| Content update | Yes | No |
| Schema update | No | Yes |
| Application version update | No (notified only) | N/A - requires manual action |

---

## 2. Content Updates

Content updates deliver new and updated canonical reference data. The Product never applies layer logic - it always receives fully resolved flat data.

**Contents of a content update package (ZIP archive).** The ZIP carries metadata only:
- `manifest.json` and `manifest.json.sig` - the per-package manifest and its detached signature
- `metadata/treatments.json` - treatment reference table (id, normalized name, display name, sort order)
- `metadata/taxonomy.json` - sealed product category and sub-type reference tables
- `metadata/sets/{set_code}/set.json` - set reference data
- `metadata/sets/{set_code}/cards.json` - card reference data for that set
- `metadata/sets/{set_code}/pricing.json` - per-treatment pricing for that set
- `metadata/sealed/{product_id}.json` - one file per sealed product

**Images are not in the ZIP.** They are published as loose blobs at the package base URL (the
directory holding `manifest.json`) and fetched individually - one request per image, up to 10
concurrent - after the database transaction commits. Each is verified against its `checksums`
entry before being stored. Image failures are best-effort: they are logged and do not fail the
update. A full package is currently around 2,850 metadata files plus roughly 101,000 image
fetches, which is what sizes any CDN or proxy in front of the package origin.

Because image failures are non-fatal, an update can finish with an incomplete image set. That
is reported rather than passing as plain success: the update result names how many images are
missing and why, and an admin notification is raised. Rate limiting (HTTP 429) is called out
separately from other failures, because it means the request budget in front of the package
origin was exhausted - an operator-side fix - rather than a problem with the package. The
missing images can be retrieved afterwards with a targeted image redownload, which reports its
own shortfall the same way.

**Application behavior:**
1. Download the package ZIP from the URL in the manifest
2. Verify the SHA-256 checksum (see Section 7)
3. Apply all data changes in a single database transaction (treatments, taxonomy, sets, cards, pricing, sealed products)
4. Record the new content version in the `update_versions` table
5. Fetch and store images outside the transaction, best-effort
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

The UI notifies the admin that an update is available. An admin can trigger the redeploy in-app via `POST /api/updates/deploy` (Admin-only, demo-locked), which calls the provider-specific `ICloudDeploymentService` - Azure App Service, AWS App Runner, or GCP Cloud Run - using the deployment's managed identity to update the image tag and restart the service. No Docker socket or long-lived credentials are involved.

The image is currently pulled from `ghcr.io/darkgrotto/countorsell` on all providers. Private-registry support (e.g. Azure Container Registry) is a planned follow-up - see [roadmap.md](roadmap.md#configurable-image-registry).

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

Manifests come in two levels. The Product fetches the website manifest from the hardcoded
`https://www.countorsell.com/updates/manifest.json`, and that manifest points at a per-package
manifest for each published package. The update source is not configurable.

Only the `www` host is served - the apex `countorsell.com` is not a routed hostname at the CDN
edge and answers Cloudflare error 1016. An apex URL appearing in a manifest is rewritten to the
`www` host before it is fetched.

### Website manifest

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
      "package_id": "20260513-190155-32fb03",
      "package_type": "full",
      "download_url": "https://<package-storage-origin>/publish-a/20260513-190155-32fb03/package.zip",
      "manifest_url": "https://<package-storage-origin>/publish-a/20260513-190155-32fb03/manifest.json",
      "base_full_version": null,
      "generated_at": "<ISO 8601>"
    }
  ]
}
```

| Field | Type | Description |
|-------|------|-------------|
| `schema_version` | string | Schema version the packages are authored against |
| `generated_at` | string | ISO 8601 timestamp the manifest was generated |
| `minimum_product_version` | string | Minimum application version required to apply these packages |
| `content_versions` | object | Current published version per content type |
| `packages[].package_id` | string | Package identifier |
| `packages[].package_type` | string | `full` or `delta` |
| `packages[].download_url` | string | URL of the package ZIP |
| `packages[].manifest_url` | string | URL of the per-package manifest |
| `packages[].base_full_version` | string or null | Base full version a delta applies to; null for full packages |
| `packages[].generated_at` | string | ISO 8601 timestamp; the Product selects the most recent package |

### Package URLs and the allowed source

`download_url` and `manifest_url` are read from the website manifest, which is not signed, so
both are validated against a fixed two-host allowlist (`UpdateSource`) before any outbound
request is made. This is an SSRF guard: without it, a poisoned manifest could point the server
at an internal address such as a cloud metadata endpoint.

| Allowed host | Serves |
|--------------|--------|
| `www.countorsell.com` | The website manifest and the signing JWKS |
| `packages.countorsell.com` | `package.zip`, per-package `manifest.json`, `manifest.json.sig`, and image blobs |
| `cosadminstoreprod.blob.core.windows.net` | The same package files, transitionally - see below |

The Backend publishes packages to object storage and the website manifest links them directly
rather than proxying them through the site, so a deployment needs outbound HTTPS to both the
website host and whichever package host the manifest currently names. A URL on any other host is
rejected and the update reports "Found a package but could not fetch its manifest or signature."

`packages.countorsell.com` is a stable hostname the Backend owns, so the storage account behind
it can move without a Product release. Until it is in DNS and the website manifest emits it, the
manifest still links the storage account directly, so both hosts are allowlisted. Once the
manifest has migrated, the storage host entry can be dropped from `UpdateSource`.

Package hosts are never substituted for one another - each URL is fetched from the host it was
published on, because a CNAME's target can require its own `Host` header. The only rewrite is
apex to `www`.

The base URL for per-file image fetches is the directory portion of the package `manifest_url`
that was actually fetched, so image fetches stay on the allowed source too.

### Per-package manifest

Each package manifest carries the content versions and per-file SHA-256 checksums for that
package, and is served alongside a detached signature at `<manifest_url>.sig`. The signature is
verified against the JWKS at `https://www.countorsell.com/.well-known/cos-pubkey.json` before
any field in the manifest is used. See Section 7 for checksum verification.

```json
{
  "package_type": "full",
  "generated_at": "<ISO 8601>",
  "base_full_version": null,
  "schema_version": "1.0.0",
  "content_versions": {
    "cards": { "version": "1.2.0", "record_count": 123 },
    "sets": { "version": "1.1.0", "record_count": 42 },
    "slabs": { "version": "0.0.0", "record_count": 0 }
  },
  "retained_full_versions": ["1.0.0"],
  "checksums": {
    "metadata/treatments.json": "sha256:<hex_lowercase>",
    "images/sets/eoe/eoe019.jpg": "sha256:<hex_lowercase>"
  }
}
```

Schema updates are not shipped as separate packages. `schema_version` is a metadata field only;
migrations run on startup (Section 3). Application version availability is not part of the
manifest either - it is read from the GitHub releases API (Section 4).
