# API Reference

All endpoints are prefixed with the instance base URL. Authentication uses cookie-based sessions established via `POST /api/auth/login` or an OAuth callback.

Auth column values:
- **None** - no authentication required
- **Authenticated** - any authenticated user (Admin or GeneralUser)
- **Admin** - Admin role required

Endpoints marked **Demo-locked** return HTTP 403 with `{"error": "This action is not available in demo mode."}` when the instance is running in demo mode (`DEMO_MODE=true`), regardless of the caller's authentication state. See [docs/deployment/demo.md](deployment/demo.md) for demo mode details.

---

## Health

| Method | Path | Auth | Description | Notes |
|--------|------|------|-------------|-------|
| `GET` | `/health` | None | Returns application and database health status | Returns `{"status":"healthy","database":"reachable"}` or `{"status":"unhealthy","database":"unreachable"}` with HTTP 503 when unhealthy |

---

## Auth (`/api/auth`)

| Method | Path | Auth | Description | Notes |
|--------|------|------|-------------|-------|
| `POST` | `/api/auth/login` | None | Authenticate with username and password | Body: `{"username": "...", "password": "..."}`. Sets session cookie. Returns `{userId, username, role}` |
| `POST` | `/api/auth/logout` | None | Invalidate the current session | Clears session cookie |
| `GET` | `/api/auth/oauth/{provider}` | None | Initiate OAuth login flow for a provider | `provider` values: `google`, `microsoft`, `github`. Returns 400 if provider is not configured |
| `GET` | `/api/auth/oauth/{provider}/callback` | None | OAuth callback handler | Called by the OAuth provider after authentication. Returns 400 if provider is not configured |

---

## Users (`/api/users`)

| Method | Path | Auth | Description | Notes |
|--------|------|------|-------------|-------|
| `GET` | `/api/users` | Admin | List all user accounts | Returns id, username, displayName, role, state, authType, isBuiltinAdmin, createdAt, lastLoginAt |
| `POST` | `/api/users/{id}/disable` | Admin | Disable a user account | Returns 409 if user is the built-in admin or the last local admin |
| `POST` | `/api/users/{id}/remove` | Admin | Remove a user account | Triggers export workflow before deletion. Returns 409 if built-in admin, last local admin, or export fails. **Demo-locked:** returns 403 in demo mode |
| `POST` | `/api/users/{id}/demote` | Admin | Demote admin to general user | Returns 409 if built-in admin or last local admin |
| `POST` | `/api/users/{id}/promote` | Admin | Promote general user to admin | |
| `POST` | `/api/users/{id}/reenable` | Admin | Re-enable a disabled account | Returns 409 if account is not in Disabled state |
| `GET` | `/api/users/{id}/exports` | Admin | List removed-user export files for a user | |
| `DELETE` | `/api/users/{id}/exports/{exportId}` | Admin | Delete a removed-user export file | Returns 204 |
| `GET` | `/api/users/me/preferences` | Authenticated | Get current user's preferences | Returns `{setCompletionRegularOnly, defaultPage}` |
| `PATCH` | `/api/users/me/preferences` | Authenticated | Update current user's preferences | Body: `{setCompletionRegularOnly?: bool, defaultPage?: string}` |

---

## Collection (`/api/collection`)

All collection endpoints resolve the requesting user's own collection unless the `userId` query parameter is provided (Admin only). General users receive 403 if they supply a `userId` parameter.

| Method | Path | Auth | Description | Notes |
|--------|------|------|-------------|-------|
| `GET` | `/api/collection` | Authenticated | List collection entries | Query: `userId` (Admin only), `filter.*` (CollectionFilter fields) |
| `POST` | `/api/collection` | Authenticated | Create a collection entry | Body: CollectionEntryRequest. Returns 201 with the created entry |
| `GET` | `/api/collection/{id}` | Authenticated | Get a single collection entry | Returns 404 if not found; 403 if not owner or admin |
| `PUT` | `/api/collection/{id}` | Authenticated | Replace a collection entry | Owner only. Returns 403 if not owner |
| `DELETE` | `/api/collection/{id}` | Authenticated | Delete a collection entry | Owner only. Returns 204 |
| `PATCH` | `/api/collection/{id}/quantity` | Authenticated | Adjust quantity by a delta | Body: integer delta. Returns 400 if resulting quantity < 1 |
| `GET` | `/api/collection/metrics` | Authenticated | Get collection value and P/L metrics | Query: `userId` (Admin only), `filter.*`. Admins without `userId` get aggregate across all users |
| `GET` | `/api/collection/completion` | Authenticated | Get set completion for all sets | Query: `userId` (Admin only), `regularOnly=true/false` |
| `GET` | `/api/collection/completion/{setCode}` | Authenticated | Get set completion for a specific set | Query: `userId` (Admin only), `regularOnly=true/false` |
| `POST` | `/api/collection/refresh-price/{cardIdentifier}` | Authenticated | Query TCGPlayer for current price | Returns 400 if no API key configured; 502 if TCGPlayer returns no price. Does not persist the price to the cards table. **Demo-locked:** returns 403 in demo mode |

