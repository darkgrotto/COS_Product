import { useRef, useState } from 'react'
import { useAuth } from '@/contexts/AuthContext'
import { usePreferences } from '@/contexts/PreferencesContext'
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar'
import { Separator } from '@/components/ui/separator'
import { Trash2, Upload } from 'lucide-react'

interface Props {
  open: boolean
  onOpenChange: (open: boolean) => void
}

export function ProfileDialog({ open, onOpenChange }: Props) {
  const { user, refreshUser } = useAuth()
  const { prefs, patchPrefs } = usePreferences()
  const [displayName, setDisplayName] = useState(user?.displayName ?? '')
  const [prefsSaving, setPrefsSaving] = useState(false)
  const [nameError, setNameError] = useState('')
  const [nameSaving, setNameSaving] = useState(false)
  const [nameSaved, setNameSaved] = useState(false)

  const [avatarError, setAvatarError] = useState('')
  const [avatarWorking, setAvatarWorking] = useState(false)
  const fileRef = useRef<HTMLInputElement>(null)

  // Bust the avatar cache after upload/delete by appending a timestamp
  const [avatarBust, setAvatarBust] = useState(Date.now())

  const initials = user?.username?.slice(0, 2).toUpperCase() ?? '??'
  const avatarSrc = user?.hasAvatar ? `/api/users/me/avatar?t=${avatarBust}` : undefined

  async function saveDisplayName() {
    if (!displayName.trim()) { setNameError('Display name cannot be blank.'); return }
    if (displayName.trim().length > 100) { setNameError('Cannot exceed 100 characters.'); return }
    setNameError('')
    setNameSaving(true)
    try {
      const res = await fetch('/api/users/me/display-name', {
        method: 'PATCH',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ displayName: displayName.trim() }),
      })
      if (!res.ok) {
        const data = await res.json().catch(() => ({}))
        setNameError((data as { error?: string }).error ?? 'Failed to save display name.')
      } else {
        setNameSaved(true)
        setTimeout(() => setNameSaved(false), 2000)
        await refreshUser()
      }
    } finally {
      setNameSaving(false)
    }
  }

  async function uploadAvatar(file: File) {
    setAvatarError('')
    setAvatarWorking(true)
    try {
      const form = new FormData()
      form.append('file', file)
      const res = await fetch('/api/users/me/avatar', {
        method: 'POST',
        credentials: 'include',
        body: form,
      })
      if (!res.ok) {
        const data = await res.json().catch(() => ({}))
        setAvatarError((data as { error?: string }).error ?? 'Upload failed.')
      } else {
        setAvatarBust(Date.now())
        await refreshUser()
      }
    } finally {
      setAvatarWorking(false)
    }
  }

  async function removeAvatar() {
    setAvatarError('')
    setAvatarWorking(true)
    try {
      await fetch('/api/users/me/avatar', { method: 'DELETE', credentials: 'include' })
      setAvatarBust(Date.now())
      await refreshUser()
    } finally {
      setAvatarWorking(false)
    }
  }

  function handleFileChange(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0]
    if (file) uploadAvatar(file)
    e.target.value = ''
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Edit Profile</DialogTitle>
        </DialogHeader>

        <div className="space-y-6 pt-2">
          {/* Avatar section */}
          <div className="flex items-center gap-4">
            <Avatar className="h-16 w-16">
              {avatarSrc && <AvatarImage src={avatarSrc} alt={user?.displayName ?? user?.username} />}
              <AvatarFallback className="text-lg">{initials}</AvatarFallback>
            </Avatar>
            <div className="flex flex-col gap-2">
              <div className="flex gap-2">
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  disabled={avatarWorking}
                  onClick={() => fileRef.current?.click()}
                >
                  <Upload className="h-3.5 w-3.5 mr-1.5" />
                  {user?.hasAvatar ? 'Change' : 'Upload'}
                </Button>
                {user?.hasAvatar && (
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    disabled={avatarWorking}
                    onClick={removeAvatar}
                  >
                    <Trash2 className="h-3.5 w-3.5 mr-1.5" />
                    Remove
                  </Button>
                )}
              </div>
              <p className="text-xs text-muted-foreground">JPEG, PNG, GIF, or WebP. Max 5 MB. Resized to 200x200.</p>
              {avatarError && <p className="text-xs text-destructive">{avatarError}</p>}
            </div>
          </div>
          <input
            ref={fileRef}
            type="file"
            accept="image/jpeg,image/png,image/gif,image/webp"
            className="hidden"
            onChange={handleFileChange}
          />

          {/* Display name section */}
          <div className="space-y-2">
            <Label htmlFor="display-name">Display Name</Label>
            <div className="flex gap-2">
              <Input
                id="display-name"
                value={displayName}
                onChange={e => { setDisplayName(e.target.value); setNameError('') }}
                maxLength={100}
                placeholder="Your display name"
                className="flex-1"
              />
              <Button
                type="button"
                disabled={nameSaving || displayName.trim() === (user?.displayName ?? '')}
                onClick={saveDisplayName}
              >
                {nameSaved ? 'Saved' : nameSaving ? 'Saving...' : 'Save'}
              </Button>
            </div>
            {nameError && <p className="text-xs text-destructive">{nameError}</p>}
            <p className="text-xs text-muted-foreground">Username cannot be changed.</p>
          </div>

          <Separator />

          {/* Preferences section */}
          <div className="space-y-3">
            <p className="text-sm font-medium">Preferences</p>
            <div className="flex items-center justify-between gap-4">
              <div className="space-y-0.5">
                <Label className="text-sm">Default acquisition price to market value</Label>
                <p className="text-xs text-muted-foreground">
                  When adding a card, pre-fill the price with the current market value.
                </p>
              </div>
              <button
                type="button"
                role="switch"
                aria-checked={prefs.defaultAcquisitionPriceToMarket}
                disabled={prefsSaving}
                onClick={async () => {
                  setPrefsSaving(true)
                  try {
                    await patchPrefs({ defaultAcquisitionPriceToMarket: !prefs.defaultAcquisitionPriceToMarket })
                  } finally { setPrefsSaving(false) }
                }}
                className={`relative inline-flex h-5 w-9 shrink-0 cursor-pointer rounded-full border-2 border-transparent transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50 ${prefs.defaultAcquisitionPriceToMarket ? 'bg-primary' : 'bg-input'}`}
              >
                <span
                  className={`pointer-events-none block h-4 w-4 rounded-full bg-background shadow-lg ring-0 transition-transform ${prefs.defaultAcquisitionPriceToMarket ? 'translate-x-4' : 'translate-x-0'}`}
                />
              </button>
            </div>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  )
}
