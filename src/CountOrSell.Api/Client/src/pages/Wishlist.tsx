import { useEffect, useState, useCallback } from 'react'
import { Download, Plus, Search, Star, Trash2, X } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import {
  SortTh, SortDir, ToggleChip, QuickAddDialog,
  Treatment, AddableCard, sortTreatments, fmt,
  COLORS, CARD_TYPES,
} from '@/components/cards/CardDialogs'
import {
  Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle,
} from '@/components/ui/dialog'
import { SetSymbol } from '@/components/ui/SetSymbol'

// ---- Types ------------------------------------------------------------------

interface WishlistEntry {
  id: string
  cardIdentifier: string
  cardName: string | null
  setCode: string | null
  color: string | null
  cardType: string | null
  marketValue: number
  createdAt: string
}

interface SearchResult extends AddableCard {
  setCode: string
  color: string | null
  cardType: string | null
  isReserved: boolean
}

// ---- Add-to-wishlist dialog --------------------------------------------------

function AddToWishlistDialog({
  onClose,
  onAdded,
}: {
  onClose: () => void
  onAdded: () => void
}) {
  const [query, setQuery] = useState('')
  const [results, setResults] = useState<SearchResult[]>([])
  const [searching, setSearching] = useState(false)
  const [adding, setAdding] = useState<string | null>(null)
  const [error, setError] = useState('')

  useEffect(() => {
    const trimmed = query.trim()
    if (trimmed.length < 2) { setResults([]); return }
    const id = setTimeout(async () => {
      setSearching(true)
      try {
        const res = await fetch(`/api/cards/search?q=${encodeURIComponent(trimmed)}`)
        if (res.ok) setResults(await res.json())
      } finally {
        setSearching(false)
      }
    }, 300)
    return () => clearTimeout(id)
  }, [query])

  async function handleAdd(card: SearchResult) {
    setAdding(card.identifier)
    setError('')
    try {
      const res = await fetch('/api/wishlist', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ cardIdentifier: card.identifier.toLowerCase() }),
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
      setAdding(null)
    }
  }

  return (
    <Dialog open onOpenChange={v => !v && onClose()}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>Add to Wishlist</DialogTitle>
        </DialogHeader>
        <div className="space-y-3">
          <div className="relative">
            <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground pointer-events-none" />
            <Input
              autoFocus
              placeholder="Search by name or identifier..."
              value={query}
              onChange={e => setQuery(e.target.value)}
              className="pl-8"
            />
          </div>
          {error && <p className="text-sm text-destructive">{error}</p>}
          {searching && (
            <p className="text-sm text-muted-foreground text-center py-2">Searching...</p>
          )}
          {!searching && results.length === 0 && query.trim().length >= 2 && (
            <p className="text-sm text-muted-foreground text-center py-2">No cards found.</p>
          )}
          {results.length > 0 && (
            <div className="rounded-md border divide-y max-h-72 overflow-y-auto">
              {results.map(r => (
                <div key={r.identifier} className="flex items-center justify-between px-3 py-2 gap-2">
                  <div className="min-w-0">
                    <div className="flex items-center gap-1">
                      <span className="text-sm font-medium truncate">{r.name}</span>
                      {r.isReserved && (
                        <Star className="h-3 w-3 text-amber-500 fill-amber-500 shrink-0" />
                      )}
                    </div>
                    <div className="flex items-center gap-1.5 text-xs text-muted-foreground">
                      <SetSymbol setCode={r.setCode} className="text-sm" />
                      <span className="font-mono">{r.setCode?.toUpperCase()}</span>
                      <span className="font-mono">{r.identifier}</span>
                      {r.currentMarketValue != null && (
                        <span>{fmt(r.currentMarketValue)}</span>
                      )}
                    </div>
                  </div>
                  <Button
                    size="sm"
                    variant="outline"
                    className="h-7 text-xs shrink-0"
                    disabled={adding === r.identifier}
                    onClick={() => handleAdd(r)}
                  >
                    <Plus className="h-3 w-3 mr-1" />
                    {adding === r.identifier ? 'Adding...' : 'Add'}
                  </Button>
                </div>
              ))}
            </div>
          )}
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={onClose}>Close</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}

// ---- Main page --------------------------------------------------------------

