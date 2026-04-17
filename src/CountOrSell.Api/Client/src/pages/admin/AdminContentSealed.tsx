import { useState, useEffect } from 'react'
import { ArrowLeft, ChevronUp, ChevronDown, ChevronsUpDown, Search, Package } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
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

interface SealedProductSummary {
  identifier: string
  name: string
  setCode: string | null    // null for products not linked to a set
  categorySlug: string | null
  subTypeSlug: string | null
  currentMarketValue: number | null
  updatedAt: string
  hasImage: boolean
}

interface SealedProductDetail {
  identifier: string
  name: string
  setCode: string | null
  categorySlug: string | null
  subTypeSlug: string | null
  currentMarketValue: number | null
  updatedAt: string
  imageUrl: string
  supplementalImageUrl: string
}

interface TaxonomyCategory {
  slug: string
  displayName: string
  subTypes: Array<{ slug: string; displayName: string }>
}

// ---- Helpers ----

function fmtDate(iso: string) {
  return new Date(iso).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })
}

type SortDir = 'asc' | 'desc'
type SealedSortField = 'name' | 'setCode' | 'identifier' | 'updatedAt'

function SortIcon({ active, dir }: { active: boolean; dir: SortDir }) {
  if (!active) return <ChevronsUpDown className="h-3 w-3 ml-1 inline opacity-40" />
  return dir === 'asc'
    ? <ChevronUp className="h-3 w-3 ml-1 inline" />
    : <ChevronDown className="h-3 w-3 ml-1 inline" />
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

// ---- Image Thumbnail ----

function ProductThumbnail({ identifier, name }: { identifier: string; name: string }) {
  const [failed, setFailed] = useState(false)

  if (failed) {
    return (
      <div className="w-12 h-12 bg-muted rounded flex items-center justify-center border">
        <Package className="h-5 w-5 text-muted-foreground" />
      </div>
    )
  }

  return (
    <img
      src={`/api/images/sealed/${identifier}.jpg`}
      alt={name}
      className="w-12 h-12 object-contain rounded border bg-muted block"
      onError={() => setFailed(true)}
    />
  )
}

// ---- Detail View ----

function SealedDetailView({
  identifier,
  onBack,
  taxonomyMap,
}: {
  identifier: string
  onBack: () => void
  taxonomyMap: Map<string, string>
}) {
  const [detail, setDetail] = useState<SealedProductDetail | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [frontFailed, setFrontFailed] = useState(false)
  const [suppFailed, setSuppFailed] = useState(false)

  useEffect(() => {
    setDetail(null)
    setError(null)
    setFrontFailed(false)
    setSuppFailed(false)
    fetch(`/api/sealed-products/${encodeURIComponent(identifier)}`, { credentials: 'include' })
      .then(res => {
        if (!res.ok) throw new Error(`HTTP ${res.status}`)
        return res.json() as Promise<SealedProductDetail>
      })
      .then(setDetail)
      .catch((e: Error) => setError(e.message))
  }, [identifier])

  return (
    <div className="space-y-3">
      <Button variant="ghost" size="sm" onClick={onBack} className="gap-1">
        <ArrowLeft className="h-4 w-4" />
        Back to Sealed Products
      </Button>

      {error && <p className="text-sm text-destructive">Error loading product: {error}</p>}
      {!error && !detail && <p className="text-sm text-muted-foreground">Loading...</p>}

      {detail && (
        <div className="border rounded-md p-6 space-y-5">
          {/* Name */}
          <div>
            <div className="text-2xl font-bold mb-1">{detail.name}</div>
            {detail.setCode && (
              <div className="flex items-center gap-2 text-sm text-muted-foreground">
                <SetSymbol setCode={detail.setCode} />
                <span className="font-mono">{detail.setCode}</span>
              </div>
            )}
          </div>

          {/* Images */}
          <div className="flex gap-6 items-start">
            <div className="flex flex-col items-center gap-1">
              {!frontFailed ? (
                <img
                  src={detail.imageUrl}
                  alt={`${detail.name} - Front`}
                  className="w-44 rounded-lg shadow-md block"
                  onError={() => setFrontFailed(true)}
                />
              ) : (
                <div className="w-44 h-44 bg-muted rounded-lg border flex items-center justify-center">
                  <Package className="h-10 w-10 text-muted-foreground" />
                </div>
              )}
              <span className="text-xs text-muted-foreground">Front</span>
            </div>
            {!suppFailed && (
              <div className="flex flex-col items-center gap-1">
                <img
                  src={detail.supplementalImageUrl}
                  alt={`${detail.name} - Supplemental`}
                  className="w-44 rounded-lg shadow-md block"
                  onError={() => setSuppFailed(true)}
                />
                <span className="text-xs text-muted-foreground">Supplemental</span>
              </div>
            )}
          </div>

          {/* Meta */}
          <div className="grid grid-cols-[auto_1fr] gap-x-6 gap-y-1.5 text-sm max-w-md">
            <span className="text-muted-foreground font-medium">Product ID</span>
            <span className="font-mono">{detail.identifier}</span>
            <span className="text-muted-foreground font-medium">Set</span>
            <span>
              {detail.setCode
                ? <span className="font-mono">{detail.setCode}</span>
                : <span className="text-muted-foreground">None</span>}
            </span>
            <span className="text-muted-foreground font-medium">Category</span>
            <span>
              {detail.categorySlug
                ? (taxonomyMap.get(detail.categorySlug) ?? detail.categorySlug)
                : <span className="text-muted-foreground">None</span>}
            </span>
            <span className="text-muted-foreground font-medium">Sub-Type</span>
            <span>
              {detail.subTypeSlug
                ? (taxonomyMap.get(detail.subTypeSlug) ?? detail.subTypeSlug)
                : <span className="text-muted-foreground">None</span>}
            </span>
            {detail.currentMarketValue != null && (
              <>
                <span className="text-muted-foreground font-medium">Market Value</span>
                <span>${detail.currentMarketValue.toFixed(2)}</span>
              </>
            )}
            <span className="text-muted-foreground font-medium">Last Updated</span>
            <span className="text-muted-foreground">{fmtDate(detail.updatedAt)}</span>
          </div>
        </div>
      )}
    </div>
  )
}

// ---- Sealed Products Table ----

function SealedTable({
  onSelectProduct,
  taxonomyMap,
}: {
  onSelectProduct: (identifier: string) => void
  taxonomyMap: Map<string, string>
}) {
  const [products, setProducts] = useState<SealedProductSummary[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [filter, setFilter] = useState('')
  const [setLinkedFilter, setSetLinkedFilter] = useState<'linked' | 'unlinked' | null>(null)
  const [sortField, setSortField] = useState<SealedSortField>('name')
  const [sortDir, setSortDir] = useState<SortDir>('asc')

  useEffect(() => {
    fetch('/api/sealed-products/all', { credentials: 'include' })
      .then(res => {
        if (!res.ok) throw new Error(`HTTP ${res.status}`)
        return res.json() as Promise<SealedProductSummary[]>
      })
      .then(setProducts)
      .catch((e: Error) => setError(e.message))
  }, [])

  function handleSort(field: SealedSortField) {
    if (sortField === field) {
      setSortDir(d => d === 'asc' ? 'desc' : 'asc')
    } else {
      setSortField(field)
      setSortDir('asc')
    }
  }

  const q = filter.toLowerCase()
  let visible = products ?? []
  if (setLinkedFilter === 'linked') visible = visible.filter(p => p.setCode != null)
  if (setLinkedFilter === 'unlinked') visible = visible.filter(p => p.setCode == null)
  if (filter) visible = visible.filter(p =>
    p.identifier.toLowerCase().includes(q) ||
    p.name.toLowerCase().includes(q) ||
    (p.setCode?.toLowerCase().includes(q) ?? false)
  )

  visible = [...visible].sort((a, b) => {
    let cmp = 0
    if (sortField === 'name') cmp = a.name.localeCompare(b.name)
    else if (sortField === 'setCode') cmp = (a.setCode ?? '').localeCompare(b.setCode ?? '')
    else if (sortField === 'identifier') cmp = a.identifier.localeCompare(b.identifier)
    else if (sortField === 'updatedAt') cmp = a.updatedAt.localeCompare(b.updatedAt)
    return sortDir === 'asc' ? cmp : -cmp
  })

  return (
    <div className="space-y-3">
      {/* Filters */}
      <div className="flex flex-wrap items-center gap-2">
        <div className="relative shrink-0">
          <Search className="absolute left-2 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-muted-foreground" />
          <Input
            className="pl-7 h-7 text-xs w-52"
            placeholder="Search sealed products..."
            value={filter}
            onChange={e => setFilter(e.target.value)}
          />
        </div>
        <FilterChip
          label="Set Linked"
          active={setLinkedFilter === 'linked'}
          onClick={() => setSetLinkedFilter(setLinkedFilter === 'linked' ? null : 'linked')}
        />
        <FilterChip
          label="No Set"
          active={setLinkedFilter === 'unlinked'}
          onClick={() => setSetLinkedFilter(setLinkedFilter === 'unlinked' ? null : 'unlinked')}
        />
        {products && (
          <span className="ml-auto text-xs text-muted-foreground whitespace-nowrap">
            {visible.length}{visible.length !== products.length ? `/${products.length}` : ''}{' '}
            product{products.length !== 1 ? 's' : ''}
          </span>
        )}
      </div>

      {/* Table */}
      {!products && !error && <p className="text-sm text-muted-foreground">Loading sealed products...</p>}
      {error && <p className="text-sm text-destructive">Error: {error}</p>}
      {products && (
        <div className="border rounded-md overflow-auto">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead className="w-16" />
                <TableHead
                  className="cursor-pointer select-none"
                  onClick={() => handleSort('name')}
                >
                  Name <SortIcon active={sortField === 'name'} dir={sortDir} />
                </TableHead>
                <TableHead
                  className="cursor-pointer select-none whitespace-nowrap"
                  onClick={() => handleSort('setCode')}
                >
                  Set <SortIcon active={sortField === 'setCode'} dir={sortDir} />
                </TableHead>
                <TableHead>Category</TableHead>
                <TableHead>Sub-Type</TableHead>
                <TableHead
                  className="cursor-pointer select-none whitespace-nowrap"
                  onClick={() => handleSort('identifier')}
                >
                  Product ID <SortIcon active={sortField === 'identifier'} dir={sortDir} />
                </TableHead>
                <TableHead className="text-right whitespace-nowrap">Market Value</TableHead>
                <TableHead
                  className="cursor-pointer select-none whitespace-nowrap"
                  onClick={() => handleSort('updatedAt')}
                >
                  Updated <SortIcon active={sortField === 'updatedAt'} dir={sortDir} />
                </TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {visible.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={8} className="text-center text-muted-foreground">
                    {filter || setLinkedFilter ? 'No products match the filter.' : 'No sealed products found.'}
                  </TableCell>
                </TableRow>
              ) : (
                visible.map(p => (
                  <TableRow
                    key={p.identifier}
                    className="cursor-pointer hover:bg-accent"
                    onClick={() => onSelectProduct(p.identifier)}
                  >
                    <TableCell className="w-16 p-2">
                      {p.hasImage ? (
                        <ProductThumbnail identifier={p.identifier} name={p.name} />
                      ) : (
                        <div className="w-12 h-12 bg-muted rounded border flex items-center justify-center">
                          <Package className="h-5 w-5 text-muted-foreground" />
                        </div>
                      )}
                    </TableCell>
                    <TableCell className="font-medium">{p.name}</TableCell>
                    <TableCell>
                      {p.setCode ? (
                        <span className="flex items-center gap-1.5">
                          <SetSymbol setCode={p.setCode} className="text-base leading-none" />
                          <span className="font-mono text-xs">{p.setCode}</span>
                        </span>
                      ) : (
                        <span className="text-muted-foreground">-</span>
                      )}
                    </TableCell>
                    <TableCell className="text-sm text-muted-foreground">
                      {p.categorySlug
                        ? (taxonomyMap.get(p.categorySlug) ?? p.categorySlug)
                        : <span className="text-muted-foreground">-</span>}
                    </TableCell>
                    <TableCell className="text-sm text-muted-foreground">
                      {p.subTypeSlug
                        ? (taxonomyMap.get(p.subTypeSlug) ?? p.subTypeSlug)
                        : <span className="text-muted-foreground">-</span>}
                    </TableCell>
                    <TableCell className="font-mono text-xs">{p.identifier}</TableCell>
                    <TableCell className="text-right tabular-nums text-sm">
                      {p.currentMarketValue != null
                        ? `$${p.currentMarketValue.toFixed(2)}`
                        : <span className="text-muted-foreground">-</span>}
                    </TableCell>
                    <TableCell className="text-muted-foreground text-sm whitespace-nowrap">
                      {fmtDate(p.updatedAt)}
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

// ---- Main Export ----

export function AdminContentSealed() {
  const [selectedIdentifier, setSelectedIdentifier] = useState<string | null>(null)
  const [taxonomyMap, setTaxonomyMap] = useState<Map<string, string>>(new Map())

  useEffect(() => {
    fetch('/api/sealed-product-taxonomy/categories', { credentials: 'include' })
      .then(res => res.ok ? res.json() as Promise<TaxonomyCategory[]> : Promise.resolve([]))
      .then((cats: TaxonomyCategory[]) => {
        const map = new Map<string, string>()
        cats.forEach(c => {
          map.set(c.slug, c.displayName)
          c.subTypes.forEach(s => map.set(s.slug, s.displayName))
        })
        setTaxonomyMap(map)
      })
      .catch(() => {})
  }, [])

  if (selectedIdentifier) {
    return (
      <SealedDetailView
        identifier={selectedIdentifier}
        onBack={() => setSelectedIdentifier(null)}
        taxonomyMap={taxonomyMap}
      />
    )
  }

  return <SealedTable onSelectProduct={setSelectedIdentifier} taxonomyMap={taxonomyMap} />
}
