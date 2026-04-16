import { useEffect, useState, useCallback } from 'react'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { Button } from '@/components/ui/button'
import { RefreshCw } from 'lucide-react'

interface AuditLogEntry {
  timestamp: string
  actorDisplayName: string
  actor: string
  actionType: string
  target: string | null
  result: string
  ipAddress: string | null
}

const LIMIT_OPTIONS = [50, 100, 250, 500]

export function LogViewer() {
  const [entries, setEntries] = useState<AuditLogEntry[]>([])
  const [actionTypes, setActionTypes] = useState<string[]>([])
  const [limit, setLimit] = useState(100)
  const [actionType, setActionType] = useState('all')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    fetch('/api/audit/logs/action-types', { credentials: 'include' })
      .then(res => {
        if (!res.ok) throw new Error(`HTTP ${res.status}`)
        return res.json() as Promise<string[]>
      })
      .then(data => setActionTypes(data))
      .catch(() => {
        // non-fatal - filter just won't have options
      })
  }, [])

  const loadLogs = useCallback(() => {
    setLoading(true)
    setError(null)
    const params = new URLSearchParams({ limit: String(limit) })
    if (actionType !== 'all') params.set('actionType', actionType)
    fetch(`/api/audit/logs?${params.toString()}`, { credentials: 'include' })
      .then(res => {
        if (!res.ok) throw new Error(`HTTP ${res.status}`)
        return res.json() as Promise<AuditLogEntry[]>
      })
      .then(data => {
        setEntries(data)
        setLoading(false)
      })
      .catch(err => {
        setError(err instanceof Error ? err.message : 'Failed to load audit log')
        setLoading(false)
      })
  }, [limit, actionType])

  useEffect(() => {
    loadLogs()
  }, [loadLogs])

  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-center justify-between">
        <h2 className="text-lg font-semibold">Audit Log</h2>
        <Button variant="outline" size="sm" onClick={loadLogs} disabled={loading}>
          <RefreshCw className={`h-4 w-4 mr-2 ${loading ? 'animate-spin' : ''}`} />
          Refresh
        </Button>
      </div>

      <div className="flex items-center gap-3">
        <div className="flex items-center gap-2">
          <label className="text-sm font-medium">Limit</label>
          <Select value={String(limit)} onValueChange={v => setLimit(Number(v))}>
            <SelectTrigger className="w-24">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {LIMIT_OPTIONS.map(opt => (
                <SelectItem key={opt} value={String(opt)}>
                  {opt}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
        <div className="flex items-center gap-2">
          <label className="text-sm font-medium">Action type</label>
          <Select value={actionType} onValueChange={setActionType}>
            <SelectTrigger className="w-48">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All</SelectItem>
              {actionTypes.map(t => (
                <SelectItem key={t} value={t}>
                  {t}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      </div>

      {error && <p className="text-sm text-destructive">Error: {error}</p>}

      {!loading && !error && entries.length === 0 && (
        <p className="text-sm text-muted-foreground">No audit log entries found.</p>
      )}

      {!error && (
        <div className="overflow-auto">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Timestamp</TableHead>
                <TableHead>Actor</TableHead>
                <TableHead>Action</TableHead>
                <TableHead>Target</TableHead>
                <TableHead>Result</TableHead>
                <TableHead>IP Address</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {loading ? (
                <TableRow>
                  <TableCell colSpan={6} className="text-center text-muted-foreground">
                    Loading...
                  </TableCell>
                </TableRow>
              ) : entries.length === 0 ? null : (
                entries.map((entry, idx) => (
                  <TableRow key={idx}>
                    <TableCell className="text-xs whitespace-nowrap">
                      {new Date(entry.timestamp).toLocaleString()}
                    </TableCell>
                    <TableCell
                      className="text-sm"
                      title={entry.actor}
                    >
                      {entry.actorDisplayName}
                    </TableCell>
                    <TableCell>
                      <code className="text-xs bg-muted px-1 py-0.5 rounded">{entry.actionType}</code>
                    </TableCell>
                    <TableCell className="text-sm">{entry.target ?? '-'}</TableCell>
                    <TableCell className="text-sm">{entry.result}</TableCell>
                    <TableCell className="text-xs font-mono">{entry.ipAddress ?? '-'}</TableCell>
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
