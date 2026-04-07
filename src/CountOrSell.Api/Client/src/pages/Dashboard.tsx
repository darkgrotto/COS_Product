import { useEffect, useState } from 'react'

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
}

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
        if (cRes.ok) {
          const all: SetCompletion[] = await cRes.json()
          // Only show sets where user owns something, sorted by % completion desc
          setCompletion(all.filter(s => s.ownedCount > 0).slice(0, 8))
        }
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
          {completion.length > 0 && (
            <div>
              <h2 className="text-sm font-medium text-muted-foreground uppercase tracking-wide mb-3">
                Set Completion
              </h2>
              <div className="space-y-2">
                {completion.map(s => (
                  <div key={s.setCode} className="rounded-md border px-4 py-3">
                    <div className="flex items-center justify-between mb-1.5">
                      <div>
                        <span className="font-medium">{s.setName}</span>
                        <span className="text-xs text-muted-foreground ml-2">{s.setCode}</span>
                      </div>
                      <span className="text-sm text-muted-foreground">
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
            </div>
          )}
        </>
      )}
    </div>
  )
}
