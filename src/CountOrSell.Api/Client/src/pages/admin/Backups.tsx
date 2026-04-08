import { useCallback, useEffect, useRef, useState } from 'react'
import { HardDrive, Plus, Trash2, CheckCircle, XCircle, Download, RotateCcw, Upload } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Badge } from '@/components/ui/badge'
import {
  Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle,
} from '@/components/ui/dialog'
import {
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue,
} from '@/components/ui/select'

// ---- Helpers ----------------------------------------------------------------

function fmtBytes(n: number | null | undefined) {
  if (n == null) return '-'
  if (n < 1024) return `${n} B`
  if (n < 1048576) return `${(n / 1024).toFixed(1)} KB`
  return `${(n / 1048576).toFixed(1)} MB`
}

function fmtDate(s: string | null | undefined) {
  if (!s) return '-'
  return new Date(s).toLocaleString()
}

// ---- Types ------------------------------------------------------------------

// BackupType enum: 0 = Scheduled, 1 = PreUpdate
const BACKUP_TYPE_LABELS: Record<number, string> = { 0: 'Scheduled', 1: 'Pre-Update' }

const DEST_TYPE_LABELS: Record<string, string> = {
  'local': 'Local',
  'azure-blob': 'Azure Blob',
  'aws-s3': 'AWS S3',
  'gcp-storage': 'GCP Storage',
}

interface BackupSummary {
  id: string
  label: string
  createdAt: string
  schemaVersion: string
}

interface DestinationRow {
  id: string
  type: string
  label: string
  isActive: boolean
}

interface BackupRecord {
  id: string
  label: string
  backupType: number
  schemaVersion: string
  createdAt: string
  fileSizeBytes: number | null
  isAvailable: boolean
}

interface HistoryPage {
  total: number
  page: number
  pageSize: number
  records: BackupRecord[]
}

// ---- Add Destination Dialog --------------------------------------------------

