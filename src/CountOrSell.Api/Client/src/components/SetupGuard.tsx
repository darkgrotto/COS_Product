import { useEffect, useState } from 'react'
import { Navigate, useLocation } from 'react-router-dom'

export function SetupGuard({ children }: { children: React.ReactNode }) {
  const [state, setState] = useState<'loading' | 'needs-setup' | 'ready'>('loading')
  const location = useLocation()

  useEffect(() => {
    fetch('/api/setup/status')
      .then(r => r.ok ? r.json() : null)
      .then((d: { needsSetup?: boolean } | null) => {
        setState(d?.needsSetup ? 'needs-setup' : 'ready')
      })
      .catch(() => setState('ready'))
  }, [])

  if (state === 'loading') {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <p className="text-muted-foreground text-sm">Loading...</p>
      </div>
    )
  }

  if (state === 'needs-setup' && location.pathname !== '/setup') {
    return <Navigate to="/setup" replace />
  }

  return <>{children}</>
}
