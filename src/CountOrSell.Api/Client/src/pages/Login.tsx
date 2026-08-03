import { useEffect, useState } from 'react'
import { useNavigate, useLocation, useSearchParams } from 'react-router-dom'
import { useAuth } from '@/contexts/AuthContext'
import { useBranding } from '@/contexts/BrandingContext'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'

interface OAuthProvider {
  id: string
  displayName: string
}

// Error codes appended by the server-side OAuth callback redirect.
const OAUTH_ERROR_MESSAGES: Record<string, string> = {
  oauth_failed: 'Sign-in with the external provider failed. Please try again.',
  oauth_not_provisioned:
    'No account on this instance is linked to that sign-in. The administrator has been notified.',
  account_disabled: 'This account is disabled. Contact your administrator.',
}

export function LoginPage() {
  const { login } = useAuth()
  const { instanceName } = useBranding()
  const navigate = useNavigate()
  const location = useLocation()
  const [searchParams, setSearchParams] = useSearchParams()
  const from = (location.state as { from?: string })?.from ?? '/dashboard'

  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const [providers, setProviders] = useState<OAuthProvider[]>([])

  useEffect(() => {
    const code = searchParams.get('error')
    if (code) {
      setError(OAUTH_ERROR_MESSAGES[code] ?? 'Sign-in failed. Please try again.')
      setSearchParams(params => {
        params.delete('error')
        return params
      }, { replace: true })
    }
  }, [searchParams, setSearchParams])

  useEffect(() => {
    let cancelled = false
    fetch('/api/auth/oauth/providers', { credentials: 'include' })
      .then(res => (res.ok ? res.json() : []))
      .then((data: OAuthProvider[]) => {
        if (!cancelled) setProviders(data)
      })
      .catch(() => { /* no OAuth buttons when the lookup fails */ })
    return () => { cancelled = true }
  }, [])

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError('')
    setLoading(true)
    try {
      await login(username, password)
      navigate(from, { replace: true })
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Login failed')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-muted/40 p-4">
      <Card className="w-full max-w-sm">
        <CardHeader className="space-y-1">
          <CardTitle className="text-2xl">{instanceName}</CardTitle>
          <CardDescription>Sign in to your account</CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="username">Username</Label>
              <Input
                id="username"
                type="text"
                autoComplete="username"
                value={username}
                onChange={e => setUsername(e.target.value)}
                required
                disabled={loading}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="password">Password</Label>
              <Input
                id="password"
                type="password"
                autoComplete="current-password"
                value={password}
                onChange={e => setPassword(e.target.value)}
                required
                disabled={loading}
              />
            </div>
            {error && (
              <p className="text-sm text-destructive">{error}</p>
            )}
            <Button type="submit" className="w-full" disabled={loading}>
              {loading ? 'Signing in...' : 'Sign in'}
            </Button>
          </form>

          {providers.length > 0 && (
            <div className="mt-4 space-y-3">
              <div className="flex items-center gap-3">
                <div className="h-px flex-1 bg-border" />
                <span className="text-xs text-muted-foreground">or continue with</span>
                <div className="h-px flex-1 bg-border" />
              </div>
              <div className="space-y-2">
                {providers.map(p => (
                  <Button
                    key={p.id}
                    type="button"
                    variant="outline"
                    className="w-full"
                    disabled={loading}
                    onClick={() => {
                      // Full-page navigation: the OAuth flow is a server-side
                      // redirect chain, not an XHR.
                      window.location.href = `/api/auth/oauth/${encodeURIComponent(p.id)}`
                    }}
                  >
                    {p.displayName}
                  </Button>
                ))}
              </div>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
