import { useState, useEffect } from 'react'
import { ArrowLeft, ChevronUp, ChevronDown, ChevronsUpDown, Search } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import { SetSymbol } from '@/components/ui/SetSymbol'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { cn } from '@/lib/utils'

// ---- Types ----

interface SetSummary {
  code: string        // uppercase from API
  name: string
  totalCards: number
  setType: string | null
  releaseDate: string | null
}

interface CardSummary {
  identifier: string  // uppercase from API
  name: string
  manaCost: string | null
  color: string | null  // comma-joined e.g. "W,U"
  cardType: string | null
  rarity: string | null
  isReserved: boolean
}

interface CardDetailDto {
  identifier: string
  setCode: string    // lowercase from API
  name: string
  manaCost: string | null
  cmc: number | null
  color: string | null
  colorIdentity: string | null
  keywords: string | null
  cardType: string | null
  rarity: string | null
  oracleText: string | null
  layout: string | null
  oracleRulingUrl: string | null
  flavorText: string | null
  currentMarketValue: number | null
  updatedAt: string
  isReserved: boolean
  validTreatments: string[]
}

interface ScryfallRuling {
  source: string
  published_at: string
  comment: string
}

// ---- Helpers ----

const COLOR_LABELS: Record<string, string> = {
  W: 'White', U: 'Blue', B: 'Black', R: 'Red', G: 'Green',
}
const RARITIES = ['common', 'uncommon', 'rare', 'mythic']
const STANDARD_SET_TYPES = ['core', 'expansion', 'masters', 'funny', 'planechase', 'draft_innovation']

function deriveCardType(typeLine: string | null): string {
  if (!typeLine) return 'Other'
  const primary = typeLine.split('\u2014')[0].trim()
  for (const t of ['Creature', 'Instant', 'Sorcery', 'Enchantment', 'Artifact', 'Land', 'Planeswalker', 'Battle']) {
    if (primary.includes(t)) return t
  }
  return 'Other'
}

function splitColors(color: string | null): string[] {
  if (!color) return []
  return color.split(',').map(c => c.trim()).filter(Boolean)
}

function fmtSetType(t: string) {
  return t.replace(/_/g, ' ').replace(/\b\w/g, c => c.toUpperCase())
}

function fmtTreatment(t: string) {
  return t.replace(/-/g, ' ').replace(/\b\w/g, c => c.toUpperCase())
}

function capitalise(s: string) {
  return s.charAt(0).toUpperCase() + s.slice(1)
}

function fmtDate(iso: string) {
  return new Date(iso).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })
}

function fmtRulingDate(date: string) {
  const [year, month, day] = date.split('-').map(Number)
  return new Date(year, month - 1, day).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })
}

type SortDir = 'asc' | 'desc'

function SortIcon({ active, dir }: { active: boolean; dir: SortDir }) {
  if (!active) return <ChevronsUpDown className="h-3 w-3 ml-1 inline opacity-40" />
  return dir === 'asc'
    ? <ChevronUp className="h-3 w-3 ml-1 inline" />
    : <ChevronDown className="h-3 w-3 ml-1 inline" />
}

function RarityBadge({ rarity }: { rarity: string | null }) {
  if (!rarity) return null
  const cls = ({
    common: 'border-transparent bg-zinc-500 text-white hover:bg-zinc-500',
    uncommon: 'border-transparent bg-sky-600 text-white hover:bg-sky-600',
    rare: 'border-transparent bg-amber-500 text-white hover:bg-amber-500',
    mythic: 'border-transparent bg-orange-500 text-white hover:bg-orange-500',
  } as Record<string, string>)[rarity] ?? 'border-transparent bg-muted text-muted-foreground'
  return <Badge className={cls}>{capitalise(rarity)}</Badge>
}

function FilterChip({ label, active, onClick }: { label: string; active: boolean; onClick: () => void }) {
  return (
    <button
      onClick={onClick}
      className={cn(
        'px-2 py-0.5 text-xs rounded border transition-colors whitespace-nowrap',
        active
          ? 'bg-primary text-primary-foreground border-primary'
          : 'bg-background text-foreground border-border hover:bg-accent'
      )}
    >
      {label}
    </button>
  )
}

// ---- Rulings Panel ----

