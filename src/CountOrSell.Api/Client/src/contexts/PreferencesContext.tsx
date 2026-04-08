import React, { createContext, useCallback, useContext, useEffect, useState } from 'react'
import { useAuth } from '@/contexts/AuthContext'

export interface UserPreferences {
  setCompletionRegularOnly: boolean
  defaultPage: string | null
  defaultAcquisitionPriceToMarket: boolean
  darkMode: boolean
  navLayout: 'sidebar' | 'top'
}

const DEFAULT_PREFS: UserPreferences = {
  setCompletionRegularOnly: false,
  defaultPage: null,
  defaultAcquisitionPriceToMarket: true,
  darkMode: false,
  navLayout: 'sidebar',
}

interface PreferencesContextValue {
  prefs: UserPreferences
  patchPrefs: (patch: Partial<UserPreferences>) => Promise<void>
}

const PreferencesContext = createContext<PreferencesContextValue | null>(null)

export function PreferencesProvider({ children }: { children: React.ReactNode }) {
  const { user } = useAuth()
  const [prefs, setPrefs] = useState<UserPreferences>(DEFAULT_PREFS)

  useEffect(() => {
    if (!user) { setPrefs(DEFAULT_PREFS); return }
    fetch('/api/users/me/preferences', { credentials: 'include' })
      .then(r => r.ok ? r.json() : null)
      .then((data: UserPreferences | null) => {
        if (data) setPrefs({ ...DEFAULT_PREFS, ...data })
      })
      .catch(() => {})
  }, [user])

  useEffect(() => {
    document.documentElement.classList.toggle('dark', prefs.darkMode)
  }, [prefs.darkMode])

  const patchPrefs = useCallback(async (patch: Partial<UserPreferences>) => {
    const res = await fetch('/api/users/me/preferences', {
      method: 'PATCH',
      credentials: 'include',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(patch),
    })
    if (!res.ok) throw new Error('Failed to save preferences.')
    setPrefs(prev => ({ ...prev, ...patch }))
  }, [])

  return (
    <PreferencesContext.Provider value={{ prefs, patchPrefs }}>
      {children}
    </PreferencesContext.Provider>
  )
}

export function usePreferences() {
  const ctx = useContext(PreferencesContext)
  if (!ctx) throw new Error('usePreferences must be used within PreferencesProvider')
  return ctx
}
