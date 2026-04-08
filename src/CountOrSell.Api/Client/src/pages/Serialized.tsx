import { useEffect, useState, useCallback, useRef } from 'react'
import { Plus, Pencil, Trash2, X } from 'lucide-react'
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
import { CardDetailDialog, QuickAddDialog, AddableCard } from '@/components/cards/CardDialogs'

// ---- Types ----------------------------------------------------------------

interface SerializedEntry {
  id: string
  cardIdentifier: string
  cardName: string | null
  setCode: string | null
  marketValue: number | null
  treatmentKey: string
  serialNumber: number
  printRunTotal: number
  condition: string
  autographed: boolean
  acquisitionDate: string
  acquisitionPrice: number
  notes: string | null
  oracleRulingUrl?: string | null
}

interface Treatment { key: string; displayName: string; sortOrder: number }
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
  serialNumber: string
  printRunTotal: string
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

// ---- Card search ----------------------------------------------------------

function CardSearch({ value, onSelect }: {
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
        placeholder="Search by card name..."
        value={query}
        onChange={e => handleChange(e.target.value)}
        onFocus={() => results.length > 0 && setOpen(true)}
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

// ---- Add / Edit dialog ----------------------------------------------------

function EntryDialog({ open, onOpenChange, treatments, initial, onSave }: {
  open: boolean
  onOpenChange: (v: boolean) => void
  treatments: Treatment[]
  initial?: SerializedEntry | null
  onSave: () => void
}) {
  const isEdit = !!initial
  const blank = (): EntryForm => ({
    cardIdentifier: '', cardName: '',
    treatment: treatments[0]?.key ?? 'regular',
    serialNumber: '', printRunTotal: '',
    condition: 'NM', autographed: false,
    acquisitionDate: '', acquisitionPrice: '', notes: '',
  })
  const [form, setForm] = useState<EntryForm>(blank())
  const [error, setError] = useState('')
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    if (!open) return
    if (initial) {
      setForm({
        cardIdentifier: initial.cardIdentifier,
        cardName: initial.cardName ?? initial.cardIdentifier,
        treatment: initial.treatmentKey,
        serialNumber: initial.serialNumber.toString(),
        printRunTotal: initial.printRunTotal.toString(),
        condition: initial.condition,
        autographed: initial.autographed,
        acquisitionDate: initial.acquisitionDate.slice(0, 10),
        acquisitionPrice: initial.acquisitionPrice.toString(),
        notes: initial.notes ?? '',
      })
    } else {
      setForm(blank())
    }
    setError('')
  }, [open, initial, treatments])

  function handleCardSelect(card: CardSearchResult) {
    setForm(f => ({ ...f, cardIdentifier: card.identifier, cardName: card.name }))
  }

  async function handleSave() {
    if (!form.cardIdentifier) { setError('Select a card.'); return }
    const serial = parseInt(form.serialNumber)
    if (isNaN(serial) || serial < 1) { setError('Serial number must be a positive integer.'); return }
    const printRun = parseInt(form.printRunTotal)
    if (isNaN(printRun) || printRun < 1) { setError('Print run total must be a positive integer.'); return }
    if (serial > printRun) { setError('Serial number cannot exceed print run total.'); return }
    if (!form.acquisitionDate) { setError('Acquisition date is required.'); return }
    const price = parseFloat(form.acquisitionPrice)
    if (isNaN(price) || price < 0) { setError('Enter a valid acquisition price.'); return }

    setSaving(true)
    setError('')
    try {
      const body = {
        cardIdentifier: form.cardIdentifier,
        treatment: form.treatment,
        serialNumber: serial,
        printRunTotal: printRun,
        condition: form.condition,
        autographed: form.autographed,
        acquisitionDate: form.acquisitionDate,
        acquisitionPrice: price,
        notes: form.notes || null,
      }
      const url = isEdit ? `/api/serialized/${initial!.id}` : '/api/serialized'
      const method = isEdit ? 'PUT' : 'POST'
      const res = await fetch(url, {
        method, credentials: 'include',
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
    } finally { setSaving(false) }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>{isEdit ? 'Edit Serialized Card' : 'Add Serialized Card'}</DialogTitle>
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
              <Label>Serial Number</Label>
              <Input
                type="number" min={1} placeholder="e.g. 42"
                value={form.serialNumber}
                onChange={e => setForm(f => ({ ...f, serialNumber: e.target.value }))}
              />
            </div>
            <div className="grid gap-1.5">
              <Label>Print Run Total</Label>
              <Input
                type="number" min={1} placeholder="e.g. 500"
                value={form.printRunTotal}
                onChange={e => setForm(f => ({ ...f, printRunTotal: e.target.value }))}
              />
            </div>
          </div>

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
              <Label>Autographed</Label>
              <div className="flex items-center h-10">
                <input
                  type="checkbox" id="ser-aut"
                  checked={form.autographed}
                  onChange={e => setForm(f => ({ ...f, autographed: e.target.checked }))}
                  className="h-4 w-4 rounded border-input"
                />
                <label htmlFor="ser-aut" className="ml-2 text-sm">Yes</label>
              </div>
            </div>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="grid gap-1.5">
              <Label>Acquisition Date</Label>
              <Input
                type="date" value={form.acquisitionDate}
                onChange={e => setForm(f => ({ ...f, acquisitionDate: e.target.value }))}
              />
            </div>
            <div className="grid gap-1.5">
              <Label>Acquisition Price</Label>
              <Input
                type="number" min={0} step="0.01" placeholder="0.00"
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

// ---- Filters --------------------------------------------------------------

interface Filters { treatment: string; condition: string }

function FiltersPanel({ filters, treatments, onChange, onClear }: {
  filters: Filters
  treatments: Treatment[]
  onChange: (f: Filters) => void
  onClear: () => void
}) {
  const active = filters.treatment || filters.condition
  return (
    <div className="flex flex-wrap items-end gap-3 mb-4">
      <div className="grid gap-1">
        <Label className="text-xs text-muted-foreground">Treatment</Label>
        <Select value={filters.treatment || '__all__'} onValueChange={v => onChange({ ...filters, treatment: v === '__all__' ? '' : v })}>
          <SelectTrigger className="h-8 w-36 text-xs"><SelectValue placeholder="All" /></SelectTrigger>
          <SelectContent>
            <SelectItem value="__all__">All</SelectItem>
            {treatments.map(t => <SelectItem key={t.key} value={t.key}>{t.displayName}</SelectItem>)}
          </SelectContent>
        </Select>
      </div>
      <div className="grid gap-1">
        <Label className="text-xs text-muted-foreground">Condition</Label>
        <Select value={filters.condition || '__all__'} onValueChange={v => onChange({ ...filters, condition: v === '__all__' ? '' : v })}>
          <SelectTrigger className="h-8 w-28 text-xs"><SelectValue placeholder="All" /></SelectTrigger>
          <SelectContent>
            <SelectItem value="__all__">All</SelectItem>
            {CONDITIONS.map(c => <SelectItem key={c} value={c}>{c}</SelectItem>)}
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

export function SerializedPage() {
  const [entries, setEntries] = useState<SerializedEntry[]>([])
  const [treatments, setTreatments] = useState<Treatment[]>([])
  const [loading, setLoading] = useState(true)
  const [filters, setFilters] = useState<Filters>({ treatment: '', condition: '' })
  const [addOpen, setAddOpen] = useState(false)
  const [editEntry, setEditEntry] = useState<SerializedEntry | null>(null)
  const [deleteEntry, setDeleteEntry] = useState<SerializedEntry | null>(null)
  const [detailId, setDetailId] = useState<string | null>(null)
  const [addFromDetail, setAddFromDetail] = useState<AddableCard | null>(null)
  const [selected, setSelected] = useState<Set<string>>(new Set())
  const [bulkDeleteConfirm, setBulkDeleteConfirm] = useState(false)

  const treatmentMap = Object.fromEntries(treatments.map(t => [t.key, t.displayName]))

  async function load() {
    const params = new URLSearchParams()
    if (filters.treatment) params.set('filter.treatment', filters.treatment)
    if (filters.condition) params.set('filter.condition', filters.condition)
    const res = await fetch(`/api/serialized?${params}`, { credentials: 'include' })
    if (res.ok) setEntries(await res.json())
  }

  useEffect(() => {
    fetch('/api/treatments', { credentials: 'include' })
      .then(r => r.ok ? r.json() : [])
      .then(setTreatments)
  }, [])

  useEffect(() => {
    setLoading(true)
    load().finally(() => setLoading(false))
  }, [filters])

  const handleSave = useCallback(() => { load() }, [filters])

  async function handleDelete() {
    if (!deleteEntry) return
    const res = await fetch(`/api/serialized/${deleteEntry.id}`, { method: 'DELETE', credentials: 'include' })
    if (!res.ok) throw new Error('Failed to delete.')
    await load()
  }

  async function handleBulkDelete() {
    const ids = Array.from(selected)
    await fetch('/api/serialized/bulk-delete', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ ids }),
    })
    setSelected(new Set())
    await load()
  }

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

  const totalValue = entries.reduce((s, e) => s + (e.marketValue ?? 0), 0)
  const hasValues = entries.some(e => e.marketValue != null)

  return (
    <div>
      <div className="flex items-center justify-between mb-4">
        <div>
          <h1 className="text-2xl font-semibold">Serialized Cards</h1>
          {!loading && (
            <p className="text-sm text-muted-foreground mt-0.5">
              {entries.length} card{entries.length !== 1 ? 's' : ''}
              {hasValues && ` \u00b7 ${fmt(totalValue)} value`}
            </p>
          )}
        </div>
        <Button size="sm" onClick={() => setAddOpen(true)}>
          <Plus className="h-4 w-4 mr-1" /> Add Card
        </Button>
      </div>

      <FiltersPanel
        filters={filters} treatments={treatments}
        onChange={f => setFilters(f)}
        onClear={() => setFilters({ treatment: '', condition: '' })}
      />

      {selected.size > 0 && (
        <div className="mb-3 flex items-center gap-2 p-3 rounded-md border bg-muted/30 text-sm">
          <span className="font-medium">{selected.size} selected</span>
          <Button variant="destructive" size="sm" className="h-7 text-xs" onClick={() => setBulkDeleteConfirm(true)}>
            <Trash2 className="h-3 w-3 mr-1" /> Remove all
          </Button>
          <Button variant="ghost" size="sm" className="h-7 text-xs ml-auto" onClick={() => setSelected(new Set())}>
            <X className="h-3 w-3 mr-1" /> Clear
          </Button>
        </div>
      )}

      {loading ? (
        <p className="text-sm text-muted-foreground py-8 text-center">Loading...</p>
      ) : entries.length === 0 ? (
        <p className="text-sm text-muted-foreground py-8 text-center">
          {Object.values(filters).some(Boolean) ? 'No cards match the current filters.' : 'No serialized cards yet.'}
        </p>
      ) : (
        <div className="rounded-md border overflow-x-auto">
          <table className="w-full text-sm">
            <thead className="border-b bg-muted/40">
              <tr>
                <th className="px-3 py-2 w-8">
                  <input
                    type="checkbox"
                    checked={entries.length > 0 && entries.every(e => selected.has(e.id))}
                    onChange={e => toggleSelectAll(e.target.checked)}
                    className="h-4 w-4 rounded border-input"
                    aria-label="Select all"
                  />
                </th>
                <th className="px-3 py-2 text-left font-medium">Card</th>
                <th className="px-3 py-2 text-left font-medium">Set</th>
                <th className="px-3 py-2 text-left font-medium">Treatment</th>
                <th className="px-3 py-2 text-center font-medium">Serial</th>
                <th className="px-3 py-2 text-left font-medium">Cond.</th>
                <th className="px-3 py-2 text-right font-medium">Market</th>
                <th className="px-3 py-2 text-right font-medium">Acq.</th>
                <th className="px-3 py-2 text-right font-medium">P/L</th>
                <th className="px-3 py-2 text-right font-medium">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y">
              {entries.map(entry => {
                const pl = entry.marketValue != null ? entry.marketValue - entry.acquisitionPrice : null
                const isSelected = selected.has(entry.id)
                return (
                  <tr key={entry.id} className={`hover:bg-muted/30 ${isSelected ? 'bg-accent/30' : ''}`}>
                    <td className="px-3 py-2">
                      <input
                        type="checkbox"
                        checked={isSelected}
                        onChange={() => toggleSelect(entry.id)}
                        className="h-4 w-4 rounded border-input"
                        aria-label="Select row"
                      />
                    </td>
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
                          <div className="leading-tight">
                            <button
                              type="button"
                              className="font-medium hover:underline text-left"
                              onClick={() => setDetailId(entry.cardIdentifier)}
                            >
                              {entry.cardName ?? entry.cardIdentifier}
                            </button>
                            {entry.autographed && (
                              <Badge variant="outline" className="ml-1.5 text-xs py-0">Auto</Badge>
                            )}
                          </div>
                          <div className="text-xs text-muted-foreground">
                            {entry.cardIdentifier}
                          </div>
                        </div>
                      </div>
                    </td>
                    <td className="px-3 py-2 text-muted-foreground">{entry.setCode ?? '-'}</td>
                    <td className="px-3 py-2">{treatmentMap[entry.treatmentKey] ?? entry.treatmentKey}</td>
                    <td className="px-3 py-2 text-center tabular-nums text-muted-foreground">
                      <span className="font-medium text-foreground">{entry.serialNumber}</span>
                      <span className="text-xs"> / {entry.printRunTotal}</span>
                    </td>
                    <td className="px-3 py-2">{entry.condition}</td>
                    <td className="px-3 py-2 text-right tabular-nums">{fmt(entry.marketValue)}</td>
                    <td className="px-3 py-2 text-right tabular-nums">{fmt(entry.acquisitionPrice)}</td>
                    <td className={`px-3 py-2 text-right tabular-nums ${plColor(pl)}`}>
                      {pl != null ? `${pl >= 0 ? '+' : ''}${fmt(pl)}` : '-'}
                    </td>
                    <td className="px-3 py-2 text-right">
                      <div className="flex justify-end gap-1">
                        <Button variant="ghost" size="icon" className="h-7 w-7" onClick={() => setEditEntry(entry)}>
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

      <EntryDialog open={addOpen} onOpenChange={setAddOpen} treatments={treatments} onSave={handleSave} />
      <EntryDialog
        open={!!editEntry} onOpenChange={v => { if (!v) setEditEntry(null) }}
        treatments={treatments} initial={editEntry} onSave={handleSave}
      />
      {detailId && (
        <CardDetailDialog
          identifier={detailId}
          onClose={() => setDetailId(null)}
          onAdd={() => {
            const e = entries.find(x => x.cardIdentifier === detailId)
            if (e) setAddFromDetail({ identifier: e.cardIdentifier, name: e.cardName ?? e.cardIdentifier, currentMarketValue: e.marketValue })
            setDetailId(null)
          }}
        />
      )}
      {addFromDetail && (
        <QuickAddDialog
          card={addFromDetail}
          treatments={treatments}
          onClose={() => setAddFromDetail(null)}
          onAdded={() => setAddFromDetail(null)}
        />
      )}
      <ConfirmDialog
        open={!!deleteEntry}
        onOpenChange={v => { if (!v) setDeleteEntry(null) }}
        title="Remove serialized card"
        description={`Remove ${deleteEntry?.cardName ?? deleteEntry?.cardIdentifier} #${deleteEntry?.serialNumber}/${deleteEntry?.printRunTotal}?`}
        confirmLabel="Remove"
        destructive
        onConfirm={handleDelete}
      />
      <ConfirmDialog
        open={bulkDeleteConfirm}
        onOpenChange={v => { if (!v) setBulkDeleteConfirm(false) }}
        title={`Remove ${selected.size} card${selected.size !== 1 ? 's' : ''}?`}
        description="This will permanently remove the selected serialized cards from your collection."
        confirmLabel="Remove All"
        destructive
        onConfirm={handleBulkDelete}
      />
    </div>
  )
}
