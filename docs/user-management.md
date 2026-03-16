# User Management

---

## 1. Roles Overview

| Role | Description | Has Collection | Can View Others' Collections | Can Modify Others' Collections | Admin Actions |
|------|-------------|---------------|------------------------------|-------------------------------|---------------|
| Admin | Manages the instance and its users | No | Yes (read-only) | No | Yes |
| GeneralUser | Tracks their own collection and wishlist | Yes | No | No | No |

Admins have no collection of their own. They can view (never modify) all general user collections.

---

## 2. Built-in Local Admin

The built-in local admin account is created during the first-run wizard (Step 9). It has the following enforced constraints:

- **Always a local account** - cannot be converted to OAuth
- **Cannot be removed** - the removal endpoint returns an error
- **Cannot be disabled** - the disable endpoint returns an error
- **Cannot be demoted** - the demote endpoint returns an error
- **No collection** - admin accounts have no collection

These constraints are enforced in code by `UserService`, not by convention. The `IsBuiltinAdmin` flag is set on creation and cannot be changed.

The purpose of this account is guaranteed emergency access - even if all other admin accounts are removed or disabled, this account remains available.

---

## 3. Admin Accounts

Multiple admin accounts are supported. Any admin (including the built-in admin) can create additional admin accounts.

**Creating an admin:** Admins can be created as local accounts post-setup from the admin panel, or an existing general user can be promoted using `POST /api/users/{id}/promote`.

**Local vs OAuth admins:** Admin accounts can be local or OAuth (except the built-in admin, which is always local). OAuth admins authenticate through a configured provider.

**Admin account actions (via admin panel or API):**
- Add users (admin or general)
- Remove users (triggers export workflow - see Section 6)
- Disable accounts (reversible)
- Re-enable disabled accounts
- Promote general users to admin
- Demote admins to general users
- Enable or disable self-enrollment
- Manage instance configuration, branding, and OAuth settings
- Manage backup destinations, schedule, and retention
- Trigger manual content update checks and approve schema updates
- Manage TCGPlayer API key
- View and delete removed-user export files

### Last local admin protection

The system prevents the last remaining local admin (other than the built-in admin) from being removed, disabled, or demoted. Any such attempt returns a `409 Conflict` with the message `Cannot remove/disable/demote the last local admin account.`

The built-in admin is always protected independently, regardless of how many other local admins exist.

---

## 4. General User Accounts

General users have their own collection (cards, sealed product, serialized cards, slabs) and their own wishlist. They can only view and modify their own data.

**Creating a general user:** One general user is created during the wizard (Step 10). Additional users are created post-setup from the admin panel or via self-enrollment if enabled.

**Local vs OAuth general users:** General user accounts can be local or OAuth.

**Self-enrollment:** When self-enrollment is enabled, new users who authenticate (locally or via OAuth) receive immediate general user access with no admin approval required. Self-enrollment is off by default and is configured post-setup by an admin.

---

## 5. Account States

| State | Meaning | Can Log In | Data Retained | Reversible |
|-------|---------|-----------|---------------|------------|
| Active | Normal, functioning account | Yes | Yes | N/A |
| Disabled | Temporarily blocked | No | Yes | Yes |
| Removed | Permanently deleted | No | No (collection data deleted after export) | No |

**Disabling:** `POST /api/users/{id}/disable` - sets state to Disabled. The user cannot log in. All data is retained. An admin can re-enable with `POST /api/users/{id}/reenable`. Cannot be applied to the built-in admin or the last local admin.

**Removing:** `POST /api/users/{id}/remove` - triggers the export workflow (see Section 6) and permanently deletes collection data. Cannot be applied to the built-in admin or the last local admin.

---

## 6. Account Removal Export

When an account is removed, collection data is exported to a Product-specific backup format before any deletion occurs. This is enforced in code - deletion cannot succeed without a successful export.

**Workflow:**
1. Admin triggers removal via `POST /api/users/{id}/remove`
2. `ExportService` exports the user's collection data (collection entries, serialized entries, slab entries, sealed inventory entries, wishlist entries) to a `.zip` archive labeled with the username and removal timestamp
3. The export file is stored in the database as a `UserExportFile` record
4. Only after a successful export does the system delete collection data and mark the account as Removed
5. If the export fails for any reason, the entire operation is aborted and the user data is left intact. The admin receives a clear error message with the failure reason.

**Accessing export files:**
- `GET /api/users/{id}/exports` - list export files for a user (Admin only)
- `DELETE /api/users/{id}/exports/{exportId}` - delete an export file (Admin only)

Export files are retained until an admin explicitly deletes them. There is no automatic expiry.

---

## 7. OAuth Configuration

OAuth providers are configured post-setup in admin settings. See [docs/configuration.md](configuration.md) for the specific client ID and secret configuration for each provider.

Supported providers:
- **Google** - personal and workspace accounts
- **Microsoft** - personal (Live) and organizational (Entra ID) accounts
- **GitHub** - personal GitHub accounts

An OAuth provider is only available for login if both the client ID and client secret are configured. Unconfigured providers return `400 Bad Request` from the OAuth login endpoint with the message `OAuth provider '{provider}' is not configured on this instance.`

---

## 8. Self-Enrollment

Self-enrollment controls whether new users can create accounts without admin intervention.

- **Default state:** Off (disabled) on every new instance
- **When disabled:** Only admins can create new accounts
- **When enabled:** Any user who authenticates (via local signup or OAuth) immediately receives a general user account with no approval step
- **How to configure:** Admin panel (post-setup only - not configurable during wizard)

Self-enrollment status does not affect existing accounts.
