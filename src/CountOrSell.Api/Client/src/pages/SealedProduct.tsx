import { useEffect, useState, useCallback, useRef } from 'react'
import { Plus, Pencil, Trash2, ChevronUp, ChevronDown, Filter, X } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import {
  Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle,
} from '@/components/ui/dialog'
import {
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue,
} from '@/components/ui/select'
import { ConfirmDialog } from '@/components/ConfirmDialog'

// ---- Types ------------------------------------------------------------------

interface SealedInventoryEntry {
  id: string
  productIdentifier: string
  productName: string | null
  categorySlug: string | null
  categoryDisplayName: string | null
  subTypeSlug: string | null
  subTypeDisplayName: string | null
  quantity: number
  condition: string
  acquisitionDate: string
  acquisitionPrice: number
  notes: string | null
  currentMarketValue: number | null
}

interface TaxonomySubType {
  slug: string
  categorySlug: string
  displayName: string
  sortOrder: number
}

interface TaxonomyCategory {
  slug: string
  displayName: string
  sortOrder: number
  subTypes: TaxonomySubType[]
}

interface ProductSearchResult {
  identifier: string
  name: string
  setCode: string
  categorySlug: string | null
  subTypeSlug: string | null
  currentMarketValue: number | null
  imageUrl: string
}

