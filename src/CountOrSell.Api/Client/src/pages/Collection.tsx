import { useEffect, useState, useCallback, useRef } from 'react'
import { SetSymbol } from '@/components/ui/SetSymbol'
import { usePreferences } from '@/contexts/PreferencesContext'
import {
  Plus, Pencil, Trash2, ChevronUp, ChevronDown, X,
  ChevronLeft, LayoutList, LayoutGrid, Search, Upload, Download,
  CheckCircle, XCircle,
} from 'lucide-react'
import {
  CardDetailDialog, QuickAddDialog, SortTh, AddableCard,
} from '@/components/cards/CardDialogs'
import type { SortDir } from '@/components/cards/CardDialogs'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import {
  Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle,
} from '@/components/ui/dialog'
import {
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue,
} from '@/components/ui/select'
import { Badge } from '@/components/ui/badge'
import { ConfirmDialog } from '@/components/ConfirmDialog'

// ---- Types ------------------------------------------------------------------

interface CollectionEntry {
  id: string
  cardIdentifier: string
  cardName: string | null
  setCode: string | null
  marketValue: number | null
  treatmentKey: string
  quantity: number
  condition: string
  autographed: boolean
  acquisitionDate: string
  acquisitionPrice: number
  notes: string | null
}

interface SetCompletion {
  setCode: string
  setName: string
  ownedCount: number
  totalCards: number
  percentage: number
  totalValue: number | null
}

interface Treatment { key: string; displayName: string; sortOrder: number }
interface CardSet { code: string; name: string; totalCards: number }
interface CardSearchResult {
  identifier: string
  setCode: string
  name: string
  currentMarketValue: number | null
}

interface EntryForm {
  cardIdentifier: string
  cardName: string
  treatment: string
  quantity: number
  condition: string
  autographed: boolean
  acquisitionDate: string
  acquisitionPrice: string
  notes: string
}

interface Filters {
  setCode: string
  treatment: string
  condition: string
  autographed: string
  color: string
  cardType: string
  isReserved: boolean
}

// Regular first, Foil second, then alphabetical by display name.
function sortTreatments<T extends { key: string; displayName: string }>(ts: T[]): T[] {
  return [...ts].sort((a, b) => {
    if (a.key === 'regular') return -1
    if (b.key === 'regular') return 1
    if (a.key === 'foil') return -1
    if (b.key === 'foil') return 1
    return a.displayName.localeCompare(b.displayName)
  })
}

const CONDITIONS = ['NM', 'LP', 'MP', 'HP', 'DMG'] as const
const CONDITION_LABELS: Record<string, string> = {
  NM: 'Near Mint', LP: 'Lightly Played', MP: 'Moderately Played',
  HP: 'Heavily Played', DMG: 'Damaged',
}

const COLORS = [
  { key: 'W', title: 'White' },
  { key: 'U', title: 'Blue' },
  { key: 'B', title: 'Black' },
  { key: 'R', title: 'Red' },
  { key: 'G', title: 'Green' },
  { key: 'C', title: 'Colorless' },
]

const CARD_TYPES = [
  'Creature', 'Instant', 'Sorcery', 'Enchantment',
  'Artifact', 'Land', 'Planeswalker', 'Battle',
]

const BLANK_FILTERS: Filters = {
  setCode: '', treatment: '', condition: '', autographed: '', color: '', cardType: '', isReserved: false,
}

function today() { return new Date().toISOString().slice(0, 10) }

function fmt(v: number | null | undefined) {
  if (v == null) return '-'
  return `$${v.toFixed(2)}`
}

function plColor(pl: number | null | undefined) {
  if (pl == null) return 'text-muted-foreground'
  if (pl > 0) return 'text-green-600'
  if (pl < 0) return 'text-red-600'
  return 'text-muted-foreground'
}

// ---- Toggle chip (for color/type filters) -----------------------------------

