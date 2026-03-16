# Metrics and Values

---

## 1. Collection Value

Collection value is the current market price of everything owned.

**Formula:** `current market value * quantity` per entry, summed.

**Breakdown by content type:**

| Content type | How value is calculated |
|-------------|------------------------|
| cards | `SUM(card.currentMarketValue * entry.quantity)` across all collection entries |
| serialized | `SUM(card.currentMarketValue)` across all serialized entries (each is qty 1) |
| slabs | `SUM(card.currentMarketValue)` across all slab entries (each is qty 1) |
| sealed-product | Sealed product market value field is not yet populated from update packages - currently returns 0 |

**Where viewable:** Per card (via card market value endpoint), per set (via set completion + metrics endpoints with `setCode` filter), per content type, and for the whole collection via `GET /api/collection/metrics`.

Admins requesting metrics without a `userId` query parameter receive aggregate metrics across all users. Admins can also pass a specific `userId` to view an individual user's metrics.

---

## 2. Profit/Loss

Profit/loss measures gain or loss against what was paid for the collection.

**Formula:** `(current market value * quantity) - (acquisition price * quantity)` per entry, summed.

Profit/loss is available at the same levels as collection value: per content type, per set (filtered), and whole collection. Universal filters are applicable.

A positive value indicates gain; negative indicates loss relative to acquisition cost.

---

## 3. Historical Values

Only two price points are stored per entry:

- **Acquisition price** - the price paid when the entry was created (or manually updated)
- **Current market value** - the most recent market price from update packages or TCGPlayer

No intermediate price history is stored between these two points. Profit/loss is always calculated from these two values only.

**TCGPlayer price interaction:** When a TCGPlayer direct price refresh is triggered (via `POST /api/cards/{identifier}/refresh-price`), the stored `CurrentMarketValue` is updated immediately. However, the next content update package from countorsell.com will overwrite this value. There is no UI distinction between a TCGPlayer-refreshed price and an update-package price. The About view shows the date of the last overall content update, not per-card update timestamps.

---

## 4. Set Completion

Set completion shows how many cards in a set are in the collection, as a raw count and a percentage.

**Calculation:**
- Counts distinct card identifiers owned by the user within the set
- Divides by the set's `totalCards` field (from the sets reference data)
- Percentage is rounded to one decimal place (e.g., 95.7%)

**Default counting mode:** One copy of any treatment counts toward completion. Having a regular copy and a foil copy of the same card identifier still counts as one card toward completion.

**Regular-only toggle:** Users can enable a per-user preference to count only `regular` treatment cards toward completion. When enabled, foil and other treatment variants are excluded. This preference is stored in the `user_preferences` table and accessed via `GET /api/users/me/preferences` and `PATCH /api/users/me/preferences`.

**API:**
- `GET /api/collection/completion` - all sets for the user (or admin-specified user)
- `GET /api/collection/completion/{setCode}` - specific set

Both endpoints accept a `regularOnly=true` query parameter to override the user's stored preference for a single request.

Results from `GET /api/collection/completion` are ordered by completion percentage descending.

---

## 5. Counts and Breakdowns

Each content type has its own count, tracked independently.

| Metric | Source | Unit |
|--------|--------|------|
| Total card count | Sum of `quantity` across all collection entries | Individual cards (not distinct identifiers) |
| Serialized count | Count of serialized entry records | Individual serialized cards |
| Slab count | Count of slab entry records | Individual slabs |
| Sealed product count | Sum of `quantity` across sealed inventory entries | Individual units |

These counts appear in the `MetricsResult` returned by `GET /api/collection/metrics`.

The `byContentType` array in the metrics response provides per-type breakdowns including `totalValue`, `totalProfitLoss`, and `count`.

---

## 6. Wishlist Value

Each wishlist entry stores a card identifier. When the wishlist is retrieved, the current market value for each card is looked up from the cards table and returned alongside the entry.

**Per-entry value:** `card.currentMarketValue` (or 0 if the card is not found in the reference data)

**Total wishlist value:** Sum of all per-entry market values

**Response structure from `GET /api/wishlist`:**
```json
{
  "totalValue": 42.50,
  "entries": [
    {
      "id": "...",
      "cardIdentifier": "EOE019",
      "cardName": "Example Card",
      "marketValue": 12.50,
      "createdAt": "..."
    }
  ]
}
```

Universal filters are not implemented on the wishlist endpoint - all entries are returned. The wishlist is a separate feature from the collection and has its own storage.