interface EntryForm {
  productIdentifier: string
  productName: string
  categorySlug: string
  subTypeSlug: string
  quantity: number
  condition: string
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

// ---- Product search combobox ------------------------------------------------

function ProductSearch({
  value,
  onSelect,
}: {
  value: string
  onSelect: (r: ProductSearchResult) => void
}) {
  const [query, setQuery] = useState(value)
  const [results, setResults] = useState<ProductSearchResult[]>([])
  const [open, setOpen] = useState(false)
  const debounce = useRef<ReturnType<typeof setTimeout> | null>(null)

  useEffect(() => {
    setQuery(value)
  }, [value])

  function handleChange(q: string) {
    setQuery(q)
    if (debounce.current) clearTimeout(debounce.current)
    if (q.trim().length < 2) { setResults([]); setOpen(false); return }
    debounce.current = setTimeout(async () => {
      try {
        const res = await fetch(`/api/sealed-products/search?q=${encodeURIComponent(q)}`)
        if (res.ok) {
          const data: ProductSearchResult[] = await res.json()
          setResults(data)
          setOpen(data.length > 0)
        }
      } catch { /* ignore */ }
    }, 300)
  }

  return (
    <div className="relative">
      <Input
        placeholder="Search sealed products..."
        value={query}
        onChange={e => handleChange(e.target.value)}
        onFocus={() => results.length > 0 && setOpen(true)}
        onBlur={() => setTimeout(() => setOpen(false), 150)}
        autoComplete="off"
      />
      {open && (
        <div className="absolute z-50 mt-1 w-full rounded-md border bg-popover shadow-md max-h-60 overflow-y-auto">
          {results.map(r => (
            <button
              key={r.identifier}
              type="button"
              className="flex items-center gap-2 w-full px-3 py-2 text-left text-sm hover:bg-accent"
              onMouseDown={() => { onSelect(r); setOpen(false) }}
            >
              <img
                src={r.imageUrl}
                alt=""
                className="w-8 h-8 rounded object-cover flex-shrink-0"
                onError={e => { (e.target as HTMLImageElement).style.display = 'none' }}
              />
              <span className="flex-1 min-w-0">
                <span className="font-medium">{r.name}</span>
                <span className="text-muted-foreground ml-2">{r.setCode}</span>
              </span>
              {r.currentMarketValue != null && (
                <span className="text-muted-foreground">{fmt(r.currentMarketValue)}</span>
              )}
            </button>
          ))}
        </div>
      )}
    </div>
  )
}

// ---- Entry dialog -----------------------------------------------------------

function EntryDialog({
  open,
  onClose,
  onSave,
  initial,
  categories,
  isEdit,
}: {
  open: boolean
  onClose: () => void
  onSave: (f: EntryForm) => Promise<void>
  initial: Partial<EntryForm>
  categories: TaxonomyCategory[]
  isEdit: boolean
}) {
  const [form, setForm] = useState<EntryForm>({
    productIdentifier: '',
    productName: '',
    categorySlug: '',
    subTypeSlug: '',
    quantity: 1,
    condition: 'NM',
    acquisitionDate: today(),
    acquisitionPrice: '',
    notes: '',
    ...initial,
  })
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (open) {
      setForm({
        productIdentifier: '',
        productName: '',
        categorySlug: '',
        subTypeSlug: '',
        quantity: 1,
        condition: 'NM',
        acquisitionDate: today(),
        acquisitionPrice: '',
        notes: '',
        ...initial,
      })
      setError(null)
    }
  }, [open])

  const availableSubTypes = form.categorySlug
    ? (categories.find(c => c.slug === form.categorySlug)?.subTypes ?? [])
    : []

  function handleProductSelect(r: ProductSearchResult) {
    setForm(f => ({
      ...f,
      productIdentifier: r.identifier,
      productName: r.name,
      categorySlug: r.categorySlug ?? '',
      subTypeSlug: r.subTypeSlug ?? '',
      acquisitionPrice: r.currentMarketValue != null ? r.currentMarketValue.toFixed(2) : '',
    }))
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!form.productIdentifier.trim()) { setError('Select a product.'); return }
    if (!form.acquisitionPrice || isNaN(parseFloat(form.acquisitionPrice))) {
      setError('Enter acquisition price.')
      return
    }
    if (form.quantity < 1) { setError('Quantity must be at least 1.'); return }
    setSaving(true)
    setError(null)
    try {
      await onSave(form)
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Save failed.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={v => !v && onClose()}>
      <DialogContent className="max-w-lg max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>{isEdit ? 'Edit Sealed Product Entry' : 'Add Sealed Product'}</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit} className="space-y-4">
          {!isEdit && (
            <div>
              <Label>Product</Label>
              <ProductSearch
                value={form.productName}
                onSelect={handleProductSelect}
              />
              {form.productIdentifier && (
                <p className="text-xs text-muted-foreground mt-1">{form.productIdentifier}</p>
              )}
            </div>
          )}
          {isEdit && (
            <div>
              <Label>Product</Label>
              <p className="text-sm font-medium">{form.productName || form.productIdentifier}</p>
            </div>
          )}

          {categories.length > 0 && (
            <div>
              <Label>Category</Label>
              <Select
                value={form.categorySlug || '_none'}
                onValueChange={v => setForm(f => ({
                  ...f,
                  categorySlug: v === '_none' ? '' : v,
                  subTypeSlug: '',
                }))}
              >
                <SelectTrigger>
                  <SelectValue placeholder="(none)" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="_none">(none)</SelectItem>
                  {categories.map(c => (
                    <SelectItem key={c.slug} value={c.slug}>{c.displayName}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          )}

          {availableSubTypes.length > 0 && (
            <div>
              <Label>Sub-type</Label>
              <Select
                value={form.subTypeSlug || '_none'}
                onValueChange={v => setForm(f => ({ ...f, subTypeSlug: v === '_none' ? '' : v }))}
              >
                <SelectTrigger>
                  <SelectValue placeholder="(none)" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="_none">(none)</SelectItem>
                  {availableSubTypes.map(s => (
                    <SelectItem key={s.slug} value={s.slug}>{s.displayName}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          )}

          <div className="grid grid-cols-2 gap-3">
            <div>
              <Label>Quantity</Label>
              <Input
                type="number"
                min={1}
                value={form.quantity}
                onChange={e => setForm(f => ({ ...f, quantity: parseInt(e.target.value) || 1 }))}
              />
            </div>
            <div>
              <Label>Condition</Label>
              <Select value={form.condition} onValueChange={v => setForm(f => ({ ...f, condition: v }))}>
                <SelectTrigger><SelectValue /></SelectTrigger>
                <SelectContent>
                  {CONDITIONS.map(c => (
                    <SelectItem key={c} value={c}>{CONDITION_LABELS[c]}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <Label>Acquisition Date</Label>
              <Input
                type="date"
                value={form.acquisitionDate}
                onChange={e => setForm(f => ({ ...f, acquisitionDate: e.target.value }))}
              />
            </div>
            <div>
              <Label>Acquisition Price ($)</Label>
              <Input
                type="number"
                step="0.01"
                min="0"
                placeholder="0.00"
                value={form.acquisitionPrice}
                onChange={e => setForm(f => ({ ...f, acquisitionPrice: e.target.value }))}
              />
            </div>
          </div>

          <div>
            <Label>Notes</Label>
            <Input
              value={form.notes}
              onChange={e => setForm(f => ({ ...f, notes: e.target.value }))}
              placeholder="Optional notes"
            />
          </div>

          {error && <p className="text-sm text-destructive">{error}</p>}

          <DialogFooter>
            <Button type="button" variant="outline" onClick={onClose}>Cancel</Button>
            <Button type="submit" disabled={saving}>
              {saving ? 'Saving...' : isEdit ? 'Save' : 'Add'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}

// ---- Filters panel ----------------------------------------------------------

interface Filters {
  categorySlug: string
  subTypeSlug: string
}

function FiltersPanel({
  filters,
  setFilters,
  categories,
}: {
  filters: Filters
  setFilters: (f: Filters) => void
  categories: TaxonomyCategory[]
}) {
  const [open, setOpen] = useState(false)
  const active = filters.categorySlug || filters.subTypeSlug

  const subTypes = filters.categorySlug
    ? (categories.find(c => c.slug === filters.categorySlug)?.subTypes ?? [])
    : []

  return (
    <div>
      <Button
        variant={active ? 'default' : 'outline'}
        size="sm"
        onClick={() => setOpen(o => !o)}
      >
        <Filter className="mr-1 h-4 w-4" /> Filters
        {active && (
          <span
            className="ml-2 cursor-pointer"
            onClick={e => { e.stopPropagation(); setFilters({ categorySlug: '', subTypeSlug: '' }) }}
          >
            <X className="h-3 w-3" />
          </span>
        )}
      </Button>
      {open && categories.length > 0 && (
        <div className="mt-2 flex flex-wrap gap-2 items-center p-3 rounded-md border bg-muted/30">
          <div className="flex items-center gap-1">
            <span className="text-sm">Category:</span>
            <Select
              value={filters.categorySlug || '_all'}
              onValueChange={v => setFilters({
                categorySlug: v === '_all' ? '' : v,
                subTypeSlug: '',
              })}
            >
              <SelectTrigger className="h-8 w-40">
                <SelectValue placeholder="All" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="_all">All</SelectItem>
                {categories.map(c => (
                  <SelectItem key={c.slug} value={c.slug}>{c.displayName}</SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          {subTypes.length > 0 && (
            <div className="flex items-center gap-1">
              <span className="text-sm">Sub-type:</span>
              <Select
                value={filters.subTypeSlug || '_all'}
                onValueChange={v => setFilters({ ...filters, subTypeSlug: v === '_all' ? '' : v })}
              >
                <SelectTrigger className="h-8 w-40">
                  <SelectValue placeholder="All" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="_all">All</SelectItem>
                  {subTypes.map(s => (
                    <SelectItem key={s.slug} value={s.slug}>{s.displayName}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          )}

          <Button
            variant="ghost"
            size="sm"
            onClick={() => setFilters({ categorySlug: '', subTypeSlug: '' })}
          >
            Clear
          </Button>
        </div>
      )}
    </div>
  )
}

// ---- Main page --------------------------------------------------------------

export function SealedProductPage() {
  const [entries, setEntries] = useState<SealedInventoryEntry[]>([])
  const [categories, setCategories] = useState<TaxonomyCategory[]>([])
  const [loading, setLoading] = useState(true)
  const [filters, setFilters] = useState<Filters>({ categorySlug: '', subTypeSlug: '' })

  const [addOpen, setAddOpen] = useState(false)
  const [editEntry, setEditEntry] = useState<SealedInventoryEntry | null>(null)
  const [deleteEntry, setDeleteEntry] = useState<SealedInventoryEntry | null>(null)

  const fetchEntries = useCallback(async () => {
    setLoading(true)
    try {
      const params = new URLSearchParams()
      if (filters.categorySlug) params.set('categorySlug', filters.categorySlug)
      if (filters.subTypeSlug) params.set('subTypeSlug', filters.subTypeSlug)
      const res = await fetch(`/api/sealed-inventory?${params}`)
      if (res.ok) setEntries(await res.json())
    } finally {
      setLoading(false)
    }
  }, [filters])

  useEffect(() => {
    fetch('/api/sealed-product-taxonomy/categories')
      .then(r => r.json())
      .then((data: TaxonomyCategory[]) =>
        setCategories([...data].sort((a, b) => a.sortOrder - b.sortOrder))
      )
      .catch(() => {/* taxonomy optional */})
  }, [])

  useEffect(() => { fetchEntries() }, [fetchEntries])

  // Summary stats
  const totalItems = entries.reduce((s, e) => s + e.quantity, 0)
  const totalValue = entries.reduce((s, e) =>
    e.currentMarketValue != null ? s + e.currentMarketValue * e.quantity : s, 0)
  const totalCost = entries.reduce((s, e) => s + e.acquisitionPrice * e.quantity, 0)
  const totalPL = totalValue - totalCost

  async function handleAdd(form: EntryForm) {
    const res = await fetch('/api/sealed-inventory', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        productIdentifier: form.productIdentifier,
        quantity: form.quantity,
        condition: form.condition,
        acquisitionDate: form.acquisitionDate,
        acquisitionPrice: parseFloat(form.acquisitionPrice),
        notes: form.notes || null,
        categorySlug: form.categorySlug || null,
        subTypeSlug: form.subTypeSlug || null,
      }),
    })
    if (!res.ok) {
      const body = await res.json().catch(() => ({}))
      throw new Error(body.error ?? 'Failed to add entry.')
    }
    setAddOpen(false)
    fetchEntries()
  }

  async function handleEdit(form: EntryForm) {
    if (!editEntry) return
    const res = await fetch(`/api/sealed-inventory/${editEntry.id}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        productIdentifier: editEntry.productIdentifier,
        quantity: form.quantity,
        condition: form.condition,
        acquisitionDate: form.acquisitionDate,
        acquisitionPrice: parseFloat(form.acquisitionPrice),
        notes: form.notes || null,
        categorySlug: form.categorySlug || null,
        subTypeSlug: form.subTypeSlug || null,
      }),
    })
    if (!res.ok) {
      const body = await res.json().catch(() => ({}))
      throw new Error(body.error ?? 'Failed to update entry.')
    }
    setEditEntry(null)
    fetchEntries()
  }

  async function handleDelete() {
    if (!deleteEntry) return
    await fetch(`/api/sealed-inventory/${deleteEntry.id}`, { method: 'DELETE' })
    setDeleteEntry(null)
    fetchEntries()
  }

  async function adjustQty(entry: SealedInventoryEntry, delta: number) {
    const newQty = Math.max(1, entry.quantity + delta)
    if (newQty === entry.quantity) return
    await fetch(`/api/sealed-inventory/${entry.id}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        productIdentifier: entry.productIdentifier,
        quantity: newQty,
        condition: entry.condition,
        acquisitionDate: entry.acquisitionDate,
        acquisitionPrice: entry.acquisitionPrice,
        notes: entry.notes,
        categorySlug: entry.categorySlug,
        subTypeSlug: entry.subTypeSlug,
      }),
    })
    fetchEntries()
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold">Sealed Product</h1>
          {!loading && (
            <p className="text-sm text-muted-foreground mt-0.5">
              {totalItems} item{totalItems !== 1 ? 's' : ''} -
              Value: {fmt(totalValue)} -
              P/L:{' '}
              <span className={plColor(totalPL)}>{totalPL >= 0 ? '+' : ''}{fmt(totalPL)}</span>
            </p>
          )}
        </div>
        <Button onClick={() => setAddOpen(true)}>
          <Plus className="mr-1 h-4 w-4" /> Add
        </Button>
      </div>

      {categories.length > 0 && (
        <FiltersPanel
          filters={filters}
          setFilters={f => { setFilters(f); }}
          categories={categories}
        />
      )}

      {loading ? (
        <p className="text-muted-foreground text-sm">Loading...</p>
      ) : entries.length === 0 ? (
        <p className="text-muted-foreground text-sm">No sealed product entries yet.</p>
      ) : (
        <div className="overflow-x-auto rounded-md border">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b bg-muted/50 text-muted-foreground">
                <th className="px-3 py-2 text-left w-12"></th>
                <th className="px-3 py-2 text-left">Product</th>
                <th className="px-3 py-2 text-left">Category</th>
                <th className="px-3 py-2 text-left">Condition</th>
                <th className="px-3 py-2 text-right">Qty</th>
                <th className="px-3 py-2 text-right">Market</th>
                <th className="px-3 py-2 text-right">Paid</th>
                <th className="px-3 py-2 text-right">P/L</th>
                <th className="px-3 py-2"></th>
              </tr>
            </thead>
            <tbody>
              {entries.map(e => {
                const mv = e.currentMarketValue
                const pl = mv != null ? (mv - e.acquisitionPrice) * e.quantity : null
                return (
                  <tr key={e.id} className="border-b last:border-0 hover:bg-muted/20">
                    <td className="px-3 py-2">
                      <img
                        src={`/api/images/sealed/${e.productIdentifier}.jpg`}
                        alt=""
                        className="w-8 h-8 rounded object-cover"
                        onError={ev => { (ev.target as HTMLImageElement).style.display = 'none' }}
                      />
                    </td>
                    <td className="px-3 py-2">
                      <div className="font-medium">{e.productName ?? e.productIdentifier}</div>
                      <div className="text-xs text-muted-foreground">{e.productIdentifier}</div>
                    </td>
                    <td className="px-3 py-2">
                      {e.categoryDisplayName && (
                        <div>{e.categoryDisplayName}</div>
                      )}
                      {e.subTypeDisplayName && (
                        <div className="text-xs text-muted-foreground">{e.subTypeDisplayName}</div>
                      )}
                    </td>
                    <td className="px-3 py-2">{e.condition}</td>
                    <td className="px-3 py-2">
                      <div className="flex items-center justify-end gap-1">
                        <button
                          className="p-0.5 hover:bg-muted rounded"
                          onClick={() => adjustQty(e, -1)}
                          aria-label="Decrease"
                        >
                          <ChevronDown className="h-3.5 w-3.5" />
                        </button>
                        <span className="w-6 text-center">{e.quantity}</span>
                        <button
                          className="p-0.5 hover:bg-muted rounded"
                          onClick={() => adjustQty(e, 1)}
                          aria-label="Increase"
                        >
                          <ChevronUp className="h-3.5 w-3.5" />
                        </button>
                      </div>
                    </td>
                    <td className="px-3 py-2 text-right">{fmt(mv)}</td>
                    <td className="px-3 py-2 text-right">{fmt(e.acquisitionPrice)}</td>
                    <td className={`px-3 py-2 text-right ${plColor(pl)}`}>
                      {pl != null ? `${pl >= 0 ? '+' : ''}${fmt(pl)}` : '-'}
                    </td>
                    <td className="px-3 py-2">
                      <div className="flex gap-1 justify-end">
                        <Button
                          variant="ghost"
                          size="icon"
                          className="h-7 w-7"
                          onClick={() => setEditEntry(e)}
                        >
                          <Pencil className="h-3.5 w-3.5" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="icon"
                          className="h-7 w-7 text-destructive"
                          onClick={() => setDeleteEntry(e)}
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
        onClose={() => setAddOpen(false)}
        onSave={handleAdd}
        initial={{}}
        categories={categories}
        isEdit={false}
      />

      {editEntry && (
        <EntryDialog
          open
          onClose={() => setEditEntry(null)}
          onSave={handleEdit}
          initial={{
            productIdentifier: editEntry.productIdentifier,
            productName: editEntry.productName ?? '',
            categorySlug: editEntry.categorySlug ?? '',
            subTypeSlug: editEntry.subTypeSlug ?? '',
            quantity: editEntry.quantity,
            condition: editEntry.condition,
            acquisitionDate: editEntry.acquisitionDate,
            acquisitionPrice: editEntry.acquisitionPrice.toFixed(2),
            notes: editEntry.notes ?? '',
          }}
          categories={categories}
          isEdit
        />
      )}

      <ConfirmDialog
        open={!!deleteEntry}
        title="Remove entry?"
        description={`Remove ${deleteEntry?.productName ?? deleteEntry?.productIdentifier} from sealed inventory?`}
        onConfirm={handleDelete}
        onOpenChange={v => !v && setDeleteEntry(null)}
        destructive
      />
    </div>
  )
}
