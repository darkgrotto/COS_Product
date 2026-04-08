import { useEffect, useState, useCallback } from 'react'
import { ChevronLeft, ChevronUp, ChevronDown, ChevronsUpDown, Plus, Search, Star, ExternalLink } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Badge } from '@/components/ui/badge'
import {
  Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle,
} from '@/components/ui/dialog'
import {
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue,
} from '@/components/ui/select'

// ---- Types ------------------------------------------------------------------

interface BrowseSet {
  code: string
  name: string
  totalCards: number
  releaseDate: string | null
}

interface BrowseCard {
  identifier: string
  name: string
  color: string | null
  cardType: string | null
  currentMarketValue: number | null
  isReserved: boolean
}

interface CardDetail {
  identifier: string
  setCode: string
  name: string
  color: string | null
  cardType: string | null
  oracleRulingUrl: string | null
  flavorText: string | null
  currentMarketValue: number | null
  updatedAt: string
  isReserved: boolean
}

interface Treatment { key: string; displayName: string; sortOrder: number }

// ---- Constants --------------------------------------------------------------

const CONDITIONS = ['NM', 'LP', 'MP', 'HP', 'DMG'] as const
const CONDITION_LABELS: Record<string, string> = {
  NM: 'Near Mint', LP: 'Lightly Played', MP: 'Moderately Played',
  HP: 'Heavily Played', DMG: 'Damaged',
}

const COLORS = [
  { key: 'W', label: 'W', title: 'White' },
  { key: 'U', label: 'U', title: 'Blue' },
  { key: 'B', label: 'B', title: 'Black' },
  { key: 'R', label: 'R', title: 'Red' },
  { key: 'G', label: 'G', title: 'Green' },
  { key: 'C', label: 'C', title: 'Colorless' },
]

const CARD_TYPES = [
  'Creature', 'Instant', 'Sorcery', 'Enchantment',
  'Artifact', 'Land', 'Planeswalker', 'Battle',
]

function today() {
  return new Date().toISOString().slice(0, 10)
}

function fmt(v: number | null | undefined) {
  if (v == null) return '-'
  return `$${v.toFixed(2)}`
}

// Regular first, Foil second, then alphabetical.
function sortTreatments(ts: Treatment[]): Treatment[] {
  return [...ts].sort((a, b) => {
    if (a.key === 'regular') return -1
    if (b.key === 'regular') return 1
    if (a.key === 'foil') return -1
    if (b.key === 'foil') return 1
    return a.displayName.localeCompare(b.displayName)
  })
}

// ---- Filter chip ------------------------------------------------------------

function ToggleChip({
  active, onClick, children,
}: {
  active: boolean
  onClick: () => void
  children: React.ReactNode
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={`px-2.5 py-1 rounded-md text-xs font-medium border transition-colors ${
        active
          ? 'bg-primary text-primary-foreground border-primary'
          : 'bg-background text-muted-foreground border-border hover:bg-accent'
      }`}
    >
      {children}
    </button>
  )
}

// ---- Sort header ------------------------------------------------------------

type SortDir = 'asc' | 'desc'

function SortTh({
  label, sortKey, current, dir, onSort, className,
}: {
  label: string
  sortKey: string
  current: string
  dir: SortDir
  onSort: (key: string) => void
  className?: string
}) {
  const active = current === sortKey
  return (
    <th
      className={`px-3 py-2 cursor-pointer select-none hover:bg-muted/70 transition-colors ${className ?? ''}`}
      onClick={() => onSort(sortKey)}
    >
      <span className="inline-flex items-center gap-1">
        {label}
        {active
          ? dir === 'asc' ? <ChevronUp className="h-3 w-3" /> : <ChevronDown className="h-3 w-3" />
          : <ChevronsUpDown className="h-3 w-3 opacity-30" />}
      </span>
    </th>
  )
}

// ---- Card detail dialog -----------------------------------------------------

