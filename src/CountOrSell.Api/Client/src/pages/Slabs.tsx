import { useEffect, useState, useCallback, useMemo, useRef } from 'react'
import { Plus, Pencil, Trash2, ExternalLink, X } from 'lucide-react'
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
import { CardDetailDialog, QuickAddDialog, AddableCard, SortTh, SortDir } from '@/components/cards/CardDialogs'
import { Pagination } from '@/components/Pagination'
import { TableSkeleton } from '@/components/Skeleton'
import { usePreferences } from '@/contexts/PreferencesContext'

const PAGE_SIZE = 100

// ---- Types ----------------------------------------------------------------

interface SlabEntry {
  id: string
  cardIdentifier: string
  cardName: string | null
  setCode: string | null
  marketValue: number | null
  treatmentKey: string
  gradingAgencyCode: string
  grade: string
  certificateNumber: string
  serialNumber: number | null
  printRunTotal: number | null
  condition: string
  autographed: boolean
  acquisitionDate: string
  acquisitionPrice: number
  notes: string | null
}

interface Treatment { key: string; displayName: string; sortOrder: number }
interface GradingAgency {
  code: string; fullName: string; validationUrlTemplate: string
  supportsDirectLookup: boolean; active: boolean
}
interface CardSearchResult {
  identifier: string; setCode: string; name: string
  currentMarketValue: number | null
}

interface EntryForm {
  cardIdentifier: string; cardName: string
  treatment: string; gradingAgency: string; grade: string; certificateNumber: string
  serialNumber: string; printRunTotal: string
  condition: string; autographed: boolean
  acquisitionDate: string; acquisitionPrice: string; notes: string
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

function certUrl(agency: GradingAgency, certNumber: string) {
  if (!agency.validationUrlTemplate) return null
  return agency.validationUrlTemplate.replace('{cert}', encodeURIComponent(certNumber))
}

// ---- Card search ----------------------------------------------------------

function CardSearch({ value, onSelect }: {
  value: string; onSelect: (card: CardSearchResult) => void
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
        if (res.ok) { const d: CardSearchResult[] = await res.json(); setResults(d); setOpen(d.length > 0) }
      } finally { setLoading(false) }
    }, 300)
  }

  function pick(card: CardSearchResult) {
    setQuery(`${card.name} (${card.identifier})`); setOpen(false); onSelect(card)
  }

  return (
    <div className="relative">
      <Input placeholder="Search by card name..." value={query}
        onChange={e => handleChange(e.target.value)}
        onFocus={() => results.length > 0 && setOpen(true)} autoComplete="off" />
      {loading && <span className="absolute right-2 top-2 text-xs text-muted-foreground">...</span>}
      {open && (
        <div className="absolute z-50 mt-1 w-full rounded-md border bg-popover shadow-md max-h-64 overflow-y-auto">
          {results.map(c => (
            <button key={c.identifier} type="button"
              className="w-full px-3 py-2 text-left text-sm hover:bg-accent flex justify-between items-center gap-2"
              onMouseDown={() => pick(c)}>
              <span className="font-medium">{c.name}</span>
              <span className="text-muted-foreground shrink-0 text-xs">{c.identifier} &middot; {c.setCode.toUpperCase()}</span>
            </button>
          ))}
        </div>
      )}
    </div>
  )
}

// ---- Add / Edit dialog ----------------------------------------------------

