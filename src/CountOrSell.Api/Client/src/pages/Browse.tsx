import { useEffect, useState, useCallback } from 'react'
import { ChevronLeft, Plus, Search, Star, BookmarkPlus } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Badge } from '@/components/ui/badge'
import {
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue,
} from '@/components/ui/select'
import {
  Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle,
} from '@/components/ui/dialog'
import {
  SortTh, SortDir, ToggleChip, CardDetailDialog, QuickAddDialog,
  Treatment, AddableCard, sortTreatments, fmt,
  COLORS, CARD_TYPES,
} from '@/components/cards/CardDialogs'
import { SetSymbol } from '@/components/ui/SetSymbol'
import { usePreferences } from '@/contexts/PreferencesContext'

// ---- Types ------------------------------------------------------------------

interface BrowseSet {
  code: string
  name: string
  totalCards: number
  setType: string | null
  releaseDate: string | null
}

interface BrowseCard extends AddableCard {
  manaCost: string | null
  color: string | null
  cardType: string | null
  rarity: string | null
  isReserved: boolean
}

// ---- Bulk add dialog --------------------------------------------------------

const CONDITIONS = ['NM', 'LP', 'MP', 'HP', 'DMG'] as const

interface BulkAddResult { added: number; skipped: number }

function BulkAddToCollectionDialog({
  cards,
  treatments,
  onClose,
  onDone,
}: {
  cards: BrowseCard[]
  treatments: Treatment[]
  onClose: () => void
  onDone: (result: BulkAddResult) => void
}) {
  const regularKey = treatments.find(t => t.key === 'regular')?.key ?? treatments[0]?.key ?? 'regular'
  const [treatmentKey, setTreatmentKey] = useState(regularKey)
  const [condition, setCondition] = useState<string>('NM')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')

  async function handleAdd() {
    setBusy(true)
    setError('')
    let added = 0
    let skipped = 0
    for (const card of cards) {
      const res = await fetch('/api/collection', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          cardIdentifier: card.identifier.toLowerCase(),
          treatment: treatmentKey,
          quantity: 1,
          condition,
          autographed: false,
          acquisitionDate: new Date().toISOString().slice(0, 10),
          acquisitionPrice: card.currentMarketValue ?? 0,
        }),
      })
      if (res.ok) added++
      else skipped++
    }
    setBusy(false)
    onDone({ added, skipped })
    onClose()
  }

  return (
    <Dialog open onOpenChange={v => !v && onClose()}>
      <DialogContent className="sm:max-w-sm">
        <DialogHeader>
          <DialogTitle>Add {cards.length} card{cards.length !== 1 ? 's' : ''} to Collection</DialogTitle>
        </DialogHeader>
        <div className="space-y-4">
          <div className="space-y-1.5">
            <Label>Treatment</Label>
            <Select value={treatmentKey} onValueChange={setTreatmentKey}>
              <SelectTrigger><SelectValue /></SelectTrigger>
              <SelectContent>
                {treatments.map(t => <SelectItem key={t.key} value={t.key}>{t.displayName}</SelectItem>)}
              </SelectContent>
            </Select>
          </div>
          <div className="space-y-1.5">
            <Label>Condition</Label>
            <Select value={condition} onValueChange={setCondition}>
              <SelectTrigger><SelectValue /></SelectTrigger>
              <SelectContent>
                {CONDITIONS.map(c => <SelectItem key={c} value={c}>{c}</SelectItem>)}
              </SelectContent>
            </Select>
          </div>
          {error && <p className="text-sm text-destructive">{error}</p>}
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={onClose}>Cancel</Button>
          <Button onClick={handleAdd} disabled={busy}>
            {busy ? 'Adding...' : 'Add to Collection'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}

// ---- Constants --------------------------------------------------------------

// Set types considered "standard" (equivalent to COS_Backend STANDARD_SET_TYPES).
const STANDARD_SET_TYPES = new Set([
  'core', 'expansion', 'masters', 'funny', 'planechase', 'draft_innovation',
])

const RARITIES = ['common', 'uncommon', 'rare', 'mythic'] as const

// Human-readable label for a set_type slug.
function setTypeLabel(t: string): string {
  return t.replace(/_/g, ' ').replace(/\b\w/g, c => c.toUpperCase())
}

// ---- Set list view ----------------------------------------------------------

function SetListView({
  sets,
  onSelectSet,
}: {
  sets: BrowseSet[]
  onSelectSet: (s: BrowseSet) => void
}) {
  const [search, setSearch] = useState('')
  const [sortKey, setSortKey] = useState('releaseDate')
  const [sortDir, setSortDir] = useState<SortDir>('desc')
  const [typeFilter, setTypeFilter] = useState('')
  const [standardOnly, setStandardOnly] = useState(false)

  function handleSort(key: string) {
    if (sortKey === key) setSortDir(d => d === 'asc' ? 'desc' : 'asc')
    else { setSortKey(key); setSortDir('asc') }
  }

  // Available set types derived from loaded data, standard types first.
  const availableTypes = [...new Set(
    sets.map(s => s.setType).filter(Boolean) as string[]
  )].sort((a, b) => {
    const aStd = STANDARD_SET_TYPES.has(a) ? 0 : 1
    const bStd = STANDARD_SET_TYPES.has(b) ? 0 : 1
    if (aStd !== bStd) return aStd - bStd
    return a.localeCompare(b)
  })

  function handleStandardToggle() {
    setStandardOnly(v => !v)
    setTypeFilter('') // standard-only overrides specific type selection
  }

  function handleTypeChip(t: string) {
    setStandardOnly(false)
    setTypeFilter(prev => prev === t ? '' : t)
  }

  const filtered = sets
    .filter(s => {
      if (search.trim()) {
        const q = search.toLowerCase()
        if (!s.code.toLowerCase().includes(q) && !s.name.toLowerCase().includes(q))
          return false
      }
      if (standardOnly) {
        if (!s.setType || !STANDARD_SET_TYPES.has(s.setType)) return false
      } else if (typeFilter) {
        if (s.setType !== typeFilter) return false
      }
      return true
    })
    .slice()
    .sort((a, b) => {
      let cmp = 0
      if (sortKey === 'code') cmp = a.code.localeCompare(b.code)
      else if (sortKey === 'name') cmp = a.name.localeCompare(b.name)
      else if (sortKey === 'totalCards') cmp = (a.totalCards ?? 0) - (b.totalCards ?? 0)
      else if (sortKey === 'releaseDate') cmp = (a.releaseDate ?? '').localeCompare(b.releaseDate ?? '')
      return sortDir === 'asc' ? cmp : -cmp
    })

  return (
    <div className="space-y-3">
      <div className="space-y-2">
        <div className="relative">
          <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground pointer-events-none" />
          <Input
            placeholder="Search sets..."
            value={search}
            onChange={e => setSearch(e.target.value)}
            className="pl-8"
          />
        </div>

        {availableTypes.length > 0 && (
          <div className="flex flex-wrap gap-1.5 items-center">
            <span className="text-xs text-muted-foreground">Type:</span>
            <ToggleChip active={standardOnly} onClick={handleStandardToggle}>
              Standard
            </ToggleChip>
            {availableTypes.map(t => (
              <ToggleChip
                key={t}
                active={!standardOnly && typeFilter === t}
                onClick={() => handleTypeChip(t)}
              >
                {setTypeLabel(t)}
              </ToggleChip>
            ))}
          </div>
        )}
      </div>

      <div className="rounded-md border overflow-x-auto">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b bg-muted/50 text-muted-foreground">
              <SortTh label="Code" sortKey="code" current={sortKey} dir={sortDir} onSort={handleSort} className="text-left" />
              <SortTh label="Name" sortKey="name" current={sortKey} dir={sortDir} onSort={handleSort} className="text-left" />
              <SortTh label="Cards" sortKey="totalCards" current={sortKey} dir={sortDir} onSort={handleSort} className="text-right" />
              <SortTh label="Released" sortKey="releaseDate" current={sortKey} dir={sortDir} onSort={handleSort} className="text-right" />
            </tr>
          </thead>
          <tbody>
            {filtered.length === 0 ? (
              <tr>
                <td colSpan={4} className="px-4 py-8 text-center text-muted-foreground">
                  {search || typeFilter || standardOnly
                    ? 'No sets match the current filters.'
                    : 'No sets available.'}
                </td>
              </tr>
            ) : (
              filtered.map(s => (
                <tr
                  key={s.code}
                  className="border-b last:border-0 hover:bg-muted/30 cursor-pointer"
                  onClick={() => onSelectSet(s)}
                >
                  <td className="px-4 py-2.5 font-mono text-xs font-medium">{s.code.toUpperCase()}</td>
                  <td className="px-4 py-2.5">
                    <span className="flex items-center gap-2">
                      <SetSymbol setCode={s.code} className="text-base" />
                      <span className="font-medium">{s.name}</span>
                      {s.setType && (
                        <span className="text-xs text-muted-foreground">
                          {setTypeLabel(s.setType)}
                        </span>
                      )}
                    </span>
                  </td>
                  <td className="px-4 py-2.5 text-right text-muted-foreground">{s.totalCards}</td>
                  <td className="px-4 py-2.5 text-right text-muted-foreground">
                    {s.releaseDate ?? '-'}
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </div>
  )
}

// ---- Card list view ---------------------------------------------------------

function CardListView({
  set,
  treatments,
  onBack,
}: {
  set: BrowseSet
  treatments: Treatment[]
  onBack: () => void
}) {
  const [cards, setCards] = useState<BrowseCard[]>([])
  const [loading, setLoading] = useState(true)
  const [search, setSearch] = useState('')
  const [colorFilter, setColorFilter] = useState('')
  const [typeFilter, setTypeFilter] = useState('')
  const [rarityFilter, setRarityFilter] = useState('')
  const [treatmentFilter, setTreatmentFilter] = useState('')
  const [rlFilter, setRlFilter] = useState(false)
  const [phiFilter, setPhiFilter] = useState(false)
  const [hybridFilter, setHybridFilter] = useState(false)
  const { prefs } = usePreferences()
  const [addCard, setAddCard] = useState<BrowseCard | null>(null)
  const [detailCard, setDetailCard] = useState<BrowseCard | null>(null)
  const [addedIds, setAddedIds] = useState<Set<string>>(new Set())
  const [sortKey, setSortKey] = useState(prefs.cardSortDefault === 'identifier' ? 'identifier' : 'card')
  const [sortDir, setSortDir] = useState<SortDir>('asc')
  const [selected, setSelected] = useState<Set<string>>(new Set())
  const [bulkCollectionOpen, setBulkCollectionOpen] = useState(false)
  const [bulkAddResult, setBulkAddResult] = useState<BulkAddResult | null>(null)

  const loadCards = useCallback(async () => {
    setLoading(true)
    try {
      const res = await fetch(`/api/sets/${set.code.toLowerCase()}/cards`)
      if (res.ok) setCards(await res.json())
    } finally {
      setLoading(false)
    }
  }, [set.code])

  useEffect(() => { loadCards() }, [loadCards])

  function handleSort(key: string) {
    if (sortKey === key) setSortDir(d => d === 'asc' ? 'desc' : 'asc')
    else { setSortKey(key); setSortDir('asc') }
  }

  const filtered = cards
    .filter(c => {
      if (search.trim()) {
        const q = search.toLowerCase()
        if (!c.identifier.toLowerCase().includes(q) && !c.name.toLowerCase().includes(q))
          return false
      }
      if (colorFilter && !(c.color ?? '').includes(colorFilter)) return false
      if (typeFilter && !(c.cardType ?? '').includes(typeFilter)) return false
      if (rarityFilter && c.rarity !== rarityFilter) return false
      if (treatmentFilter && !(c.validTreatments ?? []).includes(treatmentFilter)) return false
      if (rlFilter && !c.isReserved) return false
      if (phiFilter && !(c.manaCost?.includes('/P}') ?? false)) return false
      if (hybridFilter && !(c.manaCost != null && /\/[WUBRG]\}/.test(c.manaCost))) return false
      return true
    })
    .slice()
    .sort((a, b) => {
      let cmp = 0
      if (sortKey === 'card') cmp = a.name.localeCompare(b.name) || a.identifier.localeCompare(b.identifier)
      else if (sortKey === 'identifier') cmp = a.identifier.localeCompare(b.identifier)
      else if (sortKey === 'color') cmp = (a.color ?? '').localeCompare(b.color ?? '')
      else if (sortKey === 'type') cmp = (a.cardType ?? '').localeCompare(b.cardType ?? '')
      else if (sortKey === 'rarity') {
        const ri = (r: string | null) => RARITIES.indexOf((r ?? '') as typeof RARITIES[number])
        cmp = ri(a.rarity) - ri(b.rarity)
      }
      else if (sortKey === 'market') cmp = (a.currentMarketValue ?? -1) - (b.currentMarketValue ?? -1)
      return sortDir === 'asc' ? cmp : -cmp
    })

  const setColors = new Set(cards.flatMap(c => c.color ? c.color.split(',') : []))
  const visibleColors = COLORS.filter(col => setColors.has(col.key))

  const setTypes = new Set(
    cards.flatMap(c => {
      if (!c.cardType) return []
      return CARD_TYPES.filter(t => c.cardType!.includes(t))
    })
  )
  const visibleTypes = CARD_TYPES.filter(t => setTypes.has(t))

  const setRarities = new Set(cards.map(c => c.rarity).filter(Boolean) as string[])
  const visibleRarities = RARITIES.filter(r => setRarities.has(r))

  // Treatments that appear for at least one card in the set (use treatments prop for display names).
  const setTreatmentKeys = new Set(cards.flatMap(c => c.validTreatments ?? []))
  const visibleTreatments = sortTreatments(treatments.filter(t => setTreatmentKeys.has(t.key)))

  const hasReserved = cards.some(c => c.isReserved)
  const hasPhiMana = cards.some(c => c.manaCost?.includes('/P}') ?? false)
  const hasHybridMana = cards.some(c => c.manaCost != null && /\/[WUBRG]\}/.test(c.manaCost))

  function handleAdded(mode: 'collection' | 'wishlist') {
    if (mode === 'collection' && addCard)
      setAddedIds(prev => new Set(prev).add(addCard.identifier))
  }

  function toggleSelect(id: string) {
    setSelected(prev => { const n = new Set(prev); if (n.has(id)) n.delete(id); else n.add(id); return n })
  }

  function toggleSelectAll(all: boolean) {
    setSelected(all ? new Set(filtered.map(c => c.identifier)) : new Set())
  }

  async function handleBulkAddWishlist() {
    const toAdd = filtered.filter(c => selected.has(c.identifier))
    for (const card of toAdd) {
      await fetch('/api/wishlist', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ cardIdentifier: card.identifier.toLowerCase() }),
      })
    }
    setAddedIds(prev => new Set([...prev, ...toAdd.map(c => c.identifier)]))
    setSelected(new Set())
  }

  const selectedCards = filtered.filter(c => selected.has(c.identifier))

  return (
    <div className="space-y-3">
      <div className="flex items-center gap-2">
        <button
          type="button"
          className="flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground"
          onClick={onBack}
        >
          <ChevronLeft className="h-4 w-4" />
          Sets
        </button>
        <span className="text-muted-foreground">/</span>
        <span className="flex items-center gap-1.5 text-sm font-medium">
          <SetSymbol setCode={set.code} />
          {set.name}
        </span>
        <span className="text-xs text-muted-foreground ml-1">({set.code.toUpperCase()})</span>
      </div>

      <div className="space-y-2">
        <div className="relative">
          <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground pointer-events-none" />
          <Input
            placeholder="Search cards..."
            value={search}
            onChange={e => setSearch(e.target.value)}
            className="pl-8"
          />
        </div>

        {visibleColors.length > 0 && (
          <div className="flex flex-wrap gap-1.5 items-center">
            <span className="text-xs text-muted-foreground">Color:</span>
            {visibleColors.map(col => (
              <ToggleChip
                key={col.key}
                active={colorFilter === col.key}
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

        {visibleRarities.length > 0 && (
          <div className="flex flex-wrap gap-1.5 items-center">
            <span className="text-xs text-muted-foreground">Rarity:</span>
            {visibleRarities.map(r => (
              <ToggleChip
                key={r}
                active={rarityFilter === r}
                onClick={() => setRarityFilter(rarityFilter === r ? '' : r)}
              >
                {r.charAt(0).toUpperCase() + r.slice(1)}
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

        {(hasReserved || hasPhiMana || hasHybridMana) && (
          <div className="flex flex-wrap gap-1.5 items-center">
            <span className="text-xs text-muted-foreground">Show:</span>
            {hasReserved && (
              <ToggleChip active={rlFilter} onClick={() => setRlFilter(v => !v)}>
                <span className="inline-flex items-center gap-1">
                  <Star className="h-3 w-3" /> Reserved List
                </span>
              </ToggleChip>
            )}
            {hasPhiMana && (
              <ToggleChip active={phiFilter} onClick={() => setPhiFilter(v => !v)}>
                Phi Mana
              </ToggleChip>
            )}
            {hasHybridMana && (
              <ToggleChip active={hybridFilter} onClick={() => setHybridFilter(v => !v)}>
                Hybrid Mana
              </ToggleChip>
            )}
          </div>
        )}
      </div>

      {selected.size > 0 && (
        <div className="flex items-center gap-3 px-3 py-2 rounded-md bg-muted border text-sm">
          <span className="font-medium">{selected.size} selected</span>
          <Button size="sm" variant="outline" className="h-7 text-xs gap-1" onClick={() => setBulkCollectionOpen(true)}>
            <Plus className="h-3.5 w-3.5" /> Add to Collection
          </Button>
          <Button size="sm" variant="outline" className="h-7 text-xs gap-1" onClick={handleBulkAddWishlist}>
            <BookmarkPlus className="h-3.5 w-3.5" /> Add to Wishlist
          </Button>
          <Button size="sm" variant="ghost" className="h-7 text-xs" onClick={() => setSelected(new Set())}>
            Clear
          </Button>
          {bulkAddResult && (
            <span className="text-xs text-muted-foreground ml-1">
              Added {bulkAddResult.added}{bulkAddResult.skipped > 0 ? `, ${bulkAddResult.skipped} skipped` : ''}
            </span>
          )}
        </div>
      )}

      {loading ? (
        <p className="text-sm text-muted-foreground py-8 text-center">Loading cards...</p>
      ) : (
        <div className="rounded-md border overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b bg-muted/50 text-muted-foreground">
                <th className="px-3 py-2 w-8">
                  <input
                    type="checkbox"
                    checked={filtered.length > 0 && filtered.every(c => selected.has(c.identifier))}
                    onChange={ev => toggleSelectAll(ev.target.checked)}
                    aria-label="Select all"
                  />
                </th>
                <th className="px-3 py-2 w-10"></th>
                <SortTh label="Card" sortKey="card" current={sortKey} dir={sortDir} onSort={handleSort} className="text-left" />
                <SortTh label="ID" sortKey="identifier" current={sortKey} dir={sortDir} onSort={handleSort} className="text-left" />
                <SortTh label="Color" sortKey="color" current={sortKey} dir={sortDir} onSort={handleSort} className="text-left" />
                <SortTh label="Type" sortKey="type" current={sortKey} dir={sortDir} onSort={handleSort} className="text-left" />
                <SortTh label="Rarity" sortKey="rarity" current={sortKey} dir={sortDir} onSort={handleSort} className="text-left" />
                <SortTh label="Market" sortKey="market" current={sortKey} dir={sortDir} onSort={handleSort} className="text-right" />
                <th className="px-3 py-2 text-right"></th>
              </tr>
            </thead>
            <tbody>
              {filtered.length === 0 ? (
                <tr>
                  <td colSpan={9} className="px-4 py-8 text-center text-muted-foreground">
                    No cards match the current filters.
                  </td>
                </tr>
              ) : (
                filtered.map(c => {
                  const owned = addedIds.has(c.identifier)
                  return (
                    <tr
                      key={c.identifier}
                      className={`border-b last:border-0 hover:bg-muted/20 cursor-pointer ${selected.has(c.identifier) ? 'bg-muted/30' : ''}`}
                      onClick={e => {
                        if ((e.target as HTMLElement).closest('button,input')) return
                        setDetailCard(c)
                      }}
                    >
                      <td className="px-3 py-2">
                        <input
                          type="checkbox"
                          checked={selected.has(c.identifier)}
                          onChange={() => toggleSelect(c.identifier)}
                          aria-label={`Select ${c.name}`}
                        />
                      </td>
                      <td className="px-3 py-2">
                        <img
                          src={`/api/images/cards/${set.code.toLowerCase()}/${c.identifier.toLowerCase()}.jpg`}
                          alt=""
                          className="h-8 w-6 rounded object-cover bg-muted"
                          loading="lazy"
                          onError={e => { (e.target as HTMLImageElement).style.display = 'none' }}
                        />
                      </td>
                      <td className="px-3 py-2">
                        <div className="flex items-center gap-1">
                          <span className="font-medium">{c.name}</span>
                          {c.isReserved && (
                            <span title="Reserved List" className="inline-flex">
                              <Star className="h-3 w-3 text-amber-500 fill-amber-500 shrink-0" />
                            </span>
                          )}
                        </div>
                      </td>
                      <td className="px-3 py-2 font-mono text-xs text-muted-foreground whitespace-nowrap">
                        {c.identifier.toUpperCase()}
                      </td>
                      <td className="px-3 py-2 text-muted-foreground font-mono text-xs">
                        {c.color || '-'}
                      </td>
                      <td className="px-3 py-2 text-muted-foreground text-xs max-w-36 truncate">
                        {c.cardType || '-'}
                      </td>
                      <td className="px-3 py-2 text-muted-foreground text-xs capitalize">
                        {c.rarity || '-'}
                      </td>
                      <td className="px-3 py-2 text-right tabular-nums">{fmt(c.currentMarketValue)}</td>
                      <td className="px-3 py-2 text-right">
                        {owned ? (
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
                })
              )}
            </tbody>
          </table>
        </div>
      )}

      {detailCard && (
        <CardDetailDialog
          identifier={detailCard.identifier}
          onClose={() => setDetailCard(null)}
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

      {bulkCollectionOpen && (
        <BulkAddToCollectionDialog
          cards={selectedCards}
          treatments={treatments}
          onClose={() => setBulkCollectionOpen(false)}
          onDone={result => {
            setBulkAddResult(result)
            setAddedIds(prev => new Set([...prev, ...selectedCards.map(c => c.identifier)]))
            setSelected(new Set())
          }}
        />
      )}
    </div>
  )
}

// ---- Main page --------------------------------------------------------------

export function BrowsePage() {
  const [sets, setSets] = useState<BrowseSet[]>([])
  const [treatments, setTreatments] = useState<Treatment[]>([])
  const [selectedSet, setSelectedSet] = useState<BrowseSet | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    Promise.all([
      fetch('/api/sets').then(r => r.ok ? r.json() : []),
      fetch('/api/treatments').then(r => r.ok ? r.json() : []),
    ]).then(([s, t]) => {
      setSets(s)
      setTreatments(sortTreatments(t))
      setLoading(false)
    })
  }, [])

  if (loading) {
    return (
      <div>
        <h1 className="text-2xl font-semibold mb-4">Browse</h1>
        <p className="text-sm text-muted-foreground">Loading...</p>
      </div>
    )
  }

  return (
    <div className="space-y-4">
      <h1 className="text-2xl font-semibold">Browse</h1>

      {selectedSet ? (
        <CardListView
          set={selectedSet}
          treatments={treatments}
          onBack={() => setSelectedSet(null)}
        />
      ) : (
        <SetListView sets={sets} onSelectSet={setSelectedSet} />
      )}
    </div>
  )
}