---

## Serialized (`/api/serialized`)

| Method | Path | Auth | Description | Notes |
|--------|------|------|-------------|-------|
| `GET` | `/api/serialized` | Authenticated | List serialized entries | Query: `userId` (Admin only) |
| `POST` | `/api/serialized` | Authenticated | Create a serialized entry | Body: SerializedEntryRequest. Returns 201 |
| `GET` | `/api/serialized/{id}` | Authenticated | Get a single serialized entry | Returns 404 if not found; 403 if not owner or admin |
| `PUT` | `/api/serialized/{id}` | Authenticated | Replace a serialized entry | Owner only |
| `DELETE` | `/api/serialized/{id}` | Authenticated | Delete a serialized entry | Owner only. Returns 204 |

---

## Slabs (`/api/slabs`)

| Method | Path | Auth | Description | Notes |
|--------|------|------|-------------|-------|
| `GET` | `/api/slabs` | Authenticated | List slab entries | Query: `userId` (Admin only) |
| `POST` | `/api/slabs` | Authenticated | Create a slab entry | Body: SlabEntryRequest. Returns 400 if `serialNumber` provided without `printRunTotal` |
| `GET` | `/api/slabs/{id}` | Authenticated | Get a single slab entry | Returns 404 if not found; 403 if not owner or admin |
| `PUT` | `/api/slabs/{id}` | Authenticated | Replace a slab entry | Owner only. Returns 400 if `serialNumber` provided without `printRunTotal` |
| `DELETE` | `/api/slabs/{id}` | Authenticated | Delete a slab entry | Owner only. Returns 204 |

---

## Sealed Inventory (`/api/sealed-inventory`)

| Method | Path | Auth | Description | Notes |
|--------|------|------|-------------|-------|
| `GET` | `/api/sealed-inventory` | Authenticated | List sealed inventory entries | Query: `userId` (Admin only) |
| `POST` | `/api/sealed-inventory` | Authenticated | Create a sealed inventory entry | Body: SealedInventoryRequest. Returns 201 |
| `GET` | `/api/sealed-inventory/{id}` | Authenticated | Get a single sealed inventory entry | Returns 404 if not found; 403 if not owner or admin |
| `PUT` | `/api/sealed-inventory/{id}` | Authenticated | Replace a sealed inventory entry | Owner only |
| `DELETE` | `/api/sealed-inventory/{id}` | Authenticated | Delete a sealed inventory entry | Owner only. Returns 204 |

---

## Wishlist (`/api/wishlist`)

| Method | Path | Auth | Description | Notes |
|--------|------|------|-------------|-------|
| `GET` | `/api/wishlist` | Authenticated | List wishlist entries with current market values | Returns `{totalValue, entries: [{id, cardIdentifier, cardName, marketValue, createdAt}]}` |
| `POST` | `/api/wishlist` | Authenticated | Add a card to the wishlist | Body: `{"cardIdentifier": "..."}`. Returns 201 |
| `GET` | `/api/wishlist/export/tcgplayer` | Authenticated | Export wishlist as a TCGPlayer mass-entry URL | Returns a redirect URL for TCGPlayer mass entry. **Demo-locked:** returns 403 in demo mode |
| `DELETE` | `/api/wishlist/{id}` | Authenticated | Remove a wishlist entry | Owner only. Returns 204 |

---

## Treatments (`/api/treatments`)

| Method | Path | Auth | Description | Notes |
|--------|------|------|-------------|-------|
| `GET` | `/api/treatments` | Authenticated | List all treatments ordered by sort order | Returns `[{key, displayName, sortOrder}]` |

---

## Cards (`/api/cards`)

| Method | Path | Auth | Description | Notes |
|--------|------|------|-------------|-------|
| `GET` | `/api/cards/{identifier}` | Authenticated | Get a card by identifier | `identifier` is case-insensitive; returned uppercase. Returns 404 if not found |
| `GET` | `/api/cards/search` | Authenticated | Search cards by name | Query: `q` (minimum 2 characters). Returns 400 if too short |
| `GET` | `/api/cards/{identifier}/market-value` | Authenticated | Get current market value for a card | Returns `{identifier, currentMarketValue, updatedAt}` |
| `POST` | `/api/cards/{identifier}/refresh-price` | Authenticated | Refresh and persist price from TCGPlayer | Returns 400 if no API key; 502 if no price returned. Persists the new price to the cards table |

---

## Grading Agencies (`/api/grading-agencies`)

| Method | Path | Auth | Description | Notes |
|--------|------|------|-------------|-------|
| `GET` | `/api/grading-agencies` | Authenticated | List all grading agencies | Returns code (uppercase), fullName, validationUrlTemplate, supportsDirectLookup, source, active |
| `POST` | `/api/grading-agencies` | Admin | Create a local grading agency | Returns 409 if code already exists |
| `PATCH` | `/api/grading-agencies/{code}` | Admin | Update a local grading agency | Returns 403 if canonical agency; supports patching fullName, validationUrlTemplate, supportsDirectLookup |
| `DELETE` | `/api/grading-agencies/{code}` | Admin | Delete a local grading agency | Returns 403 if canonical. Returns 409 with `{requiresReplacement: true, recordCount: N}` if slabs reference this agency. Provide `{"replacementCode": "..."}` in body to remap and delete |