function CardDetailDialog({
  identifier,
  onClose,
  onAdd,
}: {
  identifier: string
  onClose: () => void
  onAdd: () => void
}) {
  const [card, setCard] = useState<CardDetail | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    fetch(`/api/cards/${identifier.toLowerCase()}`)
      .then(r => r.ok ? r.json() : null)
      .then(d => { setCard(d); setLoading(false) })
      .catch(() => setLoading(false))
  }, [identifier])

  return (
    <Dialog open onOpenChange={v => !v && onClose()}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            {loading ? 'Loading...' : card?.name ?? identifier}
            {card?.isReserved && (
              <span title="Reserved List" className="inline-flex">
                <Star className="h-4 w-4 text-amber-500 fill-amber-500" />
              </span>
            )}
          </DialogTitle>
        </DialogHeader>

        {loading ? (
          <p className="text-sm text-muted-foreground py-4">Loading card details...</p>
        ) : !card ? (
          <p className="text-sm text-muted-foreground py-4">Card details not available.</p>
        ) : (
          <div className="space-y-4">
            <div className="flex gap-4">
              <img
                src={`/api/images/cards/${card.identifier.toLowerCase()}.jpg`}
                alt={card.name}
                className="h-32 w-24 rounded object-cover bg-muted shrink-0"
                onError={e => { (e.target as HTMLImageElement).style.display = 'none' }}
              />
              <div className="space-y-1.5 text-sm min-w-0">
                <div className="flex gap-4">
                  <div>
                    <p className="text-xs text-muted-foreground">Identifier</p>
                    <p className="font-mono font-medium">{card.identifier}</p>
                  </div>
                  <div>
                    <p className="text-xs text-muted-foreground">Set</p>
                    <p className="font-mono font-medium">{card.setCode.toUpperCase()}</p>
                  </div>
                </div>
                {card.color && (
                  <div>
                    <p className="text-xs text-muted-foreground">Color</p>
                    <p className="font-mono">{card.color}</p>
                  </div>
                )}
                {card.cardType && (
                  <div>
                    <p className="text-xs text-muted-foreground">Type</p>
                    <p className="text-xs leading-snug">{card.cardType}</p>
                  </div>
                )}
                <div>
                  <p className="text-xs text-muted-foreground">Market Value</p>
                  <p className="font-medium">{fmt(card.currentMarketValue)}</p>
                </div>
              </div>
            </div>

            {card.flavorText && (
              <div className="border-t pt-3">
                <p className="text-xs text-muted-foreground mb-1">Flavor Text</p>
                <p className="text-sm italic text-muted-foreground leading-relaxed">{card.flavorText}</p>
              </div>
            )}

            {card.oracleRulingUrl && (
              <div className={card.flavorText ? '' : 'border-t pt-3'}>
                <a
                  href={card.oracleRulingUrl}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="inline-flex items-center gap-1 text-xs text-primary hover:underline"
                >
                  Oracle Rulings <ExternalLink className="h-3 w-3" />
                </a>
              </div>
            )}
          </div>
        )}

        <DialogFooter>
          <Button variant="outline" onClick={onClose}>Close</Button>
          <Button onClick={() => { onClose(); onAdd() }}>
            <Plus className="h-4 w-4 mr-1" /> Add to Collection
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}

// ---- Quick-add dialog -------------------------------------------------------

interface AddForm {
  treatment: string
  quantity: number
  condition: string
  autographed: boolean
  acquisitionDate: string
  acquisitionPrice: string
  notes: string
}

