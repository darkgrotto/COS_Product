import { useCallback, useEffect, useState } from 'react'
import { Trash2, RefreshCw, AlertTriangle, Image, Database, Package, ArrowUpDown } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { ConfirmDialog } from '@/components/ConfirmDialog'

interface SetImageCount {
  setCode: string
  count: number
}

// One row per set carrying both content types, so "has metadata but no images" and
// "has images but no metadata" are visible states rather than something to infer by
// comparing two separate lists.
interface SetRow {
  setCode: string
  name: string | null
  cardCount: number
  imageCount: number
}

interface SealedRow {
  identifier: string
  name: string
  setCode: string | null
  imageCount: number
}

interface DataSummary {
  metadata: {
    setCount: number
    cardCount: number
    sealedProductCount: number
    treatmentCount: number
  }
  images: {
    totalCount: number
    sealedCount: number
    bySet: SetImageCount[]
  }
  sets: SetRow[]
  sealedProducts: SealedRow[]
}

type SetFilter = 'all' | 'missing-images' | 'missing-metadata' | 'complete'
type SetSortKey = 'setCode' | 'name' | 'cardCount' | 'imageCount'
type SortDir = 'asc' | 'desc'

type PurgeScope = 'all' | 'sealed' | { setCode: string }

interface ConfirmState {
  open: boolean
  title: string
  description: string
  onConfirm: () => Promise<void>
}

