import { useState, useEffect, useCallback } from 'react'
import {
  useReactTable, getCoreRowModel, flexRender,
  type ColumnDef,
} from '@tanstack/react-table'
import { UserPlus, MoreHorizontal, Copy, Check, ChevronDown, Mail } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Badge } from '@/components/ui/badge'
import {
  Dialog, DialogContent, DialogDescription,
  DialogFooter, DialogHeader, DialogTitle,
} from '@/components/ui/dialog'
import {
  DropdownMenu, DropdownMenuContent, DropdownMenuItem,
  DropdownMenuSeparator, DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import {
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue,
} from '@/components/ui/select'
import {
  Table, TableBody, TableCell, TableHead, TableHeader, TableRow,
} from '@/components/ui/table'
import { ConfirmDialog } from '@/components/ConfirmDialog'

interface UserRow {
  id: string
  username: string
  displayName: string
  role: string
  state: string
  authType: string
  isBuiltinAdmin: boolean
  createdAt: string
  lastLoginAt: string | null
}

interface PendingInvite {
  id: string
  email: string
  role: string
  createdAt: string
  expiresAt: string
}

function RoleBadge({ role }: { role: string }) {
  return (
    <Badge variant={role === 'Admin' ? 'default' : 'secondary'} className="font-normal">
      {role === 'Admin' ? 'Admin' : 'General User'}
    </Badge>
  )
}

function StateBadge({ state }: { state: string }) {
  if (state === 'Active') return <Badge variant="outline" className="text-green-600 border-green-300 font-normal">Active</Badge>
  if (state === 'Disabled') return <Badge variant="outline" className="text-amber-600 border-amber-300 font-normal">Disabled</Badge>
  return <Badge variant="outline" className="font-normal">{state}</Badge>
}

function formatDate(iso: string | null) {
  if (!iso) return <span className="text-muted-foreground text-xs">Never</span>
  return <span className="text-xs">{new Date(iso).toLocaleDateString()}</span>
}

// ----- Create local user dialog -----

interface CreateUserDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onCreated: () => void
}

function CreateUserDialog({ open, onOpenChange, onCreated }: CreateUserDialogProps) {
  const [username, setUsername] = useState('')
  const [displayName, setDisplayName] = useState('')
  const [password, setPassword] = useState('')
  const [role, setRole] = useState('GeneralUser')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

  function reset() {
    setUsername(''); setDisplayName(''); setPassword(''); setRole('GeneralUser'); setError('')
  }

  function handleOpenChange(next: boolean) {
    if (!next) reset()
    onOpenChange(next)
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (password.length < 15) {
      setError('Password must be at least 15 characters.')
      return
    }
    setLoading(true); setError('')
    try {
      const res = await fetch('/api/users', {
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ username, displayName, password, role }),
      })
      if (!res.ok) {
        const data = await res.json().catch(() => ({}))
        throw new Error((data as { error?: string }).error ?? 'Failed to create user')
      }
      onCreated()
      handleOpenChange(false)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create user')
    } finally {
      setLoading(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Create Local User</DialogTitle>
          <DialogDescription>
            Create a local account. The user can log in immediately with the credentials you set.
          </DialogDescription>
        </DialogHeader>
        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="cu-username">Username</Label>
            <Input
              id="cu-username"
              value={username}
              onChange={e => setUsername(e.target.value)}
              required
              disabled={loading}
              placeholder="jsmith"
              autoComplete="off"
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="cu-display">Display name <span className="text-muted-foreground font-normal">(optional)</span></Label>
            <Input
              id="cu-display"
              value={displayName}
              onChange={e => setDisplayName(e.target.value)}
              disabled={loading}
              placeholder="Jane Smith"
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="cu-password">Password</Label>
            <Input
              id="cu-password"
              type="password"
              value={password}
              onChange={e => setPassword(e.target.value)}
              required
              disabled={loading}
              placeholder="Minimum 15 characters"
              autoComplete="new-password"
            />
            {password.length > 0 && password.length < 15 && (
              <p className="text-xs text-muted-foreground">{15 - password.length} more characters needed</p>
            )}
          </div>
          <div className="space-y-2">
            <Label htmlFor="cu-role">Role</Label>
            <Select value={role} onValueChange={setRole} disabled={loading}>
              <SelectTrigger id="cu-role">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="GeneralUser">General User</SelectItem>
                <SelectItem value="Admin">Admin</SelectItem>
              </SelectContent>
            </Select>
          </div>
          {error && <p className="text-sm text-destructive">{error}</p>}
          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => handleOpenChange(false)} disabled={loading}>Cancel</Button>
            <Button type="submit" disabled={loading || password.length < 15}>
              {loading ? 'Creating...' : 'Create User'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}

// ----- Invite dialog -----

interface InviteDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onCreated: () => void
}