function QuickAddDialog({
  card,
  treatments,
  onClose,
  onAdded,
}: {
  card: BrowseCard
  treatments: Treatment[]
  onClose: () => void
  onAdded: () => void
}) {
  const sorted = sortTreatments(treatments)
  const defaultTreatment = sorted[0]?.key ?? 'regular'
  const [form, setForm] = useState<AddForm>({
    treatment: defaultTreatment,
    quantity: 1,
    condition: 'NM',
    autographed: false,
    acquisitionDate: today(),
    acquisitionPrice: card.currentMarketValue != null
      ? card.currentMarketValue.toFixed(2)
      : '',
    notes: '',
  })
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')

  async function handleSave() {
    const price = parseFloat(form.acquisitionPrice)
    if (isNaN(price) || price < 0) { setError('Enter a valid acquisition price.'); return }
    setSaving(true)
    setError('')
    try {
      const res = await fetch('/api/collection', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          cardIdentifier: card.identifier.toLowerCase(),
          treatment: form.treatment,
          quantity: form.quantity,
          condition: form.condition,
          autographed: form.autographed,
          acquisitionDate: form.acquisitionDate,
          acquisitionPrice: price,
          notes: form.notes || null,
        }),
      })
      if (!res.ok) {
        const data = await res.json().catch(() => ({}))
        throw new Error((data as { error?: string }).error ?? 'Failed to add.')
      }
      onAdded()
      onClose()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to add.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <Dialog open onOpenChange={v => !v && onClose()}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Add to Collection</DialogTitle>
        </DialogHeader>
        <div className="space-y-4">
          <div>
            <p className="font-medium">{card.name}</p>
            <p className="text-xs text-muted-foreground">{card.identifier}</p>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <Label>Treatment</Label>
              <Select value={form.treatment} onValueChange={v => setForm(f => ({ ...f, treatment: v }))}>
                <SelectTrigger><SelectValue /></SelectTrigger>
                <SelectContent>
                  {sorted.map(t => (
                    <SelectItem key={t.key} value={t.key}>{t.displayName}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div>
              <Label>Condition</Label>
              <Select value={form.condition} onValueChange={v => setForm(f => ({ ...f, condition: v }))}>
                <SelectTrigger><SelectValue /></SelectTrigger>
                <SelectContent>
                  {CONDITIONS.map(c => (
                    <SelectItem key={c} value={c}>{c} - {CONDITION_LABELS[c]}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <Label>Quantity</Label>
              <Input
                type="number" min={1}
                value={form.quantity}
                onChange={e => setForm(f => ({ ...f, quantity: parseInt(e.target.value) || 1 }))}
              />
            </div>
            <div className="flex items-end pb-1">
              <div className="flex items-center gap-2">
                <input
                  type="checkbox"
                  id="browse-aut"
                  checked={form.autographed}
                  onChange={e => setForm(f => ({ ...f, autographed: e.target.checked }))}
                  className="h-4 w-4 rounded border-input"
                />
                <label htmlFor="browse-aut" className="text-sm">Autographed</label>
              </div>
            </div>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <Label>Acquisition Date</Label>
              <Input
                type="date" value={form.acquisitionDate}
                onChange={e => setForm(f => ({ ...f, acquisitionDate: e.target.value }))}
              />
            </div>
            <div>
              <Label>Acquisition Price</Label>
              <Input
                type="number" min={0} step="0.01" placeholder="0.00"
                value={form.acquisitionPrice}
                onChange={e => setForm(f => ({ ...f, acquisitionPrice: e.target.value }))}
              />
            </div>
          </div>

          <div>
            <Label>Notes <span className="text-xs text-muted-foreground">(optional)</span></Label>
            <Input
              value={form.notes}
              onChange={e => setForm(f => ({ ...f, notes: e.target.value }))}
              placeholder="Any notes..."
            />
          </div>

          {error && <p className="text-sm text-destructive">{error}</p>}
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={onClose} disabled={saving}>Cancel</Button>
          <Button onClick={handleSave} disabled={saving}>
            {saving ? 'Adding...' : 'Add to Collection'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
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

  function handleSort(key: string) {
    if (sortKey === key) setSortDir(d => d === 'asc' ? 'desc' : 'asc')
    else { setSortKey(key); setSortDir('asc') }
  }

  const filtered = (search.trim()
    ? sets.filter(s =>
        s.code.toLowerCase().includes(search.toLowerCase()) ||
        s.name.toLowerCase().includes(search.toLowerCase())
      )
    : sets
  ).slice().sort((a, b) => {
    let cmp = 0
    if (sortKey === 'code') cmp = a.code.localeCompare(b.code)
    else if (sortKey === 'name') cmp = a.name.localeCompare(b.name)
    else if (sortKey === 'totalCards') cmp = (a.totalCards ?? 0) - (b.totalCards ?? 0)
    else if (sortKey === 'releaseDate') cmp = (a.releaseDate ?? '').localeCompare(b.releaseDate ?? '')
    return sortDir === 'asc' ? cmp : -cmp
  })

  return (
    <div className="space-y-3">
      <div className="relative">
        <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground pointer-events-none" />
        <Input
          placeholder="Search sets..."
          value={search}
          onChange={e => setSearch(e.target.value)}
          className="pl-8"
        />
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
                  {search ? 'No sets match your search.' : 'No sets available.'}
                </td>
              </tr>
            ) : (
              filtered.map(s => (
                <tr
                  key={s.code}
                  className="border-b last:border-0 hover:bg-muted/30 cursor-pointer"
                  onClick={() => onSelectSet(s)}
                >
                  <td className="px-4 py-2.5 font-mono text-xs font-medium">{s.code}</td>
                  <td className="px-4 py-2.5 font-medium">{s.name}</td>
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
      if (rlFilter && !c.isReserved) return false
      return true
    })
    .slice()
    .sort((a, b) => {
      let cmp = 0
      if (sortKey === 'card') cmp = a.name.localeCompare(b.name) || a.identifier.localeCompare(b.identifier)
      else if (sortKey === 'color') cmp = (a.color ?? '').localeCompare(b.color ?? '')
      else if (sortKey === 'type') cmp = (a.cardType ?? '').localeCompare(b.cardType ?? '')
      else if (sortKey === 'market') cmp = (a.currentMarketValue ?? -1) - (b.currentMarketValue ?? -1)
      return sortDir === 'asc' ? cmp : -cmp
    })

  const setColors = new Set(cards.flatMap(c => c.color ? c.color.split('') : []))
  const visibleColors = COLORS.filter(col => setColors.has(col.key))

  const setTypes = new Set(
    cards.flatMap(c => {
      if (!c.cardType) return []
      return CARD_TYPES.filter(t => c.cardType!.includes(t))
    })
  )
  const visibleTypes = CARD_TYPES.filter(t => setTypes.has(t))
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
        <span className="text-sm font-medium">{set.name}</span>
        <span className="text-xs text-muted-foreground ml-1">({set.code})</span>
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
                <SortTh label="Market" sortKey="market" current={sortKey} dir={sortDir} onSort={handleSort} className="text-right" />
                <th className="px-3 py-2 text-right"></th>
              </tr>
            </thead>
            <tbody>
              {filtered.length === 0 ? (
                <tr>
                  <td colSpan={6} className="px-4 py-8 text-center text-muted-foreground">
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
                      <td className="px-3 py-2 text-muted-foreground text-xs max-w-40 truncate">
                        {c.cardType || '-'}
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