export function DataManagementPage() {
  const [summary, setSummary] = useState<DataSummary | null>(null)
  const [loading, setLoading] = useState(true)
  const [setSearch, setSetSearch] = useState('')
  const [setFilter, setSetFilter] = useState<SetFilter>('all')
  const [sortKey, setSortKey] = useState<SetSortKey>('setCode')
  const [sortDir, setSortDir] = useState<SortDir>('asc')
  const [sealedSearch, setSealedSearch] = useState('')
  const [sealedFilter, setSealedFilter] = useState<'all' | 'missing-images' | 'complete'>('all')
  const [actionMessage, setActionMessage] = useState<{ text: string; error: boolean } | null>(null)
  const [confirm, setConfirm] = useState<ConfirmState>({
    open: false, title: '', description: '', onConfirm: async () => {},
  })

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const res = await fetch('/api/admin/data/summary', { credentials: 'include' })
      if (res.ok) setSummary(await res.json())
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => { load() }, [load])

  function openConfirm(title: string, description: string, onConfirm: () => Promise<void>) {
    setConfirm({ open: true, title, description, onConfirm })
    setActionMessage(null)
  }

  async function runAction(fn: () => Promise<Response>) {
    try {
      const res = await fn()
      if (!res.ok) {
        const data = await res.json().catch(() => ({}))
        setActionMessage({ text: (data as { error?: string }).error ?? 'Action failed.', error: true })
      } else {
        const data = await res.json().catch(() => ({}))
        setActionMessage({ text: (data as { message?: string }).message ?? 'Done.', error: false })
        await load()
      }
    } catch {
      setActionMessage({ text: 'Request failed. Check the application logs.', error: true })
    }
  }

  // ---- Image purge actions ----

  function purgeImages(scope: PurgeScope) {
    const scopeLabel = scope === 'all' ? 'all image data'
      : scope === 'sealed' ? 'sealed product images'
      : `images for set ${scope.setCode.toUpperCase()}`
    const url = scope === 'all' ? '/api/admin/data/images/all'
      : scope === 'sealed' ? '/api/admin/data/images/sealed'
      : `/api/admin/data/images/sets/${scope.setCode}`
    openConfirm(
      'Purge Images',
      `This will permanently delete ${scopeLabel}. Images can be restored by running a content update or force redownload. This cannot be undone. Proceed?`,
      () => runAction(() => fetch(url, { method: 'DELETE', credentials: 'include' })),
    )
  }

  // ---- Metadata purge actions ----

  function purgeMetadata(scope: PurgeScope) {
    const scopeLabel = scope === 'all' ? 'all metadata (sets, cards, sealed products, treatments, and update versions)'
      : scope === 'sealed' ? 'all sealed product metadata'
      : `metadata for set ${(scope as { setCode: string }).setCode.toUpperCase()}`
    const url = scope === 'all' ? '/api/admin/data/metadata/all'
      : scope === 'sealed' ? '/api/admin/data/metadata/sealed'
      : `/api/admin/data/metadata/sets/${(scope as { setCode: string }).setCode}`
    openConfirm(
      'Purge Metadata',
      `This will permanently delete ${scopeLabel}. All associated collection entries and wishlist entries for affected cards will also be deleted. Run a content update to restore. This cannot be undone. Proceed?`,
      () => runAction(() => fetch(url, { method: 'DELETE', credentials: 'include' })),
    )
  }

  const cardImageCount = summary
    ? summary.images.totalCount - summary.images.sealedCount
    : 0

  const filteredSets = (summary?.sets ?? [])
    .filter(r => {
      if (setSearch.trim()) {
        const q = setSearch.trim().toLowerCase()
        if (!r.setCode.toLowerCase().includes(q) && !(r.name ?? '').toLowerCase().includes(q)) return false
      }
      // "missing metadata" means images exist with no cards behind them - the orphan a
      // metadata purge leaves, which is otherwise invisible.
      if (setFilter === 'missing-images') return r.cardCount > 0 && r.imageCount === 0
      if (setFilter === 'missing-metadata') return r.imageCount > 0 && r.cardCount === 0
      if (setFilter === 'complete') return r.cardCount > 0 && r.imageCount > 0
      return true
    })
    .sort((a, b) => {
      const dir = sortDir === 'asc' ? 1 : -1
      switch (sortKey) {
        case 'name': return dir * (a.name ?? '').localeCompare(b.name ?? '')
        case 'cardCount': return dir * (a.cardCount - b.cardCount)
        case 'imageCount': return dir * (a.imageCount - b.imageCount)
        default: return dir * a.setCode.localeCompare(b.setCode)
      }
    })

  const filteredSealed = (summary?.sealedProducts ?? []).filter(r => {
    if (sealedSearch.trim()) {
      const q = sealedSearch.trim().toLowerCase()
      if (!r.name.toLowerCase().includes(q) && !r.identifier.toLowerCase().includes(q)) return false
    }
    if (sealedFilter === 'missing-images') return r.imageCount === 0
    if (sealedFilter === 'complete') return r.imageCount > 0
    return true
  })

  function toggleSort(key: SetSortKey) {
    if (sortKey === key) setSortDir(d => (d === 'asc' ? 'desc' : 'asc'))
    else { setSortKey(key); setSortDir('asc') }
  }

  function SortHeader({ label, k, className }: { label: string; k: SetSortKey; className?: string }) {
    const active = sortKey === k
    return (
      <th className={`px-3 py-2 font-medium ${className ?? 'text-left'}`}>
        <button
          type="button"
          onClick={() => toggleSort(k)}
          className="inline-flex items-center gap-1 hover:text-foreground"
          aria-sort={active ? (sortDir === 'asc' ? 'ascending' : 'descending') : 'none'}
        >
          {label}
          <ArrowUpDown className={`h-3 w-3 ${active ? 'text-foreground' : 'text-muted-foreground/50'}`} />
        </button>
      </th>
    )
  }

  const FILTERS: { key: SetFilter; label: string }[] = [
    { key: 'all', label: 'All' },
    { key: 'missing-images', label: 'Missing images' },
    { key: 'missing-metadata', label: 'Missing metadata' },
    { key: 'complete', label: 'Complete' },
  ]

  return (
    <div className="space-y-8 max-w-5xl">
      <div className="flex items-center justify-between gap-2 flex-wrap">
        <h1 className="text-2xl font-semibold">Data Management</h1>
        <Button variant="outline" size="sm" onClick={load} disabled={loading}>
          <RefreshCw className={`h-4 w-4 mr-2 ${loading ? 'animate-spin' : ''}`} />
          Refresh
        </Button>
      </div>

      {actionMessage && (
        <Alert variant={actionMessage.error ? 'destructive' : 'default'}>
          {actionMessage.error ? <AlertTriangle className="h-4 w-4" /> : null}
          <AlertTitle>{actionMessage.error ? 'Error' : 'Done'}</AlertTitle>
          <AlertDescription>{actionMessage.text}</AlertDescription>
        </Alert>
      )}

      {/* ---- Purge actions, above the tables ---- */}
      <section className="space-y-3">
        <h2 className="text-lg font-medium">Purge</h2>
        {summary && (
          <p className="text-sm text-muted-foreground">
            {summary.metadata.setCount} sets, {summary.metadata.cardCount} cards,{' '}
            {summary.metadata.sealedProductCount} sealed products,{' '}
            {summary.metadata.treatmentCount} treatments &mdash;{' '}
            {summary.images.totalCount} images ({cardImageCount} card/set, {summary.images.sealedCount} sealed)
          </p>
        )}
        <div className="flex flex-wrap gap-2">
          <Button variant="outline" size="sm" onClick={() => purgeImages('all')}
            disabled={loading || summary?.images.totalCount === 0}>
            <Trash2 className="h-4 w-4 mr-2" />Purge All Images
          </Button>
          <Button variant="outline" size="sm" onClick={() => purgeImages('sealed')}
            disabled={loading || summary?.images.sealedCount === 0}>
            <Trash2 className="h-4 w-4 mr-2" />Purge Sealed Images
          </Button>
          <Button variant="outline" size="sm" onClick={() => purgeMetadata('all')}
            disabled={loading || (summary?.metadata.setCount === 0 && summary?.metadata.sealedProductCount === 0)}>
            <Trash2 className="h-4 w-4 mr-2" />Purge All Metadata
          </Button>
          <Button variant="outline" size="sm" onClick={() => purgeMetadata('sealed')}
            disabled={loading || summary?.metadata.sealedProductCount === 0}>
            <Trash2 className="h-4 w-4 mr-2" />Purge Sealed Metadata
          </Button>
        </div>
      </section>

      {/* ---- Sets ---- */}
      <section className="space-y-3">
        <div className="flex items-center gap-2">
          <Database className="h-5 w-5 text-muted-foreground" />
          <h2 className="text-lg font-medium">Sets</h2>
          <span className="text-sm text-muted-foreground">
            {filteredSets.length} of {summary?.sets.length ?? 0}
          </span>
        </div>

        <div className="flex flex-wrap items-center gap-2">
          <Input
            value={setSearch}
            onChange={e => setSetSearch(e.target.value)}
            placeholder="Search set code or name..."
            className="h-8 w-64"
          />
          {FILTERS.map(f => (
            <button
              key={f.key}
              type="button"
              onClick={() => setSetFilter(f.key)}
              className={`px-2.5 py-1 rounded-md text-xs font-medium border transition-colors ${
                setFilter === f.key
                  ? 'bg-primary text-primary-foreground border-primary'
                  : 'bg-background text-muted-foreground border-border hover:bg-accent'
              }`}
            >
              {f.label}
            </button>
          ))}
        </div>

        {/* Header stays put while the body scrolls, so column meaning survives a long list. */}
        <div className="rounded-md border max-h-[28rem] overflow-auto">
          <table className="w-full text-sm">
            <thead className="sticky top-0 z-10 bg-muted text-muted-foreground">
              <tr>
                <SortHeader label="Set" k="setCode" />
                <SortHeader label="Name" k="name" />
                <SortHeader label="Cards" k="cardCount" className="text-right" />
                <SortHeader label="Images" k="imageCount" className="text-right" />
                <th className="px-3 py-2 text-right font-medium">Purge</th>
              </tr>
            </thead>
            <tbody className="divide-y">
              {filteredSets.map(r => (
                <tr key={r.setCode} className="hover:bg-accent/40">
                  <td className="px-3 py-2 font-mono font-medium">{r.setCode.toUpperCase()}</td>
                  <td className="px-3 py-2">
                    {r.name ?? <span className="italic text-muted-foreground">no metadata</span>}
                  </td>
                  <td className={`px-3 py-2 text-right tabular-nums ${r.cardCount === 0 ? 'text-destructive' : ''}`}>
                    {r.cardCount}
                  </td>
                  <td className={`px-3 py-2 text-right tabular-nums ${r.imageCount === 0 ? 'text-destructive' : ''}`}>
                    {r.imageCount}
                  </td>
                  <td className="px-3 py-2 text-right whitespace-nowrap">
                    <Button variant="ghost" size="sm" className="h-7 px-2"
                      disabled={r.imageCount === 0}
                      title="Purge this set's images"
                      onClick={() => purgeImages({ setCode: r.setCode })}>
                      <Image className="h-3.5 w-3.5" />
                    </Button>
                    <Button variant="ghost" size="sm" className="h-7 px-2 text-destructive hover:text-destructive"
                      disabled={r.cardCount === 0}
                      title="Purge this set's metadata"
                      onClick={() => purgeMetadata({ setCode: r.setCode })}>
                      <Database className="h-3.5 w-3.5" />
                    </Button>
                  </td>
                </tr>
              ))}
              {filteredSets.length === 0 && (
                <tr>
                  <td colSpan={5} className="px-3 py-6 text-center text-muted-foreground">
                    {summary?.sets.length ? 'No sets match this filter.' : 'No sets stored.'}
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </section>

      {/* ---- Sealed products, their own table ---- */}
      <section className="space-y-3">
        <div className="flex items-center gap-2">
          <Package className="h-5 w-5 text-muted-foreground" />
          <h2 className="text-lg font-medium">Sealed Products</h2>
          <span className="text-sm text-muted-foreground">
            {filteredSealed.length} of {summary?.sealedProducts.length ?? 0}
          </span>
        </div>

        <div className="flex flex-wrap items-center gap-2">
          <Input
            value={sealedSearch}
            onChange={e => setSealedSearch(e.target.value)}
            placeholder="Search product name or id..."
            className="h-8 w-64"
          />
          {(['all', 'missing-images', 'complete'] as const).map(k => (
            <button
              key={k}
              type="button"
              onClick={() => setSealedFilter(k)}
              className={`px-2.5 py-1 rounded-md text-xs font-medium border transition-colors ${
                sealedFilter === k
                  ? 'bg-primary text-primary-foreground border-primary'
                  : 'bg-background text-muted-foreground border-border hover:bg-accent'
              }`}
            >
              {k === 'all' ? 'All' : k === 'missing-images' ? 'Missing images' : 'Has images'}
            </button>
          ))}
        </div>

        <div className="rounded-md border max-h-[28rem] overflow-auto">
          <table className="w-full text-sm">
            <thead className="sticky top-0 z-10 bg-muted text-muted-foreground">
              <tr>
                <th className="px-3 py-2 text-left font-medium">Product</th>
                <th className="px-3 py-2 text-left font-medium">Identifier</th>
                <th className="px-3 py-2 text-left font-medium">Set</th>
                <th className="px-3 py-2 text-right font-medium">Images</th>
              </tr>
            </thead>
            <tbody className="divide-y">
              {filteredSealed.map(r => (
                <tr key={r.identifier} className="hover:bg-accent/40">
                  <td className="px-3 py-2">{r.name}</td>
                  <td className="px-3 py-2 font-mono text-xs text-muted-foreground">{r.identifier}</td>
                  <td className="px-3 py-2 font-mono text-xs">{r.setCode?.toUpperCase() ?? '-'}</td>
                  <td className={`px-3 py-2 text-right tabular-nums ${r.imageCount === 0 ? 'text-destructive' : ''}`}>
                    {r.imageCount}
                  </td>
                </tr>
              ))}
              {filteredSealed.length === 0 && (
                <tr>
                  <td colSpan={4} className="px-3 py-6 text-center text-muted-foreground">
                    {summary?.sealedProducts.length ? 'No products match this filter.' : 'No sealed products stored.'}
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </section>

      <ConfirmDialog
        open={confirm.open}
        onOpenChange={(v) => setConfirm(prev => ({ ...prev, open: v }))}
        title={confirm.title}
        description={confirm.description}
        confirmLabel="Confirm"
        destructive
        onConfirm={confirm.onConfirm}
      />
    </div>
  )
}
