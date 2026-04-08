import { useEffect, useMemo, useState } from 'react'

// ---- Types ------------------------------------------------------------------

interface ContentTypeBreakdown {
  contentType: string
  totalValue: number
  totalProfitLoss: number
  count: number
}

interface MetricsResult {
  totalValue: number
  totalProfitLoss: number
  totalCardCount: number
  serializedCount: number
  slabCount: number
  sealedProductCount: number
  sealedProductValue: number
  byContentType: ContentTypeBreakdown[]
}

interface SetCompletion {
  setCode: string
  setName: string
  ownedCount: number
  totalCards: number
  percentage: number
  totalValue: number | null
  totalProfitLoss: number | null
  releaseDate: string | null
}

type CompletionStatus = 'all' | 'completed' | 'in-progress' | 'unstarted'
type CompletionSort = 'missing' | 'owned' | 'age' | 'code' | 'name'

// ---- Helpers ----------------------------------------------------------------

function fmt(v: number | null | undefined) {
  if (v == null) return '-'
  return `$${v.toFixed(2)}`
}

function fmtPl(v: number | null | undefined) {
  if (v == null) return '-'
  const sign = v >= 0 ? '+' : ''
  return `${sign}$${v.toFixed(2)}`
}

function plColor(v: number | null | undefined) {
  if (v == null) return 'text-muted-foreground'
  if (v > 0) return 'text-green-600'
  if (v < 0) return 'text-red-600'
  return 'text-muted-foreground'
}

const CONTENT_TYPE_LABELS: Record<string, string> = {
  cards: 'Cards',
  serialized: 'Serialized',
  slabs: 'Slabs',
  sealed: 'Sealed Product',
}

// ---- Stat card --------------------------------------------------------------

function StatCard({
  label,
  value,
  sub,
  subColor,
}: {
  label: string
  value: string
  sub?: string
  subColor?: string
}) {
  return (
    <div className="rounded-lg border p-4 space-y-1">
      <p className="text-xs text-muted-foreground uppercase tracking-wide">{label}</p>
      <p className="text-2xl font-semibold">{value}</p>
      {sub && <p className={`text-sm ${subColor ?? 'text-muted-foreground'}`}>{sub}</p>}
    </div>
  )
}

// ---- Set completion section -------------------------------------------------

const STATUS_OPTS: { value: CompletionStatus; label: string }[] = [
  { value: 'all', label: 'All' },
  { value: 'completed', label: 'Completed' },
  { value: 'in-progress', label: 'In Progress' },
  { value: 'unstarted', label: 'Unstarted' },
]

const SORT_OPTS: { value: CompletionSort; label: string }[] = [
  { value: 'missing', label: 'Cards Missing' },
  { value: 'owned', label: 'Cards Owned' },
  { value: 'age', label: 'Set Age' },
  { value: 'code', label: 'Set Code' },
  { value: 'name', label: 'Set Name' },
]

