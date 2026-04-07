import { useEffect, useState, useCallback, useRef } from 'react'
import {
  Plus, Pencil, Trash2, ChevronUp, ChevronDown, Filter, X, ExternalLink,
} from 'lucide-react'
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

// ---- Types ----------------------------------------------------------------

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
  oracleRulingUrl: string | null
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

const CONDITIONS = ['NM', 'LP', 'MP', 'HP', 'DMG'] as const
const CONDITION_LABELS: Record<string, string> = {
  NM: 'Near Mint', LP: 'Lightly Played', MP: 'Moderately Played',
  HP: 'Heavily Played', DMG: 'Damaged',
}

function today() {
  return new Date().toISOString().slice(0, 10)
}

function fmt(v: number | null | undefined) {
  if (v == null) return '-'
  return `$${v.toFixed(2)}`
}

function plColor(pl: number | null) {
  if (pl == null) return 'text-muted-foreground'
  if (pl > 0) return 'text-green-600'
  if (pl < 0) return 'text-red-600'
  return 'text-muted-foreground'
}

// ---- Card search combobox -------------------------------------------------

function CardSearch({
  value, onSelect,
}: {
  value: string
  onSelect: (card: CardSearchResult) => void
}) {
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
        const res = await fetch(`/api/cards/search?q=${encodeURIComponent(q)}`, { credentials: 'include' })
        if (res.ok) {
          const data: CardSearchResult[] = await res.json()
          setResults(data)
          setOpen(data.length > 0)
        }
      } finally {
        setLoading(false)
      }
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
        placeholder="Search by card name..."
        value={query}
        onChange={e => handleChange(e.target.value)}
        onFocus={() => results.length > 0 && setOpen(true)}
        autoComplete="off"
      />
      {loading && (
        <span className="absolute right-2 top-2 text-xs text-muted-foreground">...</span>
      )}
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

// ---- Add / Edit dialog ----------------------------------------------------