function InviteDialog({ open, onOpenChange, onCreated }: InviteDialogProps) {
  const [email, setEmail] = useState('')
  const [role, setRole] = useState('GeneralUser')
  const [inviteUrl, setInviteUrl] = useState('')
  const [copied, setCopied] = useState(false)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

  function reset() {
    setEmail(''); setRole('GeneralUser'); setInviteUrl(''); setCopied(false); setError('')
  }

  function handleOpenChange(next: boolean) {
    if (!next) { reset(); onCreated() }
    onOpenChange(next)
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setLoading(true); setError('')
    try {
      const res = await fetch('/api/invitations', {
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, role }),
      })
      if (!res.ok) {
        const data = await res.json().catch(() => ({}))
        throw new Error((data as { error?: string }).error ?? 'Failed to create invitation')
      }
      const data = await res.json()
      setInviteUrl(data.inviteUrl)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create invitation')
    } finally {
      setLoading(false)
    }
  }

  async function copyUrl() {
    await navigator.clipboard.writeText(inviteUrl)
    setCopied(true)
    setTimeout(() => setCopied(false), 2000)
  }

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Invite User</DialogTitle>
          <DialogDescription>
            An invitation link will be generated. Send it to the user out of band.
          </DialogDescription>
        </DialogHeader>

        {inviteUrl ? (
          <div className="space-y-4">
            <p className="text-sm">Invitation created for <strong>{email}</strong> as <strong>{role === 'Admin' ? 'Admin' : 'General User'}</strong>.</p>
            <div className="space-y-2">
              <Label>Invitation link</Label>
              <div className="flex gap-2">
                <Input value={inviteUrl} readOnly className="text-xs font-mono" />
                <Button size="icon" variant="outline" onClick={copyUrl}>
                  {copied ? <Check className="h-4 w-4 text-green-500" /> : <Copy className="h-4 w-4" />}
                </Button>
              </div>
              <p className="text-xs text-muted-foreground">This link expires in 48 hours. Copy and send it now.</p>
            </div>
            <DialogFooter>
              <Button onClick={() => handleOpenChange(false)}>Done</Button>
            </DialogFooter>
          </div>
        ) : (
          <form onSubmit={handleSubmit} className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="invite-email">Email address</Label>
              <Input
                id="invite-email"
                type="email"
                value={email}
                onChange={e => setEmail(e.target.value)}
                required
                disabled={loading}
                placeholder="user@example.com"
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="invite-role">Role</Label>
              <Select value={role} onValueChange={setRole} disabled={loading}>
                <SelectTrigger id="invite-role">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="GeneralUser">General User</SelectItem>
                  <SelectItem value="Admin">Admin</SelectItem>
                </SelectContent>
              </Select>
            </div>
            {error && <p className="text-sm text-destructive">{error}</p>}
            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => handleOpenChange(false)} disabled={loading}>Cancel</Button>
              <Button type="submit" disabled={loading}>{loading ? 'Creating...' : 'Create Invitation'}</Button>
            </DialogFooter>
          </form>
        )}
      </DialogContent>
    </Dialog>
  )
}

// ----- Main page -----

