import { useEffect, useState, useCallback } from 'react'
import { Button } from '@/components/ui/button'
import { X } from 'lucide-react'

interface Notification {
  id: string
  message: string
  createdAt: string
}

export function NotificationsPanel() {
  const [notifications, setNotifications] = useState<Notification[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const loadNotifications = useCallback(() => {
    setLoading(true)
    setError(null)
    fetch('/api/updates/notifications', { credentials: 'include' })
      .then(res => {
        if (!res.ok) throw new Error(`HTTP ${res.status}`)
        return res.json() as Promise<Notification[]>
      })
      .then(data => {
        setNotifications(data)
        setLoading(false)
      })
      .catch(err => {
        setError(err instanceof Error ? err.message : 'Failed to load notifications')
        setLoading(false)
      })
  }, [])

  useEffect(() => {
    loadNotifications()
  }, [loadNotifications])

  const dismiss = async (id: string) => {
    try {
      const res = await fetch(`/api/updates/notifications/${id}/read`, {
        method: 'POST',
        credentials: 'include',
      })
      if (!res.ok) throw new Error(`HTTP ${res.status}`)
      setNotifications(prev => prev.filter(n => n.id !== id))
    } catch {
      // silently fail - notification remains in list
    }
  }

  const markAllRead = async () => {
    try {
      const res = await fetch('/api/updates/notifications/read-all', {
        method: 'POST',
        credentials: 'include',
      })
      if (!res.ok) throw new Error(`HTTP ${res.status}`)
      setNotifications([])
    } catch {
      // silently fail
    }
  }

  return (
    <div>
      <div className="flex items-center justify-between mb-4">
        <h2 className="text-lg font-semibold">Notifications</h2>
        {notifications.length > 0 && (
          <Button variant="outline" size="sm" onClick={markAllRead}>
            Mark all read
          </Button>
        )}
      </div>

      {loading && <p className="text-sm text-muted-foreground">Loading...</p>}
      {error && <p className="text-sm text-destructive">Error: {error}</p>}

      {!loading && !error && notifications.length === 0 && (
        <p className="text-sm text-muted-foreground">No pending notifications.</p>
      )}

      {!loading && !error && notifications.length > 0 && (
        <div className="flex flex-col gap-2">
          {notifications.map(n => (
            <div
              key={n.id}
              className="flex items-start justify-between gap-3 border rounded-md px-4 py-3"
            >
              <div className="flex-1 min-w-0">
                <p className="text-sm">{n.message}</p>
                <p className="text-xs text-muted-foreground mt-1">
                  {new Date(n.createdAt).toLocaleString()}
                </p>
              </div>
              <button
                onClick={() => dismiss(n.id)}
                className="shrink-0 text-muted-foreground hover:text-foreground transition-colors"
                aria-label="Dismiss"
              >
                <X className="h-4 w-4" />
              </button>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