function EntryDialog({
  open, onOpenChange, treatments, initial, onSave,
}: {
  open: boolean
  onOpenChange: (v: boolean) => void
  treatments: Treatment[]
  initial?: CollectionEntry | null
  onSave: () => void
}) {
  const isEdit = !!initial
  const [form, setForm] = useState<EntryForm>(blankForm(treatments))
  const [error, setError] = useState('')
  const [saving, setSaving] = useState(false)

  function blankForm(ts: Treatment[]): EntryForm {
    return {
      cardIdentifier: '',
      cardName: '',
      treatment: ts[0]?.key ?? 'regular',
      quantity: 1,
      condition: 'NM',
      autographed: false,
      acquisitionDate: today(),
      acquisitionPrice: '',
      notes: '',
    }
  }

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
      setForm(blankForm(treatments))
    }
    setError('')
  }, [open, initial, treatments])

  function handleCardSelect(card: CardSearchResult) {
    setForm(f => ({
      ...f,
      cardIdentifier: card.identifier,
      cardName: card.name,
      acquisitionPrice: f.acquisitionPrice === '' && card.currentMarketValue != null
        ? card.currentMarketValue.toFixed(2)
        : f.acquisitionPrice,
    }))
  }

  async function handleSave() {
    if (!form.cardIdentifier) { setError('Select a card.'); return }
    if (!form.treatment) { setError('Select a treatment.'); return }
    if (form.quantity < 1) { setError('Quantity must be at least 1.'); return }
    const price = parseFloat(form.acquisitionPrice)
    if (isNaN(price) || price < 0) { setError('Enter a valid acquisition price.'); return }

    setSaving(true)
    setError('')
    try {
      const body = {
        cardIdentifier: form.cardIdentifier,
        treatment: form.treatment,
        quantity: form.quantity,
        condition: form.condition,
        autographed: form.autographed,
        acquisitionDate: form.acquisitionDate,
        acquisitionPrice: price,
        notes: form.notes || null,
      }
      const url = isEdit ? `/api/collection/${initial!.id}` : '/api/collection'
      const method = isEdit ? 'PUT' : 'POST'
      const res = await fetch(url, {
        method,
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body),
      })
      if (!res.ok) {
        const data = await res.json().catch(() => ({}))
        throw new Error((data as { error?: string }).error ?? 'Failed to save.')
      }
      onSave()
      onOpenChange(false)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>{isEdit ? 'Edit Entry' : 'Add Card'}</DialogTitle>
        </DialogHeader>
        <div className="grid gap-4 py-2">
          {!isEdit ? (
            <div className="grid gap-1.5">
              <Label>Card</Label>
              <CardSearch value={form.cardName} onSelect={handleCardSelect} />
              {form.cardIdentifier && (
                <p className="text-xs text-muted-foreground">{form.cardIdentifier}</p>
              )}
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
              <Input
                type="number"
                min={1}
                value={form.quantity}
                onChange={e => setForm(f => ({ ...f, quantity: parseInt(e.target.value) || 1 }))}
              />
            </div>
            <div className="grid gap-1.5">
              <Label>Autographed</Label>
              <div className="flex items-center h-10">
                <input
                  type="checkbox"
                  id="aut-chk"
                  checked={form.autographed}
                  onChange={e => setForm(f => ({ ...f, autographed: e.target.checked }))}
                  className="h-4 w-4 rounded border-input"
                />
                <label htmlFor="aut-chk" className="ml-2 text-sm">Yes</label>
              </div>
            </div>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="grid gap-1.5">
              <Label>Acquisition Date</Label>
              <Input
                type="date"
                value={form.acquisitionDate}
                onChange={e => setForm(f => ({ ...f, acquisitionDate: e.target.value }))}
              />
            </div>
            <div className="grid gap-1.5">
              <Label>Acquisition Price</Label>
              <Input
                type="number"
                min={0}
                step="0.01"
                placeholder="0.00"
                value={form.acquisitionPrice}
                onChange={e => setForm(f => ({ ...f, acquisitionPrice: e.target.value }))}
              />
            </div>
          </div>

          <div className="grid gap-1.5">
            <Label>Notes <span className="text-muted-foreground text-xs">(optional)</span></Label>
            <Input
              value={form.notes}
              onChange={e => setForm(f => ({ ...f, notes: e.target.value }))}
              placeholder="Any notes..."
            />
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

// ---- Bulk add set dialog --------------------------------------------------

function BulkAddDialog({
  open, onOpenChange, sets, treatments, onSave,
}: {
  open: boolean
  onOpenChange: (v: boolean) => void
  sets: CardSet[]
  treatments: Treatment[]
  onSave: () => void
}) {
  const [setCode, setSetCode] = useState('')
  const [treatment, setTreatment] = useState('')
  const [condition, setCondition] = useState('NM')
  const [acquisitionDate, setAcquisitionDate] = useState(today())
  const [acquisitionPrice, setAcquisitionPrice] = useState('')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [result, setResult] = useState<{ added: number; skipped: number } | null>(null)

  useEffect(() => {
    if (!open) return
    setSetCode('')
    setTreatment(treatments[0]?.key ?? 'regular')
    setCondition('NM')
    setAcquisitionDate(today())
    setAcquisitionPrice('')
    setError('')
    setResult(null)
  }, [open, treatments])

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
        credentials: 'include',
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
    } finally {
      setSaving(false)
    }
  }

  const selectedSet = sets.find(s => s.code.toLowerCase() === setCode.toLowerCase())

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Bulk Add Set</DialogTitle>
        </DialogHeader>
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
              <Select value={setCode} onValueChange={setSetCode}>
                <SelectTrigger><SelectValue placeholder="Select a set..." /></SelectTrigger>
                <SelectContent className="max-h-64">
                  {sets.map(s => (
                    <SelectItem key={s.code} value={s.code.toLowerCase()}>
                      {s.code} - {s.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
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
                <Input
                  type="date"
                  value={acquisitionDate}
                  onChange={e => setAcquisitionDate(e.target.value)}
                />
              </div>
              <div className="grid gap-1.5">
                <Label>Price <span className="text-muted-foreground text-xs">(blank = market)</span></Label>
                <Input
                  type="number"
                  min={0}
                  step="0.01"
                  placeholder="Market value"
                  value={acquisitionPrice}
                  onChange={e => setAcquisitionPrice(e.target.value)}
                />
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

// ---- Filters panel --------------------------------------------------------

interface Filters {
  setCode: string
  treatment: string
  condition: string
  autographed: string
}

function FiltersPanel({
  filters, sets, treatments, onChange, onClear,
}: {
  filters: Filters
  sets: CardSet[]
  treatments: Treatment[]
  onChange: (f: Filters) => void
  onClear: () => void
}) {
  const active = filters.setCode || filters.treatment || filters.condition || filters.autographed

  return (
    <div className="flex flex-wrap items-end gap-3 mb-4">
      <div className="grid gap-1">
        <Label className="text-xs text-muted-foreground">Set</Label>
        <Select
          value={filters.setCode || '__all__'}
          onValueChange={v => onChange({ ...filters, setCode: v === '__all__' ? '' : v })}
        >
          <SelectTrigger className="h-8 w-44 text-xs">
            <SelectValue placeholder="All sets" />
          </SelectTrigger>
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
  )
}

// ---- Main page ------------------------------------------------------------

export function CollectionPage() {
  const [entries, setEntries] = useState<CollectionEntry[]>([])
  const [treatments, setTreatments] = useState<Treatment[]>([])
  const [sets, setSets] = useState<CardSet[]>([])
  const [loading, setLoading] = useState(true)
  const [filters, setFilters] = useState<Filters>({ setCode: '', treatment: '', condition: '', autographed: '' })

  const [addOpen, setAddOpen] = useState(false)
  const [editEntry, setEditEntry] = useState<CollectionEntry | null>(null)
  const [bulkOpen, setBulkOpen] = useState(false)
  const [deleteEntry, setDeleteEntry] = useState<CollectionEntry | null>(null)

  const treatmentMap = Object.fromEntries(treatments.map(t => [t.key, t.displayName]))

  async function load() {
    const params = new URLSearchParams()
    if (filters.setCode) params.set('filter.setCode', filters.setCode)
    if (filters.treatment) params.set('filter.treatment', filters.treatment)
    if (filters.condition) params.set('filter.condition', filters.condition)
    if (filters.autographed) params.set('filter.autographed', filters.autographed)
    const res = await fetch(`/api/collection?${params}`, { credentials: 'include' })
    if (res.ok) setEntries(await res.json())
  }

  useEffect(() => {
    Promise.all([
      fetch('/api/treatments', { credentials: 'include' }).then(r => r.ok ? r.json() : []),
      fetch('/api/sets', { credentials: 'include' }).then(r => r.ok ? r.json() : []),
    ]).then(([t, s]) => {
      setTreatments(t)
      setSets(s)
    })
  }, [])

  useEffect(() => {
    setLoading(true)
    load().finally(() => setLoading(false))
  }, [filters])

  const handleSave = useCallback(() => { load() }, [filters])

  async function handleDelete() {
    if (!deleteEntry) return
    const res = await fetch(`/api/collection/${deleteEntry.id}`, {
      method: 'DELETE', credentials: 'include',
    })
    if (!res.ok) throw new Error('Failed to delete.')
    await load()
  }

  async function adjustQuantity(entry: CollectionEntry, delta: number) {
    const res = await fetch(`/api/collection/${entry.id}/quantity`, {
      method: 'PATCH',
      credentials: 'include',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(delta),
    })
    if (res.ok) {
      const updated = await res.json()
      setEntries(prev => prev.map(e => e.id === entry.id ? { ...e, quantity: updated.quantity } : e))
    }
  }

  const totalCards = entries.reduce((n, e) => n + e.quantity, 0)
  const totalValue = entries.reduce((sum, e) => sum + (e.marketValue ?? 0) * e.quantity, 0)
  const totalCost = entries.reduce((sum, e) => sum + e.acquisitionPrice * e.quantity, 0)
  const totalPl = totalValue - totalCost
  const hasValues = entries.some(e => e.marketValue != null)

  return (
    <div>
      <div className="flex items-center justify-between mb-4">
        <div>
          <h1 className="text-2xl font-semibold">Collection</h1>
          {!loading && (
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
        <div className="flex gap-2">
          <Button variant="outline" size="sm" onClick={() => setBulkOpen(true)}>
            <Filter className="h-4 w-4 mr-1" /> Bulk Add Set
          </Button>
          <Button size="sm" onClick={() => setAddOpen(true)}>
            <Plus className="h-4 w-4 mr-1" /> Add Card
          </Button>
        </div>
      </div>

      <FiltersPanel
        filters={filters}
        sets={sets}
        treatments={treatments}
        onChange={f => setFilters(f)}
        onClear={() => setFilters({ setCode: '', treatment: '', condition: '', autographed: '' })}
      />

      {loading ? (
        <p className="text-sm text-muted-foreground py-8 text-center">Loading...</p>
      ) : entries.length === 0 ? (
        <p className="text-sm text-muted-foreground py-8 text-center">
          {Object.values(filters).some(Boolean)
            ? 'No cards match the current filters.'
            : 'No cards in your collection yet.'}
        </p>
      ) : (
        <div className="rounded-md border overflow-x-auto">
          <table className="w-full text-sm">
            <thead className="border-b bg-muted/40">
              <tr>
                <th className="px-3 py-2 text-left font-medium">Card</th>
                <th className="px-3 py-2 text-left font-medium">Set</th>
                <th className="px-3 py-2 text-left font-medium">Treatment</th>
                <th className="px-3 py-2 text-center font-medium">Qty</th>
                <th className="px-3 py-2 text-left font-medium">Cond.</th>
                <th className="px-3 py-2 text-right font-medium">Market</th>
                <th className="px-3 py-2 text-right font-medium">Acq.</th>
                <th className="px-3 py-2 text-right font-medium">P/L</th>
                <th className="px-3 py-2 text-right font-medium">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y">
              {entries.map(entry => {
                const pl = entry.marketValue != null
                  ? (entry.marketValue - entry.acquisitionPrice) * entry.quantity
                  : null
                return (
                  <tr key={entry.id} className="hover:bg-muted/30">
                    <td className="px-3 py-2">
                      <div className="flex items-center gap-2">
                        <img
                          src={`/api/images/cards/${entry.cardIdentifier.toLowerCase()}.jpg`}
                          alt=""
                          className="h-8 w-6 rounded object-cover shrink-0 bg-muted"
                          loading="lazy"
                          onError={e => { (e.target as HTMLImageElement).style.display = 'none' }}
                        />
                        <div>
                          <div className="font-medium leading-tight">
                            {entry.cardName ?? entry.cardIdentifier}
                            {entry.autographed && (
                              <Badge variant="outline" className="ml-1.5 text-xs py-0">Auto</Badge>
                            )}
                          </div>
                          <div className="text-xs text-muted-foreground flex items-center gap-1">
                            {entry.cardIdentifier}
                            {entry.oracleRulingUrl && (
                              <a
                                href={entry.oracleRulingUrl}
                                target="_blank"
                                rel="noopener noreferrer"
                                className="hover:text-foreground"
                              >
                                <ExternalLink className="h-3 w-3" />
                              </a>
                            )}
                          </div>
                        </div>
                      </div>
                    </td>
                    <td className="px-3 py-2 text-muted-foreground">{entry.setCode ?? '-'}</td>
                    <td className="px-3 py-2">{treatmentMap[entry.treatmentKey] ?? entry.treatmentKey}</td>
                    <td className="px-3 py-2">
                      <div className="flex items-center justify-center gap-1">
                        <button
                          className="rounded p-0.5 hover:bg-accent disabled:opacity-30"
                          onClick={() => adjustQuantity(entry, -1)}
                          disabled={entry.quantity <= 1}
                          aria-label="Decrease quantity"
                        >
                          <ChevronDown className="h-3.5 w-3.5" />
                        </button>
                        <span className="w-6 text-center tabular-nums">{entry.quantity}</span>
                        <button
                          className="rounded p-0.5 hover:bg-accent"
                          onClick={() => adjustQuantity(entry, 1)}
                          aria-label="Increase quantity"
                        >
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
                        <Button
                          variant="ghost" size="icon" className="h-7 w-7"
                          onClick={() => setEditEntry(entry)}
                        >
                          <Pencil className="h-3.5 w-3.5" />
                        </Button>
                        <Button
                          variant="ghost" size="icon"
                          className="h-7 w-7 text-destructive hover:text-destructive"
                          onClick={() => setDeleteEntry(entry)}
                        >
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
      )}

      <EntryDialog
        open={addOpen}
        onOpenChange={setAddOpen}
        treatments={treatments}
        onSave={handleSave}
      />
      <EntryDialog
        open={!!editEntry}
        onOpenChange={v => { if (!v) setEditEntry(null) }}
        treatments={treatments}
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
      <ConfirmDialog
        open={!!deleteEntry}
        onOpenChange={v => { if (!v) setDeleteEntry(null) }}
        title="Remove from collection"
        description={`Remove ${deleteEntry?.cardName ?? deleteEntry?.cardIdentifier} from your collection?`}
        confirmLabel="Remove"
        destructive
        onConfirm={handleDelete}
      />
    </div>
  )
}
