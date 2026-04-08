import { useCallback, useEffect, useState } from 'react'
import { Settings } from 'lucide-react'
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

// ---- Types ------------------------------------------------------------------

interface BackupSettings {
  schedule: string
  retentionScheduled: number
  retentionPreUpdate: number
}

interface TcgPlayerStatus {
  configured: boolean
  maskedKey: string | null
}

interface OAuthProvider {
  provider: string
  clientId: string | null
  secretConfigured: boolean
}

// ---- Helpers ----------------------------------------------------------------

const PROVIDER_LABELS: Record<string, string> = {
  google: 'Google',
  microsoft: 'Microsoft',
  github: 'GitHub',
}

// ---- Configure OAuth Dialog -------------------------------------------------

function ConfigureOAuthDialog({
  provider,
  existing,
  onClose,
  onSaved,
}: {
  provider: string
  existing: OAuthProvider
  onClose: () => void
  onSaved: () => void
}) {
  const [clientId, setClientId] = useState(existing.clientId ?? '')
  const [clientSecret, setClientSecret] = useState('')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')

  async function handleSave() {
    if (!clientId.trim() && !clientSecret.trim()) {
      setError('Enter a Client ID or Client Secret to update.')
      return
    }
    setError('')
    setSaving(true)
    try {
      const res = await fetch(`/api/settings/oauth/${provider}`, {
        method: 'PATCH',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          clientId: clientId.trim() || null,
          clientSecret: clientSecret.trim() || null,
        }),
      })
      if (!res.ok) {
        const d = await res.json().catch(() => ({}))
        throw new Error((d as { error?: string }).error ?? 'Failed to save.')
      }
      onSaved()
      onClose()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <Dialog open onOpenChange={v => !v && onClose()}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Configure {PROVIDER_LABELS[provider] ?? provider} OAuth</DialogTitle>
        </DialogHeader>
        <div className="space-y-4">
          <div>
            <Label>Client ID</Label>
            <Input
              value={clientId}
              onChange={e => setClientId(e.target.value)}
              placeholder="Client ID"
              autoComplete="off"
            />
          </div>
          <div>
            <Label>Client Secret</Label>
            <Input
              type="password"
              value={clientSecret}
              onChange={e => setClientSecret(e.target.value)}
              placeholder={existing.secretConfigured ? 'Leave blank to keep existing' : 'Client Secret'}
              autoComplete="new-password"
            />
          </div>
          {error && <p className="text-sm text-destructive">{error}</p>}
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={onClose} disabled={saving}>Cancel</Button>
          <Button onClick={handleSave} disabled={saving}>{saving ? 'Saving...' : 'Save'}</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}

// ---- Section wrapper --------------------------------------------------------

function Section({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="rounded-lg border">
      <div className="px-4 py-3 border-b bg-muted/30">
        <h2 className="text-sm font-semibold">{title}</h2>
      </div>
      <div className="p-4 space-y-4">{children}</div>
    </div>
  )
}

// ---- Flash state hook -------------------------------------------------------

function useFlash() {
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')

  function flash(msg: string, isError = false) {
    setError(''); setSuccess('')
    if (isError) setError(msg); else setSuccess(msg)
    setTimeout(() => { setError(''); setSuccess('') }, 5000)
  }

  return { error, success, flash }
}

// ---- Main page --------------------------------------------------------------