function SetCompletionSection({ sets }: { sets: SetCompletion[] }) {
  const [status, setStatus] = useState<CompletionStatus>('in-progress')
  const [sort, setSort] = useState<CompletionSort>('missing')

  const filtered = useMemo(() => {
    let result = sets
    if (status === 'completed') result = result.filter(s => s.percentage >= 100)
    else if (status === 'in-progress') result = result.filter(s => s.ownedCount > 0 && s.percentage < 100)
    else if (status === 'unstarted') result = result.filter(s => s.ownedCount === 0)

    return [...result].sort((a, b) => {
      switch (sort) {
        case 'missing': return (a.totalCards - a.ownedCount) - (b.totalCards - b.ownedCount)
        case 'owned': return b.ownedCount - a.ownedCount
        case 'age': {
          const da = a.releaseDate ?? ''
          const db = b.releaseDate ?? ''
          return da < db ? 1 : da > db ? -1 : 0
        }
        case 'code': return a.setCode.localeCompare(b.setCode)
        case 'name': return a.setName.localeCompare(b.setName)
        default: return 0
      }
    })
  }, [sets, status, sort])

  return (
    <div>
      <div className="flex flex-wrap items-center justify-between gap-2 mb-3">
        <h2 className="text-sm font-medium text-muted-foreground uppercase tracking-wide">
          Set Completion
        </h2>
        <div className="flex items-center gap-2">
          <div className="flex gap-1">
            {STATUS_OPTS.map(opt => (
              <button
                key={opt.value}
                type="button"
                onClick={() => setStatus(opt.value)}
                className={`px-2.5 py-1 rounded-md text-xs font-medium border transition-colors ${
                  status === opt.value
                    ? 'bg-primary text-primary-foreground border-primary'
                    : 'bg-background text-muted-foreground border-border hover:bg-accent'
                }`}
              >
                {opt.label}
              </button>
            ))}
          </div>
          <select
            value={sort}
            onChange={e => setSort(e.target.value as CompletionSort)}
            className="text-xs border rounded-md px-2 py-1 bg-background text-foreground"
          >
            {SORT_OPTS.map(opt => (
              <option key={opt.value} value={opt.value}>{opt.label}</option>
            ))}
          </select>
        </div>
      </div>

      {filtered.length === 0 ? (
        <p className="text-sm text-muted-foreground">No sets match this filter.</p>
      ) : (
        <div className="space-y-2">
          {filtered.map(s => (
            <div key={s.setCode} className="rounded-md border px-4 py-3">
              <div className="flex items-center justify-between mb-1.5">
                <div>
                  <span className="font-medium">{s.setName}</span>
                  <span className="text-xs text-muted-foreground ml-2">{s.setCode}</span>
                  {s.releaseDate && (
                    <span className="text-xs text-muted-foreground ml-2">{s.releaseDate.slice(0, 4)}</span>
                  )}
                </div>
                <span className="text-sm text-muted-foreground shrink-0">
                  {s.ownedCount}/{s.totalCards} ({s.percentage}%)
                </span>
              </div>
              <div className="w-full bg-muted rounded-full h-1.5">
                <div
                  className="bg-primary h-1.5 rounded-full transition-all"
                  style={{ width: `${Math.min(100, s.percentage)}%` }}
                />
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}

// ---- Main page --------------------------------------------------------------

export function DashboardPage() {
  const [metrics, setMetrics] = useState<MetricsResult | null>(null)
  const [completion, setCompletion] = useState<SetCompletion[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    async function load() {
      setLoading(true)
      try {
        const [mRes, cRes] = await Promise.all([
          fetch('/api/collection/metrics'),
          fetch('/api/collection/completion'),
        ])
        if (mRes.ok) setMetrics(await mRes.json())
        if (cRes.ok) setCompletion(await cRes.json())
      } finally {
        setLoading(false)
      }
    }
    load()
  }, [])

  if (loading) {
    return (
      <div>
        <h1 className="text-2xl font-semibold mb-4">Dashboard</h1>
        <p className="text-muted-foreground text-sm">Loading...</p>
      </div>
    )
  }

  if (!metrics) {
    return (
      <div>
        <h1 className="text-2xl font-semibold mb-4">Dashboard</h1>
        <p className="text-muted-foreground text-sm">No data available.</p>
      </div>
    )
  }

  const isEmpty =
    metrics.totalCardCount === 0 &&
    metrics.serializedCount === 0 &&
    metrics.slabCount === 0 &&
    metrics.sealedProductCount === 0

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-semibold">Dashboard</h1>

      {isEmpty ? (
        <p className="text-muted-foreground text-sm">
          Your collection is empty. Add cards, sealed product, serialized cards, or slabs to get started.
        </p>
      ) : (
        <>
          {/* Portfolio summary */}
          <div>
            <h2 className="text-sm font-medium text-muted-foreground uppercase tracking-wide mb-3">
              Portfolio
            </h2>
            <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
              <StatCard
                label="Total Value"
                value={fmt(metrics.totalValue)}
                sub={`P/L: ${fmtPl(metrics.totalProfitLoss)}`}
                subColor={plColor(metrics.totalProfitLoss)}
              />
              <StatCard
                label="Cards"
                value={metrics.totalCardCount.toString()}
                sub={fmt(metrics.byContentType.find(b => b.contentType === 'cards')?.totalValue)}
              />
              <StatCard
                label="Serialized"
                value={metrics.serializedCount.toString()}
                sub={fmt(metrics.byContentType.find(b => b.contentType === 'serialized')?.totalValue)}
              />
              <StatCard
                label="Slabs"
                value={metrics.slabCount.toString()}
                sub={fmt(metrics.byContentType.find(b => b.contentType === 'slabs')?.totalValue)}
              />
            </div>
          </div>

          {/* Content type breakdown */}
          {metrics.byContentType.filter(b => b.count > 0).length > 0 && (
            <div>
              <h2 className="text-sm font-medium text-muted-foreground uppercase tracking-wide mb-3">
                By Type
              </h2>
              <div className="rounded-md border overflow-hidden">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b bg-muted/50 text-muted-foreground">
                      <th className="px-4 py-2 text-left">Type</th>
                      <th className="px-4 py-2 text-right">Items</th>
                      <th className="px-4 py-2 text-right">Value</th>
                      <th className="px-4 py-2 text-right">P/L</th>
                    </tr>
                  </thead>
                  <tbody>
                    {metrics.byContentType
                      .filter(b => b.count > 0)
                      .map(b => (
                        <tr key={b.contentType} className="border-b last:border-0 hover:bg-muted/20">
                          <td className="px-4 py-2 font-medium">
                            {CONTENT_TYPE_LABELS[b.contentType] ?? b.contentType}
                          </td>
                          <td className="px-4 py-2 text-right">{b.count}</td>
                          <td className="px-4 py-2 text-right">{fmt(b.totalValue)}</td>
                          <td className={`px-4 py-2 text-right ${plColor(b.totalProfitLoss)}`}>
                            {fmtPl(b.totalProfitLoss)}
                          </td>
                        </tr>
                      ))}
                    <tr className="border-t bg-muted/30 font-semibold">
                      <td className="px-4 py-2">Total</td>
                      <td className="px-4 py-2 text-right">
                        {metrics.byContentType.reduce((s, b) => s + b.count, 0)}
                      </td>
                      <td className="px-4 py-2 text-right">{fmt(metrics.totalValue)}</td>
                      <td className={`px-4 py-2 text-right ${plColor(metrics.totalProfitLoss)}`}>
                        {fmtPl(metrics.totalProfitLoss)}
                      </td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>
          )}

          {/* Set completion */}
          {completion.length > 0 && <SetCompletionSection sets={completion} />}
        </>
      )}
    </div>
  )
}
