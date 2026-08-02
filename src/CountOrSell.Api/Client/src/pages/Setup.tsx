import { useState } from 'react'
import { rawFetch } from '@/lib/csrf'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'

export function SetupPage() {
  const [setupToken, setSetupToken] = useState('')
  const [adminUsername, setAdminUsername] = useState('')
  const [adminPassword, setAdminPassword] = useState('')
  const [adminPasswordConfirm, setAdminPasswordConfirm] = useState('')
  const [generalUsername, setGeneralUsername] = useState('')
  const [generalPassword, setGeneralPassword] = useState('')
  const [generalPasswordConfirm, setGeneralPasswordConfirm] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError('')

    if (!setupToken.trim()) {
      setError('Setup token is required. Find it in the application logs (docker logs).')
      return
    }
    if (adminPassword !== adminPasswordConfirm) {
      setError('Admin passwords do not match.')
      return
    }
    if (generalPassword !== generalPasswordConfirm) {
      setError('General user passwords do not match.')
      return
    }
    if (adminPassword.length < 15) {
      setError('Admin password must be at least 15 characters.')
      return
    }
    if (generalPassword.length < 15) {
      setError('General user password must be at least 15 characters.')
      return
    }
    if (adminUsername.toLowerCase() === generalUsername.toLowerCase()) {
      setError('Admin and general user must have different usernames.')
      return
    }

    setLoading(true)
    try {
      const res = await rawFetch('/api/setup/initialize', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          setupToken: setupToken.trim(),
          adminUsername,
          adminPassword,
          generalUserUsername: generalUsername,
          generalUserPassword: generalPassword,
        }),
      })
      if (!res.ok) {
        const data = await res.json().catch(() => ({}))
        throw new Error((data as { error?: string }).error ?? 'Setup failed')
      }
      window.location.href = '/login'
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Setup failed')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-muted/40 p-4">
      <Card className="w-full max-w-lg">
        <CardHeader className="space-y-1">
          <CardTitle className="text-2xl">CountOrSell Setup</CardTitle>
          <CardDescription>
            Create the initial accounts to get started. The admin account is for
            system management. The general user account is for collection tracking.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="space-y-6">
            <div className="space-y-2">
              <Label htmlFor="setup-token">Setup Token</Label>
              <Input
                id="setup-token"
                type="text"
                autoComplete="off"
                placeholder="Find this in the application logs"
                value={setupToken}
                onChange={e => setSetupToken(e.target.value)}
                required
                disabled={loading}
              />
              <p className="text-xs text-muted-foreground">
                Run <code className="bg-muted px-1 rounded">docker logs &lt;container&gt;</code> to find the setup token.
              </p>
            </div>

            <fieldset className="space-y-3">
              <legend className="text-sm font-medium">Admin Account</legend>
              <div className="space-y-2">
                <Label htmlFor="admin-username">Username</Label>
                <Input
                  id="admin-username"
                  type="text"
                  autoComplete="off"
                  value={adminUsername}
                  onChange={e => setAdminUsername(e.target.value)}
                  required
                  disabled={loading}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="admin-password">Password (15+ characters)</Label>
                <Input
                  id="admin-password"
                  type="password"
                  autoComplete="new-password"
                  value={adminPassword}
                  onChange={e => setAdminPassword(e.target.value)}
                  required
                  minLength={15}
                  disabled={loading}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="admin-password-confirm">Confirm Password</Label>
                <Input
                  id="admin-password-confirm"
                  type="password"
                  autoComplete="new-password"
                  value={adminPasswordConfirm}
                  onChange={e => setAdminPasswordConfirm(e.target.value)}
                  required
                  minLength={15}
                  disabled={loading}
                />
              </div>
            </fieldset>

            <fieldset className="space-y-3">
              <legend className="text-sm font-medium">General User Account</legend>
              <div className="space-y-2">
                <Label htmlFor="general-username">Username</Label>
                <Input
                  id="general-username"
                  type="text"
                  autoComplete="off"
                  value={generalUsername}
                  onChange={e => setGeneralUsername(e.target.value)}
                  required
                  disabled={loading}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="general-password">Password (15+ characters)</Label>
                <Input
                  id="general-password"
                  type="password"
                  autoComplete="new-password"
                  value={generalPassword}
                  onChange={e => setGeneralPassword(e.target.value)}
                  required
                  minLength={15}
                  disabled={loading}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="general-password-confirm">Confirm Password</Label>
                <Input
                  id="general-password-confirm"
                  type="password"
                  autoComplete="new-password"
                  value={generalPasswordConfirm}
                  onChange={e => setGeneralPasswordConfirm(e.target.value)}
                  required
                  minLength={15}
                  disabled={loading}
                />
              </div>
            </fieldset>

            {error && (
              <p className="text-sm text-destructive">{error}</p>
            )}
            <Button type="submit" className="w-full" disabled={loading}>
              {loading ? 'Creating accounts...' : 'Complete Setup'}
            </Button>
          </form>
        </CardContent>
      </Card>
    </div>
  )
}
