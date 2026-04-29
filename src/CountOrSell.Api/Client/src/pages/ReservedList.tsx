import { useEffect, useMemo, useState } from 'react'
import { Star, Plus, Trash2 } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import {
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue,
} from '@/components/ui/select'
import {
  SortTh, SortDir, ToggleChip, CardDetailDialog, QuickAddDialog,
  Treatment, AddableCard, sortTreatments, fmt,
  COLORS, CARD_TYPES,
} from '@/components/cards/CardDialogs'
import { ConfirmDialog } from '@/components/ConfirmDialog'
import { usePreferences } from '@/contexts/PreferencesContext'

// ---- Types ------------------------------------------------------------------

interface RLCard extends AddableCard {
  setCode: string
  cardType: string | null
  color: string | null
  ownedQuantity: number
}

interface OwnedEntry {
  id: string
  cardIdentifier: string
  cardName: string | null
  setCode: string | null
  treatmentKey: string
  quantity: number
  condition: string
  acquisitionDate: string
  acquisitionPrice: number
}

type SortKey = 'name' | 'identifier' | 'setCode' | 'cardType' | 'currentMarketValue' | 'ownedQuantity'

// ---- Main page --------------------------------------------------------------

export function ReservedListPage() {
  const { prefs } = usePreferences()
  const [cards, setCards] = useState<RLCard[]>([])
  const [ownedEntries, setOwnedEntries] = useState<OwnedEntry[]>([])
  const [treatments, setTreatments] = useState<Treatment[]>([])
  const [loading, setLoading] = useState(true)
  const [sortKey, setSortKey] = useState<SortKey>(prefs.cardSortDefault === 'identifier' ? 'identifier' : 'name')
  const [sortDir, setSortDir] = useState<SortDir>('asc')
  const [ownedOnly, setOwnedOnly] = useState(false)
  const [unownedOnly, setUnownedOnly] = useState(false)
  const [colorFilter, setColorFilter] = useState('')
  const [typeFilter, setTypeFilter] = useState('')
  const [treatmentFilter, setTreatmentFilter] = useState('')
  const [addCard, setAddCard] = useState<RLCard | null>(null)
  const [detailCard, setDetailCard] = useState<RLCard | null>(null)
  const [addedIds, setAddedIds] = useState<Set<string>>(new Set())
  const [selected, setSelected] = useState<Set<string>>(new Set())
  const [bulkDeleteConfirm, setBulkDeleteConfirm] = useState(false)
  const [treatmentPick, setTreatmentPick] = useState('')
  const [treatmentConfirm, setTreatmentConfirm] = useState(false)

  async function loadOwned() {
    const res = await fetch('/api/collection/reserved')
    if (res.ok) setOwnedEntries(await res.json())
  }

  useEffect(() => {
    Promise.all([
      fetch('/api/cards/reserved-list').then(r => r.ok ? r.json() : []),
      fetch('/api/treatments').then(r => r.ok ? r.json() : []),
      fetch('/api/collection/reserved').then(r => r.ok ? r.json() : []),
    ]).then(([rl, t, owned]) => {
      setCards(rl)
      setTreatments(sortTreatments(t))
      setOwnedEntries(owned)
      setLoading(false)
    })
  }, [])

  function handleSort(k: string) {
    const key = k as SortKey
    if (sortKey === key) setSortDir(d => d === 'asc' ? 'desc' : 'asc')
    else { setSortKey(key); setSortDir('asc') }
  }

  // Derive available filter values from the full card list.
  const allColors = useMemo(
    () => new Set(cards.flatMap(c => c.color ? c.color.split('') : [])),
    [cards]
  )
  const visibleColors = COLORS.filter(col => allColors.has(col.key))

  const allTypes = useMemo(
    () => new Set(cards.flatMap(c => {
      if (!c.cardType) return []
      return CARD_TYPES.filter(t => c.cardType!.includes(t))
    })),
    [cards]
  )
  const visibleTypes = CARD_TYPES.filter(t => allTypes.has(t))

  const allTreatmentKeys = useMemo(
    () => new Set(cards.flatMap(c => c.validTreatments ?? [])),
    [cards]
  )
  const visibleTreatments = sortTreatments(treatments.filter(t => allTreatmentKeys.has(t.key)))

  const filtered = useMemo(() => {
    let result = cards
    if (ownedOnly) result = result.filter(c => c.ownedQuantity > 0)
    if (unownedOnly) result = result.filter(c => c.ownedQuantity === 0)
    if (colorFilter) result = result.filter(c => (c.color ?? '').includes(colorFilter))
    if (typeFilter) result = result.filter(c => (c.cardType ?? '').includes(typeFilter))
    if (treatmentFilter) result = result.filter(c => (c.validTreatments ?? []).includes(treatmentFilter))
    return result
  }, [cards, ownedOnly, unownedOnly, colorFilter, typeFilter, treatmentFilter])

  const sorted = useMemo(() => {
    return [...filtered].sort((a, b) => {
      let cmp = 0
      switch (sortKey) {
        case 'name':
          cmp = a.name.localeCompare(b.name)
          break
        case 'identifier':
          cmp = a.identifier.localeCompare(b.identifier)
          break
        case 'setCode':
          cmp = a.setCode.localeCompare(b.setCode) || a.name.localeCompare(b.name)
          break
        case 'cardType':
          cmp = (a.cardType ?? '').localeCompare(b.cardType ?? '') || a.name.localeCompare(b.name)
          break
        case 'currentMarketValue':
          cmp = (a.currentMarketValue ?? -1) - (b.currentMarketValue ?? -1)
          break
        case 'ownedQuantity':
          cmp = a.ownedQuantity - b.ownedQuantity || a.name.localeCompare(b.name)
          break
      }
      return sortDir === 'asc' ? cmp : -cmp
    })
  }, [filtered, sortKey, sortDir])

  const totalOwned = cards.reduce((s, c) => s + c.ownedQuantity, 0)
  const ownedCards = cards.filter(c => c.ownedQuantity > 0)
  const totalOwnedValue = ownedCards.reduce(
    (s, c) => s + (c.currentMarketValue ?? 0) * c.ownedQuantity,
    0
  )

  function handleAdded(mode: 'collection' | 'wishlist') {
    if (mode === 'collection' && addCard) {
      setAddedIds(prev => new Set(prev).add(addCard.identifier))
      setCards(prev => prev.map(c =>
        c.identifier === addCard.identifier
          ? { ...c, ownedQuantity: c.ownedQuantity + 1 }
          : c
      ))
      loadOwned()
    }
  }

  function toggleSelect(id: string) {
    setSelected(prev => { const n = new Set(prev); if (n.has(id)) n.delete(id); else n.add(id); return n })
  }

  function toggleSelectAll(all: boolean) {
    setSelected(all ? new Set(ownedEntries.map(e => e.id)) : new Set())
  }

  async function handleBulkDelete() {
    await fetch('/api/collection/bulk-delete', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ ids: Array.from(selected) }),
    })
    setSelected(new Set())
    await loadOwned()
    const res = await fetch('/api/cards/reserved-list')
    if (res.ok) setCards(await res.json())
  }

  async function handleBulkSetTreatment() {
    await fetch('/api/collection/bulk-set-treatment', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ ids: Array.from(selected), treatment: treatmentPick }),
    })
    setSelected(new Set())
    setTreatmentPick('')
    await loadOwned()
  }

  const hasActiveFilter = ownedOnly || unownedOnly || !!colorFilter || !!typeFilter || !!treatmentFilter

  return (
    <div className="space-y-4">
      <div className="flex items-start justify-between gap-4">
        <div>
          <div className="flex items-center gap-2">
            <Star className="h-5 w-5 text-amber-500 fill-amber-500" />
            <h1 className="text-2xl font-semibold">Reserved List</h1>
          </div>
          {!loading && (
            <p className="text-sm text-muted-foreground mt-0.5">
              {cards.length} total cards &middot; {ownedCards.length} owned
              {totalOwned > 0 && ` (${totalOwned} copies, ${fmt(totalOwnedValue)} value)`}
            </p>
          )}
        </div>
      </div>

      {!loading && cards.length > 0 && (
        <div className="space-y-2">
          {visibleColors.length > 0 && (
            <div className="flex flex-wrap gap-1.5 items-center">
              <span className="text-xs text-muted-foreground">Color:</span>
              {visibleColors.map(col => (
                <ToggleChip
                  key={col.key}
                  active={colorFilter === col.key}
                  title={col.title}
                  ariaLabel={`Filter by ${col.title}`}
                  onClick={() => setColorFilter(colorFilter === col.key ? '' : col.key)}
                >
                  {col.label}
                </ToggleChip>
              ))}
            </div>
          )}

          {visibleTypes.length > 0 && (
            <div className="flex flex-wrap gap-1.5 items-center">
              <span className="text-xs text-muted-foreground">Type:</span>
              {visibleTypes.map(t => (
                <ToggleChip
                  key={t}
                  active={typeFilter === t}
                  onClick={() => setTypeFilter(typeFilter === t ? '' : t)}
                >
                  {t}
                </ToggleChip>
              ))}
            </div>
          )}

          {visibleTreatments.length > 0 && (
            <div className="flex flex-wrap gap-1.5 items-center">
              <span className="text-xs text-muted-foreground">Treatment:</span>
              {visibleTreatments.map(t => (
                <ToggleChip
                  key={t.key}
                  active={treatmentFilter === t.key}
                  onClick={() => setTreatmentFilter(treatmentFilter === t.key ? '' : t.key)}
                >
                  {t.displayName}
                </ToggleChip>
              ))}
            </div>
          )}

          <div className="flex gap-1.5 items-center">
            <span className="text-xs text-muted-foreground">Show:</span>
            <ToggleChip active={ownedOnly} onClick={() => { setOwnedOnly(v => !v); setUnownedOnly(false) }}>
              <span className="inline-flex items-center gap-1">
                <Star className="h-3 w-3" /> Owned only
              </span>
            </ToggleChip>
            <ToggleChip active={unownedOnly} onClick={() => { setUnownedOnly(v => !v); setOwnedOnly(false) }}>
              Unowned only
            </ToggleChip>
            {hasActiveFilter && (
              <button
                type="button"
                className="text-xs text-muted-foreground hover:text-foreground underline ml-1"
                onClick={() => { setColorFilter(''); setTypeFilter(''); setTreatmentFilter(''); setOwnedOnly(false); setUnownedOnly(false) }}
              >
                Clear filters
              </button>
            )}
          </div>
        </div>
      )}

      {loading ? (
        <p className="text-sm text-muted-foreground">Loading...</p>
      ) : cards.length === 0 ? (
        <div className="py-12 text-center space-y-2">
          <Star className="h-8 w-8 text-muted-foreground mx-auto" />
          <p className="text-muted-foreground">No Reserved List cards found.</p>
          <p className="text-xs text-muted-foreground">
            Run a content update to populate Reserved List data.
          </p>
        </div>
      ) : ownedOnly && !unownedOnly ? (
        // Per-treatment owned entries view
        <>
          {selected.size > 0 && (
            <div className="flex items-center gap-3 px-3 py-2 rounded-md bg-muted border text-sm">
              <span className="font-medium">{selected.size} selected</span>
              <div className="flex items-center gap-1.5">
                <Select value={treatmentPick} onValueChange={setTreatmentPick}>
                  <SelectTrigger className="h-7 text-xs w-36">
                    <SelectValue placeholder="Set treatment..." />
                  </SelectTrigger>
                  <SelectContent>
                    {treatments.map(t => (
                      <SelectItem key={t.key} value={t.key}>{t.displayName}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
                <Button
                  size="sm"
                  variant="outline"
                  className="h-7 text-xs"
                  disabled={!treatmentPick}
                  onClick={() => setTreatmentConfirm(true)}
                >
                  Apply
                </Button>
              </div>
              <Button
                size="sm"
                variant="destructive"
                className="h-7 text-xs"
                onClick={() => setBulkDeleteConfirm(true)}
              >
                <Trash2 className="h-3.5 w-3.5 mr-1" /> Remove selected
              </Button>
              <Button size="sm" variant="ghost" className="h-7 text-xs" onClick={() => setSelected(new Set())}>
                Clear
              </Button>
            </div>
          )}
          {ownedEntries.length === 0 ? (
            <div className="py-12 text-center">
              <p className="text-muted-foreground text-sm">No owned Reserved List cards.</p>
            </div>
          ) : (
            <div className="rounded-md border overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b bg-muted/50 text-muted-foreground">
                    <th className="px-3 py-2 w-8">
                      <input
                        type="checkbox"
                        checked={ownedEntries.length > 0 && ownedEntries.every(e => selected.has(e.id))}
                        onChange={ev => toggleSelectAll(ev.target.checked)}
                        aria-label="Select all"
                      />
                    </th>
                    <th className="px-3 py-2 w-10"></th>
                    <th className="px-3 py-2 text-left">Card</th>
                    <th className="px-3 py-2 text-left">Treatment</th>
                    <th className="px-3 py-2 text-left">Condition</th>
                    <th className="px-3 py-2 text-center">Qty</th>
                    <th className="px-3 py-2 text-right">Acq. Price</th>
                  </tr>
                </thead>
                <tbody>
                  {ownedEntries.map(e => {
                    const treatmentLabel = treatments.find(t => t.key === e.treatmentKey)?.displayName ?? e.treatmentKey
                    return (
                      <tr key={e.id} className={`border-b last:border-0 hover:bg-muted/20 ${selected.has(e.id) ? 'bg-muted/30' : ''}`}>
                        <td className="px-3 py-2">
                          <input
                            type="checkbox"
                            checked={selected.has(e.id)}
                            onChange={() => toggleSelect(e.id)}
                            aria-label={`Select ${e.cardName ?? e.cardIdentifier}`}
                          />
                        </td>
                        <td className="px-3 py-2">
                          <img
                            src={`/api/images/cards/${(e.setCode ?? '').toLowerCase()}/${e.cardIdentifier.toLowerCase()}.jpg`}
                            alt=""
                            className="h-8 w-6 rounded object-cover bg-muted"
                            loading="lazy"
                            onError={ev => { (ev.target as HTMLImageElement).style.display = 'none' }}
                          />
                        </td>
                        <td className="px-3 py-2">
                          <div className="font-medium leading-tight">{e.cardName ?? e.cardIdentifier}</div>
                          <div className="text-xs text-muted-foreground font-mono">{e.cardIdentifier}</div>
                        </td>
                        <td className="px-3 py-2 text-xs text-muted-foreground">{treatmentLabel}</td>
                        <td className="px-3 py-2 text-xs text-muted-foreground">{e.condition}</td>
                        <td className="px-3 py-2 text-center tabular-nums">{e.quantity}</td>
                        <td className="px-3 py-2 text-right tabular-nums">{fmt(e.acquisitionPrice)}</td>
                      </tr>
                    )
                  })}
                </tbody>
              </table>
            </div>
          )}
        </>
      ) : sorted.length === 0 ? (
        <div className="py-12 text-center">
          <p className="text-muted-foreground text-sm">No cards match the current filters.</p>
        </div>
      ) : (
        // Catalog view - single row per card
        <div className="rounded-md border overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b bg-muted/50 text-muted-foreground">
                <th className="px-3 py-2 w-10"></th>
                <SortTh label="Card" sortKey="name" current={sortKey} dir={sortDir} onSort={handleSort} className="text-left" />
                <SortTh label="ID" sortKey="identifier" current={sortKey} dir={sortDir} onSort={handleSort} className="text-left" />
                <SortTh label="Set" sortKey="setCode" current={sortKey} dir={sortDir} onSort={handleSort} className="text-left" />
                <SortTh label="Type" sortKey="cardType" current={sortKey} dir={sortDir} onSort={handleSort} className="text-left" />
                <SortTh label="Market" sortKey="currentMarketValue" current={sortKey} dir={sortDir} onSort={handleSort} className="text-right" />
                <SortTh label="Owned" sortKey="ownedQuantity" current={sortKey} dir={sortDir} onSort={handleSort} className="text-center" />
                <th className="px-3 py-2 text-right"></th>
              </tr>
            </thead>
            <tbody>
              {sorted.map(c => {
                const recentlyAdded = addedIds.has(c.identifier)
                return (
                  <tr
                    key={c.identifier}
                    className="border-b last:border-0 hover:bg-muted/20 cursor-pointer"
                    onClick={e => {
                      if ((e.target as HTMLElement).closest('button')) return
                      setDetailCard(c)
                    }}
                  >
                    <td className="px-3 py-2">
                      <img
                        src={`/api/images/cards/${c.setCode.toLowerCase()}/${c.identifier.toLowerCase()}.jpg`}
                        alt=""
                        className="h-8 w-6 rounded object-cover bg-muted"
                        loading="lazy"
                        onError={ev => { (ev.target as HTMLImageElement).style.display = 'none' }}
                      />
                    </td>
                    <td className="px-3 py-2">
                      <div className="font-medium leading-tight">{c.name}</div>
                    </td>
                    <td className="px-3 py-2 font-mono text-xs text-muted-foreground whitespace-nowrap">
                      {c.identifier.toUpperCase()}
                    </td>
                    <td className="px-3 py-2 font-mono text-xs">{c.setCode.toUpperCase()}</td>
                    <td className="px-3 py-2 text-muted-foreground text-xs max-w-36 truncate">
                      {c.cardType || '-'}
                    </td>
                    <td className="px-3 py-2 text-right tabular-nums">{fmt(c.currentMarketValue)}</td>
                    <td className="px-3 py-2 text-center">
                      {c.ownedQuantity > 0 ? (
                        <Badge variant="secondary" className="tabular-nums">
                          {c.ownedQuantity}
                        </Badge>
                      ) : (
                        <span className="text-muted-foreground text-xs">-</span>
                      )}
                    </td>
                    <td className="px-3 py-2 text-right">
                      {recentlyAdded ? (
                        <Badge variant="secondary" className="text-xs">Added</Badge>
                      ) : (
                        <Button
                          size="sm"
                          variant="outline"
                          className="h-7 text-xs gap-1"
                          onClick={() => setAddCard(c)}
                        >
                          <Plus className="h-3 w-3" /> Add
                        </Button>
                      )}
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      )}

      {detailCard && (
        <CardDetailDialog
          identifier={detailCard.identifier}
          onClose={() => setDetailCard(null)}
          onPriceRefreshed={async () => {
            const res = await fetch('/api/cards/reserved-list')
            if (res.ok) setCards(await res.json())
          }}
          onAdd={() => setAddCard(detailCard)}
        />
      )}

      {addCard && (
        <QuickAddDialog
          card={addCard}
          treatments={treatments}
          onClose={() => setAddCard(null)}
          onAdded={handleAdded}
        />
      )}

      <ConfirmDialog
        open={bulkDeleteConfirm}
        onOpenChange={v => { if (!v) setBulkDeleteConfirm(false) }}
        title={`Remove ${selected.size} ${selected.size === 1 ? 'entry' : 'entries'}?`}
        description="This will permanently remove the selected entries from your collection."
        confirmLabel="Remove All"
        destructive
        onConfirm={handleBulkDelete}
      />
      <ConfirmDialog
        open={treatmentConfirm}
        onOpenChange={v => { if (!v) setTreatmentConfirm(false) }}
        title={`Set treatment on ${selected.size} ${selected.size === 1 ? 'entry' : 'entries'}?`}
        description={`Change treatment to "${treatments.find(t => t.key === treatmentPick)?.displayName ?? treatmentPick}" for the selected entries.`}
        confirmLabel="Apply"
        onConfirm={handleBulkSetTreatment}
      />
    </div>
  )
}