function ToggleChip({
  active, onClick, children,
}: { active: boolean; onClick: () => void; children: React.ReactNode }) {
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

// ---- Card search combobox ---------------------------------------------------

function CardSearch({
  value, onSelect, setCode,
}: { value: string; onSelect: (card: CardSearchResult) => void; setCode?: string }) {
  const [query, setQuery] = useState(value)
  const [results, setResults] = useState<CardSearchResult[]>([])
  const [open, setOpen] = useState(false)
  const [loading, setLoading] = useState(false)
  const timer = useRef<ReturnType<typeof setTimeout> | null>(null)

  useEffect(() => { setQuery(value) }, [value])

  function handleChange(q: string) {
    setQuery(q)
    if (timer.current) clearTimeout(timer.current)
    if (q.length < 2) { setResults([]); setOpen(false); return }
    timer.current = setTimeout(async () => {
      setLoading(true)
      try {
        const params = new URLSearchParams({ q })
        if (setCode) params.set('setCode', setCode)
        const res = await fetch(`/api/cards/search?${params}`)
        if (res.ok) {
          const data: CardSearchResult[] = await res.json()
          setResults(data)
          setOpen(data.length > 0)
        }
      } finally { setLoading(false) }
    }, 300)
  }

  function pick(card: CardSearchResult) {
    setQuery(`${card.name} (${card.identifier})`)
    setOpen(false)
    onSelect(card)
  }

  return (
    <div className="relative">
      <Input
        placeholder={setCode ? 'Search by name or identifier...' : 'Search by card name...'}
        value={query}
        onChange={e => handleChange(e.target.value)}
        onFocus={() => results.length > 0 && setOpen(true)}
        onBlur={() => setTimeout(() => setOpen(false), 150)}
        autoComplete="off"
      />
      {loading && <span className="absolute right-2 top-2 text-xs text-muted-foreground">...</span>}
      {open && (
        <div className="absolute z-50 mt-1 w-full rounded-md border bg-popover shadow-md max-h-64 overflow-y-auto">
          {results.map(c => (
            <button
              key={c.identifier}
              type="button"
              className="w-full px-3 py-2 text-left text-sm hover:bg-accent flex justify-between items-center gap-2"
              onMouseDown={() => pick(c)}
            >
              <span className="font-medium">{c.name}</span>
              <span className="text-muted-foreground shrink-0 text-xs">
                {c.identifier} &middot; {c.setCode.toUpperCase()}
              </span>
            </button>
          ))}
        </div>
      )}
    </div>
  )
}

// ---- Add/Edit dialog --------------------------------------------------------

function EntryDialog({
  open, onOpenChange, treatments, sets, initial, onSave,
}: {
  open: boolean
  onOpenChange: (v: boolean) => void
  treatments: Treatment[]
  sets: CardSet[]
  initial?: CollectionEntry | null
  onSave: () => void
}) {
  const isEdit = !!initial
  const regularKey = treatments.find(t => t.key === 'regular')?.key ?? treatments[0]?.key ?? 'regular'
  const { prefs } = usePreferences()

  function blankForm(): EntryForm {
    return {
      cardIdentifier: '', cardName: '', treatment: regularKey,
      quantity: 1, condition: 'NM', autographed: false,
      acquisitionDate: today(), acquisitionPrice: '', notes: '',
    }
  }

  const [form, setForm] = useState<EntryForm>(blankForm())
  const [error, setError] = useState('')
  const [saving, setSaving] = useState(false)
  const [selectedSet, setSelectedSet] = useState<string>('')
  const [setSearch, setSetSearch] = useState('')

  const filteredSets = sets.filter(s =>
    s.code.includes(setSearch.toLowerCase()) ||
    s.name.toLowerCase().includes(setSearch.toLowerCase())
  )

  useEffect(() => {
    if (!open) return
    if (initial) {
      setForm({
        cardIdentifier: initial.cardIdentifier,
        cardName: initial.cardName ?? initial.cardIdentifier,
        treatment: initial.treatmentKey,
        quantity: initial.quantity,
        condition: initial.condition,
        autographed: initial.autographed,
        acquisitionDate: initial.acquisitionDate.slice(0, 10),
        acquisitionPrice: initial.acquisitionPrice.toString(),
        notes: initial.notes ?? '',
      })
    } else {
      setForm(blankForm())
      setSelectedSet('')
      setSetSearch('')
    }
    setError('')
  }, [open, initial])

  function handleCardSelect(card: CardSearchResult) {
    setForm(f => ({
      ...f,
      cardIdentifier: card.identifier,
      cardName: card.name,
      acquisitionPrice: prefs.defaultAcquisitionPriceToMarket && f.acquisitionPrice === '' && card.currentMarketValue != null
        ? card.currentMarketValue.toFixed(2)
        : f.acquisitionPrice,
    }))
  }

  async function handleSave() {
    if (!form.cardIdentifier) { setError('Select a card.'); return }
    const price = parseFloat(form.acquisitionPrice)
    if (isNaN(price) || price < 0) { setError('Enter a valid acquisition price.'); return }
    setSaving(true)
    setError('')
    try {
      const url = isEdit ? `/api/collection/${initial!.id}` : '/api/collection'
      const res = await fetch(url, {
        method: isEdit ? 'PUT' : 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          cardIdentifier: form.cardIdentifier,
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
        throw new Error((data as { error?: string }).error ?? 'Failed to save.')
      }
      onSave()
      onOpenChange(false)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save.')
    } finally { setSaving(false) }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>{isEdit ? 'Edit Entry' : 'Add Card'}</DialogTitle>
        </DialogHeader>
        <div className="grid gap-4 py-2">
          {!isEdit ? (
            <div className="grid gap-3">
              <div className="grid gap-1.5">
                <Label>Set <span className="text-muted-foreground font-normal">(optional - narrows search)</span></Label>
                <Select
                  value={selectedSet || '__all__'}
                  onValueChange={v => {
                    setSelectedSet(v === '__all__' ? '' : v)
                    setForm(f => ({ ...f, cardIdentifier: '', cardName: '' }))
                    setSetSearch('')
                  }}
                >
                  <SelectTrigger>
                    <SelectValue placeholder="All sets" />
                  </SelectTrigger>
                  <SelectContent>
                    <div className="px-2 py-1.5">
                      <Input
                        placeholder="Filter sets..."
                        value={setSearch}
                        onChange={e => setSetSearch(e.target.value)}
                        className="h-7 text-sm"
                        onKeyDown={e => e.stopPropagation()}
                      />
                    </div>
                    <SelectItem value="__all__">All sets</SelectItem>
                    {filteredSets.map(s => (
                      <SelectItem key={s.code} value={s.code}>
                        {s.name} ({s.code.toUpperCase()})
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
              <div className="grid gap-1.5">
                <Label>Card</Label>
                <CardSearch
                  value={form.cardName}
                  onSelect={handleCardSelect}
                  setCode={selectedSet || undefined}
                />
                {form.cardIdentifier && (
                  <p className="text-xs text-muted-foreground">{form.cardIdentifier}</p>
                )}
              </div>
            </div>
          ) : (
            <div className="grid gap-1.5">
              <Label>Card</Label>
              <p className="text-sm font-medium">{form.cardName}</p>
              <p className="text-xs text-muted-foreground">{form.cardIdentifier}</p>
            </div>
          )}
          <div className="grid grid-cols-2 gap-4">
            <div className="grid gap-1.5">
              <Label>Treatment</Label>
              <Select value={form.treatment} onValueChange={v => setForm(f => ({ ...f, treatment: v }))}>
                <SelectTrigger><SelectValue /></SelectTrigger>
                <SelectContent>
                  {treatments.map(t => (
                    <SelectItem key={t.key} value={t.key}>{t.displayName}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="grid gap-1.5">
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
          <div className="grid grid-cols-2 gap-4">
            <div className="grid gap-1.5">
              <Label>Quantity</Label>
              <Input type="number" min={1} value={form.quantity}
                onChange={e => setForm(f => ({ ...f, quantity: parseInt(e.target.value) || 1 }))} />
            </div>
            <div className="grid gap-1.5">
              <Label>Autographed</Label>
              <div className="flex items-center h-10">
                <input type="checkbox" id="aut-chk" checked={form.autographed}
                  onChange={e => setForm(f => ({ ...f, autographed: e.target.checked }))}
                  className="h-4 w-4 rounded border-input" />
                <label htmlFor="aut-chk" className="ml-2 text-sm">Yes</label>
              </div>
            </div>
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div className="grid gap-1.5">
              <Label>Acquisition Date</Label>
              <Input type="date" value={form.acquisitionDate}
                onChange={e => setForm(f => ({ ...f, acquisitionDate: e.target.value }))} />
            </div>
            <div className="grid gap-1.5">
              <Label>Acquisition Price</Label>
              <Input type="number" min={0} step="0.01" placeholder="0.00"
                value={form.acquisitionPrice}
                onChange={e => setForm(f => ({ ...f, acquisitionPrice: e.target.value }))} />
            </div>
          </div>
          <div className="grid gap-1.5">
            <Label>Notes <span className="text-muted-foreground text-xs">(optional)</span></Label>
            <Input value={form.notes} onChange={e => setForm(f => ({ ...f, notes: e.target.value }))}
              placeholder="Any notes..." />
          </div>
          {error && <p className="text-sm text-destructive">{error}</p>}
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)} disabled={saving}>Cancel</Button>
          <Button onClick={handleSave} disabled={saving}>{saving ? 'Saving...' : 'Save'}</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}

// ---- Bulk add set dialog ----------------------------------------------------

function BulkAddDialog({
  open, onOpenChange, sets, treatments, onSave,
}: {
  open: boolean
  onOpenChange: (v: boolean) => void
  sets: CardSet[]
  treatments: Treatment[]
  onSave: () => void
}) {
  const regularKey = treatments.find(t => t.key === 'regular')?.key ?? treatments[0]?.key ?? 'regular'
  const [setSearch, setSetSearch] = useState('')
  const [setCode, setSetCode] = useState('')
  const [treatment, setTreatment] = useState(regularKey)
  const [condition, setCondition] = useState('NM')
  const [acquisitionDate, setAcquisitionDate] = useState(today())
  const [acquisitionPrice, setAcquisitionPrice] = useState('')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [result, setResult] = useState<{ added: number; skipped: number } | null>(null)

  useEffect(() => {
    if (!open) return
    setSetSearch('')
    setSetCode('')
    setTreatment(regularKey)
    setCondition('NM')
    setAcquisitionDate(today())
    setAcquisitionPrice('')
    setError('')
    setResult(null)
  }, [open, regularKey])

  const filteredSets = setSearch.trim()
    ? sets.filter(s =>
        s.code.toLowerCase().includes(setSearch.toLowerCase()) ||
        s.name.toLowerCase().includes(setSearch.toLowerCase())
      )
    : sets

  async function handleAdd() {
    if (!setCode) { setError('Select a set.'); return }
    const price = acquisitionPrice !== '' ? parseFloat(acquisitionPrice) : null
    if (price !== null && (isNaN(price) || price < 0)) {
      setError('Enter a valid price or leave blank to use market value.')
      return
    }
    setSaving(true)
    setError('')
    try {
      const res = await fetch('/api/collection/bulk-add-set', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          setCode: setCode.toLowerCase(),
          treatment,
          condition,
          acquisitionDate,
          acquisitionPrice: price,
        }),
      })
      if (!res.ok) {
        const data = await res.json().catch(() => ({}))
        throw new Error((data as { error?: string }).error ?? 'Failed.')
      }
      const data = await res.json()
      setResult(data)
      onSave()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed.')
    } finally { setSaving(false) }
  }

  const selectedSet = sets.find(s => s.code.toLowerCase() === setCode.toLowerCase())

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader><DialogTitle>Bulk Add Set</DialogTitle></DialogHeader>
        {result ? (
          <div className="py-4 text-sm space-y-1">
            <p className="text-green-600 font-medium">
              {result.added} card{result.added !== 1 ? 's' : ''} added.
            </p>
            {result.skipped > 0 && (
              <p className="text-muted-foreground">
                {result.skipped} already in collection - skipped.
              </p>
            )}
          </div>
        ) : (
          <div className="grid gap-4 py-2">
            <div className="grid gap-1.5">
              <Label>Set</Label>
              {/* Searchable set picker */}
              <div className="relative">
                <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground pointer-events-none" />
                <Input
                  placeholder="Search sets..."
                  value={setSearch}
                  onChange={e => setSetSearch(e.target.value)}
                  className="pl-8"
                />
              </div>
              <div className="rounded-md border max-h-44 overflow-y-auto">
                {filteredSets.length === 0 ? (
                  <p className="px-3 py-2 text-sm text-muted-foreground">No sets match.</p>
                ) : (
                  filteredSets.map(s => (
                    <button
                      key={s.code}
                      type="button"
                      className={`w-full text-left px-3 py-2 text-sm hover:bg-accent flex justify-between items-center ${
                        setCode === s.code.toLowerCase() ? 'bg-accent' : ''
                      }`}
                      onClick={() => setSetCode(s.code.toLowerCase())}
                    >
                      <span>
                        <span className="font-mono text-xs mr-2">{s.code}</span>
                        {s.name}
                      </span>
                      <span className="text-xs text-muted-foreground shrink-0 ml-2">
                        {s.totalCards}
                      </span>
                    </button>
                  ))
                )}
              </div>
              {selectedSet && (
                <p className="text-xs text-muted-foreground">{selectedSet.totalCards} cards in set</p>
              )}
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div className="grid gap-1.5">
                <Label>Treatment</Label>
                <Select value={treatment} onValueChange={setTreatment}>
                  <SelectTrigger><SelectValue /></SelectTrigger>
                  <SelectContent>
                    {treatments.map(t => (
                      <SelectItem key={t.key} value={t.key}>{t.displayName}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
              <div className="grid gap-1.5">
                <Label>Condition</Label>
                <Select value={condition} onValueChange={setCondition}>
                  <SelectTrigger><SelectValue /></SelectTrigger>
                  <SelectContent>
                    {CONDITIONS.map(c => (
                      <SelectItem key={c} value={c}>{c}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div className="grid gap-1.5">
                <Label>Acquisition Date</Label>
                <Input type="date" value={acquisitionDate}
                  onChange={e => setAcquisitionDate(e.target.value)} />
              </div>
              <div className="grid gap-1.5">
                <Label>Price <span className="text-muted-foreground text-xs">(blank = market)</span></Label>
                <Input type="number" min={0} step="0.01" placeholder="Market value"
                  value={acquisitionPrice} onChange={e => setAcquisitionPrice(e.target.value)} />
              </div>
            </div>
            {error && <p className="text-sm text-destructive">{error}</p>}
          </div>
        )}
        <DialogFooter>
          {result ? (
            <Button onClick={() => onOpenChange(false)}>Done</Button>
          ) : (
            <>
              <Button variant="outline" onClick={() => onOpenChange(false)} disabled={saving}>Cancel</Button>
              <Button onClick={handleAdd} disabled={saving}>
                {saving ? 'Adding...' : 'Add Cards'}
              </Button>
            </>
          )}
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}

// ---- Bulk action bar --------------------------------------------------------

function BulkActionBar({
  count,
  treatments,
  onDelete,
  onSetTreatment,
  onSetDate,
  onClear,
}: {
  count: number
  treatments: Treatment[]
  onDelete: () => void
  onSetTreatment: (t: string) => void
  onSetDate: (d: string) => void
  onClear: () => void
}) {
  const [treatmentPick, setTreatmentPick] = useState('')
  const [datePick, setDatePick] = useState('')

  return (
    <div className="flex flex-wrap items-center gap-2 p-3 rounded-md border bg-muted/30 text-sm">
      <span className="font-medium shrink-0">{count} selected</span>

      <div className="flex items-center gap-1">
        <Select value={treatmentPick} onValueChange={setTreatmentPick}>
          <SelectTrigger className="h-7 w-36 text-xs"><SelectValue placeholder="Set treatment..." /></SelectTrigger>
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
          onClick={() => { onSetTreatment(treatmentPick); setTreatmentPick('') }}
        >
          Apply
        </Button>
      </div>

      <div className="flex items-center gap-1">
        <Input
          type="date"
          className="h-7 w-36 text-xs"
          value={datePick}
          onChange={e => setDatePick(e.target.value)}
        />
        <Button
          size="sm"
          variant="outline"
          className="h-7 text-xs"
          disabled={!datePick}
          onClick={() => { onSetDate(datePick); setDatePick('') }}
        >
          Apply
        </Button>
      </div>

      <Button variant="destructive" size="sm" className="h-7 text-xs" onClick={onDelete}>
        <Trash2 className="h-3 w-3 mr-1" /> Remove all
      </Button>
      <Button variant="ghost" size="sm" className="h-7 text-xs ml-auto" onClick={onClear}>
        <X className="h-3 w-3 mr-1" /> Clear
      </Button>
    </div>
  )
}

// ---- Filters panel ----------------------------------------------------------

function FiltersPanel({
  filters, sets, treatments, onChange, onClear,
}: {
  filters: Filters
  sets: CardSet[]
  treatments: Treatment[]
  onChange: (f: Filters) => void
  onClear: () => void
}) {
  const active = filters.setCode || filters.treatment || filters.condition ||
    filters.autographed || filters.color || filters.cardType || filters.isReserved

  return (
    <div className="space-y-2">
      <div className="flex flex-wrap items-end gap-3">
        <div className="grid gap-1">
          <Label className="text-xs text-muted-foreground">Set</Label>
          <Select
            value={filters.setCode || '__all__'}
            onValueChange={v => onChange({ ...filters, setCode: v === '__all__' ? '' : v })}
          >
            <SelectTrigger className="h-8 w-44 text-xs"><SelectValue placeholder="All sets" /></SelectTrigger>
            <SelectContent className="max-h-64">
              <SelectItem value="__all__">All sets</SelectItem>
              {sets.map(s => (
                <SelectItem key={s.code} value={s.code.toLowerCase()}>
                  {s.code} - {s.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
        <div className="grid gap-1">
          <Label className="text-xs text-muted-foreground">Treatment</Label>
          <Select
            value={filters.treatment || '__all__'}
            onValueChange={v => onChange({ ...filters, treatment: v === '__all__' ? '' : v })}
          >
            <SelectTrigger className="h-8 w-36 text-xs"><SelectValue placeholder="All" /></SelectTrigger>
            <SelectContent>
              <SelectItem value="__all__">All</SelectItem>
              {treatments.map(t => (
                <SelectItem key={t.key} value={t.key}>{t.displayName}</SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
        <div className="grid gap-1">
          <Label className="text-xs text-muted-foreground">Condition</Label>
          <Select
            value={filters.condition || '__all__'}
            onValueChange={v => onChange({ ...filters, condition: v === '__all__' ? '' : v })}
          >
            <SelectTrigger className="h-8 w-28 text-xs"><SelectValue placeholder="All" /></SelectTrigger>
            <SelectContent>
              <SelectItem value="__all__">All</SelectItem>
              {CONDITIONS.map(c => (
                <SelectItem key={c} value={c}>{c}</SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
        <div className="grid gap-1">
          <Label className="text-xs text-muted-foreground">Autographed</Label>
          <Select
            value={filters.autographed || '__all__'}
            onValueChange={v => onChange({ ...filters, autographed: v === '__all__' ? '' : v })}
          >
            <SelectTrigger className="h-8 w-28 text-xs"><SelectValue placeholder="All" /></SelectTrigger>
            <SelectContent>
              <SelectItem value="__all__">All</SelectItem>
              <SelectItem value="true">Yes</SelectItem>
              <SelectItem value="false">No</SelectItem>
            </SelectContent>
          </Select>
        </div>
        {active && (
          <Button variant="ghost" size="sm" className="h-8 gap-1 text-xs" onClick={onClear}>
            <X className="h-3 w-3" /> Clear
          </Button>
        )}
      </div>
      {/* Color + type chips */}
      <div className="flex flex-wrap gap-1.5 items-center">
        <span className="text-xs text-muted-foreground">Color:</span>
        {COLORS.map(col => (
          <ToggleChip
            key={col.key}
            active={filters.color === col.key}
            onClick={() => onChange({ ...filters, color: filters.color === col.key ? '' : col.key })}
          >
            {col.key}
          </ToggleChip>
        ))}
        <span className="text-xs text-muted-foreground ml-3">Type:</span>
        {CARD_TYPES.map(t => (
          <ToggleChip
            key={t}
            active={filters.cardType === t}
            onClick={() => onChange({ ...filters, cardType: filters.cardType === t ? '' : t })}
          >
            {t}
          </ToggleChip>
        ))}
        <span className="text-xs text-muted-foreground ml-3">|</span>
        <ToggleChip
          active={filters.isReserved}
          onClick={() => onChange({ ...filters, isReserved: !filters.isReserved })}
        >
          Reserved List
        </ToggleChip>
      </div>
    </div>
  )
}

// ---- By-set grouped view ----------------------------------------------------

function SetGroupedView({
  completion,
  onDrillIn,
}: {
  completion: SetCompletion[]
  onDrillIn: (setCode: string) => void
}) {
  const totalValue = completion.reduce((s, c) => s + (c.totalValue ?? 0), 0)

  return (
    <div className="space-y-2">
      <div className="rounded-md border overflow-x-auto">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b bg-muted/50 text-muted-foreground">
              <th className="px-4 py-2 text-left">Set</th>
              <th className="px-4 py-2 text-right">Owned</th>
              <th className="px-4 py-2 text-right">Total</th>
              <th className="px-4 py-2 text-right">Complete</th>
              <th className="px-4 py-2 text-right">Value</th>
            </tr>
          </thead>
          <tbody>
            {completion.length === 0 ? (
              <tr>
                <td colSpan={5} className="px-4 py-8 text-center text-muted-foreground">
                  No cards in your collection yet.
                </td>
              </tr>
            ) : (
              <>
                {completion.map(s => (
                  <tr
                    key={s.setCode}
                    className="border-b last:border-0 hover:bg-muted/30 cursor-pointer"
                    onClick={() => onDrillIn(s.setCode.toLowerCase())}
                  >
                    <td className="px-4 py-2.5">
                      <div className="flex items-center gap-2 font-medium">
                        <SetSymbol setCode={s.setCode} className="text-base shrink-0" />
                        {s.setName}
                      </div>
                      <div className="text-xs text-muted-foreground font-mono">{s.setCode}</div>
                    </td>
                    <td className="px-4 py-2.5 text-right tabular-nums">{s.ownedCount}</td>
                    <td className="px-4 py-2.5 text-right tabular-nums text-muted-foreground">
                      {s.totalCards}
                    </td>
                    <td className="px-4 py-2.5 text-right tabular-nums">
                      <div className="flex items-center justify-end gap-2">
                        <div className="w-16 bg-muted rounded-full h-1.5 hidden sm:block">
                          <div
                            className="bg-primary h-1.5 rounded-full"
                            style={{ width: `${Math.min(100, s.percentage)}%` }}
                          />
                        </div>
                        <span>{s.percentage}%</span>
                      </div>
                    </td>
                    <td className="px-4 py-2.5 text-right tabular-nums">{fmt(s.totalValue)}</td>
                  </tr>
                ))}
                <tr className="border-t bg-muted/20 font-semibold">
                  <td className="px-4 py-2.5">Total</td>
                  <td className="px-4 py-2.5 text-right tabular-nums">
                    {completion.reduce((s, c) => s + c.ownedCount, 0)}
                  </td>
                  <td colSpan={2} />
                  <td className="px-4 py-2.5 text-right tabular-nums">{fmt(totalValue)}</td>
                </tr>
              </>
            )}
          </tbody>
        </table>
      </div>
    </div>
  )
}

// ---- Cards flat table -------------------------------------------------------

const CONDITION_ORDER: Record<string, number> = { NM: 0, LP: 1, MP: 2, HP: 3, DMG: 4 }

function CardsTable({
  entries,
  treatments,
  selected,
  onToggleSelect,
  onToggleSelectAll,
  onEdit,
  onDelete,
  onAdjustQty,
  onDetail,
  defaultSortKey,
}: {
  entries: CollectionEntry[]
  treatments: Treatment[]
  selected: Set<string>
  onToggleSelect: (id: string) => void
  onToggleSelectAll: (all: boolean) => void
  onEdit: (e: CollectionEntry) => void
  onDelete: (e: CollectionEntry) => void
  onAdjustQty: (e: CollectionEntry, delta: number) => void
  onDetail: (id: string) => void
  defaultSortKey: string
}) {
  const [sortKey, setSortKey] = useState(defaultSortKey)
  const [sortDir, setSortDir] = useState<SortDir>('asc')

  function handleSort(key: string) {
    if (key === sortKey) setSortDir(d => d === 'asc' ? 'desc' : 'asc')
    else { setSortKey(key); setSortDir('asc') }
  }

  const treatmentMap = Object.fromEntries(treatments.map(t => [t.key, t.displayName]))
  const allSelected = entries.length > 0 && entries.every(e => selected.has(e.id))

  const sorted = [...entries].sort((a, b) => {
    let cmp = 0
    switch (sortKey) {
      case 'card': cmp = (a.cardName ?? a.cardIdentifier).localeCompare(b.cardName ?? b.cardIdentifier); break
      case 'identifier': cmp = a.cardIdentifier.localeCompare(b.cardIdentifier); break
      case 'set': cmp = (a.setCode ?? '').localeCompare(b.setCode ?? ''); break
      case 'treatment': cmp = (treatmentMap[a.treatmentKey] ?? a.treatmentKey).localeCompare(treatmentMap[b.treatmentKey] ?? b.treatmentKey); break
      case 'qty': cmp = a.quantity - b.quantity; break
      case 'condition': cmp = (CONDITION_ORDER[a.condition] ?? 99) - (CONDITION_ORDER[b.condition] ?? 99); break
      case 'market': cmp = (a.marketValue ?? -1) - (b.marketValue ?? -1); break
      case 'acq': cmp = a.acquisitionPrice - b.acquisitionPrice; break
      case 'pl': {
        const pa = a.marketValue != null ? (a.marketValue - a.acquisitionPrice) * a.quantity : null
        const pb = b.marketValue != null ? (b.marketValue - b.acquisitionPrice) * b.quantity : null
        cmp = (pa ?? -Infinity) - (pb ?? -Infinity)
        break
      }
    }
    return sortDir === 'asc' ? cmp : -cmp
  })

  if (entries.length === 0) {
    return (
      <p className="text-sm text-muted-foreground py-8 text-center">
        No cards match the current filters.
      </p>
    )
  }

  return (
    <div className="rounded-md border overflow-x-auto">
      <table className="w-full text-sm">
        <thead className="border-b bg-muted/40 text-left">
          <tr>
            <th className="px-3 py-2 w-8">
              <input
                type="checkbox"
                checked={allSelected}
                onChange={e => onToggleSelectAll(e.target.checked)}
                className="h-4 w-4 rounded border-input"
                aria-label="Select all"
              />
            </th>
            <SortTh label="Card" sortKey="card" current={sortKey} dir={sortDir} onSort={handleSort} />
            <SortTh label="ID" sortKey="identifier" current={sortKey} dir={sortDir} onSort={handleSort} />
            <SortTh label="Set" sortKey="set" current={sortKey} dir={sortDir} onSort={handleSort} />
            <SortTh label="Treatment" sortKey="treatment" current={sortKey} dir={sortDir} onSort={handleSort} />
            <SortTh label="Qty" sortKey="qty" current={sortKey} dir={sortDir} onSort={handleSort} className="text-center" />
            <SortTh label="Cond." sortKey="condition" current={sortKey} dir={sortDir} onSort={handleSort} />
            <SortTh label="Market" sortKey="market" current={sortKey} dir={sortDir} onSort={handleSort} className="text-right" />
            <SortTh label="Acq." sortKey="acq" current={sortKey} dir={sortDir} onSort={handleSort} className="text-right" />
            <SortTh label="P/L" sortKey="pl" current={sortKey} dir={sortDir} onSort={handleSort} className="text-right" />
            <th className="px-3 py-2 text-right font-medium">Actions</th>
          </tr>
        </thead>
        <tbody className="divide-y">
          {sorted.map(entry => {
            const pl = entry.marketValue != null
              ? (entry.marketValue - entry.acquisitionPrice) * entry.quantity
              : null
            const isSelected = selected.has(entry.id)
            return (
              <tr key={entry.id} className={`hover:bg-muted/30 ${isSelected ? 'bg-accent/30' : ''}`}>
                <td className="px-3 py-2">
                  <input
                    type="checkbox"
                    checked={isSelected}
                    onChange={() => onToggleSelect(entry.id)}
                    className="h-4 w-4 rounded border-input"
                    aria-label="Select row"
                  />
                </td>
                <td className="px-3 py-2">
                  <div className="flex items-center gap-2">
                    <img
                      src={`/api/images/cards/${(entry.setCode ?? '').toLowerCase()}/${entry.cardIdentifier.toLowerCase()}.jpg`}
                      alt=""
                      className="h-8 w-6 rounded object-cover shrink-0 bg-muted"
                      loading="lazy"
                      onError={e => { (e.target as HTMLImageElement).style.display = 'none' }}
                    />
                    <div>
                      <button
                        type="button"
                        className="font-medium leading-tight hover:underline text-left"
                        onClick={() => onDetail(entry.cardIdentifier)}
                      >
                        {entry.cardName ?? entry.cardIdentifier}
                      </button>
                      {entry.autographed && (
                        <Badge variant="outline" className="ml-1.5 text-xs py-0">Auto</Badge>
                      )}
                    </div>
                  </div>
                </td>
                <td className="px-3 py-2 font-mono text-xs text-muted-foreground whitespace-nowrap">
                  {entry.cardIdentifier.toUpperCase()}
                </td>
                <td className="px-3 py-2 text-muted-foreground">{entry.setCode ?? '-'}</td>
                <td className="px-3 py-2">{treatmentMap[entry.treatmentKey] ?? entry.treatmentKey}</td>
                <td className="px-3 py-2">
                  <div className="flex items-center justify-center gap-1">
                    <button className="rounded p-0.5 hover:bg-accent disabled:opacity-30"
                      onClick={() => onAdjustQty(entry, -1)} disabled={entry.quantity <= 1}
                      aria-label="Decrease quantity">
                      <ChevronDown className="h-3.5 w-3.5" />
                    </button>
                    <span className="w-6 text-center tabular-nums">{entry.quantity}</span>
                    <button className="rounded p-0.5 hover:bg-accent"
                      onClick={() => onAdjustQty(entry, 1)} aria-label="Increase quantity">
                      <ChevronUp className="h-3.5 w-3.5" />
                    </button>
                  </div>
                </td>
                <td className="px-3 py-2">{entry.condition}</td>
                <td className="px-3 py-2 text-right tabular-nums">{fmt(entry.marketValue)}</td>
                <td className="px-3 py-2 text-right tabular-nums">{fmt(entry.acquisitionPrice)}</td>
                <td className={`px-3 py-2 text-right tabular-nums ${plColor(pl)}`}>
                  {pl != null ? `${pl >= 0 ? '+' : ''}${fmt(pl)}` : '-'}
                </td>
                <td className="px-3 py-2 text-right">
                  <div className="flex justify-end gap-1">
                    <Button variant="ghost" size="icon" className="h-7 w-7"
                      onClick={() => onEdit(entry)}>
                      <Pencil className="h-3.5 w-3.5" />
                    </Button>
                    <Button variant="ghost" size="icon"
                      className="h-7 w-7 text-destructive hover:text-destructive"
                      onClick={() => onDelete(entry)}>
                      <Trash2 className="h-3.5 w-3.5" />
                    </Button>
                  </div>
                </td>
              </tr>
            )
          })}
        </tbody>
      </table>
    </div>
  )
}

// ---- Main page --------------------------------------------------------------

type ViewMode = 'by-set' | 'cards'

// ---- Import / Export dialog -------------------------------------------------

const FORMAT_OPTIONS = [
  { value: 'cos',         label: 'CountOrSell (lossless)' },
  { value: 'moxfield',   label: 'Moxfield' },
  { value: 'deckbox',    label: 'Deckbox' },
  { value: 'tcgplayer',  label: 'TCGPlayer' },
  { value: 'dragonshield', label: 'Dragon Shield' },
  { value: 'manabox',    label: 'Manabox' },
]

interface ImportResult {
  added: number
  skipped: number
  failed: number
  failures: string[]
}

function ImportExportDialog({
  open,
  onOpenChange,
  onImportDone,
}: {
  open: boolean
  onOpenChange: (v: boolean) => void
  onImportDone: () => void
}) {
  const [tab, setTab] = useState<'export' | 'import'>('export')
  const [format, setFormat] = useState('cos')
  const [busy, setBusy] = useState(false)
  const [importResult, setImportResult] = useState<ImportResult | null>(null)
  const [importError, setImportError] = useState<string | null>(null)
  const fileRef = useRef<HTMLInputElement>(null)

  function reset() {
    setTab('export')
    setFormat('cos')
    setBusy(false)
    setImportResult(null)
    setImportError(null)
  }

  function handleOpenChange(v: boolean) {
    if (!v) reset()
    onOpenChange(v)
  }

  async function handleExport() {
    setBusy(true)
    try {
      const res = await fetch(`/api/collection/export?format=${format}`)
      if (!res.ok) throw new Error(`Export failed (${res.status})`)
      const blob = await res.blob()
      const cd = res.headers.get('content-disposition') ?? ''
      const match = cd.match(/filename="?([^"]+)"?/)
      const name = match ? match[1] : `collection-${format}.csv`
      const url = URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.download = name
      a.click()
      URL.revokeObjectURL(url)
      handleOpenChange(false)
    } catch (e: unknown) {
      setImportError(e instanceof Error ? e.message : 'Export failed')
    } finally {
      setBusy(false)
    }
  }

  async function handleImport() {
    const file = fileRef.current?.files?.[0]
    if (!file) return
    setBusy(true)
    setImportResult(null)
    setImportError(null)
    try {
      const fd = new FormData()
      fd.append('file', file)
      const res = await fetch(`/api/collection/import?format=${format}`, {
        method: 'POST',
        body: fd,
      })
      if (!res.ok) {
        const body = await res.json().catch(() => ({}))
        throw new Error(body.error ?? `Import failed (${res.status})`)
      }
      const result: ImportResult = await res.json()
      setImportResult(result)
      if (result.added > 0) onImportDone()
    } catch (e: unknown) {
      setImportError(e instanceof Error ? e.message : 'Import failed')
    } finally {
      setBusy(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Import / Export</DialogTitle>
        </DialogHeader>

        {/* Tab strip */}
        <div className="flex border-b mb-4">
          {(['export', 'import'] as const).map(t => (
            <button
              key={t}
              type="button"
              className={`px-4 py-2 text-sm capitalize border-b-2 transition-colors ${
                tab === t
                  ? 'border-primary text-primary font-medium'
                  : 'border-transparent text-muted-foreground hover:text-foreground'
              }`}
              onClick={() => { setTab(t); setImportResult(null); setImportError(null) }}
            >
              {t === 'export' ? <><Download className="h-3.5 w-3.5 inline mr-1" />Export</> : <><Upload className="h-3.5 w-3.5 inline mr-1" />Import</>}
            </button>
          ))}
        </div>

        <div className="space-y-4">
          <div className="space-y-1.5">
            <Label>Format</Label>
            <Select value={format} onValueChange={setFormat}>
              <SelectTrigger>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {FORMAT_OPTIONS.map(o => (
                  <SelectItem key={o.value} value={o.value}>{o.label}</SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          {tab === 'import' && (
            <div className="space-y-1.5">
              <Label>CSV file</Label>
              <input
                ref={fileRef}
                type="file"
                accept=".csv,text/csv"
                className="block w-full text-sm file:mr-3 file:py-1 file:px-3 file:rounded-md file:border file:border-input file:text-sm file:bg-background file:cursor-pointer cursor-pointer"
              />
              <p className="text-xs text-muted-foreground">
                {format === 'cos'
                  ? 'Import a previously exported CountOrSell CSV. All fields are restored.'
                  : `Import a CSV exported from ${FORMAT_OPTIONS.find(o => o.value === format)?.label ?? format}. Cards not found in the database are skipped.`}
              </p>
            </div>
          )}

          {importError && (
            <p className="flex items-center gap-1.5 text-sm text-destructive">
              <XCircle className="h-4 w-4 shrink-0" />
              {importError}
            </p>
          )}

          {importResult && (
            <div className="rounded-md border p-3 space-y-1 text-sm">
              <p className="flex items-center gap-1.5 font-medium text-green-600 dark:text-green-400">
                <CheckCircle className="h-4 w-4 shrink-0" />
                Import complete
              </p>
              <p className="text-muted-foreground">
                Added: {importResult.added} &nbsp;&middot;&nbsp;
                Skipped: {importResult.skipped} &nbsp;&middot;&nbsp;
                Failed: {importResult.failed}
              </p>
              {importResult.failures.length > 0 && (
                <details className="mt-2">
                  <summary className="cursor-pointer text-xs text-muted-foreground hover:text-foreground">
                    Show {importResult.failures.length} failure{importResult.failures.length !== 1 ? 's' : ''}
                  </summary>
                  <ul className="mt-1 space-y-0.5 max-h-40 overflow-y-auto">
                    {importResult.failures.map((f, i) => (
                      <li key={i} className="text-xs text-muted-foreground">{f}</li>
                    ))}
                  </ul>
                </details>
              )}
            </div>
          )}
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => handleOpenChange(false)}>
            {importResult ? 'Close' : 'Cancel'}
          </Button>
          {!importResult && (
            <Button onClick={tab === 'export' ? handleExport : handleImport} disabled={busy}>
              {busy ? (tab === 'export' ? 'Exporting...' : 'Importing...') : (tab === 'export' ? 'Export' : 'Import')}
            </Button>
          )}
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}

export function CollectionPage() {
  const { prefs } = usePreferences()
  const [entries, setEntries] = useState<CollectionEntry[]>([])
  const [completion, setCompletion] = useState<SetCompletion[]>([])
  const [treatments, setTreatments] = useState<Treatment[]>([])
  const [sets, setSets] = useState<CardSet[]>([])
  const [loading, setLoading] = useState(true)
  const [viewMode, setViewMode] = useState<ViewMode>('by-set')
  const [filters, setFilters] = useState<Filters>(BLANK_FILTERS)
  const [selected, setSelected] = useState<Set<string>>(new Set())

  const [addOpen, setAddOpen] = useState(false)
  const [editEntry, setEditEntry] = useState<CollectionEntry | null>(null)
  const [bulkOpen, setBulkOpen] = useState(false)
  const [importExportOpen, setImportExportOpen] = useState(false)
  const [deleteEntry, setDeleteEntry] = useState<CollectionEntry | null>(null)
  const [bulkDeleteConfirm, setBulkDeleteConfirm] = useState(false)
  const [detailId, setDetailId] = useState<string | null>(null)
  const [addFromDetail, setAddFromDetail] = useState<AddableCard | null>(null)

  async function loadCompletion() {
    const res = await fetch('/api/collection/completion')
    if (res.ok) {
      const all: SetCompletion[] = await res.json()
      setCompletion(all.filter(s => s.ownedCount > 0))
    }
  }

  async function loadEntries() {
    const params = new URLSearchParams()
    if (filters.setCode) params.set('filter.setCode', filters.setCode)
    if (filters.treatment) params.set('filter.treatment', filters.treatment)
    if (filters.condition) params.set('filter.condition', filters.condition)
    if (filters.autographed) params.set('filter.autographed', filters.autographed)
    if (filters.color) params.set('filter.color', filters.color)
    if (filters.cardType) params.set('filter.cardType', filters.cardType)
    if (filters.isReserved) params.set('filter.isReserved', 'true')
    const res = await fetch(`/api/collection?${params}`)
    if (res.ok) setEntries(await res.json())
  }

  useEffect(() => {
    Promise.all([
      fetch('/api/treatments').then(r => r.ok ? r.json() : []),
      fetch('/api/sets').then(r => r.ok ? r.json() : []),
    ]).then(([t, s]) => {
      setTreatments(sortTreatments(t))
      setSets(s)
    })
  }, [])

  useEffect(() => {
    setLoading(true)
    Promise.all([loadCompletion(), loadEntries()])
      .finally(() => setLoading(false))
  }, [filters])

  const handleSave = useCallback(() => {
    loadCompletion()
    loadEntries()
  }, [filters])

  function handleDrillIn(setCode: string) {
    setFilters(f => ({ ...f, setCode }))
    setViewMode('cards')
  }

  async function handleDelete() {
    if (!deleteEntry) return
    await fetch(`/api/collection/${deleteEntry.id}`, { method: 'DELETE' })
    await Promise.all([loadCompletion(), loadEntries()])
  }

  async function adjustQuantity(entry: CollectionEntry, delta: number) {
    const res = await fetch(`/api/collection/${entry.id}/quantity`, {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(delta),
    })
    if (res.ok) {
      const updated = await res.json()
      setEntries(prev => prev.map(e => e.id === entry.id ? { ...e, quantity: updated.quantity } : e))
    }
  }

  // Multi-select handlers
  function toggleSelect(id: string) {
    setSelected(prev => {
      const next = new Set(prev)
      if (next.has(id)) next.delete(id); else next.add(id)
      return next
    })
  }

  function toggleSelectAll(all: boolean) {
    setSelected(all ? new Set(entries.map(e => e.id)) : new Set())
  }

  async function handleBulkDelete() {
    const ids = Array.from(selected)
    await fetch('/api/collection/bulk-delete', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ ids }),
    })
    setSelected(new Set())
    await Promise.all([loadCompletion(), loadEntries()])
  }

  async function handleBulkSetTreatment(treatment: string) {
    const ids = Array.from(selected)
    await fetch('/api/collection/bulk-set-treatment', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ ids, treatment }),
    })
    setSelected(new Set())
    await loadEntries()
  }

  async function handleBulkSetDate(date: string) {
    const ids = Array.from(selected)
    await fetch('/api/collection/bulk-set-acquisition-date', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ ids, acquisitionDate: date }),
    })
    setSelected(new Set())
    await loadEntries()
  }

  const totalCards = entries.reduce((n, e) => n + e.quantity, 0)
  const totalValue = entries.reduce((sum, e) => sum + (e.marketValue ?? 0) * e.quantity, 0)
  const totalCost = entries.reduce((sum, e) => sum + e.acquisitionPrice * e.quantity, 0)
  const totalPl = totalValue - totalCost
  const hasValues = entries.some(e => e.marketValue != null)
  const hasActiveFilters = !!(filters.setCode || filters.treatment || filters.condition ||
    filters.autographed || filters.color || filters.cardType || filters.isReserved)

  return (
    <div>
      <div className="flex items-center justify-between mb-4">
        <div>
          <h1 className="text-2xl font-semibold">Collection</h1>
          {!loading && viewMode === 'cards' && (
            <p className="text-sm text-muted-foreground mt-0.5">
              {totalCards} card{totalCards !== 1 ? 's' : ''}
              {hasValues && ` \u00b7 ${fmt(totalValue)} value`}
              {hasValues && (
                <span className={`ml-2 ${plColor(totalPl)}`}>
                  {totalPl >= 0 ? '+' : ''}{fmt(totalPl)} P/L
                </span>
              )}
            </p>
          )}
        </div>
        <div className="flex gap-2 items-center">
          {/* View toggle */}
          <div className="flex rounded-md border overflow-hidden">
            <button
              type="button"
              className={`px-3 py-1.5 text-xs flex items-center gap-1 ${
                viewMode === 'by-set' ? 'bg-primary text-primary-foreground' : 'hover:bg-accent'
              }`}
              onClick={() => setViewMode('by-set')}
              title="By Set"
            >
              <LayoutGrid className="h-3.5 w-3.5" /> By Set
            </button>
            <button
              type="button"
              className={`px-3 py-1.5 text-xs flex items-center gap-1 border-l ${
                viewMode === 'cards' ? 'bg-primary text-primary-foreground' : 'hover:bg-accent'
              }`}
              onClick={() => setViewMode('cards')}
              title="Cards"
            >
              <LayoutList className="h-3.5 w-3.5" /> Cards
            </button>
          </div>
          <Button variant="outline" size="sm" onClick={() => setImportExportOpen(true)}>
            <Upload className="h-4 w-4 mr-1" /> Import / Export
          </Button>
          <Button variant="outline" size="sm" onClick={() => setBulkOpen(true)}>
            Bulk Add Set
          </Button>
          <Button size="sm" onClick={() => setAddOpen(true)}>
            <Plus className="h-4 w-4 mr-1" /> Add Card
          </Button>
        </div>
      </div>

      {/* Drill-in back button when in cards view with set filter */}
      {viewMode === 'cards' && filters.setCode && (
        <div className="flex items-center gap-2 mb-3">
          <button
            type="button"
            className="flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground"
            onClick={() => {
              setFilters(f => ({ ...f, setCode: '' }))
              setViewMode('by-set')
            }}
          >
            <ChevronLeft className="h-4 w-4" />
            All Sets
          </button>
          <span className="text-muted-foreground">/</span>
          <span className="text-sm font-medium">
            {sets.find(s => s.code.toLowerCase() === filters.setCode)?.name ?? filters.setCode.toUpperCase()}
          </span>
        </div>
      )}

      {viewMode === 'cards' && (
        <div className="mb-4">
          <FiltersPanel
            filters={filters}
            sets={sets}
            treatments={treatments}
            onChange={f => { setFilters(f); setSelected(new Set()) }}
            onClear={() => { setFilters(BLANK_FILTERS); setSelected(new Set()) }}
          />
        </div>
      )}

      {selected.size > 0 && viewMode === 'cards' && (
        <div className="mb-3">
          <BulkActionBar
            count={selected.size}
            treatments={treatments}
            onDelete={() => setBulkDeleteConfirm(true)}
            onSetTreatment={handleBulkSetTreatment}
            onSetDate={handleBulkSetDate}
            onClear={() => setSelected(new Set())}
          />
        </div>
      )}

      {loading ? (
        <p className="text-sm text-muted-foreground py-8 text-center">Loading...</p>
      ) : viewMode === 'by-set' ? (
        <SetGroupedView completion={completion} onDrillIn={handleDrillIn} />
      ) : (
        entries.length === 0 && !hasActiveFilters ? (
          <p className="text-sm text-muted-foreground py-8 text-center">
            No cards in your collection yet.
          </p>
        ) : (
          <CardsTable
            entries={entries}
            treatments={treatments}
            selected={selected}
            onToggleSelect={toggleSelect}
            onToggleSelectAll={toggleSelectAll}
            onEdit={setEditEntry}
            onDelete={setDeleteEntry}
            onAdjustQty={adjustQuantity}
            onDetail={setDetailId}
            defaultSortKey={prefs.cardSortDefault === 'identifier' ? 'identifier' : 'card'}
          />
        )
      )}

      <EntryDialog
        open={addOpen}
        onOpenChange={setAddOpen}
        treatments={treatments}
        sets={sets}
        onSave={handleSave}
      />
      <EntryDialog
        open={!!editEntry}
        onOpenChange={v => { if (!v) setEditEntry(null) }}
        treatments={treatments}
        sets={sets}
        initial={editEntry}
        onSave={handleSave}
      />
      <BulkAddDialog
        open={bulkOpen}
        onOpenChange={setBulkOpen}
        sets={sets}
        treatments={treatments}
        onSave={handleSave}
      />
      <ImportExportDialog
        open={importExportOpen}
        onOpenChange={setImportExportOpen}
        onImportDone={handleSave}
      />
      {detailId && (
        <CardDetailDialog
          identifier={detailId}
          onClose={() => setDetailId(null)}
          onAdd={() => {
            const e = entries.find(x => x.cardIdentifier === detailId)
            if (e) {
              setAddFromDetail({
                identifier: e.cardIdentifier,
                name: e.cardName ?? e.cardIdentifier,
                currentMarketValue: e.marketValue,
              })
            }
            setDetailId(null)
          }}
        />
      )}
      {addFromDetail && (
        <QuickAddDialog
          card={addFromDetail}
          treatments={treatments}
          onClose={() => setAddFromDetail(null)}
          onAdded={() => { setAddFromDetail(null); handleSave() }}
        />
      )}
      <ConfirmDialog
        open={!!deleteEntry}
        onOpenChange={v => { if (!v) setDeleteEntry(null) }}
        title="Remove from collection"
        description={`Remove ${deleteEntry?.cardName ?? deleteEntry?.cardIdentifier} from your collection?`}
        confirmLabel="Remove"
        destructive
        onConfirm={handleDelete}
      />
      <ConfirmDialog
        open={bulkDeleteConfirm}
        onOpenChange={v => { if (!v) setBulkDeleteConfirm(false) }}
        title={`Remove ${selected.size} entries?`}
        description="This will permanently remove the selected entries from your collection."
        confirmLabel="Remove All"
        destructive
        onConfirm={handleBulkDelete}
      />
    </div>
  )
}