---

## Updates (`/api/updates`)

All update endpoints require Admin authentication.

| Method | Path | Auth | Description | Notes |
|--------|------|------|-------------|-------|
| `GET` | `/api/updates/status` | Admin | Get current update status | Returns `{currentContentVersion, pendingSchemaUpdate, latestApplicationVersion, applicationUpdatePending}` |
| `GET` | `/api/updates/notifications` | Admin | List unread admin notifications | Returns `[{id, message, category, isRead, createdAt}]` |
| `POST` | `/api/updates/check` | Admin | Trigger an immediate update check | **Demo-locked:** returns 403 in demo mode |
| `POST` | `/api/updates/schema/{id}/approve` | Admin | Approve and execute a pending schema update | Returns 422 if the update fails; check notifications for details. **Demo-locked:** returns 403 in demo mode |
| `POST` | `/api/updates/notifications/{id}/read` | Admin | Mark a notification as read | |

---

## Demo (`/api/demo`)

| Method | Path | Auth | Description | Notes |
|--------|------|------|-------------|-------|
| `GET` | `/api/demo/status` | None | Get demo mode state | Returns 404 when not in demo mode. When demo mode is active returns `{isDemo: true, expiresAt, secondsRemaining, visitorId, demoSets}`. `visitorId` is a per-session UUID stored in ASP.NET Core session. `secondsRemaining` is 0 when no expiry is set or when the expiry has passed |

---

## About (`/api/about`)

| Method | Path | Auth | Description | Notes |
|--------|------|------|-------------|-------|
| `GET` | `/api/about` | Authenticated | Get instance information | Returns `{currentVersion, latestReleasedVersion, updatePending, lastContentUpdate, instanceName, isDemo, demoSets, license: {name, fullName, url}}`. `isDemo` is `false` and `demoSets` is empty when not in demo mode. `instanceName` is overridden to `"CountOrSell Demo"` in demo mode |

---

## Backup (`/api/backup`, `/api/restore`)

All backup and restore endpoints require Admin authentication.

| Method | Path | Auth | Description | Notes |
|--------|------|------|-------------|-------|
| `GET` | `/api/backup/status` | Admin | Get backup status summary | Returns last scheduled backup, last pre-update backup, next scheduled time, and active destinations |
| `GET` | `/api/backup/history` | Admin | Get paginated backup history | Query: `page` (default 1), `pageSize` (default 20, max 100) |
| `POST` | `/api/backup/trigger` | Admin | Trigger a manual scheduled backup | Returns `{id, label, createdAt}`. **Demo-locked:** returns 403 in demo mode |
| `GET` | `/api/backup/{id}/download` | Admin | Download a backup ZIP from local storage | Returns 404 if not in local storage; 410 if pruned |
| `GET` | `/api/backup/destinations` | Admin | List all backup destinations | |
| `POST` | `/api/backup/destinations` | Admin | Add a backup destination | Body: `{destinationType, label, configurationJson}`. **Demo-locked:** returns 403 in demo mode |
| `DELETE` | `/api/backup/destinations/{id}` | Admin | Remove a backup destination | **Demo-locked:** returns 403 in demo mode |
| `POST` | `/api/backup/destinations/{id}/test` | Admin | Test a backup destination connection | Returns `{"success": true/false}` |
| `POST` | `/api/restore` | Admin | Restore from an uploaded backup ZIP file | Multipart form upload, max 500 MB. Returns 409 if backup schema version > deployment schema version; 422 on restore failure. **Demo-locked:** returns 403 in demo mode |
| `POST` | `/api/restore/{backupId}` | Admin | Restore from an existing backup record in local storage | Returns 404 if record not found or file missing; 409 on schema version conflict; 422 on failure. **Demo-locked:** returns 403 in demo mode |

---

## Settings (`/api/settings`)

All settings endpoints require Admin authentication.

| Method | Path | Auth | Description | Notes |
|--------|------|------|-------------|-------|
| `GET` | `/api/settings/backup` | Admin | Get current backup settings | Returns `{schedule, retentionScheduled, retentionPreUpdate}` |
| `PATCH` | `/api/settings/backup` | Admin | Update backup settings | Body: `{schedule?: string, retentionScheduled?: int, retentionPreUpdate?: int}`. All fields optional |
| `PATCH` | `/api/settings/instance` | Admin | Update instance settings (e.g. name) | **Demo-locked:** returns 403 in demo mode |
| `PATCH` | `/api/settings/self-enrollment` | Admin | Enable or disable self-enrollment | **Demo-locked:** returns 403 in demo mode |
| `PATCH` | `/api/settings/oauth/{provider}` | Admin | Configure an OAuth provider | `provider` values: `google`, `microsoft`, `github`. **Demo-locked:** returns 403 in demo mode |
| `DELETE` | `/api/settings/oauth/{provider}` | Admin | Clear an OAuth provider configuration | **Demo-locked:** returns 403 in demo mode |