function RulingsPanel({ rulingUrl }: { rulingUrl: string }) {
  const [rulings, setRulings] = useState<ScryfallRuling[] | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    setRulings(null)
    setError(null)
    fetch(rulingUrl, { headers: { Accept: 'application/json' } })
      .then(r => {
        if (!r.ok) throw new Error(`${r.status} ${r.statusText}`)
        return r.json() as Promise<{ data: ScryfallRuling[] }>
      })
      .then(body => setRulings(body.data))
      .catch((e: Error) => setError(e.message))
  }, [rulingUrl])

  return (
    <div className="mt-5">
      <div className="text-xs font-semibold text-muted-foreground uppercase tracking-wide mb-3">
        Rulings
      </div>
      {error && <p className="text-sm text-destructive">Failed to load rulings: {error}</p>}
      {!error && !rulings && <p className="text-sm text-muted-foreground">Loading rulings...</p>}
      {rulings && rulings.length === 0 && (
        <p className="text-sm text-muted-foreground">No rulings on record.</p>
      )}
      {rulings && rulings.length > 0 && (
        <div className="flex flex-col gap-3">
          {rulings.map((r, i) => (
            <div
              key={i}
              className={cn(
                'px-3.5 py-2.5 rounded-md border-l-[3px] bg-muted/30',
                r.source === 'wotc' ? 'border-l-primary' : 'border-l-muted-foreground'
              )}
            >
              <div className="flex gap-3 text-xs text-muted-foreground mb-1.5">
                <span>{fmtRulingDate(r.published_at)}</span>
                <span>{r.source === 'wotc' ? 'Wizards of the Coast' : 'Scryfall'}</span>
              </div>
              <div className="text-sm leading-relaxed">{r.comment}</div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}

// ---- Card Detail View ----

function CardDetailView({
  identifier,
  setCode,
  setName,
  onBack,
}: {
  identifier: string
  setCode: string  // uppercase
  setName: string
  onBack: () => void
}) {
  const [detail, setDetail] = useState<CardDetailDto | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    setDetail(null)
    setError(null)
    fetch(`/api/cards/${identifier.toLowerCase()}`, { credentials: 'include' })
      .then(res => {
        if (!res.ok) throw new Error(`HTTP ${res.status}`)
        return res.json() as Promise<CardDetailDto>
      })
      .then(setDetail)
      .catch((e: Error) => setError(e.message))
  }, [identifier])

  const collectorNumber = identifier.slice(setCode.length)

  return (
    <div className="space-y-3">
      <Button variant="ghost" size="sm" onClick={onBack} className="gap-1">
        <ArrowLeft className="h-4 w-4" />
        Back to {setName}
      </Button>

      {error && <p className="text-sm text-destructive">Error loading card: {error}</p>}
      {!error && !detail && <p className="text-sm text-muted-foreground">Loading...</p>}

      {detail && (
        <div className="border rounded-md p-6 space-y-5">
          {/* Name and badges */}
          <div>
            <div className="flex flex-wrap items-center gap-2 mb-1">
              <span className="text-2xl font-bold">{detail.name}</span>
              {detail.isReserved && (
                <Badge variant="destructive" title="On the Magic: The Gathering Reserved List">
                  Reserved List
                </Badge>
              )}
            </div>
            <div className="text-sm text-muted-foreground">{detail.cardType}</div>
          </div>

          {/* Meta grid */}
          <div className="grid grid-cols-[auto_1fr] gap-x-6 gap-y-1.5 text-sm max-w-xl">
            {detail.manaCost && (
              <>
                <span className="text-muted-foreground font-medium">Mana Cost</span>
                <span className="font-mono">{detail.manaCost}</span>
              </>
            )}
            {detail.cmc != null && (
              <>
                <span className="text-muted-foreground font-medium">CMC</span>
                <span>{String(detail.cmc)}</span>
              </>
            )}
            <span className="text-muted-foreground font-medium">Rarity</span>
            <span><RarityBadge rarity={detail.rarity} /></span>
            {detail.layout && (
              <>
                <span className="text-muted-foreground font-medium">Layout</span>
                <span>{capitalise(detail.layout)}</span>
              </>
            )}
            <span className="text-muted-foreground font-medium">Collector #</span>
            <span className="font-mono">{collectorNumber}</span>
            <span className="text-muted-foreground font-medium">Card ID</span>
            <span className="font-mono">{detail.identifier}</span>
            {detail.color && splitColors(detail.color).length > 0 && (
              <>
                <span className="text-muted-foreground font-medium">Colors</span>
                <span>
                  {splitColors(detail.color).map(c => COLOR_LABELS[c] ?? c).join(', ')}
                </span>
              </>
            )}
            {detail.colorIdentity && splitColors(detail.colorIdentity).length > 0 && (
              <>
                <span className="text-muted-foreground font-medium">Color Identity</span>
                <span>
                  {splitColors(detail.colorIdentity).map(c => COLOR_LABELS[c] ?? c).join(', ')}
                </span>
              </>
            )}
            {detail.keywords && (
              <>
                <span className="text-muted-foreground font-medium">Keywords</span>
                <span>
                  {detail.keywords.split(',').map(k => k.trim()).filter(Boolean).join(', ')}
                </span>
              </>
            )}
            {detail.validTreatments.length > 0 && (
              <>
                <span className="text-muted-foreground font-medium">Treatments</span>
                <span className="flex flex-wrap gap-1.5">
                  {detail.validTreatments.map(t => (
                    <Badge key={t} variant="secondary">{fmtTreatment(t)}</Badge>
                  ))}
                </span>
              </>
            )}
            {detail.currentMarketValue != null && (
              <>
                <span className="text-muted-foreground font-medium">Market Value</span>
                <span>${detail.currentMarketValue.toFixed(2)}</span>
              </>
            )}
            <span className="text-muted-foreground font-medium">Last Updated</span>
            <span className="text-muted-foreground">{fmtDate(detail.updatedAt)}</span>
          </div>

          {/* Oracle text */}
          {detail.oracleText && (
            <div className="bg-muted/30 rounded-md px-4 py-3 text-sm leading-relaxed whitespace-pre-wrap">
              {detail.oracleText}
            </div>
          )}

          {/* Flavor text */}
          {detail.flavorText && (
            <div className="text-sm text-muted-foreground italic border-l-2 border-muted pl-3">
              {detail.flavorText}
            </div>
          )}

          {/* Rulings */}
          {detail.oracleRulingUrl && <RulingsPanel rulingUrl={detail.oracleRulingUrl} />}
        </div>
      )}
    </div>
  )
}

// ---- Cards Table ----

type CardSortField = 'collectorNumber' | 'identifier' | 'name' | 'cardType' | 'rarity'

function CardsTable({
  set,
  onBack,
  onSelectCard,
}: {
  set: SetSummary
  onBack: () => void
  onSelectCard: (c: CardSummary) => void
}) {
  const [cards, setCards] = useState<CardSummary[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [filter, setFilter] = useState('')
  const [colorFilter, setColorFilter] = useState<string | null>(null)
  const [typeFilter, setTypeFilter] = useState<string | null>(null)
  const [rarityFilter, setRarityFilter] = useState<string | null>(null)
  const [sortField, setSortField] = useState<CardSortField>('collectorNumber')
  const [sortDir, setSortDir] = useState<SortDir>('asc')

  useEffect(() => {
    fetch(`/api/sets/${set.code.toLowerCase()}/cards`, { credentials: 'include' })
      .then(res => {
        if (!res.ok) throw new Error(`HTTP ${res.status}`)
        return res.json() as Promise<CardSummary[]>
      })
      .then(setCards)
      .catch((e: Error) => setError(e.message))
  }, [set.code])

  function handleSort(field: CardSortField) {
    if (sortField === field) {
      setSortDir(d => d === 'asc' ? 'desc' : 'asc')
    } else {
      setSortField(field)
      setSortDir('asc')
    }
  }

  const setCodeLen = set.code.length

  // Derive available filter options from loaded data
  const availableColors = cards
    ? (() => {
        const seen = new Set<string>()
        cards.forEach(c => {
          const cols = splitColors(c.color)
          if (cols.length === 0) seen.add('C')
          else cols.forEach(col => seen.add(col))
        })
        return [...Object.keys(COLOR_LABELS), 'C'].filter(col => seen.has(col))
      })()
    : []

  const availableTypes = cards
    ? Array.from(new Set(cards.map(c => deriveCardType(c.cardType)))).sort()
    : []

  const availableRarities = RARITIES.filter(r => cards?.some(c => c.rarity === r))

  // Apply filters
  const q = filter.toLowerCase()
  let visible = cards ?? []
  if (colorFilter) {
    visible = visible.filter(c => {
      const cols = splitColors(c.color)
      return colorFilter === 'C' ? cols.length === 0 : cols.includes(colorFilter)
    })
  }
  if (typeFilter) visible = visible.filter(c => deriveCardType(c.cardType) === typeFilter)
  if (rarityFilter) visible = visible.filter(c => c.rarity === rarityFilter)
  if (filter) visible = visible.filter(c =>
    c.identifier.toLowerCase().includes(q) ||
    c.name.toLowerCase().includes(q) ||
    (c.cardType?.toLowerCase().includes(q) ?? false)
  )

  // Sort
  visible = [...visible].sort((a, b) => {
    let cmp = 0
    if (sortField === 'collectorNumber') {
      const na = parseInt(a.identifier.slice(setCodeLen), 10)
      const nb = parseInt(b.identifier.slice(setCodeLen), 10)
      cmp = isNaN(na) || isNaN(nb)
        ? a.identifier.slice(setCodeLen).localeCompare(b.identifier.slice(setCodeLen))
        : na - nb
    } else if (sortField === 'identifier') {
      cmp = a.identifier.localeCompare(b.identifier)
    } else if (sortField === 'name') {
      cmp = a.name.localeCompare(b.name)
    } else if (sortField === 'cardType') {
      cmp = (a.cardType ?? '').localeCompare(b.cardType ?? '')
    } else if (sortField === 'rarity') {
      cmp = RARITIES.indexOf(a.rarity ?? '') - RARITIES.indexOf(b.rarity ?? '')
    }
    return sortDir === 'asc' ? cmp : -cmp
  })

  return (
    <div className="space-y-3">
      {/* Header */}
      <div className="flex items-center gap-3">
        <Button variant="ghost" size="sm" onClick={onBack} className="gap-1 shrink-0">
          <ArrowLeft className="h-4 w-4" />
          Back to Sets
        </Button>
        <span className="font-medium">
          <span className="font-mono">{set.code}</span>
          {' - '}
          {set.name}
        </span>
      </div>

      {/* Filters */}
      <div className="flex flex-wrap items-center gap-2">
        {availableColors.length > 0 && (
          <div className="flex gap-1 flex-wrap">
            {availableColors.map(col => (
              <FilterChip
                key={col}
                label={col}
                active={colorFilter === col}
                onClick={() => setColorFilter(colorFilter === col ? null : col)}
              />
            ))}
          </div>
        )}
        {availableTypes.length > 1 && (
          <div className="flex gap-1 flex-wrap">
            {availableTypes.map(t => (
              <FilterChip
                key={t}
                label={t}
                active={typeFilter === t}
                onClick={() => setTypeFilter(typeFilter === t ? null : t)}
              />
            ))}
          </div>
        )}
        {availableRarities.length > 1 && (
          <div className="flex gap-1 flex-wrap">
            {availableRarities.map(r => (
              <FilterChip
                key={r}
                label={capitalise(r)}
                active={rarityFilter === r}
                onClick={() => setRarityFilter(rarityFilter === r ? null : r)}
              />
            ))}
          </div>
        )}
        <div className="ml-auto flex items-center gap-2">
          {cards && (
            <span className="text-xs text-muted-foreground whitespace-nowrap">
              {visible.length}{visible.length !== cards.length ? `/${cards.length}` : ''}{' '}
              card{cards.length !== 1 ? 's' : ''}
            </span>
          )}
          <div className="relative">
            <Search className="absolute left-2 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-muted-foreground" />
            <Input
              className="pl-7 h-7 text-xs w-44"
              placeholder="Filter..."
              value={filter}
              onChange={e => setFilter(e.target.value)}
            />
          </div>
        </div>
      </div>

      {/* Table */}
      {!cards && !error && <p className="text-sm text-muted-foreground">Loading cards...</p>}
      {error && <p className="text-sm text-destructive">Error: {error}</p>}
      {cards && (
        <div className="border rounded-md overflow-auto">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead
                  className="cursor-pointer select-none text-right w-12 whitespace-nowrap"
                  onClick={() => handleSort('collectorNumber')}
                >
                  # <SortIcon active={sortField === 'collectorNumber'} dir={sortDir} />
                </TableHead>
                <TableHead
                  className="cursor-pointer select-none whitespace-nowrap"
                  onClick={() => handleSort('identifier')}
                >
                  ID <SortIcon active={sortField === 'identifier'} dir={sortDir} />
                </TableHead>
                <TableHead
                  className="cursor-pointer select-none"
                  onClick={() => handleSort('name')}
                >
                  Name <SortIcon active={sortField === 'name'} dir={sortDir} />
                </TableHead>
                <TableHead
                  className="cursor-pointer select-none"
                  onClick={() => handleSort('cardType')}
                >
                  Type <SortIcon active={sortField === 'cardType'} dir={sortDir} />
                </TableHead>
                <TableHead>Mana Cost</TableHead>
                <TableHead
                  className="cursor-pointer select-none whitespace-nowrap"
                  onClick={() => handleSort('rarity')}
                >
                  Rarity <SortIcon active={sortField === 'rarity'} dir={sortDir} />
                </TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {visible.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={6} className="text-center text-muted-foreground">
                    No cards found.
                  </TableCell>
                </TableRow>
              ) : (
                visible.map(card => (
                  <TableRow
                    key={card.identifier}
                    className="cursor-pointer hover:bg-accent"
                    onClick={() => onSelectCard(card)}
                  >
                    <TableCell className="text-right font-mono text-xs text-muted-foreground tabular-nums">
                      {card.identifier.slice(setCodeLen)}
                    </TableCell>
                    <TableCell className="font-mono text-xs">{card.identifier}</TableCell>
                    <TableCell className="font-medium">
                      {card.name}
                      {card.isReserved && (
                        <Badge
                          variant="destructive"
                          className="ml-2 text-[10px] px-1 py-0 align-middle"
                          title="Reserved List"
                        >
                          R
                        </Badge>
                      )}
                    </TableCell>
                    <TableCell className="text-muted-foreground text-sm max-w-[200px] truncate">
                      {card.cardType ?? '-'}
                    </TableCell>
                    <TableCell className="font-mono text-xs">
                      {card.manaCost ?? <span className="text-muted-foreground">-</span>}
                    </TableCell>
                    <TableCell>
                      <RarityBadge rarity={card.rarity} />
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </div>
      )}
    </div>
  )
}

// ---- Sets Table ----

type SetSortField = 'code' | 'name' | 'setType' | 'releaseDate' | 'totalCards'

function SetsTable({ onSelectSet }: { onSelectSet: (s: SetSummary) => void }) {
  const [sets, setSets] = useState<SetSummary[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [filter, setFilter] = useState('')
  const [typeFilter, setTypeFilter] = useState<string | null>(null)
  const [standardOnly, setStandardOnly] = useState(false)
  const [sortField, setSortField] = useState<SetSortField>('name')
  const [sortDir, setSortDir] = useState<SortDir>('asc')

  useEffect(() => {
    fetch('/api/sets', { credentials: 'include' })
      .then(res => {
        if (!res.ok) throw new Error(`HTTP ${res.status}`)
        return res.json() as Promise<SetSummary[]>
      })
      .then(setSets)
      .catch((e: Error) => setError(e.message))
  }, [])

  function handleSort(field: SetSortField) {
    if (sortField === field) {
      setSortDir(d => d === 'asc' ? 'desc' : 'asc')
    } else {
      setSortField(field)
      setSortDir('asc')
    }
  }

  const availableTypes = sets
    ? Array.from(new Set(sets.map(s => s.setType).filter((t): t is string => t != null))).sort()
    : []

  // Apply filters
  const q = filter.toLowerCase()
  let visible = sets ?? []
  if (standardOnly) visible = visible.filter(s => s.setType != null && STANDARD_SET_TYPES.includes(s.setType))
  if (typeFilter) visible = visible.filter(s => s.setType === typeFilter)
  if (filter) visible = visible.filter(s =>
    s.code.toLowerCase().includes(q) || s.name.toLowerCase().includes(q)
  )

  // Sort
  visible = [...visible].sort((a, b) => {
    let cmp = 0
    if (sortField === 'code') cmp = a.code.localeCompare(b.code)
    else if (sortField === 'name') cmp = a.name.localeCompare(b.name)
    else if (sortField === 'setType') cmp = (a.setType ?? '').localeCompare(b.setType ?? '')
    else if (sortField === 'releaseDate') cmp = (a.releaseDate ?? '').localeCompare(b.releaseDate ?? '')
    else if (sortField === 'totalCards') cmp = a.totalCards - b.totalCards
    return sortDir === 'asc' ? cmp : -cmp
  })

  return (
    <div className="space-y-3">
      {/* Filters */}
      <div className="flex flex-wrap items-center gap-2">
        <FilterChip
          label="All"
          active={!standardOnly && !typeFilter}
          onClick={() => { setStandardOnly(false); setTypeFilter(null) }}
        />
        <FilterChip
          label="Standard Sets"
          active={standardOnly && !typeFilter}
          onClick={() => { setStandardOnly(prev => !prev); setTypeFilter(null) }}
        />
        {availableTypes.map(t => (
          <FilterChip
            key={t}
            label={fmtSetType(t)}
            active={typeFilter === t}
            onClick={() => { setTypeFilter(typeFilter === t ? null : t); setStandardOnly(false) }}
          />
        ))}
        <div className="ml-auto flex items-center gap-2">
          {sets && (
            <span className="text-xs text-muted-foreground whitespace-nowrap">
              {visible.length}{visible.length !== sets.length ? `/${sets.length}` : ''}{' '}
              set{sets.length !== 1 ? 's' : ''}
            </span>
          )}
          <div className="relative">
            <Search className="absolute left-2 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-muted-foreground" />
            <Input
              className="pl-7 h-7 text-xs w-44"
              placeholder="Filter..."
              value={filter}
              onChange={e => setFilter(e.target.value)}
            />
          </div>
        </div>
      </div>

      {/* Table */}
      {!sets && !error && <p className="text-sm text-muted-foreground">Loading sets...</p>}
      {error && <p className="text-sm text-destructive">Error: {error}</p>}
      {sets && (
        <div className="border rounded-md overflow-auto">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead className="w-9" />
                <TableHead
                  className="cursor-pointer select-none whitespace-nowrap"
                  onClick={() => handleSort('code')}
                >
                  Code <SortIcon active={sortField === 'code'} dir={sortDir} />
                </TableHead>
                <TableHead
                  className="cursor-pointer select-none"
                  onClick={() => handleSort('name')}
                >
                  Name <SortIcon active={sortField === 'name'} dir={sortDir} />
                </TableHead>
                <TableHead
                  className="cursor-pointer select-none whitespace-nowrap"
                  onClick={() => handleSort('setType')}
                >
                  Type <SortIcon active={sortField === 'setType'} dir={sortDir} />
                </TableHead>
                <TableHead
                  className="cursor-pointer select-none whitespace-nowrap"
                  onClick={() => handleSort('releaseDate')}
                >
                  Released <SortIcon active={sortField === 'releaseDate'} dir={sortDir} />
                </TableHead>
                <TableHead
                  className="cursor-pointer select-none text-right whitespace-nowrap"
                  onClick={() => handleSort('totalCards')}
                >
                  Cards <SortIcon active={sortField === 'totalCards'} dir={sortDir} />
                </TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {visible.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={6} className="text-center text-muted-foreground">
                    No sets found.
                  </TableCell>
                </TableRow>
              ) : (
                visible.map(set => (
                  <TableRow
                    key={set.code}
                    className="cursor-pointer hover:bg-accent"
                    onClick={() => onSelectSet(set)}
                  >
                    <TableCell className="text-center text-lg leading-none">
                      <SetSymbol setCode={set.code} />
                    </TableCell>
                    <TableCell className="font-mono font-medium">{set.code}</TableCell>
                    <TableCell className="font-medium">{set.name}</TableCell>
                    <TableCell className="text-muted-foreground text-sm">
                      {set.setType ? fmtSetType(set.setType) : '-'}
                    </TableCell>
                    <TableCell className="text-muted-foreground text-sm whitespace-nowrap">
                      {set.releaseDate ? new Date(set.releaseDate).getFullYear() : '-'}
                    </TableCell>
                    <TableCell className="text-right tabular-nums">{set.totalCards}</TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </div>
      )}
    </div>
  )
}

// ---- Main Export ----

export function AdminContentCards() {
  const [selectedSet, setSelectedSet] = useState<SetSummary | null>(null)
  const [selectedCard, setSelectedCard] = useState<CardSummary | null>(null)

  function handleSelectSet(s: SetSummary) {
    setSelectedSet(s)
    setSelectedCard(null)
  }

  function handleBackToSets() {
    setSelectedSet(null)
    setSelectedCard(null)
  }

  if (selectedSet && selectedCard) {
    return (
      <CardDetailView
        identifier={selectedCard.identifier}
        setCode={selectedSet.code}
        setName={`${selectedSet.code} - ${selectedSet.name}`}
        onBack={() => setSelectedCard(null)}
      />
    )
  }

  if (selectedSet) {
    return (
      <CardsTable
        set={selectedSet}
        onBack={handleBackToSets}
        onSelectCard={setSelectedCard}
      />
    )
  }

  return <SetsTable onSelectSet={handleSelectSet} />
}
