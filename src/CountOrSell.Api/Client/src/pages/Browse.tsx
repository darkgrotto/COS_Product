import { useEffect, useState, useCallback } from 'react'
import { ChevronLeft, Plus, Search, Star } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import {
  SortTh, SortDir, ToggleChip, CardDetailDialog, QuickAddDialog,
  Treatment, AddableCard, sortTreatments, fmt,
  COLORS, CARD_TYPES,
} from '@/components/cards/CardDialogs'
import { SetSymbol } from '@/components/ui/SetSymbol'

// ---- Types ------------------------------------------------------------------

interface BrowseSet {
  code: string
  name: string
  totalCards: number
  setType: string | null
  releaseDate: string | null
}

interface BrowseCard extends AddableCard {
  color: string | null
  cardType: string | null
  rarity: string | null
  isReserved: boolean
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
  const [sortKey, setSortKey] = useState('name')
  const [sortDir, setSortDir] = useState<SortDir>('asc')
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
  const [rlFilter, setRlFilter] = useState(false)
  const [addCard, setAddCard] = useState<BrowseCard | null>(null)
  const [detailCard, setDetailCard] = useState<BrowseCard | null>(null)
  const [addedIds, setAddedIds] = useState<Set<string>>(new Set())
  const [sortKey, setSortKey] = useState('card')
  const [sortDir, setSortDir] = useState<SortDir>('asc')

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
      if (rlFilter && !c.isReserved) return false
      return true
    })
    .slice()
    .sort((a, b) => {
      let cmp = 0
      if (sortKey === 'card') cmp = a.name.localeCompare(b.name) || a.identifier.localeCompare(b.identifier)
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

  const hasReserved = cards.some(c => c.isReserved)

  function handleAdded() {
    if (addCard) setAddedIds(prev => new Set(prev).add(addCard.identifier))
  }

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

        {hasReserved && (
          <div className="flex gap-1.5 items-center">
            <span className="text-xs text-muted-foreground">Show:</span>
            <ToggleChip active={rlFilter} onClick={() => setRlFilter(v => !v)}>
              <span className="inline-flex items-center gap-1">
                <Star className="h-3 w-3" /> Reserved List
              </span>
            </ToggleChip>
          </div>
        )}
      </div>

      {loading ? (
        <p className="text-sm text-muted-foreground py-8 text-center">Loading cards...</p>
      ) : (
        <div className="rounded-md border overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b bg-muted/50 text-muted-foreground">
                <th className="px-3 py-2 w-10"></th>
                <SortTh label="Card" sortKey="card" current={sortKey} dir={sortDir} onSort={handleSort} className="text-left" />
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
                  <td colSpan={7} className="px-4 py-8 text-center text-muted-foreground">
                    No cards match the current filters.
                  </td>
                </tr>
              ) : (
                filtered.map(c => {
                  const owned = addedIds.has(c.identifier)
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
                          src={`/api/images/cards/${c.identifier.toLowerCase()}.jpg`}
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
                        <div className="text-xs text-muted-foreground">{c.identifier}</div>
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