function AddDestinationDialog({
  onClose,
  onAdded,
}: {
  onClose: () => void
  onAdded: () => void
}) {
  const [destType, setDestType] = useState('local')
  const [label, setLabel] = useState('')
  const [localPath, setLocalPath] = useState('')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')

  async function handleSave() {
    if (!label.trim()) { setError('Label is required.'); return }
    setError('')
    setSaving(true)
    try {
      const configJson = destType === 'local'
        ? JSON.stringify({ path: localPath.trim() || null })
        : '{}'
      const res = await fetch('/api/backup/destinations', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          destinationType: destType,
          label: label.trim(),
          configurationJson: configJson,
        }),
      })
      if (!res.ok) {
        const d = await res.json().catch(() => ({}))
        throw new Error((d as { error?: string }).error ?? 'Failed to add destination.')
      }
      onAdded()
      onClose()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to add destination.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <Dialog open onOpenChange={v => !v && onClose()}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Add Backup Destination</DialogTitle>
        </DialogHeader>
        <div className="space-y-4">
          <div>
            <Label>Destination Type</Label>
            <Select value={destType} onValueChange={setDestType}>
              <SelectTrigger><SelectValue /></SelectTrigger>
              <SelectContent>
                {Object.entries(DEST_TYPE_LABELS).map(([k, v]) => (
                  <SelectItem key={k} value={k}>{v}</SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          <div>
            <Label>Label</Label>
            <Input
              value={label}
              onChange={e => setLabel(e.target.value)}
              placeholder="e.g. Primary Local Backup"
            />
          </div>
          {destType === 'local' && (
            <div>
              <Label>Path <span className="text-xs text-muted-foreground">(optional, defaults to app data folder)</span></Label>
              <Input
                value={localPath}
                onChange={e => setLocalPath(e.target.value)}
                placeholder="/path/to/backup/folder"
              />
            </div>
          )}
          {destType !== 'local' && (
            <p className="text-sm text-muted-foreground">
              {DEST_TYPE_LABELS[destType]} credentials are read from environment variables configured at deployment time.
            </p>
          )}
          {error && <p className="text-sm text-destructive">{error}</p>}
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={onClose} disabled={saving}>Cancel</Button>
          <Button onClick={handleSave} disabled={saving}>{saving ? 'Adding...' : 'Add Destination'}</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}

// ---- Confirm Dialog ---------------------------------------------------------

function ConfirmDialog({
  title,
  description,
  confirmLabel = 'Confirm',
  destructive = false,
  onConfirm,
  onClose,
}: {
  title: string
  description: string
  confirmLabel?: string
  destructive?: boolean
  onConfirm: () => void
  onClose: () => void
}) {
  return (
    <Dialog open onOpenChange={v => !v && onClose()}>
      <DialogContent className="sm:max-w-sm">
        <DialogHeader>
          <DialogTitle>{title}</DialogTitle>
        </DialogHeader>
        <p className="text-sm text-muted-foreground">{description}</p>
        <DialogFooter>
          <Button variant="outline" onClick={onClose}>Cancel</Button>
          <Button
            variant={destructive ? 'destructive' : 'default'}
            onClick={() => { onConfirm(); onClose() }}
          >
            {confirmLabel}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}

// ---- Main Page --------------------------------------------------------------

export function BackupsPage() {
  const [lastScheduled, setLastScheduled] = useState<BackupSummary | null>(null)
  const [lastPreUpdate, setLastPreUpdate] = useState<BackupSummary | null>(null)
  const [nextScheduled, setNextScheduled] = useState<string | null>(null)
  const [destinations, setDestinations] = useState<DestinationRow[]>([])
  const [history, setHistory] = useState<BackupRecord[]>([])
  const [historyTotal, setHistoryTotal] = useState(0)
  const [historyPage, setHistoryPage] = useState(1)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')

  // Dialog / action state
  const [showAddDest, setShowAddDest] = useState(false)
  const [confirmTrigger, setConfirmTrigger] = useState(false)
  const [confirmDeleteDest, setConfirmDeleteDest] = useState<DestinationRow | null>(null)
  const [confirmRestoreRecord, setConfirmRestoreRecord] = useState<BackupRecord | null>(null)
  const [confirmRestoreFile, setConfirmRestoreFile] = useState<File | null>(null)

  const [triggerBusy, setTriggerBusy] = useState(false)
  const [testBusy, setTestBusy] = useState<Record<string, boolean>>({})
  const [deleteBusy, setDeleteBusy] = useState<Record<string, boolean>>({})
  const [restoreBusy, setRestoreBusy] = useState<Record<string, boolean>>({})
  const [testResults, setTestResults] = useState<Record<string, boolean | null>>({})

  const fileInputRef = useRef<HTMLInputElement>(null)
  const PAGE_SIZE = 20

  const load = useCallback(async (page = 1) => {
    try {
      const [statusRes, histRes] = await Promise.all([
        fetch('/api/backup/status'),
        fetch(`/api/backup/history?page=${page}&pageSize=${PAGE_SIZE}`),
      ])
      if (statusRes.ok) {
        const s = await statusRes.json()
        setLastScheduled(s.lastScheduledBackup ?? null)
        setLastPreUpdate(s.lastPreUpdateBackup ?? null)
        setNextScheduled(s.nextScheduledBackup ?? null)
        setDestinations((s.destinations ?? []).map((d: { id: string; type: string; label: string; isActive: boolean }) => ({
          id: d.id, type: d.type, label: d.label, isActive: d.isActive,
        })))
      }
      if (histRes.ok) {
        const h: HistoryPage = await histRes.json()
        setHistory(h.records)
        setHistoryTotal(h.total)
        setHistoryPage(h.page)
      }
    } catch {
      // non-fatal - data may be partial
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => { load(1) }, [load])

  function flash(msg: string, isError = false) {
    setSuccess(''); setError('')
    if (isError) setError(msg); else setSuccess(msg)
    setTimeout(() => { setError(''); setSuccess('') }, 5000)
  }

  async function handleTrigger() {
    setTriggerBusy(true)
    try {
      const res = await fetch('/api/backup/trigger', { method: 'POST' })
      if (!res.ok) {
        const d = await res.json().catch(() => ({}))
        throw new Error((d as { error?: string }).error ?? 'Backup failed.')
      }
      flash('Backup completed successfully.')
      load(1)
    } catch (err) {
      flash(err instanceof Error ? err.message : 'Backup failed.', true)
    } finally {
      setTriggerBusy(false)
    }
  }

  async function handleTestDest(id: string) {
    setTestBusy(prev => ({ ...prev, [id]: true }))
    try {
      const res = await fetch(`/api/backup/destinations/${id}/test`, { method: 'POST' })
      const d = await res.json().catch(() => ({ success: false }))
      setTestResults(prev => ({ ...prev, [id]: (d as { success: boolean }).success }))
    } catch {
      setTestResults(prev => ({ ...prev, [id]: false }))
    } finally {
      setTestBusy(prev => ({ ...prev, [id]: false }))
    }
  }

  async function handleDeleteDest(id: string) {
    setDeleteBusy(prev => ({ ...prev, [id]: true }))
    try {
      const res = await fetch(`/api/backup/destinations/${id}`, { method: 'DELETE' })
      if (!res.ok) throw new Error('Failed to remove destination.')
      flash('Destination removed.')
      load(1)
    } catch (err) {
      flash(err instanceof Error ? err.message : 'Failed to remove destination.', true)
    } finally {
      setDeleteBusy(prev => ({ ...prev, [id]: false }))
    }
  }

  async function handleRestoreFromRecord(record: BackupRecord) {
    setRestoreBusy(prev => ({ ...prev, [record.id]: true }))
    try {
      const res = await fetch(`/api/restore/${record.id}`, { method: 'POST' })
      if (!res.ok) {
        const d = await res.json().catch(() => ({}))
        throw new Error((d as { error?: string }).error ?? 'Restore failed.')
      }
      flash('Restore completed. The application will restart shortly.')
    } catch (err) {
      flash(err instanceof Error ? err.message : 'Restore failed.', true)
    } finally {
      setRestoreBusy(prev => ({ ...prev, [record.id]: false }))
    }
  }

  async function handleRestoreFromFile(file: File) {
    setRestoreBusy(prev => ({ ...prev, '__file__': true }))
    try {
      const formData = new FormData()
      formData.append('file', file)
      const res = await fetch('/api/restore', { method: 'POST', body: formData })
      if (!res.ok) {
        const d = await res.json().catch(() => ({}))
        throw new Error((d as { error?: string }).error ?? 'Restore failed.')
      }
      flash('Restore completed. The application will restart shortly.')
    } catch (err) {
      flash(err instanceof Error ? err.message : 'Restore failed.', true)
    } finally {
      setRestoreBusy(prev => ({ ...prev, '__file__': false }))
      if (fileInputRef.current) fileInputRef.current.value = ''
    }
  }

  const totalPages = Math.max(1, Math.ceil(historyTotal / PAGE_SIZE))

  return (
    <div className="space-y-6">
      <div className="flex items-start justify-between gap-4">
        <div className="flex items-center gap-2">
          <HardDrive className="h-5 w-5 text-muted-foreground" />
          <h1 className="text-2xl font-semibold">Backups</h1>
        </div>
        <Button
          onClick={() => setConfirmTrigger(true)}
          disabled={triggerBusy}
          className="gap-1.5"
        >
          <HardDrive className="h-4 w-4" />
          {triggerBusy ? 'Running...' : 'Run Backup Now'}
        </Button>
      </div>

      {error && (
        <div className="rounded-md border border-destructive/50 bg-destructive/10 px-4 py-3 text-sm text-destructive">
          {error}
        </div>
      )}
      {success && (
        <div className="rounded-md border border-green-500/50 bg-green-500/10 px-4 py-3 text-sm text-green-700 dark:text-green-400">
          {success}
        </div>
      )}

      {loading ? (
        <p className="text-sm text-muted-foreground">Loading...</p>
      ) : (
        <>
          {/* Status cards */}
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
            <div className="rounded-lg border p-4 space-y-1">
              <p className="text-xs text-muted-foreground">Last Scheduled Backup</p>
              {lastScheduled ? (
                <>
                  <p className="text-sm font-medium truncate" title={lastScheduled.label}>{lastScheduled.label}</p>
                  <p className="text-xs text-muted-foreground">{fmtDate(lastScheduled.createdAt)}</p>
                </>
              ) : (
                <p className="text-sm text-muted-foreground">None</p>
              )}
            </div>
            <div className="rounded-lg border p-4 space-y-1">
              <p className="text-xs text-muted-foreground">Last Pre-Update Backup</p>
              {lastPreUpdate ? (
                <>
                  <p className="text-sm font-medium truncate" title={lastPreUpdate.label}>{lastPreUpdate.label}</p>
                  <p className="text-xs text-muted-foreground">{fmtDate(lastPreUpdate.createdAt)}</p>
                </>
              ) : (
                <p className="text-sm text-muted-foreground">None</p>
              )}
            </div>
            <div className="rounded-lg border p-4 space-y-1">
              <p className="text-xs text-muted-foreground">Next Scheduled Backup</p>
              <p className="text-sm font-medium">{fmtDate(nextScheduled)}</p>
            </div>
          </div>

          {/* Destinations */}
          <div className="space-y-3">
            <div className="flex items-center justify-between">
              <h2 className="text-base font-semibold">Destinations</h2>
              <Button size="sm" variant="outline" className="gap-1" onClick={() => setShowAddDest(true)}>
                <Plus className="h-3.5 w-3.5" /> Add
              </Button>
            </div>
            {destinations.length === 0 ? (
              <p className="text-sm text-muted-foreground">No backup destinations configured.</p>
            ) : (
              <div className="rounded-md border divide-y">
                {destinations.map(d => (
                  <div key={d.id} className="flex items-center gap-3 px-4 py-3">
                    <div className="flex-1 min-w-0">
                      <p className="text-sm font-medium truncate">{d.label}</p>
                      <p className="text-xs text-muted-foreground">{DEST_TYPE_LABELS[d.type] ?? d.type}</p>
                    </div>
                    {testResults[d.id] !== undefined && (
                      testResults[d.id]
                        ? <CheckCircle className="h-4 w-4 text-green-500 shrink-0" />
                        : <XCircle className="h-4 w-4 text-destructive shrink-0" />
                    )}
                    <Button
                      size="sm"
                      variant="outline"
                      className="text-xs h-7"
                      disabled={testBusy[d.id]}
                      onClick={() => handleTestDest(d.id)}
                    >
                      {testBusy[d.id] ? 'Testing...' : 'Test'}
                    </Button>
                    <Button
                      size="sm"
                      variant="ghost"
                      className="text-destructive hover:text-destructive h-7 w-7 p-0"
                      disabled={deleteBusy[d.id]}
                      onClick={() => setConfirmDeleteDest(d)}
                    >
                      <Trash2 className="h-3.5 w-3.5" />
                    </Button>
                  </div>
                ))}
              </div>
            )}
          </div>

          {/* History */}
          <div className="space-y-3">
            <h2 className="text-base font-semibold">History</h2>
            {history.length === 0 ? (
              <p className="text-sm text-muted-foreground">No backup records yet.</p>
            ) : (
              <>
                <div className="rounded-md border overflow-x-auto">
                  <table className="w-full text-sm">
                    <thead>
                      <tr className="border-b bg-muted/50 text-muted-foreground">
                        <th className="px-3 py-2 text-left">Label</th>
                        <th className="px-3 py-2 text-left">Type</th>
                        <th className="px-3 py-2 text-left">Schema</th>
                        <th className="px-3 py-2 text-left">Created</th>
                        <th className="px-3 py-2 text-right">Size</th>
                        <th className="px-3 py-2 text-right"></th>
                      </tr>
                    </thead>
                    <tbody>
                      {history.map(rec => (
                        <tr key={rec.id} className="border-b last:border-0 hover:bg-muted/20">
                          <td className="px-3 py-2 font-mono text-xs max-w-48 truncate" title={rec.label}>
                            {rec.label}
                          </td>
                          <td className="px-3 py-2">
                            <Badge variant="outline" className="text-xs">
                              {BACKUP_TYPE_LABELS[rec.backupType] ?? rec.backupType}
                            </Badge>
                          </td>
                          <td className="px-3 py-2 font-mono text-xs text-muted-foreground">{rec.schemaVersion}</td>
                          <td className="px-3 py-2 text-xs text-muted-foreground whitespace-nowrap">{fmtDate(rec.createdAt)}</td>
                          <td className="px-3 py-2 text-right text-xs tabular-nums">{fmtBytes(rec.fileSizeBytes)}</td>
                          <td className="px-3 py-2">
                            <div className="flex items-center gap-1 justify-end">
                              {rec.isAvailable && (
                                <a
                                  href={`/api/backup/${rec.id}/download`}
                                  download
                                  className="inline-flex items-center justify-center h-7 w-7 rounded-md border border-input hover:bg-accent transition-colors"
                                  title="Download"
                                >
                                  <Download className="h-3.5 w-3.5" />
                                </a>
                              )}
                              <Button
                                size="sm"
                                variant="outline"
                                className="h-7 w-7 p-0"
                                title="Restore from this backup"
                                disabled={!rec.isAvailable || restoreBusy[rec.id]}
                                onClick={() => setConfirmRestoreRecord(rec)}
                              >
                                <RotateCcw className="h-3.5 w-3.5" />
                              </Button>
                            </div>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
                {totalPages > 1 && (
                  <div className="flex items-center justify-between text-sm text-muted-foreground">
                    <span>{historyTotal} records</span>
                    <div className="flex items-center gap-2">
                      <Button
                        size="sm" variant="outline" className="h-7 text-xs"
                        disabled={historyPage <= 1}
                        onClick={() => load(historyPage - 1)}
                      >
                        Previous
                      </Button>
                      <span>Page {historyPage} of {totalPages}</span>
                      <Button
                        size="sm" variant="outline" className="h-7 text-xs"
                        disabled={historyPage >= totalPages}
                        onClick={() => load(historyPage + 1)}
                      >
                        Next
                      </Button>
                    </div>
                  </div>
                )}
              </>
            )}
          </div>

          {/* Restore from file */}
          <div className="space-y-3">
            <h2 className="text-base font-semibold">Restore from File</h2>
            <p className="text-sm text-muted-foreground">
              Upload a backup archive to restore from a file not listed above.
            </p>
            <div className="flex items-center gap-2">
              <input
                ref={fileInputRef}
                type="file"
                accept=".zip"
                className="hidden"
                onChange={e => {
                  const f = e.target.files?.[0]
                  if (f) setConfirmRestoreFile(f)
                }}
              />
              <Button
                variant="outline"
                className="gap-1.5"
                disabled={restoreBusy['__file__']}
                onClick={() => fileInputRef.current?.click()}
              >
                <Upload className="h-4 w-4" />
                {restoreBusy['__file__'] ? 'Restoring...' : 'Choose Backup File...'}
              </Button>
            </div>
          </div>
        </>
      )}

      {/* Dialogs */}
      {showAddDest && (
        <AddDestinationDialog
          onClose={() => setShowAddDest(false)}
          onAdded={() => { flash('Destination added.'); load(1) }}
        />
      )}

      {confirmTrigger && (
        <ConfirmDialog
          title="Run Backup Now"
          description="This will trigger a scheduled backup immediately. Continue?"
          confirmLabel="Run Backup"
          onConfirm={handleTrigger}
          onClose={() => setConfirmTrigger(false)}
        />
      )}

      {confirmDeleteDest && (
        <ConfirmDialog
          title="Remove Destination"
          description={`Remove backup destination "${confirmDeleteDest.label}"? Existing backup records are not affected.`}
          confirmLabel="Remove"
          destructive
          onConfirm={() => handleDeleteDest(confirmDeleteDest.id)}
          onClose={() => setConfirmDeleteDest(null)}
        />
      )}

      {confirmRestoreRecord && (
        <ConfirmDialog
          title="Restore from Backup"
          description={`Restore from "${confirmRestoreRecord.label}"? All current data will be replaced. This cannot be undone.`}
          confirmLabel="Restore"
          destructive
          onConfirm={() => handleRestoreFromRecord(confirmRestoreRecord)}
          onClose={() => setConfirmRestoreRecord(null)}
        />
      )}

      {confirmRestoreFile && (
        <ConfirmDialog
          title="Restore from File"
          description={`Restore from "${confirmRestoreFile.name}"? All current data will be replaced. This cannot be undone.`}
          confirmLabel="Restore"
          destructive
          onConfirm={() => handleRestoreFromFile(confirmRestoreFile)}
          onClose={() => { setConfirmRestoreFile(null); if (fileInputRef.current) fileInputRef.current.value = '' }}
        />
      )}
    </div>
  )
}