function EntryDialog({ open, onOpenChange, treatments, agencies, initial, onSave }: {
  open: boolean; onOpenChange: (v: boolean) => void
  treatments: Treatment[]; agencies: GradingAgency[]
  initial?: SlabEntry | null; onSave: () => void
}) {
  const isEdit = !!initial
  const activeAgencies = agencies.filter(a => a.active)
  const blank = (): EntryForm => ({
    cardIdentifier: '', cardName: '',
    treatment: treatments[0]?.key ?? 'regular',
    gradingAgency: activeAgencies[0]?.code ?? '',
    grade: '', certificateNumber: '',
    serialNumber: '', printRunTotal: '',
    condition: 'NM', autographed: false,
    acquisitionDate: '', acquisitionPrice: '', notes: '',
  })
  const [form, setForm] = useState<EntryForm>(blank())
  const [error, setError] = useState('')
  const [saving, setSaving] = useState(false)
  const [validTreatments, setValidTreatments] = useState<string[]>([])

  async function fetchValidTreatments(identifier: string) {
    try {
      const res = await fetch(`/api/cards/${identifier.toLowerCase()}`)
      if (res.ok) {
        const data = await res.json()
        setValidTreatments(data.validTreatments ?? [])
      }
    } catch {
      setValidTreatments([])
    }
  }

  useEffect(() => {
    if (!open) return
    if (initial) {
      setForm({
        cardIdentifier: initial.cardIdentifier,
        cardName: initial.cardName ?? initial.cardIdentifier,
        treatment: initial.treatmentKey,
        gradingAgency: initial.gradingAgencyCode,
        grade: initial.grade,
        certificateNumber: initial.certificateNumber,
        serialNumber: initial.serialNumber?.toString() ?? '',
        printRunTotal: initial.printRunTotal?.toString() ?? '',
        condition: initial.condition,
        autographed: initial.autographed,
        acquisitionDate: initial.acquisitionDate.slice(0, 10),
        acquisitionPrice: initial.acquisitionPrice.toString(),
        notes: initial.notes ?? '',
      })
      fetchValidTreatments(initial.cardIdentifier)
    } else {
      setForm(blank())
      setValidTreatments([])
    }
    setError('')
  }, [open, initial, treatments, agencies])

  function handleCardSelect(card: CardSearchResult) {
    setForm(f => ({ ...f, cardIdentifier: card.identifier, cardName: card.name }))
    fetchValidTreatments(card.identifier)
  }

  async function handleSave() {
    if (!form.cardIdentifier) { setError('Select a card.'); return }
    if (!form.gradingAgency) { setError('Select a grading agency.'); return }
    if (!form.grade.trim()) { setError('Grade is required.'); return }
    if (!form.certificateNumber.trim()) { setError('Certificate number is required.'); return }
    if (!form.acquisitionDate) { setError('Acquisition date is required.'); return }
    const price = parseFloat(form.acquisitionPrice)
    if (isNaN(price) || price < 0) { setError('Enter a valid acquisition price.'); return }

    const serial = form.serialNumber ? parseInt(form.serialNumber) : null
    const printRun = form.printRunTotal ? parseInt(form.printRunTotal) : null
    if (serial !== null && (isNaN(serial) || serial < 1)) { setError('Serial number must be a positive integer.'); return }
    if (printRun !== null && (isNaN(printRun) || printRun < 1)) { setError('Print run total must be a positive integer.'); return }
    if (serial !== null && printRun === null) { setError('Print run total is required when serial number is provided.'); return }
    if (serial !== null && printRun !== null && serial > printRun) { setError('Serial number cannot exceed print run total.'); return }

    setSaving(true); setError('')
    try {
      const body = {
        cardIdentifier: form.cardIdentifier,
        treatment: form.treatment,
        gradingAgency: form.gradingAgency,
        grade: form.grade.trim(),
        certificateNumber: form.certificateNumber.trim(),
        serialNumber: serial, printRunTotal: printRun,
        condition: form.condition, autographed: form.autographed,
        acquisitionDate: form.acquisitionDate, acquisitionPrice: price,
        notes: form.notes || null,
      }
      const url = isEdit ? `/api/slabs/${initial!.id}` : '/api/slabs'
      const res = await fetch(url, {
        method: isEdit ? 'PUT' : 'POST', credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body),
      })
      if (!res.ok) {
        const data = await res.json().catch(() => ({}))
        throw new Error((data as { error?: string }).error ?? 'Failed to save.')
      }
      onSave(); onOpenChange(false)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save.')
    } finally { setSaving(false) }
  }

  const availableTreatments = validTreatments.length > 0
    ? treatments.filter(t => validTreatments.includes(t.key))
    : treatments

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-lg max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>{isEdit ? 'Edit Slab' : 'Add Slab'}</DialogTitle>
        </DialogHeader>
        <div className="grid gap-4 py-2">
          {!isEdit ? (
            <div className="grid gap-1.5">
              <Label>Card</Label>
              <CardSearch value={form.cardName} onSelect={handleCardSelect} />
              {form.cardIdentifier && <p className="text-xs text-muted-foreground">{form.cardIdentifier}</p>}
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
              <Label>Grading Agency</Label>
              <Select value={form.gradingAgency} onValueChange={v => setForm(f => ({ ...f, gradingAgency: v }))}>
                <SelectTrigger><SelectValue /></SelectTrigger>
                <SelectContent>
                  {activeAgencies.map(a => (
                    <SelectItem key={a.code} value={a.code}>{a.code.toUpperCase()} - {a.fullName}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="grid gap-1.5">
              <Label>Grade</Label>
              <Input placeholder="e.g. 9.5" value={form.grade}
                onChange={e => setForm(f => ({ ...f, grade: e.target.value }))} />
            </div>
          </div>

          <div className="grid gap-1.5">
            <Label>Certificate Number</Label>
            <Input placeholder="Cert number" value={form.certificateNumber}
              onChange={e => setForm(f => ({ ...f, certificateNumber: e.target.value }))} />
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="grid gap-1.5">
              <Label>Treatment</Label>
              <Select value={form.treatment} onValueChange={v => setForm(f => ({ ...f, treatment: v }))}>
                <SelectTrigger><SelectValue /></SelectTrigger>
                <SelectContent>
                  {availableTreatments.map(t => <SelectItem key={t.key} value={t.key}>{t.displayName}</SelectItem>)}
                </SelectContent>
              </Select>
            </div>
            <div className="grid gap-1.5">
              <Label>Condition</Label>
              <Select value={form.condition} onValueChange={v => setForm(f => ({ ...f, condition: v }))}>
                <SelectTrigger><SelectValue /></SelectTrigger>
                <SelectContent>
                  {CONDITIONS.map(c => <SelectItem key={c} value={c}>{c} - {CONDITION_LABELS[c]}</SelectItem>)}
                </SelectContent>
              </Select>
            </div>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="grid gap-1.5">
              <Label>Serial # <span className="text-muted-foreground text-xs">(optional)</span></Label>
              <Input type="number" min={1} placeholder="e.g. 42"
                value={form.serialNumber}
                onChange={e => setForm(f => ({ ...f, serialNumber: e.target.value }))} />
            </div>
            <div className="grid gap-1.5">
              <Label>Print Run <span className="text-muted-foreground text-xs">(if serialized)</span></Label>
              <Input type="number" min={1} placeholder="e.g. 500"
                value={form.printRunTotal}
                onChange={e => setForm(f => ({ ...f, printRunTotal: e.target.value }))} />
            </div>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="grid gap-1.5">
              <Label>Autographed</Label>
              <div className="flex items-center h-10">
                <input type="checkbox" id="slab-aut" checked={form.autographed}
                  onChange={e => setForm(f => ({ ...f, autographed: e.target.checked }))}
                  className="h-4 w-4 rounded border-input" />
                <label htmlFor="slab-aut" className="ml-2 text-sm">Yes</label>
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
            <Input value={form.notes}
              onChange={e => setForm(f => ({ ...f, notes: e.target.value }))}
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

// ---- Filters --------------------------------------------------------------

interface Filters { gradingAgency: string; condition: string; treatment: string }

function FiltersPanel({ filters, agencies, treatments, onChange, onClear }: {
  filters: Filters; agencies: GradingAgency[]; treatments: Treatment[]
  onChange: (f: Filters) => void; onClear: () => void
}) {
  const active = filters.gradingAgency || filters.condition || filters.treatment
  return (
    <div className="flex flex-wrap items-end gap-3 mb-4">
      <div className="grid gap-1">
        <Label className="text-xs text-muted-foreground">Agency</Label>
        <Select value={filters.gradingAgency || '__all__'} onValueChange={v => onChange({ ...filters, gradingAgency: v === '__all__' ? '' : v })}>
          <SelectTrigger className="h-8 w-36 text-xs"><SelectValue placeholder="All" /></SelectTrigger>
          <SelectContent>
            <SelectItem value="__all__">All</SelectItem>
            {agencies.map(a => <SelectItem key={a.code} value={a.code}>{a.code.toUpperCase()}</SelectItem>)}
          </SelectContent>
        </Select>
      </div>
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

export function SlabsPage() {
  const { prefs } = usePreferences()
  const [sortKey, setSortKey] = useState(prefs.cardSortDefault === 'identifier' ? 'identifier' : 'card')
  const [sortDir, setSortDir] = useState<SortDir>('asc')
  const [entries, setEntries] = useState<SlabEntry[]>([])
  const [page, setPage] = useState(1)
  const [total, setTotal] = useState(0)
  const [treatments, setTreatments] = useState<Treatment[]>([])
  const [agencies, setAgencies] = useState<GradingAgency[]>([])
  const [loading, setLoading] = useState(true)
  const [filters, setFilters] = useState<Filters>({ gradingAgency: '', condition: '', treatment: '' })
  const [addOpen, setAddOpen] = useState(false)
  const [editEntry, setEditEntry] = useState<SlabEntry | null>(null)
  const [deleteEntry, setDeleteEntry] = useState<SlabEntry | null>(null)
  const [detailId, setDetailId] = useState<string | null>(null)
  const [addFromDetail, setAddFromDetail] = useState<AddableCard | null>(null)
  const [selected, setSelected] = useState<Set<string>>(new Set())
  const [bulkDeleteConfirm, setBulkDeleteConfirm] = useState(false)
  const [conditionPick, setConditionPick] = useState('')
  const [conditionConfirm, setConditionConfirm] = useState(false)

  const treatmentMap = useMemo(
    () => Object.fromEntries(treatments.map(t => [t.key, t.displayName])),
    [treatments],
  )
  const agencyMap = useMemo(
    () => Object.fromEntries(agencies.map(a => [a.code, a])),
    [agencies],
  )

  const handleSort = useCallback((key: string) => {
    setSortKey(prevKey => {
      if (prevKey === key) {
        setSortDir(d => d === 'asc' ? 'desc' : 'asc')
        return prevKey
      }
      setSortDir('asc')
      return key
    })
  }, [])

  const sorted = useMemo(() => [...entries].sort((a, b) => {
    let cmp = 0
    if (sortKey === 'card') cmp = (a.cardName ?? a.cardIdentifier).localeCompare(b.cardName ?? b.cardIdentifier)
    else if (sortKey === 'identifier') cmp = a.cardIdentifier.localeCompare(b.cardIdentifier)
    return sortDir === 'asc' ? cmp : -cmp
  }), [entries, sortKey, sortDir])

  async function load() {
    const params = new URLSearchParams()
    if (filters.gradingAgency) params.set('filter.gradingAgency', filters.gradingAgency)
    if (filters.condition) params.set('filter.condition', filters.condition)
    if (filters.treatment) params.set('filter.treatment', filters.treatment)
    params.set('page', String(page))
    params.set('pageSize', String(PAGE_SIZE))
    const res = await fetch(`/api/slabs?${params}`, { credentials: 'include' })
    if (res.ok) {
      const data = await res.json()
      setEntries(data.items)
      setTotal(data.total)
    }
  }

  useEffect(() => {
    Promise.all([
      fetch('/api/treatments', { credentials: 'include' }).then(r => r.ok ? r.json() : []),
      fetch('/api/grading-agencies', { credentials: 'include' }).then(r => r.ok ? r.json() : []),
    ]).then(([t, a]) => { setTreatments(t); setAgencies(a) })
  }, [])

  // Reset to page 1 when filters change so we don't request a page that no longer exists.
  useEffect(() => { setPage(1) }, [filters])

  useEffect(() => {
    setLoading(true)
    load().finally(() => setLoading(false))
  }, [filters, page])

  const handleSave = useCallback(() => { load() }, [filters, page])

  async function handleDelete() {
    if (!deleteEntry) return
    const res = await fetch(`/api/slabs/${deleteEntry.id}`, { method: 'DELETE', credentials: 'include' })
    if (!res.ok) throw new Error('Failed to delete.')
    await load()
  }

  async function handleBulkDelete() {
    const ids = Array.from(selected)
    await fetch('/api/slabs/bulk-delete', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ ids }),
    })
    setSelected(new Set())
    await load()
  }

  async function handleBulkSetCondition() {
    await fetch('/api/slabs/bulk-set-condition', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ ids: Array.from(selected), condition: conditionPick }),
    })
    setSelected(new Set())
    setConditionPick('')
    await load()
  }

  const toggleSelect = useCallback((id: string) => {
    setSelected(prev => {
      const next = new Set(prev)
      if (next.has(id)) next.delete(id); else next.add(id)
      return next
    })
  }, [])

  const toggleSelectAll = useCallback((all: boolean) => {
    setSelected(all ? new Set(entries.map(e => e.id)) : new Set())
  }, [entries])

  const totalValue = entries.reduce((s, e) => s + (e.marketValue ?? 0), 0)
  const hasValues = entries.some(e => e.marketValue != null)

  return (
    <div>
      <div className="flex items-center justify-between mb-4">
        <div>
          <h1 className="text-2xl font-semibold">Slabs</h1>
          {!loading && (
            <p className="text-sm text-muted-foreground mt-0.5">
              {total} slab{total !== 1 ? 's' : ''}
              {hasValues && ` \u00b7 ${fmt(totalValue)} value (this page)`}
            </p>
          )}
        </div>
        <Button size="sm" onClick={() => setAddOpen(true)}>
          <Plus className="h-4 w-4 mr-1" /> Add Slab
        </Button>
      </div>

      <FiltersPanel filters={filters} agencies={agencies} treatments={treatments}
        onChange={f => setFilters(f)}
        onClear={() => setFilters({ gradingAgency: '', condition: '', treatment: '' })} />

      {selected.size > 0 && (
        <div className="mb-3 flex flex-wrap items-center gap-2 p-3 rounded-md border bg-muted/30 text-sm">
          <span className="font-medium">{selected.size} selected</span>
          <div className="flex items-center gap-1">
            <Select value={conditionPick} onValueChange={setConditionPick}>
              <SelectTrigger className="h-7 text-xs w-28">
                <SelectValue placeholder="Condition..." />
              </SelectTrigger>
              <SelectContent>
                {['NM', 'LP', 'MP', 'HP', 'DMG'].map(c => (
                  <SelectItem key={c} value={c}>{c}</SelectItem>
                ))}
              </SelectContent>
            </Select>
            <Button size="sm" variant="outline" className="h-7 text-xs" disabled={!conditionPick} onClick={() => setConditionConfirm(true)}>
              Apply
            </Button>
          </div>
          <Button variant="destructive" size="sm" className="h-7 text-xs" onClick={() => setBulkDeleteConfirm(true)}>
            <Trash2 className="h-3 w-3 mr-1" /> Remove
          </Button>
          <Button variant="ghost" size="sm" className="h-7 text-xs" onClick={() => setSelected(new Set())}>
            <X className="h-3 w-3 mr-1" /> Clear
          </Button>
        </div>
      )}

      {loading ? (
        <TableSkeleton rows={8} columns={12} />
      ) : entries.length === 0 ? (
        <p className="text-sm text-muted-foreground py-8 text-center">
          {Object.values(filters).some(Boolean) ? 'No slabs match the current filters.' : 'No slabs yet.'}
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
                <SortTh label="Card" sortKey="card" current={sortKey} dir={sortDir} onSort={handleSort} className="text-left" />
                <SortTh label="ID" sortKey="identifier" current={sortKey} dir={sortDir} onSort={handleSort} className="text-left" />
                <th className="px-3 py-2 text-left font-medium">Set</th>
                <th className="px-3 py-2 text-left font-medium">Treatment</th>
                <th className="px-3 py-2 text-left font-medium">Agency</th>
                <th className="px-3 py-2 text-left font-medium">Grade</th>
                <th className="px-3 py-2 text-left font-medium">Certificate</th>
                <th className="px-3 py-2 text-center font-medium">Serial</th>
                <th className="px-3 py-2 text-right font-medium">Market</th>
                <th className="px-3 py-2 text-right font-medium">Acq.</th>
                <th className="px-3 py-2 text-right font-medium">P/L</th>
                <th className="px-3 py-2 text-right font-medium">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y">
              {sorted.map(entry => {
                const pl = entry.marketValue != null ? entry.marketValue - entry.acquisitionPrice : null
                const agency = agencyMap[entry.gradingAgencyCode]
                const certLink = agency ? certUrl(agency, entry.certificateNumber) : null
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
                          src={`/api/images/cards/${(entry.setCode ?? '').toLowerCase()}/${entry.cardIdentifier.toLowerCase()}.jpg`}
                          alt="" className="h-8 w-6 rounded object-cover shrink-0 bg-muted" loading="lazy"
                          onError={e => { (e.target as HTMLImageElement).style.display = 'none' }}
                        />
                        <div>
                          <button
                            type="button"
                            className="font-medium leading-tight hover:underline text-left"
                            onClick={() => setDetailId(entry.cardIdentifier)}
                          >
                            {entry.cardName ?? entry.cardIdentifier}
                          </button>
                          {entry.autographed && <Badge variant="outline" className="ml-1.5 text-xs py-0">Auto</Badge>}
                        </div>
                      </div>
                    </td>
                    <td className="px-3 py-2 font-mono text-xs text-muted-foreground whitespace-nowrap">
                      {entry.cardIdentifier.toUpperCase()}
                    </td>
                    <td className="px-3 py-2 text-muted-foreground">{entry.setCode ?? '-'}</td>
                    <td className="px-3 py-2">{treatmentMap[entry.treatmentKey] ?? entry.treatmentKey}</td>
                    <td className="px-3 py-2 font-medium">{entry.gradingAgencyCode}</td>
                    <td className="px-3 py-2 font-medium">{entry.grade}</td>
                    <td className="px-3 py-2">
                      <div className="flex items-center gap-1">
                        <span className="text-xs text-muted-foreground font-mono">{entry.certificateNumber}</span>
                        {certLink ? (
                          <a href={certLink} target="_blank" rel="noopener noreferrer" className="hover:text-foreground text-muted-foreground">
                            <ExternalLink className="h-3 w-3" />
                          </a>
                        ) : agency && !agency.supportsDirectLookup ? (
                          <span className="text-xs text-muted-foreground">(manual lookup)</span>
                        ) : null}
                      </div>
                    </td>
                    <td className="px-3 py-2 text-center tabular-nums text-muted-foreground">
                      {entry.serialNumber != null ? (
                        <>
                          <span className="font-medium text-foreground">{entry.serialNumber}</span>
                          <span className="text-xs"> / {entry.printRunTotal}</span>
                        </>
                      ) : '-'}
                    </td>
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
                        <Button variant="ghost" size="icon"
                          className="h-7 w-7 text-destructive hover:text-destructive"
                          onClick={() => setDeleteEntry(entry)}>
                          <Trash2 className="h-3.5 w-3.5" />
                        </Button>
                      </div>
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
          <Pagination page={page} pageSize={PAGE_SIZE} total={total} onPageChange={setPage} />
        </div>
      )}

      <EntryDialog open={addOpen} onOpenChange={setAddOpen}
        treatments={treatments} agencies={agencies} onSave={handleSave} />
      <EntryDialog open={!!editEntry} onOpenChange={v => { if (!v) setEditEntry(null) }}
        treatments={treatments} agencies={agencies} initial={editEntry} onSave={handleSave} />
      {detailId && (
        <CardDetailDialog
          identifier={detailId}
          onClose={() => setDetailId(null)}
          onPriceRefreshed={() => { void load() }}
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
        open={!!deleteEntry} onOpenChange={v => { if (!v) setDeleteEntry(null) }}
        title="Remove slab"
        description={`Remove ${deleteEntry?.cardName ?? deleteEntry?.cardIdentifier} (${deleteEntry?.gradingAgencyCode} ${deleteEntry?.grade})?`}
        confirmLabel="Remove" destructive onConfirm={handleDelete}
      />
      <ConfirmDialog
        open={bulkDeleteConfirm}
        onOpenChange={v => { if (!v) setBulkDeleteConfirm(false) }}
        title={`Remove ${selected.size} slab${selected.size !== 1 ? 's' : ''}?`}
        description="This will permanently remove the selected slabs from your collection."
        confirmLabel="Remove All"
        destructive
        onConfirm={handleBulkDelete}
      />
      <ConfirmDialog
        open={conditionConfirm}
        onOpenChange={v => { if (!v) setConditionConfirm(false) }}
        title={`Set condition on ${selected.size} slab${selected.size !== 1 ? 's' : ''}?`}
        description={`Change condition to "${conditionPick}" for the selected slabs.`}
        confirmLabel="Apply"
        onConfirm={handleBulkSetCondition}
      />
    </div>
  )
}
