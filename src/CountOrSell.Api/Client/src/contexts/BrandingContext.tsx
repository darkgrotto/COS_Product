import React, { createContext, useContext, useEffect, useState } from 'react'

interface BrandingContextValue {
  instanceName: string
}

const BrandingContext = createContext<BrandingContextValue>({ instanceName: 'CountOrSell' })

export function BrandingProvider({ children }: { children: React.ReactNode }) {
  const [instanceName, setInstanceName] = useState('CountOrSell')

  useEffect(() => {
    fetch('/api/branding')
      .then(r => r.ok ? r.json() : null)
      .then((d: { instanceName?: string } | null) => {
        if (d?.instanceName) setInstanceName(d.instanceName)
      })
      .catch(() => {})
  }, [])

  useEffect(() => {
    document.title = `${instanceName} - CountOrSell`
  }, [instanceName])

  return (
    <BrandingContext.Provider value={{ instanceName }}>
      {children}
    </BrandingContext.Provider>
  )
}

export function useBranding() {
  return useContext(BrandingContext)
}