export function WishlistPage() {
  const [entries, setEntries] = useState<WishlistEntry[]>([])
  const [treatments, setTreatments] = useState<Treatment[]>([])
  const [loading, setLoading] = useState(true)
  const [addDialogOpen, setAddDialogOpen] = useState(false)
  const [addToCollection, setAddToCollection] = useState<AddableCard | null>(null)
  const [removing, setRemoving] = useState<string | null>(null)

  // Filters
  const [search, setSearch] = useState('')
  const [setFilter, setSetFilter] = useState('')
  const [colorFilter, setColorFilter] = useState('')
  const [typeFilter, setTypeFilter] = useState('')
  const [rlFilter, setRlFilter] = useState(false)

  // Sort
  const [sortKey, setSortKey] = useState('name')
  const [sortDir, setSortDir] = useState<SortDir>('asc')

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const [wishRes, treatRes] = await Promise.all([
        fetch('/api/wishlist'),
        fetch('/api/treatments'),
      ])
      if (wishRes.ok) {
        const data = await wishRes.json()
        setEntries(data.entries ?? [])
      }
      if (treatRes.ok) setTreatments(sortTreatments(await treatRes.json()))
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => { load() }, [load])

  function handleSort(key: string) {
    if (sortKey === key) setSortDir(d => d === 'asc' ? 'desc' : 'asc')
    else { setSortKey(key); setSortDir('asc') }
  }

  async function handleRemove(id: string) {
    setRemoving(id)
    try {
      await fetch(`/api/wishlist/${id}`, { method: 'DELETE' })
      setEntries(prev => prev.filter(e => e.id !== id))
    } finally {
      setRemoving(null)
    }
  }

  async function handleExport() {
    const res = await fetch('/api/wishlist/export/tcgplayer')
    if (!res.ok) return
    const text = await res.text()
    const blob = new Blob([text], { type: 'text/plain' })
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = 'wishlist-tcgplayer.txt'
    a.click()
    URL.revokeObjectURL(url)
  }

  const totalValue = entries.reduce((sum, e) => sum + (e.marketValue ?? 0), 0)

  // Derived filter options from loaded data
  const setCodes = [...new Set(entries.map(e => e.setCode).filter(Boolean) as string[])].sort()
  const entryColors = new Set(entries.flatMap(e => e.color ? e.color.split(',') : []))
  const visibleColors = COLORS.filter(col => entryColors.has(col.key))
  const entryTypes = new Set(entries.flatMap(e => {
    if (!e.cardType) return []
    return CARD_TYPES.filter(t => e.cardType!.includes(t))
  }))
  const visibleTypes = CARD_TYPES.filter(t => entryTypes.has(t))

  const hasFilters = search || setFilter || colorFilter || typeFilter || rlFilter

  const filtered = entries
    .filter(e => {
      if (search.trim()) {
        const q = search.toLowerCase()
        if (
          !e.cardIdentifier.toLowerCase().includes(q) &&
          !(e.cardName ?? '').toLowerCase().includes(q)
        ) return false
      }
      if (setFilter && e.setCode !== setFilter) return false
      if (colorFilter && !(e.color ?? '').split(',').includes(colorFilter)) return false
      if (typeFilter && !(e.cardType ?? '').includes(typeFilter)) return false
      return true
    })
    .slice()
    .sort((a, b) => {
      let cmp = 0
      if (sortKey === 'name') cmp = (a.cardName ?? '').localeCompare(b.cardName ?? '') || a.cardIdentifier.localeCompare(b.cardIdentifier)
      else if (sortKey === 'set') cmp = (a.setCode ?? '').localeCompare(b.setCode ?? '')
      else if (sortKey === 'value') cmp = (a.marketValue ?? -1) - (b.marketValue ?? -1)
      else if (sortKey === 'added') cmp = a.createdAt.localeCompare(b.createdAt)
      return sortDir === 'asc' ? cmp : -cmp
    })

  const filteredTotal = filtered.reduce((sum, e) => sum + (e.marketValue ?? 0), 0)

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between gap-2">
        <div>
          <h1 className="text-2xl font-semibold">Wishlist</h1>
          <p className="text-sm text-muted-foreground">
            {entries.length} {entries.length === 1 ? 'card' : 'cards'} - total value {fmt(totalValue)}
          </p>
        </div>
        <div className="flex items-center gap-2">
          {entries.length > 0 && (
            <Button variant="outline" size="sm" className="gap-1" onClick={handleExport}>
              <Download className="h-4 w-4" /> Export TCGPlayer
            </Button>
          )}
          <Button size="sm" className="gap-1" onClick={() => setAddDialogOpen(true)}>
            <Plus className="h-4 w-4" /> Add Card
          </Button>
        </div>
      </div>

      {/* Filters */}
      {entries.length > 0 && (
        <div className="space-y-2">
          <div className="relative">
            <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground pointer-events-none" />
            <Input
              placeholder="Search wishlist..."
              value={search}
              onChange={e => setSearch(e.target.value)}
              className="pl-8"
            />
          </div>

          {setCodes.length > 1 && (
            <div className="flex flex-wrap gap-1.5 items-center">
              <span className="text-xs text-muted-foreground">Set:</span>
              {setCodes.map(code => (
                <ToggleChip
                  key={code}
                  active={setFilter === code}
                  onClick={() => setSetFilter(setFilter === code ? '' : code)}
                >
                  <span className="inline-flex items-center gap-1">
                    <SetSymbol setCode={code} className="text-xs" />
                    {code}
                  </span>
                </ToggleChip>
              ))}
            </div>
          )}

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

          {hasFilters && (
            <button
              type="button"
              className="inline-flex items-center gap-1 text-xs text-muted-foreground hover:text-foreground"
              onClick={() => { setSearch(''); setSetFilter(''); setColorFilter(''); setTypeFilter(''); setRlFilter(false) }}
            >
              <X className="h-3 w-3" /> Clear filters
            </button>
          )}
        </div>
      )}

      {loading ? (
        <p className="text-sm text-muted-foreground py-8 text-center">Loading wishlist...</p>
      ) : entries.length === 0 ? (
        <div className="rounded-md border py-12 text-center">
          <p className="text-muted-foreground text-sm mb-3">Your wishlist is empty.</p>
          <Button size="sm" className="gap-1" onClick={() => setAddDialogOpen(true)}>
            <Plus className="h-4 w-4" /> Add your first card
          </Button>
        </div>
      ) : (
        <div className="rounded-md border overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b bg-muted/50 text-muted-foreground">
                <SortTh label="Card" sortKey="name" current={sortKey} dir={sortDir} onSort={handleSort} className="text-left" />
                <SortTh label="Set" sortKey="set" current={sortKey} dir={sortDir} onSort={handleSort} className="text-left" />
                <th className="px-3 py-2 text-left">Type</th>
                <SortTh label="Market" sortKey="value" current={sortKey} dir={sortDir} onSort={handleSort} className="text-right" />
                <SortTh label="Added" sortKey="added" current={sortKey} dir={sortDir} onSort={handleSort} className="text-right" />
                <th className="px-3 py-2 text-right"></th>
              </tr>
            </thead>
            <tbody>
              {filtered.length === 0 ? (
                <tr>
                  <td colSpan={6} className="px-4 py-8 text-center text-muted-foreground">
                    No entries match the current filters.
                  </td>
                </tr>
              ) : (
                filtered.map(e => (
                  <tr key={e.id} className="border-b last:border-0 hover:bg-muted/20">
                    <td className="px-3 py-2.5">
                      <div className="font-medium">{e.cardName ?? e.cardIdentifier}</div>
                      <div className="text-xs text-muted-foreground font-mono">{e.cardIdentifier}</div>
                    </td>
                    <td className="px-3 py-2.5">
                      {e.setCode ? (
                        <div className="flex items-center gap-1.5">
                          <SetSymbol setCode={e.setCode} className="text-sm" />
                          <span className="font-mono text-xs">{e.setCode}</span>
                        </div>
                      ) : '-'}
                    </td>
                    <td className="px-3 py-2.5 text-muted-foreground text-xs max-w-36 truncate">
                      {e.cardType ?? '-'}
                    </td>
                    <td className="px-3 py-2.5 text-right tabular-nums">
                      {fmt(e.marketValue)}
                    </td>
                    <td className="px-3 py-2.5 text-right text-xs text-muted-foreground">
                      {new Date(e.createdAt).toLocaleDateString()}
                    </td>
                    <td className="px-3 py-2.5 text-right">
                      <div className="flex items-center justify-end gap-1">
                        <Button
                          size="sm"
                          variant="outline"
                          className="h-7 text-xs gap-1"
                          onClick={() => setAddToCollection({
                            identifier: e.cardIdentifier,
                            name: e.cardName ?? e.cardIdentifier,
                            currentMarketValue: e.marketValue,
                          })}
                        >
                          <Plus className="h-3 w-3" /> Collect
                        </Button>
                        <Button
                          size="sm"
                          variant="ghost"
                          className="h-7 w-7 p-0 text-muted-foreground hover:text-destructive"
                          disabled={removing === e.id}
                          onClick={() => handleRemove(e.id)}
                        >
                          <Trash2 className="h-3.5 w-3.5" />
                        </Button>
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
            {filtered.length > 0 && (
              <tfoot>
                <tr className="border-t bg-muted/30 text-muted-foreground text-xs">
                  <td colSpan={3} className="px-3 py-2">
                    {filtered.length} {filtered.length === 1 ? 'entry' : 'entries'}
                    {hasFilters && ` (filtered from ${entries.length})`}
                  </td>
                  <td className="px-3 py-2 text-right tabular-nums font-medium">
                    {fmt(filteredTotal)}
                  </td>
                  <td colSpan={2} />
                </tr>
              </tfoot>
            )}
          </table>
        </div>
      )}

      {addDialogOpen && (
        <AddToWishlistDialog
          onClose={() => setAddDialogOpen(false)}
          onAdded={load}
        />
      )}

      {addToCollection && (
        <QuickAddDialog
          card={addToCollection}
          treatments={treatments}
          onClose={() => setAddToCollection(null)}
          onAdded={() => setAddToCollection(null)}
        />
      )}
    </div>
  )
}
