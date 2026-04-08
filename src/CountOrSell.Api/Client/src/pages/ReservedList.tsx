import { useEffect, useMemo, useState } from 'react'
import { Star, Plus } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import {
  SortTh, SortDir, ToggleChip, CardDetailDialog, QuickAddDialog,
  Treatment, AddableCard, sortTreatments, fmt,
  COLORS, CARD_TYPES,
} from '@/components/cards/CardDialogs'

// ---- Types ------------------------------------------------------------------

interface RLCard extends AddableCard {
  setCode: string
  cardType: string | null
  color: string | null
  ownedQuantity: number
}

type SortKey = 'name' | 'setCode' | 'cardType' | 'currentMarketValue' | 'ownedQuantity'

// ---- Main page --------------------------------------------------------------

export function ReservedListPage() {
  const [cards, setCards] = useState<RLCard[]>([])
  const [treatments, setTreatments] = useState<Treatment[]>([])
  const [loading, setLoading] = useState(true)
  const [sortKey, setSortKey] = useState<SortKey>('name')
  const [sortDir, setSortDir] = useState<SortDir>('asc')
  const [ownedOnly, setOwnedOnly] = useState(false)
  const [colorFilter, setColorFilter] = useState('')
  const [typeFilter, setTypeFilter] = useState('')
  const [addCard, setAddCard] = useState<RLCard | null>(null)
  const [detailCard, setDetailCard] = useState<RLCard | null>(null)
  const [addedIds, setAddedIds] = useState<Set<string>>(new Set())

  useEffect(() => {
    Promise.all([
      fetch('/api/cards/reserved-list').then(r => r.ok ? r.json() : []),
      fetch('/api/treatments').then(r => r.ok ? r.json() : []),
    ]).then(([rl, t]) => {
      setCards(rl)
      setTreatments(sortTreatments(t))
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

  const filtered = useMemo(() => {
    let result = cards
    if (ownedOnly) result = result.filter(c => c.ownedQuantity > 0)
    if (colorFilter) result = result.filter(c => (c.color ?? '').includes(colorFilter))
    if (typeFilter) result = result.filter(c => (c.cardType ?? '').includes(typeFilter))
    return result
  }, [cards, ownedOnly, colorFilter, typeFilter])

  const sorted = useMemo(() => {
    return [...filtered].sort((a, b) => {
      let cmp = 0
      switch (sortKey) {
        case 'name':
          cmp = a.name.localeCompare(b.name)
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

  function handleAdded() {
    if (addCard) {
      setAddedIds(prev => new Set(prev).add(addCard.identifier))
      setCards(prev => prev.map(c =>
        c.identifier === addCard.identifier
          ? { ...c, ownedQuantity: c.ownedQuantity + 1 }
          : c
      ))
    }
  }

  const hasActiveFilter = ownedOnly || !!colorFilter || !!typeFilter

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

          <div className="flex gap-1.5 items-center">
            <span className="text-xs text-muted-foreground">Show:</span>
            <ToggleChip active={ownedOnly} onClick={() => setOwnedOnly(v => !v)}>
              <span className="inline-flex items-center gap-1">
                <Star className="h-3 w-3" /> Owned only
              </span>
            </ToggleChip>
            {hasActiveFilter && (
              <button
                type="button"
                className="text-xs text-muted-foreground hover:text-foreground underline ml-1"
                onClick={() => { setColorFilter(''); setTypeFilter(''); setOwnedOnly(false) }}
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
      ) : sorted.length === 0 ? (
        <div className="py-12 text-center">
          <p className="text-muted-foreground text-sm">No cards match the current filters.</p>
        </div>
      ) : (
        <div className="rounded-md border overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b bg-muted/50 text-muted-foreground">
                <th className="px-3 py-2 w-10"></th>
                <SortTh label="Card" sortKey="name" current={sortKey} dir={sortDir} onSort={handleSort} className="text-left" />
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
                        src={`/api/images/cards/${c.identifier.toLowerCase()}.jpg`}
                        alt=""
                        className="h-8 w-6 rounded object-cover bg-muted"
                        loading="lazy"
                        onError={ev => { (ev.target as HTMLImageElement).style.display = 'none' }}
                      />
                    </td>
                    <td className="px-3 py-2">
                      <div className="font-medium leading-tight">{c.name}</div>
                      <div className="text-xs text-muted-foreground font-mono">{c.identifier}</div>
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