export function UsersPage() {
  const [users, setUsers] = useState<UserRow[]>([])
  const [invites, setInvites] = useState<PendingInvite[]>([])
  const [createOpen, setCreateOpen] = useState(false)
  const [inviteOpen, setInviteOpen] = useState(false)
  const [confirm, setConfirm] = useState<{
    title: string; description: string; confirmLabel: string
    destructive: boolean; action: () => Promise<void>
  } | null>(null)

  const load = useCallback(async () => {
    const [ur, ir] = await Promise.all([
      fetch('/api/users', { credentials: 'include' }),
      fetch('/api/invitations', { credentials: 'include' }),
    ])
    if (ur.ok) setUsers(await ur.json())
    if (ir.ok) setInvites(await ir.json())
  }, [])

  useEffect(() => { load() }, [load])

  function confirm_(opts: typeof confirm) { setConfirm(opts) }

  function actionFor(user: UserRow) {
    async function post(path: string) {
      const res = await fetch(path, { method: 'POST', credentials: 'include' })
      if (!res.ok) {
        const data = await res.json().catch(() => ({}))
        throw new Error((data as { error?: string }).error ?? 'Action failed')
      }
      await load()
    }

    return {
      disable: () => confirm_({
        title: 'Disable Account',
        description: `Disable ${user.username}? They will not be able to log in. This can be reversed.`,
        confirmLabel: 'Disable',
        destructive: false,
        action: () => post(`/api/users/${user.id}/disable`),
      }),
      reenable: () => confirm_({
        title: 'Re-enable Account',
        description: `Re-enable ${user.username}? They will be able to log in again.`,
        confirmLabel: 'Re-enable',
        destructive: false,
        action: () => post(`/api/users/${user.id}/reenable`),
      }),
      promote: () => confirm_({
        title: 'Promote to Admin',
        description: `Promote ${user.username} to Admin? They will gain full administrative access.`,
        confirmLabel: 'Promote',
        destructive: false,
        action: () => post(`/api/users/${user.id}/promote`),
      }),
      demote: () => confirm_({
        title: 'Demote to General User',
        description: `Demote ${user.username} to General User? They will lose administrative access.`,
        confirmLabel: 'Demote',
        destructive: false,
        action: () => post(`/api/users/${user.id}/demote`),
      }),
      remove: () => confirm_({
        title: 'Remove User',
        description: `Remove ${user.username}? Their collection data will be exported before deletion. This is permanent and cannot be undone.`,
        confirmLabel: 'Remove User',
        destructive: true,
        action: () => post(`/api/users/${user.id}/remove`),
      }),
    }
  }

  const columns: ColumnDef<UserRow>[] = [
    {
      accessorKey: 'username',
      header: 'Username',
      cell: ({ row }) => (
        <div>
          <p className="font-medium text-sm">{row.original.username}</p>
          {row.original.displayName !== row.original.username && (
            <p className="text-xs text-muted-foreground">{row.original.displayName}</p>
          )}
        </div>
      ),
    },
    {
      accessorKey: 'role',
      header: 'Role',
      cell: ({ row }) => <RoleBadge role={row.original.role} />,
    },
    {
      accessorKey: 'state',
      header: 'State',
      cell: ({ row }) => <StateBadge state={row.original.state} />,
    },
    {
      accessorKey: 'authType',
      header: 'Auth',
      cell: ({ row }) => (
        <span className="text-sm text-muted-foreground">
          {row.original.authType === 'Local' ? 'Local' : row.original.authType}
        </span>
      ),
    },
    {
      accessorKey: 'lastLoginAt',
      header: 'Last Login',
      cell: ({ row }) => formatDate(row.original.lastLoginAt),
    },
    {
      id: 'actions',
      cell: ({ row }) => {
        const u = row.original
        if (u.isBuiltinAdmin) return null
        const a = actionFor(u)
        return (
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="icon" className="h-8 w-8">
                <MoreHorizontal className="h-4 w-4" />
                <span className="sr-only">Actions</span>
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              {u.state === 'Active' && u.role === 'GeneralUser' && (
                <DropdownMenuItem onClick={a.promote}>Promote to Admin</DropdownMenuItem>
              )}
              {u.state === 'Active' && u.role === 'Admin' && (
                <DropdownMenuItem onClick={a.demote}>Demote to General User</DropdownMenuItem>
              )}
              <DropdownMenuSeparator />
              {u.state === 'Active' && (
                <DropdownMenuItem onClick={a.disable}>Disable</DropdownMenuItem>
              )}
              {u.state === 'Disabled' && (
                <DropdownMenuItem onClick={a.reenable}>Re-enable</DropdownMenuItem>
              )}
              <DropdownMenuSeparator />
              <DropdownMenuItem onClick={a.remove} className="text-destructive focus:text-destructive">
                Remove
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        )
      },
    },
  ]

  const table = useReactTable({
    data: users,
    columns,
    getCoreRowModel: getCoreRowModel(),
  })

  async function revokeInvite(id: string) {
    await fetch(`/api/invitations/${id}`, { method: 'DELETE', credentials: 'include' })
    setInvites(prev => prev.filter(i => i.id !== id))
  }

  return (
    <div className="space-y-8">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-semibold">Users</h1>
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button>
              <UserPlus className="h-4 w-4 mr-2" />
              Add User
              <ChevronDown className="h-4 w-4 ml-2" />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            <DropdownMenuItem onClick={() => setCreateOpen(true)}>
              <UserPlus className="h-4 w-4 mr-2" />
              Create local user
            </DropdownMenuItem>
            <DropdownMenuItem onClick={() => setInviteOpen(true)}>
              <Mail className="h-4 w-4 mr-2" />
              Send invitation link
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>

      <div className="rounded-md border">
        <Table>
          <TableHeader>
            {table.getHeaderGroups().map(hg => (
              <TableRow key={hg.id}>
                {hg.headers.map(h => (
                  <TableHead key={h.id}>
                    {flexRender(h.column.columnDef.header, h.getContext())}
                  </TableHead>
                ))}
              </TableRow>
            ))}
          </TableHeader>
          <TableBody>
            {table.getRowModel().rows.length === 0 ? (
              <TableRow>
                <TableCell colSpan={columns.length} className="text-center text-muted-foreground py-8">
                  No users found.
                </TableCell>
              </TableRow>
            ) : (
              table.getRowModel().rows.map(row => (
                <TableRow key={row.id}>
                  {row.getVisibleCells().map(cell => (
                    <TableCell key={cell.id}>
                      {flexRender(cell.column.columnDef.cell, cell.getContext())}
                    </TableCell>
                  ))}
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </div>

      {invites.length > 0 && (
        <div className="space-y-3">
          <h2 className="text-sm font-medium text-muted-foreground uppercase tracking-wide">
            Pending Invitations
          </h2>
          <div className="rounded-md border divide-y">
            {invites.map(inv => (
              <div key={inv.id} className="flex items-center justify-between px-4 py-3 text-sm">
                <div>
                  <p className="font-medium">{inv.email}</p>
                  <p className="text-xs text-muted-foreground">
                    {inv.role === 'Admin' ? 'Admin' : 'General User'} - expires {new Date(inv.expiresAt).toLocaleDateString()}
                  </p>
                </div>
                <Button
                  variant="ghost"
                  size="sm"
                  className="text-destructive hover:text-destructive"
                  onClick={() => revokeInvite(inv.id)}
                >
                  Revoke
                </Button>
              </div>
            ))}
          </div>
        </div>
      )}

      <CreateUserDialog open={createOpen} onOpenChange={setCreateOpen} onCreated={load} />
      <InviteDialog open={inviteOpen} onOpenChange={setInviteOpen} onCreated={load} />

      {confirm && (
        <ConfirmDialog
          open={true}
          onOpenChange={open => { if (!open) setConfirm(null) }}
          title={confirm.title}
          description={confirm.description}
          confirmLabel={confirm.confirmLabel}
          destructive={confirm.destructive}
          onConfirm={confirm.action}
        />
      )}
    </div>
  )
}
