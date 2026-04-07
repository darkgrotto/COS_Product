import { useEffect, useState } from 'react'
import { Star, ExternalLink } from 'lucide-react'

// ---- Types ------------------------------------------------------------------

interface ReservedEntry {
  entryId: string
  cardIdentifier: string
  cardName: string
  setCode: string
  cardType: string | null
  treatment: string
  quantity: number
  condition: string
  autographed: boolean
  acquisitionPrice: number
  marketValue: number | null
}

// ---- Helpers ----------------------------------------------------------------

function fmt(v: number | null | undefined) {
  if (v == null) return '-'
  return `$${v.toFixed(2)}`
}

function fmtPl(v: number) {
  const sign = v >= 0 ? '+' : ''
  return `${sign}$${v.toFixed(2)}`
}

function plColor(v: number | null) {
  if (v == null) return 'text-muted-foreground'
  if (v > 0) return 'text-green-600'
  if (v < 0) return 'text-red-600'
  return 'text-muted-foreground'
}

// ---- Main page --------------------------------------------------------------

export function ReservedListPage() {
  const [entries, setEntries] = useState<ReservedEntry[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    fetch('/api/collection/reserved')
      .then(r => r.ok ? r.json() : [])
      .then((data: ReservedEntry[]) => setEntries(data))
      .finally(() => setLoading(false))
  }, [])

  const totalQty = entries.reduce((s, e) => s + e.quantity, 0)
  const totalValue = entries.reduce((s, e) => s + (e.marketValue ?? 0) * e.quantity, 0)
  const totalCost = entries.reduce((s, e) => s + e.acquisitionPrice * e.quantity, 0)
  const totalPl = totalValue - totalCost
  const hasValues = entries.some(e => e.marketValue != null)

  return (
    <div className="space-y-4">
      <div>
        <div className="flex items-center gap-2">
          <Star className="h-5 w-5 text-amber-500 fill-amber-500" />
          <h1 className="text-2xl font-semibold">Reserved List</h1>
        </div>
        {!loading && entries.length > 0 && (
          <p className="text-sm text-muted-foreground mt-0.5">
            {totalQty} card{totalQty !== 1 ? 's' : ''} across {entries.length} unique printings
            {hasValues && ` \u00b7 ${fmt(totalValue)} value`}
            {hasValues && (
              <span className={`ml-2 ${plColor(totalPl)}`}>
                {fmtPl(totalPl)} P/L
              </span>
            )}
          </p>
        )}
      </div>

      {loading ? (
        <p className="text-sm text-muted-foreground">Loading...</p>
      ) : entries.length === 0 ? (
        <div className="py-12 text-center space-y-2">
          <Star className="h-8 w-8 text-muted-foreground mx-auto" />
          <p className="text-muted-foreground">
            No Reserved List cards in your collection.
          </p>
          <p className="text-xs text-muted-foreground">
            Cards on the Magic: The Gathering Reserved List will never be reprinted.
          </p>
        </div>
      ) : (
        <div className="rounded-md border overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b bg-muted/50 text-muted-foreground">
                <th className="px-3 py-2 text-left w-10"></th>
                <th className="px-3 py-2 text-left">Card</th>
                <th className="px-3 py-2 text-left">Set</th>
                <th className="px-3 py-2 text-left">Type</th>
                <th className="px-3 py-2 text-left">Treatment</th>
                <th className="px-3 py-2 text-center">Qty</th>
                <th className="px-3 py-2 text-left">Cond.</th>
                <th className="px-3 py-2 text-right">Market</th>
                <th className="px-3 py-2 text-right">Paid</th>
                <th className="px-3 py-2 text-right">P/L</th>
              </tr>
            </thead>
            <tbody>
              {entries.map(e => {
                const pl = e.marketValue != null
                  ? (e.marketValue - e.acquisitionPrice) * e.quantity
                  : null
                return (
                  <tr key={e.entryId} className="border-b last:border-0 hover:bg-muted/20">
                    <td className="px-3 py-2">
                      <img
                        src={`/api/images/cards/${e.cardIdentifier.toLowerCase()}.jpg`}
                        alt=""
                        className="h-8 w-6 rounded object-cover bg-muted"
                        loading="lazy"
                        onError={ev => { (ev.target as HTMLImageElement).style.display = 'none' }}
                      />
                    </td>
                    <td className="px-3 py-2">
                      <div className="font-medium leading-tight">{e.cardName}</div>
                      <div className="text-xs text-muted-foreground">{e.cardIdentifier}</div>
                    </td>
                    <td className="px-3 py-2 font-mono text-xs">{e.setCode}</td>
                    <td className="px-3 py-2 text-muted-foreground text-xs max-w-36 truncate">
                      {e.cardType || '-'}
                    </td>
                    <td className="px-3 py-2">{e.treatment}</td>
                    <td className="px-3 py-2 text-center tabular-nums">{e.quantity}</td>
                    <td className="px-3 py-2">
                      {e.condition}{e.autographed ? ' - Auto' : ''}
                    </td>
                    <td className="px-3 py-2 text-right tabular-nums">{fmt(e.marketValue)}</td>
                    <td className="px-3 py-2 text-right tabular-nums">{fmt(e.acquisitionPrice)}</td>
                    <td className={`px-3 py-2 text-right tabular-nums ${plColor(pl)}`}>
                      {pl != null ? fmtPl(pl) : '-'}
                    </td>
                  </tr>
                )
              })}
            </tbody>
            {hasValues && (
              <tfoot>
                <tr className="border-t bg-muted/20 font-semibold">
                  <td colSpan={7} className="px-3 py-2">Total</td>
                  <td className="px-3 py-2 text-right tabular-nums">{fmt(totalValue)}</td>
                  <td className="px-3 py-2 text-right tabular-nums">{fmt(totalCost)}</td>
                  <td className={`px-3 py-2 text-right tabular-nums ${plColor(totalPl)}`}>
                    {fmtPl(totalPl)}
                  </td>
                </tr>
              </tfoot>
            )}
          </table>
        </div>
      )}
    </div>
  )
}
