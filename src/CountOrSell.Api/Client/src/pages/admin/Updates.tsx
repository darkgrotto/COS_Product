import { useState, useEffect, useCallback } from 'react'

// Formats the stored content version key. Since the fix for cards-only version tracking,
// the key is an ISO timestamp. Old records may hold a semver string like "1.0.0".
function fmtContentVersion(v: string | null | undefined): string {
  if (!v) return 'No updates applied'
  const d = new Date(v)
  if (isNaN(d.getTime())) return v
  return d.toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' })
}
import { RefreshCw, AlertTriangle, CheckCircle, Info, X, RotateCcw, ChevronRight } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { ConfirmDialog } from '@/components/ConfirmDialog'
import {
  Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter, DialogDescription,
} from '@/components/ui/dialog'

interface UpdateStatus {
  currentContentVersion: string | null
  pendingSchemaUpdate: {
    id: number
    schemaVersion: string
    description: string
    detectedAt: string
    isApproved: boolean
  } | null
  latestApplicationVersion: string | null
  applicationUpdatePending: boolean
}

interface Notification {
  id: number
  message: string
  category: string
  createdAt: string
}

type ContentType = 'all' | 'metadata' | 'images'
type Scope = 'all' | 'cards-sets' | 'sealed'

const CONTENT_TYPE_LABELS: Record<ContentType, string> = {
  all: 'All (Metadata + Images)',
  metadata: 'Metadata Only',
  images: 'Images Only',
}

const SCOPE_LABELS: Record<Scope, string> = {
  all: 'All Content (Cards/Sets + Sealed Products)',
  'cards-sets': 'Cards & Sets Only',
  sealed: 'Sealed Products Only',
}

function RadioOption({
  selected, label, description, onSelect,
}: {
  selected: boolean; label: string; description?: string; onSelect: () => void
}) {
  return (
    <button
      type="button"
      onClick={onSelect}
      className={`w-full text-left px-4 py-3 rounded-md border-2 transition-colors ${
        selected ? 'border-primary bg-primary/5' : 'border-border hover:border-muted-foreground'
      }`}
    >
      <div className="flex items-center gap-3">
        <div className={`h-4 w-4 rounded-full border-2 flex items-center justify-center shrink-0 ${
          selected ? 'border-primary' : 'border-muted-foreground'
        }`}>
          {selected && <div className="h-2 w-2 rounded-full bg-primary" />}
        </div>
        <div>
          <div className="text-sm font-medium">{label}</div>
          {description && <div className="text-xs text-muted-foreground mt-0.5">{description}</div>}
        </div>
      </div>
    </button>
  )
}

function RedownloadDialog({
  open, onOpenChange, onStarted,
}: {
  open: boolean
  onOpenChange: (v: boolean) => void
  onStarted: (msg: string) => void
}) {
  const [contentType, setContentType] = useState<ContentType>('all')
  const [scope, setScope] = useState<Scope>('all')
  const [useFullPackage, setUseFullPackage] = useState(false)
  const [confirmOpen, setConfirmOpen] = useState(false)
  const [running, setRunning] = useState(false)

  const imageWillBeCleared = contentType === 'all' || contentType === 'images'

  const confirmDescription = [
    `Content: ${CONTENT_TYPE_LABELS[contentType]}`,
    `Scope: ${SCOPE_LABELS[scope]}`,
    `Package: ${useFullPackage ? 'Full package only' : 'Latest available (full or delta)'}`,
    imageWillBeCleared
      ? 'Existing images in the selected scope will be deleted before redownloading.'
      : null,
    'This runs in the background - check the audit log for results.',
  ].filter(Boolean).join('\n')

  async function handleConfirm() {
    setRunning(true)
    try {
      const res = await fetch('/api/updates/redownload-targeted', {
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ contentType, scope, useFullPackage }),
      })
      if (!res.ok) throw new Error('Request failed')
      const data = await res.json()
      onStarted(data.message)
      onOpenChange(false)
    } finally {
      setRunning(false)
    }
  }

  return (
    <>
      <Dialog open={open} onOpenChange={onOpenChange}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader>
            <DialogTitle>Force Redownload</DialogTitle>
            <DialogDescription>
              Configure what to redownload from countorsell.com.
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-5 py-2">
            <div className="space-y-2">
              <p className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">Content</p>
              <div className="space-y-1.5">
                {(['all', 'metadata', 'images'] as ContentType[]).map(ct => (
                  <RadioOption
                    key={ct}
                    selected={contentType === ct}
                    label={CONTENT_TYPE_LABELS[ct]}
                    description={
                      ct === 'images'
                        ? 'Images only - existing images in scope are purged first'
                        : ct === 'metadata'
                        ? 'Database records only - images unchanged'
                        : 'Full redownload - existing images in scope are purged first'
                    }
                    onSelect={() => setContentType(ct)}
                  />
                ))}
              </div>
            </div>

            <div className="space-y-2">
              <p className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">Scope</p>
              <div className="space-y-1.5">
                {(['all', 'cards-sets', 'sealed'] as Scope[]).map(sc => (
                  <RadioOption
                    key={sc}
                    selected={scope === sc}
                    label={SCOPE_LABELS[sc]}
                    onSelect={() => setScope(sc)}
                  />
                ))}
              </div>
            </div>

            <div className="space-y-2">
              <p className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">Package</p>
              <div className="space-y-1.5">
                <RadioOption
                  selected={!useFullPackage}
                  label="Latest available"
                  description="Use the most recent package (delta or full)"
                  onSelect={() => setUseFullPackage(false)}
                />
                <RadioOption
                  selected={useFullPackage}
                  label="Full package only"
                  description="Require a full content package - slower but complete"
                  onSelect={() => setUseFullPackage(true)}
                />
              </div>
            </div>

            {imageWillBeCleared && (
              <Alert variant="warning">
                <AlertTriangle className="h-4 w-4" />
                <AlertDescription className="text-xs">
                  Existing images in the selected scope will be permanently deleted before
                  redownloading. This cannot be undone without another redownload.
                </AlertDescription>
              </Alert>
            )}
          </div>

          <DialogFooter>
            <Button variant="outline" onClick={() => onOpenChange(false)}>Cancel</Button>
            <Button onClick={() => setConfirmOpen(true)} disabled={running}>
              Review & Confirm <ChevronRight className="h-4 w-4 ml-1" />
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <ConfirmDialog
        open={confirmOpen}
        onOpenChange={setConfirmOpen}
        title="Confirm Redownload"
        description={confirmDescription}
        confirmLabel="Start Redownload"
        destructive={imageWillBeCleared}
        onConfirm={handleConfirm}
      />
    </>
  )
}

