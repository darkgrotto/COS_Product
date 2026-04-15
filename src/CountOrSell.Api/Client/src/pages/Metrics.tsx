import { useEffect, useState, useCallback } from 'react'
import { X, Download } from 'lucide-react'
import { useAuth } from '@/contexts/AuthContext'
import { usePreferences } from '@/contexts/PreferencesContext'
import { Button } from '@/components/ui/button'
import { Label } from '@/components/ui/label'
import {
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue,
} from '@/components/ui/select'
import { SetSymbol } from '@/components/ui/SetSymbol'
import {
  SortTh, ToggleChip, sortTreatments, fmt,
  COLORS, CARD_TYPES,
} from '@/components/cards/CardDialogs'
import type { SortDir, Treatment } from '@/components/cards/CardDialogs'

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

interface CardSet { code: string; name: string }

interface UserRow {
  id: string
  username: string
  displayName: string
  role: string
  state: string
}

interface Filters {
  setCode: string
  treatment: string
  condition: string
  autographed: string
  color: string
  cardType: string
  isReserved: boolean
  hasPhyrexianMana: boolean
  hasHybridMana: boolean
}

// ---- Constants --------------------------------------------------------------

const CONDITIONS = ['NM', 'LP', 'MP', 'HP', 'DMG'] as const

const BLANK_FILTERS: Filters = {
  setCode: '', treatment: '', condition: '', autographed: '', color: '', cardType: '',
  isReserved: false, hasPhyrexianMana: false, hasHybridMana: false,
}

const CONTENT_TYPE_LABELS: Record<string, string> = {
  cards: 'Cards',
  serialized: 'Serialized',
  slabs: 'Slabs',
  sealed: 'Sealed Product',
}

// ---- Helpers ----------------------------------------------------------------

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

// ---- Stat card --------------------------------------------------------------

