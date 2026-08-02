# Roadmap and Future Approaches

This page lists work that is deliberately deferred, with enough context to pick it up later. It is the reader-facing view of the **Open Decisions** section in [CLAUDE.md](../CLAUDE.md), which stays the source of truth for contributors. Each item notes the current state and the intended next step.

Deferred items are architected so they are not blocked - none of the shortcuts below prevent the full approach later.

---

## Backup and Restore

### Partial restore
- **State:** Full restore works end to end (same-instance recovery, new-instance migration, first-run restoration all share one code path). There is no selection UI and no apply-subset backend path.
- **Next:** Design the selection UI (which content types / users / date ranges), then a `RestoreService.RestoreSubsetAsync` that runs inside a single transaction.

---

## Notifications

### Email notification service
- **State:** In-app admin notifications work via `AdminNotificationService`. `EmailNotificationService.SendUpdateNotificationAsync` is a stub that logs but does not send. (Invitation logging was hardened to stop emitting the recipient address and invite-token URL.)
- **Next:** Pick a provider strategy (SMTP direct, or an abstraction over SES / SendGrid / Mailgun / Postmark), add the provider-config keys to [configuration.md](configuration.md), and wire credentials through `IConfiguration` the way the OAuth providers are.

---

## Cloud Deployment

### Configurable image registry
- **State:** The single-public-registry model ships every deployment from `ghcr.io/darkgrotto/countorsell`. The in-app "update to tag" path in the cloud deployment services hardcodes that reference, so a private-registry deployment (for example Azure Container Registry with `AcrPull`) would be silently repointed to GHCR.
- **Next:** Make the registry/image reference configurable (env var, defaulting to GHCR) and grant the deployment's managed identity the matching pull role in Terraform for private registries. The default stays GHCR - it is the canonical cross-provider registry, and there is no product reason to move off it otherwise.

---

## Mobile / Responsive

The app is responsive: the sidebar collapses to a hamburger drawer below the `md` breakpoint, the collection-family tables reflow into labeled cards below `sm`, and dialog form fields stack on small screens. Two follow-ups remain.

### Collection table card-reflow
- **State:** Serialized, Wishlist, Slabs, Sealed Product, and Reserved List reflow to cards on phones (the `.card-table` CSS plus `data-label` cells). The much larger `Collection` page stays on polished horizontal scroll (`max-sm:min-w-max`) to avoid a risky reflow rewrite.
- **Next:** Apply the same `.card-table` + `data-label` pattern to the Collection table, or extract a shared data-table component the collection pages render through so the reflow is defined once.

### Top-nav layout mobile drawer
- **State:** The default sidebar layout has a hamburger off-canvas drawer. The opt-in top-nav layout (`navLayout: 'top'`) degrades to a horizontally scrolling nav bar on phones - usable but not a first-class mobile pattern.
- **Next:** Give the top-nav layout the same hamburger/off-canvas treatment so both nav layouts are equivalent on mobile.

---

## Not planned (by design)

These are intentional non-goals for the Product, recorded so they are not re-proposed:

- Native iOS/Android or React Native apps (responsive web only).
- SQLite or any non-PostgreSQL database.
- Configurable content-update sources (countorsell.com is the only source).
- Docker socket access from within any container.
- Layer-resolution logic in the Product (it always receives fully resolved flat data).