export function UpdatesPage() {
  const [status, setStatus] = useState<UpdateStatus | null>(null)
  const [notifications, setNotifications] = useState<Notification[]>([])
  const [checking, setChecking] = useState(false)
  const [redownloading, setRedownloading] = useState(false)
  const [approving, setApproving] = useState(false)
  const [schemaConfirmOpen, setSchemaConfirmOpen] = useState(false)
  const [redownloadOpen, setRedownloadOpen] = useState(false)
  const [error, setError] = useState('')
  const [checkMessage, setCheckMessage] = useState<{ text: string; applied: boolean } | null>(null)

  const load = useCallback(async () => {
    const [sr, nr] = await Promise.all([
      fetch('/api/updates/status', { credentials: 'include' }),
      fetch('/api/updates/notifications', { credentials: 'include' }),
    ])
    if (sr.ok) setStatus(await sr.json())
    if (nr.ok) setNotifications(await nr.json())
  }, [])

  useEffect(() => { load() }, [load])

  async function handleCheck() {
    setChecking(true)
    setError('')
    setCheckMessage(null)
    try {
      const res = await fetch('/api/updates/check', { method: 'POST', credentials: 'include' })
      if (!res.ok) throw new Error('Check failed')
      const data = await res.json()
      setCheckMessage({ text: data.message, applied: data.packagesAvailable })
      await load()
    } catch {
      setError('Update check failed. Check the application logs.')
    } finally {
      setChecking(false)
    }
  }

  async function handleApproveSchema() {
    if (!status?.pendingSchemaUpdate) return
    setApproving(true)
    try {
      const res = await fetch(`/api/updates/schema/${status.pendingSchemaUpdate.id}/approve`, {
        method: 'POST',
        credentials: 'include',
      })
      if (!res.ok) {
        const data = await res.json().catch(() => ({}))
        throw new Error((data as { error?: string }).error ?? 'Approval failed')
      }
      await load()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Schema update failed')
    } finally {
      setApproving(false)
    }
  }

  async function dismissNotification(id: number) {
    await fetch(`/api/updates/notifications/${id}/read`, { method: 'POST', credentials: 'include' })
    setNotifications(prev => prev.filter(n => n.id !== id))
  }

  async function clearAllNotifications() {
    await fetch('/api/updates/notifications/read-all', { method: 'POST', credentials: 'include' })
    setNotifications([])
  }

  const allClear = status &&
    !status.pendingSchemaUpdate &&
    !status.applicationUpdatePending &&
    notifications.length === 0

  return (
    <div className="space-y-6 max-w-3xl">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-semibold">Updates</h1>
        <div className="flex gap-2">
          <Button
            variant="outline"
            onClick={() => { setRedownloadOpen(true); setRedownloading(true) }}
            disabled={redownloading || checking}
          >
            <RotateCcw className={`h-4 w-4 mr-2 ${redownloading ? 'animate-spin' : ''}`} />
            {redownloading ? 'Running...' : 'Force Redownload...'}
          </Button>
          <Button onClick={handleCheck} disabled={checking || redownloading}>
            <RefreshCw className={`h-4 w-4 mr-2 ${checking ? 'animate-spin' : ''}`} />
            {checking ? 'Checking...' : 'Check for Updates'}
          </Button>
        </div>
      </div>

      {error && (
        <Alert variant="destructive">
          <AlertTriangle className="h-4 w-4" />
          <AlertTitle>Error</AlertTitle>
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}

      {checkMessage && (
        <Alert variant={checkMessage.applied ? 'success' : 'default'}>
          {checkMessage.applied
            ? <CheckCircle className="h-4 w-4" />
            : <Info className="h-4 w-4" />}
          <AlertTitle>{checkMessage.applied ? 'Update Applied' : 'Check Complete'}</AlertTitle>
          <AlertDescription>{checkMessage.text}</AlertDescription>
        </Alert>
      )}

      <div className="grid gap-4 sm:grid-cols-2">
        <Card>
          <CardHeader className="pb-3">
            <CardDescription>Content Version</CardDescription>
            <CardTitle className="text-base font-medium">
              {fmtContentVersion(status?.currentContentVersion)}
            </CardTitle>
          </CardHeader>
        </Card>

        <Card>
          <CardHeader className="pb-3">
            <CardDescription>Application Version</CardDescription>
            <CardTitle className="text-base font-medium flex items-center gap-2">
              {status?.latestApplicationVersion
                ? (status.applicationUpdatePending
                    ? <><span>Update available</span><Badge variant="outline">{status.latestApplicationVersion}</Badge></>
                    : 'Up to date')
                : 'Unknown'}
            </CardTitle>
          </CardHeader>
        </Card>
      </div>

      {status?.pendingSchemaUpdate && (
        <Alert variant="warning">
          <AlertTriangle className="h-4 w-4" />
          <AlertTitle>Schema Update Pending Approval</AlertTitle>
          <AlertDescription className="space-y-3 mt-1">
            <p>
              <strong>Version {status.pendingSchemaUpdate.schemaVersion}</strong> -{' '}
              {status.pendingSchemaUpdate.description}
            </p>
            <p className="text-xs opacity-75">
              Detected {new Date(status.pendingSchemaUpdate.detectedAt).toLocaleString()}.
              A pre-update backup is taken automatically before applying.
              If the update fails, the backup is restored.
            </p>
            <Button size="sm" onClick={() => setSchemaConfirmOpen(true)} disabled={approving}>
              {approving ? 'Applying...' : 'Approve and Apply'}
            </Button>
          </AlertDescription>
        </Alert>
      )}

      {status?.applicationUpdatePending && (
        <Alert>
          <Info className="h-4 w-4" />
          <AlertTitle>Application Update Available</AlertTitle>
          <AlertDescription className="space-y-2 mt-1">
            <p>Run the update script from your deployment directory to pull the latest image:</p>
            <code className="block bg-muted px-3 py-2 rounded text-xs font-mono">
              ./docker/scripts/update.sh
            </code>
          </AlertDescription>
        </Alert>
      )}

      {notifications.length > 0 && (
        <div className="space-y-2">
          <div className="flex items-center justify-between">
            <p className="text-xs font-medium text-muted-foreground uppercase tracking-wide">
              Notifications
            </p>
            {notifications.length > 1 && (
              <button
                onClick={clearAllNotifications}
                className="text-xs text-muted-foreground hover:text-foreground transition-colors"
              >
                Clear all
              </button>
            )}
          </div>
          {notifications.map(n => (
            <div
              key={n.id}
              className="flex items-start gap-3 rounded-lg border px-4 py-3 text-sm"
            >
              <Info className="h-4 w-4 mt-0.5 text-muted-foreground shrink-0" />
              <div className="flex-1 space-y-1">
                <p>{n.message}</p>
                <p className="text-xs text-muted-foreground">
                  {new Date(n.createdAt).toLocaleString()}
                </p>
              </div>
              <button
                onClick={() => dismissNotification(n.id)}
                className="text-muted-foreground hover:text-foreground transition-colors"
                aria-label="Dismiss"
              >
                <X className="h-4 w-4" />
              </button>
            </div>
          ))}
        </div>
      )}

      {allClear && (
        <div className="flex items-center gap-2 text-sm text-muted-foreground">
          <CheckCircle className="h-4 w-4 text-green-500" />
          Everything is up to date.
        </div>
      )}

      <ConfirmDialog
        open={schemaConfirmOpen}
        onOpenChange={setSchemaConfirmOpen}
        title="Apply Schema Update"
        description="A pre-update backup will be taken automatically before the schema is updated. If the update fails the backup is restored automatically. This operation cannot be undone. Proceed?"
        confirmLabel="Apply Schema Update"
        destructive
        onConfirm={handleApproveSchema}
      />

      <RedownloadDialog
        open={redownloadOpen}
        onOpenChange={(v) => {
          setRedownloadOpen(v)
          if (!v) setRedownloading(false)
        }}
        onStarted={(msg) => {
          setCheckMessage({ text: msg + ' Check the audit log for results.', applied: true })
          setRedownloading(false)
        }}
      />
    </div>
  )
}
