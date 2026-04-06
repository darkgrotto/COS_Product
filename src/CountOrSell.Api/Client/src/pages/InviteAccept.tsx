import { useState, useEffect } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'

interface InviteInfo {
  email: string
  role: string
  expiresAt: string
}

export function InviteAcceptPage() {
  const { token } = useParams<{ token: string }>()
  const navigate = useNavigate()

  const [info, setInfo] = useState<InviteInfo | null>(null)
  const [invalid, setInvalid] = useState(false)
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const [done, setDone] = useState(false)

  useEffect(() => {
    if (!token) { setInvalid(true); return }
    fetch(`/api/invitations/${token}/validate`)
      .then(res => {
        if (!res.ok) throw new Error()
        return res.json()
      })
      .then((data: InviteInfo) => setInfo(data))
      .catch(() => setInvalid(true))
  }, [token])

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError('')

    if (password.length < 15) {
      setError('Password must be at least 15 characters.')
      return
    }
    if (password !== confirmPassword) {
      setError('Passwords do not match.')
      return
    }

    setLoading(true)
    try {
      const res = await fetch(`/api/invitations/${token}/accept`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ username, password }),
      })
      if (!res.ok) {
        const data = await res.json().catch(() => ({}))
        throw new Error((data as { error?: string }).error ?? 'Failed to create account')
      }
      setDone(true)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create account')
    } finally {
      setLoading(false)
    }
  }

  if (invalid) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-muted/40 p-4">
        <Card className="w-full max-w-sm text-center">
          <CardHeader>
            <CardTitle>Invalid Invitation</CardTitle>
            <CardDescription>This invitation link is invalid or has expired.</CardDescription>
          </CardHeader>
        </Card>
      </div>
    )
  }

  if (!info) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <p className="text-muted-foreground text-sm">Validating invitation...</p>
      </div>
    )
  }

  if (done) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-muted/40 p-4">
        <Card className="w-full max-w-sm text-center">
          <CardHeader>
            <CardTitle>Account Created</CardTitle>
            <CardDescription>
              Your account has been created. You can now sign in with your username and password.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <Button className="w-full" onClick={() => navigate('/login')}>Go to Sign In</Button>
          </CardContent>
        </Card>
      </div>
    )
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-muted/40 p-4">
      <Card className="w-full max-w-sm">
        <CardHeader className="space-y-1">
          <CardTitle>Create Your Account</CardTitle>
          <CardDescription>
            You were invited as{' '}
            <Badge variant="secondary" className="font-normal">
              {info.role === 'Admin' ? 'Admin' : 'General User'}
            </Badge>
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="space-y-4">
            <div className="space-y-1">
              <p className="text-xs text-muted-foreground">Invitation for {info.email}</p>
            </div>
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
                autoComplete="new-password"
                value={password}
                onChange={e => setPassword(e.target.value)}
                required
                disabled={loading}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="confirm">Confirm Password</Label>
              <Input
                id="confirm"
                type="password"
                autoComplete="new-password"
                value={confirmPassword}
                onChange={e => setConfirmPassword(e.target.value)}
                required
                disabled={loading}
              />
            </div>
            {error && <p className="text-sm text-destructive">{error}</p>}
            <Button type="submit" className="w-full" disabled={loading}>
              {loading ? 'Creating Account...' : 'Create Account'}
            </Button>
          </form>
        </CardContent>
      </Card>
    </div>
  )
}
