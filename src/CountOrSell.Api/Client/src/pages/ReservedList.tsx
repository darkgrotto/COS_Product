import { useEffect, useMemo, useState } from 'react'
import { Star, ChevronUp, ChevronDown, ChevronsUpDown } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'

// ---- Types ------------------------------------------------------------------

interface RLCard {
  identifier: string
  setCode: string
  name: string
  cardType: string | null
  color: string | null
  currentMarketValue: number | null
  ownedQuantity: number
}

type SortKey = 'name' | 'setCode' | 'cardType' | 'currentMarketValue' | 'ownedQuantity'
type SortDir = 'asc' | 'desc'

// ---- Helpers ----------------------------------------------------------------

function fmt(v: number | null | undefined) {
  if (v == null) return '-'
  return `$${v.toFixed(2)}`
}

function SortTh({
  label,
  sortKey,
  active,
  dir,
  onSort,
  className,
}: {
  label: string
  sortKey: SortKey
  active: SortKey
  dir: SortDir
  onSort: (k: SortKey) => void
  className?: string
}) {
  const isActive = active === sortKey
  const Icon = isActive ? (dir === 'asc' ? ChevronUp : ChevronDown) : ChevronsUpDown
  return (
    <th className={className}>
      <button
        className="flex items-center gap-1 text-muted-foreground hover:text-foreground transition-colors w-full"
        onClick={() => onSort(sortKey)}
      >
        {label}
        <Icon className="h-3 w-3 shrink-0" />
      </button>
    </th>
  )
}

// ---- Main page --------------------------------------------------------------

export function ReservedListPage() {
  const [cards, setCards] = useState<RLCard[]>([])
  const [loading, setLoading] = useState(true)
  const [sortKey, setSortKey] = useState<SortKey>('name')
  const [sortDir, setSortDir] = useState<SortDir>('asc')
  const [ownedOnly, setOwnedOnly] = useState(false)

  useEffect(() => {
    fetch('/api/cards/reserved-list')
      .then(r => r.ok ? r.json() : [])
      .then((data: RLCard[]) => setCards(data))
      .finally(() => setLoading(false))
  }, [])

  function handleSort(k: SortKey) {
    if (sortKey === k) setSortDir(d => d === 'asc' ? 'desc' : 'asc')
    else { setSortKey(k); setSortDir('asc') }
  }

  const filtered = useMemo(
    () => ownedOnly ? cards.filter(c => c.ownedQuantity > 0) : cards,
    [cards, ownedOnly]
  )

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

  const thCls = 'px-3 py-2 text-left font-medium'

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
        {!loading && cards.length > 0 && (
          <Button
            variant={ownedOnly ? 'default' : 'outline'}
            size="sm"
            onClick={() => setOwnedOnly(v => !v)}
          >
            {ownedOnly ? 'Showing owned only' : 'Show owned only'}
          </Button>
        )}
      </div>

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
          <p className="text-muted-foreground text-sm">No owned Reserved List cards.</p>
        </div>
      ) : (
        <div className="rounded-md border overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b bg-muted/50">
                <th className="px-3 py-2 w-10"></th>
                <SortTh label="Card" sortKey="name" active={sortKey} dir={sortDir} onSort={handleSort} className={thCls} />
                <SortTh label="Set" sortKey="setCode" active={sortKey} dir={sortDir} onSort={handleSort} className={thCls} />
                <SortTh label="Type" sortKey="cardType" active={sortKey} dir={sortDir} onSort={handleSort} className={thCls} />
                <SortTh label="Market" sortKey="currentMarketValue" active={sortKey} dir={sortDir} onSort={handleSort} className={`${thCls} text-right`} />
                <SortTh label="Owned" sortKey="ownedQuantity" active={sortKey} dir={sortDir} onSort={handleSort} className={`${thCls} text-center`} />
              </tr>
            </thead>
            <tbody>
              {sorted.map(c => (
                <tr key={c.identifier} className="border-b last:border-0 hover:bg-muted/20">
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
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
