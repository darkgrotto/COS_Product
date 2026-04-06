import { useState, useEffect, useCallback } from 'react'
import { RefreshCw, AlertTriangle, CheckCircle, Info, X } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { ConfirmDialog } from '@/components/ConfirmDialog'

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

export function UpdatesPage() {
  const [status, setStatus] = useState<UpdateStatus | null>(null)
  const [notifications, setNotifications] = useState<Notification[]>([])
  const [checking, setChecking] = useState(false)
  const [approving, setApproving] = useState(false)
  const [schemaConfirmOpen, setSchemaConfirmOpen] = useState(false)
  const [error, setError] = useState('')

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
    try {
      const res = await fetch('/api/updates/check', { method: 'POST', credentials: 'include' })
      if (!res.ok) throw new Error('Check failed')
      await new Promise(r => setTimeout(r, 2500))
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

  const allClear = status &&
    !status.pendingSchemaUpdate &&
    !status.applicationUpdatePending &&
    notifications.length === 0

  return (
    <div className="space-y-6 max-w-3xl">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-semibold">Updates</h1>
        <Button onClick={handleCheck} disabled={checking}>
          <RefreshCw className={`h-4 w-4 mr-2 ${checking ? 'animate-spin' : ''}`} />
          {checking ? 'Checking...' : 'Check for Updates'}
        </Button>
      </div>

      {error && (
        <Alert variant="destructive">
          <AlertTriangle className="h-4 w-4" />
          <AlertTitle>Error</AlertTitle>
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}

      <div className="grid gap-4 sm:grid-cols-2">
        <Card>
          <CardHeader className="pb-3">
            <CardDescription>Content Version</CardDescription>
            <CardTitle className="text-base font-medium">
              {status?.currentContentVersion ?? 'No updates applied'}
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
          <p className="text-xs font-medium text-muted-foreground uppercase tracking-wide">
            Notifications
          </p>
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
    </div>
  )
}