function StatCard({ label, value, sub, subColor }: {
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

// ---- Filter panel -----------------------------------------------------------

function FiltersPanel({
  filters, sets, treatments, onChange, onClear,
}: {
  filters: Filters
  sets: CardSet[]
  treatments: Treatment[]
  onChange: (f: Filters) => void
  onClear: () => void
}) {
  const active = filters.setCode || filters.treatment || filters.condition ||
    filters.autographed || filters.color || filters.cardType ||
    filters.isReserved || filters.hasPhyrexianMana || filters.hasHybridMana

  return (
    <div className="space-y-2">
      <div className="flex flex-wrap items-end gap-3">
        <div className="grid gap-1">
          <Label className="text-xs text-muted-foreground">Set</Label>
          <Select
            value={filters.setCode || '__all__'}
            onValueChange={v => onChange({ ...filters, setCode: v === '__all__' ? '' : v })}
          >
            <SelectTrigger className="h-8 w-44 text-xs"><SelectValue placeholder="All sets" /></SelectTrigger>
            <SelectContent className="max-h-64">
              <SelectItem value="__all__">All sets</SelectItem>
              {sets.map(s => (
                <SelectItem key={s.code} value={s.code.toLowerCase()}>
                  {s.code.toUpperCase()} - {s.name}
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
            <SelectTrigger className="h-8 w-36 text-xs"><SelectValue /></SelectTrigger>
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
            <SelectTrigger className="h-8 w-28 text-xs"><SelectValue /></SelectTrigger>
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
            <SelectTrigger className="h-8 w-28 text-xs"><SelectValue /></SelectTrigger>
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
      <div className="flex flex-wrap gap-1.5 items-center">
        <span className="text-xs text-muted-foreground">Color:</span>
        {COLORS.map(col => (
          <ToggleChip
            key={col.key}
            active={filters.color === col.key}
            onClick={() => onChange({ ...filters, color: filters.color === col.key ? '' : col.key })}
          >
            {col.label}
          </ToggleChip>
        ))}
        <span className="text-xs text-muted-foreground ml-3">Type:</span>
        {CARD_TYPES.map(t => (
          <ToggleChip
            key={t}
            active={filters.cardType === t}
            onClick={() => onChange({ ...filters, cardType: filters.cardType === t ? '' : t })}
          >
            {t}
          </ToggleChip>
        ))}
        <span className="text-xs text-muted-foreground ml-3">|</span>
        <ToggleChip
          active={filters.isReserved}
          onClick={() => onChange({ ...filters, isReserved: !filters.isReserved })}
        >
          Reserved List
        </ToggleChip>
        <ToggleChip
          active={filters.hasPhyrexianMana}
          onClick={() => onChange({ ...filters, hasPhyrexianMana: !filters.hasPhyrexianMana })}
        >
          Phi Mana
        </ToggleChip>
        <ToggleChip
          active={filters.hasHybridMana}
          onClick={() => onChange({ ...filters, hasHybridMana: !filters.hasHybridMana })}
        >
          Hybrid Mana
        </ToggleChip>
      </div>
    </div>
  )
}

// ---- Set breakdown table ----------------------------------------------------

type SetSortKey = 'name' | 'code' | 'value' | 'pl' | 'completion' | 'release'

function SetBreakdownSection({
  completion,
  activeSetCode,
  regularOnly,
  onRegularOnlyChange,
  onSetClick,
}: {
  completion: SetCompletion[]
  activeSetCode: string
  regularOnly: boolean
  onRegularOnlyChange: (v: boolean) => void
  onSetClick: (code: string) => void
}) {
  const [sortKey, setSortKey] = useState<SetSortKey>('value')
  const [sortDir, setSortDir] = useState<SortDir>('desc')

  function handleSort(key: string) {
    const k = key as SetSortKey
    if (sortKey === k) {
      setSortDir(d => d === 'asc' ? 'desc' : 'asc')
    } else {
      setSortKey(k)
      setSortDir(k === 'name' || k === 'code' ? 'asc' : 'desc')
    }
  }

  const sorted = [...completion].sort((a, b) => {
    let cmp = 0
    switch (sortKey) {
      case 'name': cmp = a.setName.localeCompare(b.setName); break
      case 'code': cmp = a.setCode.localeCompare(b.setCode); break
      case 'value': cmp = (a.totalValue ?? -1) - (b.totalValue ?? -1); break
      case 'pl': cmp = (a.totalProfitLoss ?? 0) - (b.totalProfitLoss ?? 0); break
      case 'completion': cmp = a.percentage - b.percentage; break
      case 'release': cmp = (a.releaseDate ?? '').localeCompare(b.releaseDate ?? ''); break
    }
    return sortDir === 'asc' ? cmp : -cmp
  })

  return (
    <div>
      <div className="flex items-center justify-between mb-3">
        <h2 className="text-sm font-medium text-muted-foreground uppercase tracking-wide">
          By Set
        </h2>
        <button
          type="button"
          className="flex items-center gap-2 text-xs text-muted-foreground hover:text-foreground"
          onClick={() => onRegularOnlyChange(!regularOnly)}
        >
          <span
            role="switch"
            aria-checked={regularOnly}
            className={`relative inline-flex h-4 w-7 shrink-0 rounded-full border-2 border-transparent transition-colors ${regularOnly ? 'bg-primary' : 'bg-input'}`}
          >
            <span
              className={`pointer-events-none block h-3 w-3 rounded-full bg-background shadow ring-0 transition-transform ${regularOnly ? 'translate-x-3' : 'translate-x-0'}`}
            />
          </span>
          Regular only
        </button>
      </div>

      <div className="rounded-md border overflow-x-auto">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b bg-muted/50 text-muted-foreground">
              <SortTh label="Set" sortKey="name" current={sortKey} dir={sortDir} onSort={handleSort} className="text-left" />
              <SortTh label="Code" sortKey="code" current={sortKey} dir={sortDir} onSort={handleSort} className="text-left" />
              <SortTh label="Completion" sortKey="completion" current={sortKey} dir={sortDir} onSort={handleSort} className="text-right" />
              <SortTh label="Value" sortKey="value" current={sortKey} dir={sortDir} onSort={handleSort} className="text-right" />
              <SortTh label="P/L" sortKey="pl" current={sortKey} dir={sortDir} onSort={handleSort} className="text-right" />
              <SortTh label="Released" sortKey="release" current={sortKey} dir={sortDir} onSort={handleSort} className="text-right" />
            </tr>
          </thead>
          <tbody>
            {sorted.length === 0 ? (
              <tr>
                <td colSpan={6} className="px-4 py-8 text-center text-muted-foreground">
                  No sets in collection.
                </td>
              </tr>
            ) : (
              sorted.map(s => {
                const isActive = s.setCode.toLowerCase() === activeSetCode
                return (
                  <tr
                    key={s.setCode}
                    className={`border-b last:border-0 hover:bg-muted/20 cursor-pointer ${isActive ? 'bg-primary/5 ring-1 ring-inset ring-primary/20' : ''}`}
                    onClick={() => onSetClick(isActive ? '' : s.setCode.toLowerCase())}
                  >
                    <td className="px-4 py-2.5">
                      <span className="flex items-center gap-2">
                        <SetSymbol setCode={s.setCode.toLowerCase()} className="text-base" />
                        <span className="font-medium">{s.setName}</span>
                      </span>
                    </td>
                    <td className="px-4 py-2.5 font-mono text-xs text-muted-foreground">
                      {s.setCode}
                    </td>
                    <td className="px-4 py-2.5 text-right">
                      <div className="flex items-center justify-end gap-2">
                        <span className="text-muted-foreground text-xs whitespace-nowrap">
                          {s.ownedCount}/{s.totalCards}
                        </span>
                        <div className="w-14 bg-muted rounded-full h-1.5 hidden sm:block shrink-0">
                          <div
                            className="bg-primary h-1.5 rounded-full"
                            style={{ width: `${Math.min(100, s.percentage)}%` }}
                          />
                        </div>
                        <span className="text-xs tabular-nums">{s.percentage}%</span>
                      </div>
                    </td>
                    <td className="px-4 py-2.5 text-right tabular-nums">{fmt(s.totalValue)}</td>
                    <td className={`px-4 py-2.5 text-right tabular-nums ${plColor(s.totalProfitLoss)}`}>
                      {fmtPl(s.totalProfitLoss)}
                    </td>
                    <td className="px-4 py-2.5 text-right text-muted-foreground text-xs">
                      {s.releaseDate?.slice(0, 4) ?? '-'}
                    </td>
                  </tr>
                )
              })
            )}
          </tbody>
        </table>
      </div>
    </div>
  )
}

// ---- Main page --------------------------------------------------------------

const ALL_USERS_KEY = '__all__'

export function MetricsPage() {
  const { user } = useAuth()
  const { prefs, patchPrefs } = usePreferences()
  const isAdmin = user?.role === 'Admin'

  const [adminUsers, setAdminUsers] = useState<UserRow[]>([])
  // '__all__' means aggregate (admin) or current user (non-admin)
  const [selectedUserId, setSelectedUserId] = useState<string>(ALL_USERS_KEY)

  const [filters, setFilters] = useState<Filters>(BLANK_FILTERS)
  const [sets, setSets] = useState<CardSet[]>([])
  const [treatments, setTreatments] = useState<Treatment[]>([])

  const [metrics, setMetrics] = useState<MetricsResult | null>(null)
  const [completion, setCompletion] = useState<SetCompletion[]>([])
  const [loading, setLoading] = useState(true)
  const [exportFormat, setExportFormat] = useState('cos')
  const [exporting, setExporting] = useState(false)

  // Load reference data once on mount
  useEffect(() => {
    const base: Promise<unknown>[] = [
      fetch('/api/sets').then(r => r.ok ? r.json() : []),
      fetch('/api/treatments').then(r => r.ok ? r.json() : []),
    ]
    if (isAdmin) {
      base.push(fetch('/api/users', { credentials: 'include' }).then(r => r.ok ? r.json() : []))
    }
    void Promise.all(base).then(([s, t, u]) => {
      setSets(s as CardSet[])
      setTreatments(sortTreatments(t as Treatment[]))
      if (isAdmin && u) {
        setAdminUsers(
          (u as UserRow[]).filter(usr => usr.role !== 'Admin' && usr.state === 'Active')
        )
      }
    })
  }, [isAdmin])

  // Reload metrics whenever filters, user selection, or regularOnly pref changes
  const loadMetrics = useCallback(async () => {
    setLoading(true)
    try {
      const mParams = new URLSearchParams()
      if (isAdmin && selectedUserId !== ALL_USERS_KEY) mParams.set('userId', selectedUserId)
      if (filters.setCode) mParams.set('filter.setCode', filters.setCode)
      if (filters.color) mParams.set('filter.color', filters.color)
      if (filters.condition) mParams.set('filter.condition', filters.condition)
      if (filters.treatment) mParams.set('filter.treatment', filters.treatment)
      if (filters.cardType) mParams.set('filter.cardType', filters.cardType)
      if (filters.autographed) mParams.set('filter.autographed', filters.autographed)
      if (filters.isReserved) mParams.set('filter.isReserved', 'true')
      if (filters.hasPhyrexianMana) mParams.set('filter.hasPhyrexianMana', 'true')
      if (filters.hasHybridMana) mParams.set('filter.hasHybridMana', 'true')

      // Set completion is per-user only; skip when admin is in aggregate view
      const showCompletion = !isAdmin || selectedUserId !== ALL_USERS_KEY

      const fetches: Promise<Response>[] = [
        fetch(`/api/collection/metrics?${mParams}`, { credentials: 'include' }),
      ]

      if (showCompletion) {
        const cParams = new URLSearchParams()
        if (isAdmin && selectedUserId !== ALL_USERS_KEY) cParams.set('userId', selectedUserId)
        cParams.set('regularOnly', String(prefs.setCompletionRegularOnly))
        fetches.push(fetch(`/api/collection/completion?${cParams}`, { credentials: 'include' }))
      }

      const [mRes, cRes] = await Promise.all(fetches)
      if (mRes?.ok) setMetrics(await mRes.json() as MetricsResult)
      if (cRes?.ok) setCompletion(await cRes.json() as SetCompletion[])
      else if (!showCompletion) setCompletion([])
    } finally {
      setLoading(false)
    }
  }, [isAdmin, selectedUserId, filters, prefs.setCompletionRegularOnly])

  useEffect(() => { void loadMetrics() }, [loadMetrics])

  async function handleRegularOnlyChange(v: boolean) {
    await patchPrefs({ setCompletionRegularOnly: v }).catch(() => {})
  }

  function handleSetClick(code: string) {
    setFilters(f => ({ ...f, setCode: code }))
  }

  async function handleExport() {
    setExporting(true)
    try {
      const params = new URLSearchParams({ format: exportFormat })
      if (filters.setCode) params.set('filter.setCode', filters.setCode)
      if (filters.color) params.set('filter.color', filters.color)
      if (filters.condition) params.set('filter.condition', filters.condition)
      if (filters.treatment) params.set('filter.treatment', filters.treatment)
      if (filters.cardType) params.set('filter.cardType', filters.cardType)
      if (filters.autographed) params.set('filter.autographed', filters.autographed)
      if (filters.isReserved) params.set('filter.isReserved', 'true')
      if (filters.hasPhyrexianMana) params.set('filter.hasPhyrexianMana', 'true')
      if (filters.hasHybridMana) params.set('filter.hasHybridMana', 'true')
      const res = await fetch(`/api/collection/export?${params}`, { credentials: 'include' })
      if (!res.ok) return
      const blob = await res.blob()
      const cd = res.headers.get('content-disposition') ?? ''
      const match = /filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/.exec(cd)
      const fileName = match ? match[1].replace(/['"]/g, '') : `collection-export.csv`
      const url = URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.download = fileName
      a.click()
      URL.revokeObjectURL(url)
    } finally {
      setExporting(false)
    }
  }

  const hasFilters = Object.values(filters).some(Boolean)

  const isEmpty = metrics &&
    metrics.totalCardCount === 0 &&
    metrics.serializedCount === 0 &&
    metrics.slabCount === 0 &&
    metrics.sealedProductCount === 0

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between gap-3 flex-wrap">
        <h1 className="text-2xl font-semibold">Metrics</h1>
        <div className="flex items-center gap-2 flex-wrap">
          {isAdmin && (
            <Select value={selectedUserId} onValueChange={setSelectedUserId}>
              <SelectTrigger className="h-8 w-52 text-xs"><SelectValue /></SelectTrigger>
              <SelectContent>
                <SelectItem value={ALL_USERS_KEY}>All Users</SelectItem>
                {adminUsers.map(u => (
                  <SelectItem key={u.id} value={u.id}>
                    {u.displayName || u.username}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          )}
          <Select value={exportFormat} onValueChange={setExportFormat}>
            <SelectTrigger className="h-8 w-32 text-xs"><SelectValue /></SelectTrigger>
            <SelectContent>
              <SelectItem value="cos">COS</SelectItem>
              <SelectItem value="moxfield">Moxfield</SelectItem>
              <SelectItem value="deckbox">Deckbox</SelectItem>
              <SelectItem value="tcgplayer">TCGPlayer</SelectItem>
              <SelectItem value="dragonshield">Dragon Shield</SelectItem>
              <SelectItem value="manabox">Manabox</SelectItem>
            </SelectContent>
          </Select>
          <Button variant="outline" size="sm" className="h-8 gap-1.5 text-xs" onClick={() => { void handleExport() }} disabled={exporting}>
            <Download className="h-3.5 w-3.5" />
            {exporting ? 'Exporting...' : hasFilters ? 'Export Filtered' : 'Export'}
          </Button>
        </div>
      </div>

      <FiltersPanel
        filters={filters}
        sets={sets}
        treatments={treatments}
        onChange={setFilters}
        onClear={() => setFilters(BLANK_FILTERS)}
      />

      {loading ? (
        <p className="text-sm text-muted-foreground">Loading...</p>
      ) : isEmpty ? (
        <p className="text-sm text-muted-foreground">No collection data available.</p>
      ) : metrics ? (
        <>
          {/* Summary stat cards */}
          <div>
            <h2 className="text-sm font-medium text-muted-foreground uppercase tracking-wide mb-3">
              Summary{hasFilters ? ' (filtered)' : ''}
            </h2>
            <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-6 gap-3">
              <StatCard
                label="Total Value"
                value={fmt(metrics.totalValue)}
                sub={`P/L: ${fmtPl(metrics.totalProfitLoss)}`}
                subColor={plColor(metrics.totalProfitLoss)}
              />
              <StatCard label="Cards" value={metrics.totalCardCount.toString()} />
              <StatCard label="Serialized" value={metrics.serializedCount.toString()} />
              <StatCard label="Slabs" value={metrics.slabCount.toString()} />
              <StatCard label="Sealed" value={metrics.sealedProductCount.toString()} />
              <StatCard label="Sealed Value" value={fmt(metrics.sealedProductValue)} />
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
                    {metrics.byContentType.filter(b => b.count > 0).map(b => (
                      <tr key={b.contentType} className="border-b last:border-0 hover:bg-muted/20">
                        <td className="px-4 py-2 font-medium">
                          {CONTENT_TYPE_LABELS[b.contentType] ?? b.contentType}
                        </td>
                        <td className="px-4 py-2 text-right tabular-nums">{b.count}</td>
                        <td className="px-4 py-2 text-right tabular-nums">{fmt(b.totalValue)}</td>
                        <td className={`px-4 py-2 text-right tabular-nums ${plColor(b.totalProfitLoss)}`}>
                          {fmtPl(b.totalProfitLoss)}
                        </td>
                      </tr>
                    ))}
                    <tr className="border-t bg-muted/30 font-semibold">
                      <td className="px-4 py-2">Total</td>
                      <td className="px-4 py-2 text-right tabular-nums">
                        {metrics.byContentType.reduce((s, b) => s + b.count, 0)}
                      </td>
                      <td className="px-4 py-2 text-right tabular-nums">{fmt(metrics.totalValue)}</td>
                      <td className={`px-4 py-2 text-right tabular-nums ${plColor(metrics.totalProfitLoss)}`}>
                        {fmtPl(metrics.totalProfitLoss)}
                      </td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>
          )}

          {/* Per-set breakdown - not shown for admin aggregate view */}
          {completion.length > 0 && (
            <SetBreakdownSection
              completion={completion}
              activeSetCode={filters.setCode}
              regularOnly={prefs.setCompletionRegularOnly}
              onRegularOnlyChange={v => { void handleRegularOnlyChange(v) }}
              onSetClick={handleSetClick}
            />
          )}
        </>
      ) : null}
    </div>
  )
}