export function SettingsPage() {
  // Instance branding
  const [instanceName, setInstanceName] = useState('')
  const [instanceNameInput, setInstanceNameInput] = useState('')
  const [instanceSaving, setInstanceSaving] = useState(false)

  // TCGPlayer
  const [tcg, setTcg] = useState<TcgPlayerStatus>({ configured: false, maskedKey: null })
  const [tcgKeyInput, setTcgKeyInput] = useState('')
  const [tcgSaving, setTcgSaving] = useState(false)

  // Self-enrollment
  const [selfEnrollment, setSelfEnrollment] = useState(false)
  const [selfEnrollmentSaving, setSelfEnrollmentSaving] = useState(false)

  // Backup settings
  const [backupSettings, setBackupSettings] = useState<BackupSettings>({
    schedule: 'weekly',
    retentionScheduled: 4,
    retentionPreUpdate: 4,
  })
  const [scheduleInput, setScheduleInput] = useState('weekly')
  const [retentionScheduledInput, setRetentionScheduledInput] = useState('4')
  const [retentionPreUpdateInput, setRetentionPreUpdateInput] = useState('4')
  const [backupSaving, setBackupSaving] = useState(false)

  // OAuth
  const [oauthProviders, setOauthProviders] = useState<OAuthProvider[]>([])
  const [oauthDialog, setOauthDialog] = useState<OAuthProvider | null>(null)
  const [oauthClearBusy, setOauthClearBusy] = useState<Record<string, boolean>>({})

  const [loading, setLoading] = useState(true)
  const { error, success, flash } = useFlash()

  const loadAll = useCallback(async () => {
    try {
      const [instRes, tcgRes, seRes, backupRes, oauthRes] = await Promise.all([
        fetch('/api/settings/instance'),
        fetch('/api/settings/tcgplayer'),
        fetch('/api/settings/self-enrollment'),
        fetch('/api/settings/backup'),
        fetch('/api/settings/oauth'),
      ])

      if (instRes.ok) {
        const d = await instRes.json()
        setInstanceName(d.instanceName ?? '')
        setInstanceNameInput(d.instanceName ?? '')
      }
      if (tcgRes.ok) {
        const d: TcgPlayerStatus = await tcgRes.json()
        setTcg(d)
      }
      if (seRes.ok) {
        const d = await seRes.json()
        setSelfEnrollment(d.enabled ?? false)
      }
      if (backupRes.ok) {
        const d: BackupSettings = await backupRes.json()
        setBackupSettings(d)
        setScheduleInput(d.schedule)
        setRetentionScheduledInput(String(d.retentionScheduled))
        setRetentionPreUpdateInput(String(d.retentionPreUpdate))
      }
      if (oauthRes.ok) {
        const d: OAuthProvider[] = await oauthRes.json()
        setOauthProviders(d)
      }
    } catch {
      // partial load acceptable
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => { loadAll() }, [loadAll])

  async function saveInstance() {
    if (!instanceNameInput.trim()) { flash('Instance name is required.', true); return }
    setInstanceSaving(true)
    try {
      const res = await fetch('/api/settings/instance', {
        method: 'PATCH',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ instanceName: instanceNameInput.trim() }),
      })
      if (!res.ok) {
        const d = await res.json().catch(() => ({}))
        throw new Error((d as { error?: string }).error ?? 'Failed to save.')
      }
      setInstanceName(instanceNameInput.trim())
      flash('Instance name saved.')
    } catch (err) {
      flash(err instanceof Error ? err.message : 'Failed to save.', true)
    } finally {
      setInstanceSaving(false)
    }
  }

  async function saveTcgKey() {
    if (!tcgKeyInput.trim()) { flash('API key is required.', true); return }
    setTcgSaving(true)
    try {
      const res = await fetch('/api/settings/tcgplayer', {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ apiKey: tcgKeyInput.trim() }),
      })
      if (!res.ok) {
        const d = await res.json().catch(() => ({}))
        throw new Error((d as { error?: string }).error ?? 'Failed to save key.')
      }
      setTcgKeyInput('')
      flash('TCGPlayer API key saved.')
      const freshRes = await fetch('/api/settings/tcgplayer')
      if (freshRes.ok) setTcg(await freshRes.json())
    } catch (err) {
      flash(err instanceof Error ? err.message : 'Failed to save key.', true)
    } finally {
      setTcgSaving(false)
    }
  }

  async function clearTcgKey() {
    setTcgSaving(true)
    try {
      await fetch('/api/settings/tcgplayer', { method: 'DELETE' })
      setTcg({ configured: false, maskedKey: null })
      flash('TCGPlayer API key cleared.')
    } catch {
      flash('Failed to clear key.', true)
    } finally {
      setTcgSaving(false)
    }
  }

  async function saveSelfEnrollment(val: boolean) {
    setSelfEnrollmentSaving(true)
    try {
      const res = await fetch('/api/settings/self-enrollment', {
        method: 'PATCH',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ enabled: val }),
      })
      if (!res.ok) throw new Error('Failed to save.')
      setSelfEnrollment(val)
      flash(`Self-enrollment ${val ? 'enabled' : 'disabled'}.`)
    } catch {
      flash('Failed to save self-enrollment setting.', true)
    } finally {
      setSelfEnrollmentSaving(false)
    }
  }

  async function saveBackupSettings() {
    const rs = parseInt(retentionScheduledInput)
    const rp = parseInt(retentionPreUpdateInput)
    if (isNaN(rs) || rs < 1) { flash('Scheduled retention must be at least 1.', true); return }
    if (isNaN(rp) || rp < 1) { flash('Pre-update retention must be at least 1.', true); return }
    setBackupSaving(true)
    try {
      const res = await fetch('/api/settings/backup', {
        method: 'PATCH',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          schedule: scheduleInput,
          retentionScheduled: rs,
          retentionPreUpdate: rp,
        }),
      })
      if (!res.ok) throw new Error('Failed to save backup settings.')
      setBackupSettings({ schedule: scheduleInput, retentionScheduled: rs, retentionPreUpdate: rp })
      flash('Backup settings saved.')
    } catch {
      flash('Failed to save backup settings.', true)
    } finally {
      setBackupSaving(false)
    }
  }

  async function clearOAuth(provider: string) {
    setOauthClearBusy(prev => ({ ...prev, [provider]: true }))
    try {
      const res = await fetch(`/api/settings/oauth/${provider}`, { method: 'DELETE' })
      if (!res.ok) throw new Error('Failed to clear.')
      flash(`${PROVIDER_LABELS[provider] ?? provider} OAuth credentials cleared.`)
      setOauthProviders(prev => prev.map(p =>
        p.provider === provider ? { ...p, clientId: null, secretConfigured: false } : p
      ))
    } catch {
      flash('Failed to clear OAuth credentials.', true)
    } finally {
      setOauthClearBusy(prev => ({ ...prev, [provider]: false }))
    }
  }

  const backupDirty =
    scheduleInput !== backupSettings.schedule ||
    retentionScheduledInput !== String(backupSettings.retentionScheduled) ||
    retentionPreUpdateInput !== String(backupSettings.retentionPreUpdate)

  const instanceDirty = instanceNameInput !== instanceName

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-2">
        <Settings className="h-5 w-5 text-muted-foreground" />
        <h1 className="text-2xl font-semibold">Settings</h1>
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
        <div className="space-y-4 max-w-2xl">

          {/* Instance Branding */}
          <Section title="Instance Branding">
            <div className="space-y-2">
              <Label htmlFor="instance-name">Instance Name</Label>
              <div className="flex gap-2">
                <Input
                  id="instance-name"
                  value={instanceNameInput}
                  onChange={e => setInstanceNameInput(e.target.value)}
                  placeholder="CountOrSell"
                  className="flex-1"
                />
                <Button onClick={saveInstance} disabled={instanceSaving || !instanceDirty}>
                  {instanceSaving ? 'Saving...' : 'Save'}
                </Button>
              </div>
              <p className="text-xs text-muted-foreground">
                Displayed in the sidebar header, browser tab, and login page.
              </p>
            </div>
          </Section>

          {/* TCGPlayer */}
          <Section title="TCGPlayer API">
            <div className="space-y-3">
              <div className="flex items-center gap-2">
                <span className="text-sm">Status:</span>
                {tcg.configured ? (
                  <Badge variant="secondary">Configured</Badge>
                ) : (
                  <Badge variant="outline" className="text-muted-foreground">Not configured</Badge>
                )}
                {tcg.configured && tcg.maskedKey && (
                  <span className="text-xs text-muted-foreground font-mono">{tcg.maskedKey}</span>
                )}
              </div>
              <div className="space-y-2">
                <Label htmlFor="tcg-key">{tcg.configured ? 'Replace API Key' : 'API Key'}</Label>
                <div className="flex gap-2">
                  <Input
                    id="tcg-key"
                    type="password"
                    value={tcgKeyInput}
                    onChange={e => setTcgKeyInput(e.target.value)}
                    placeholder={tcg.configured ? 'Enter new key to replace' : 'Paste your TCGPlayer API key'}
                    autoComplete="new-password"
                    className="flex-1"
                  />
                  <Button onClick={saveTcgKey} disabled={tcgSaving || !tcgKeyInput.trim()}>
                    {tcgSaving ? 'Saving...' : 'Save'}
                  </Button>
                  {tcg.configured && (
                    <Button variant="outline" onClick={clearTcgKey} disabled={tcgSaving}>
                      Clear
                    </Button>
                  )}
                </div>
              </div>
              <p className="text-xs text-muted-foreground">
                Required for per-card TCGPlayer price refresh. Key is stored securely and never displayed in full.
              </p>
            </div>
          </Section>

          {/* Self-Enrollment */}
          <Section title="Self-Enrollment">
            <div className="space-y-3">
              <div className="flex items-start gap-3">
                <input
                  type="checkbox"
                  id="self-enrollment"
                  checked={selfEnrollment}
                  disabled={selfEnrollmentSaving}
                  onChange={e => saveSelfEnrollment(e.target.checked)}
                  className="h-4 w-4 mt-0.5 rounded border-input shrink-0"
                />
                <div>
                  <label htmlFor="self-enrollment" className="text-sm font-medium cursor-pointer">
                    Allow self-enrollment
                  </label>
                  <p className="text-xs text-muted-foreground mt-0.5">
                    When enabled, new users can register without admin invitation. New accounts receive General User access immediately.
                  </p>
                </div>
              </div>
            </div>
          </Section>

          {/* Backup Schedule */}
          <Section title="Backup Schedule">
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-1.5">
                <Label>Schedule</Label>
                <Select value={scheduleInput} onValueChange={setScheduleInput}>
                  <SelectTrigger><SelectValue /></SelectTrigger>
                  <SelectContent>
                    <SelectItem value="weekly">Weekly (Sundays at 03:00 UTC)</SelectItem>
                    <SelectItem value="daily">Daily (03:00 UTC)</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              <div />
              <div className="space-y-1.5">
                <Label>Scheduled Backup Retention</Label>
                <Input
                  type="number"
                  min={1}
                  max={99}
                  value={retentionScheduledInput}
                  onChange={e => setRetentionScheduledInput(e.target.value)}
                />
                <p className="text-xs text-muted-foreground">Most recent N scheduled backups kept</p>
              </div>
              <div className="space-y-1.5">
                <Label>Pre-Update Backup Retention</Label>
                <Input
                  type="number"
                  min={1}
                  max={99}
                  value={retentionPreUpdateInput}
                  onChange={e => setRetentionPreUpdateInput(e.target.value)}
                />
                <p className="text-xs text-muted-foreground">Most recent N pre-update backups kept</p>
              </div>
            </div>
            <div className="pt-1">
              <Button onClick={saveBackupSettings} disabled={backupSaving || !backupDirty}>
                {backupSaving ? 'Saving...' : 'Save'}
              </Button>
            </div>
          </Section>

          {/* OAuth Providers */}
          <Section title="OAuth Providers">
            <p className="text-xs text-muted-foreground">
              Configure OAuth providers for user login. Redirect URI for each provider must be set to{' '}
              <span className="font-mono">/signin-[provider]</span> on your instance URL.
            </p>
            <div className="rounded-md border divide-y">
              {oauthProviders.length === 0 ? (
                <p className="px-4 py-3 text-sm text-muted-foreground">Loading providers...</p>
              ) : (
                oauthProviders.map(p => (
                  <div key={p.provider} className="flex items-center gap-3 px-4 py-3">
                    <div className="flex-1 min-w-0">
                      <p className="text-sm font-medium">{PROVIDER_LABELS[p.provider] ?? p.provider}</p>
                      <div className="flex items-center gap-2 mt-0.5">
                        {p.clientId ? (
                          <span className="text-xs text-muted-foreground font-mono truncate max-w-48" title={p.clientId}>
                            ID: {p.clientId}
                          </span>
                        ) : (
                          <span className="text-xs text-muted-foreground">Not configured</span>
                        )}
                        {p.secretConfigured && (
                          <Badge variant="outline" className="text-xs shrink-0">Secret set</Badge>
                        )}
                      </div>
                    </div>
                    <Button
                      size="sm"
                      variant="outline"
                      className="text-xs h-7 shrink-0"
                      onClick={() => setOauthDialog(p)}
                    >
                      {p.clientId ? 'Edit' : 'Configure'}
                    </Button>
                    {(p.clientId || p.secretConfigured) && (
                      <Button
                        size="sm"
                        variant="ghost"
                        className="text-xs h-7 text-destructive hover:text-destructive shrink-0"
                        disabled={oauthClearBusy[p.provider]}
                        onClick={() => clearOAuth(p.provider)}
                      >
                        Clear
                      </Button>
                    )}
                  </div>
                ))
              )}
            </div>
          </Section>

        </div>
      )}

      {oauthDialog && (
        <ConfigureOAuthDialog
          provider={oauthDialog.provider}
          existing={oauthDialog}
          onClose={() => setOauthDialog(null)}
          onSaved={() => {
            flash(`${PROVIDER_LABELS[oauthDialog.provider] ?? oauthDialog.provider} OAuth saved.`)
            loadAll()
          }}
        />
      )}
    </div>
  )
}
