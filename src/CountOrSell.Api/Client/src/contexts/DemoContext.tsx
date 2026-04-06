import React, { createContext, useContext, useEffect, useState } from 'react'

interface DemoStatus {
  isDemo: boolean
  expiresAt: string | null
  secondsRemaining: number | null
  demoSets: string[]
}

interface DemoContextValue {
  isDemo: boolean
  demoSets: string[]
  secondsRemaining: number | null
}

const DemoContext = createContext<DemoContextValue>({
  isDemo: false,
  demoSets: [],
  secondsRemaining: null,
})

export function DemoProvider({ children }: { children: React.ReactNode }) {
  const [status, setStatus] = useState<DemoStatus>({
    isDemo: false,
    expiresAt: null,
    secondsRemaining: null,
    demoSets: [],
  })

  useEffect(() => {
    fetch('/api/demo/status')
      .then(res => {
        if (res.status === 404) return null
        if (res.ok) return res.json()
        return null
      })
      .then((data: DemoStatus | null) => {
        if (data) setStatus(data)
      })
      .catch(() => {})
  }, [])

  // Count down secondsRemaining once per second when in demo mode with expiry
  useEffect(() => {
    if (!status.isDemo || status.secondsRemaining === null) return
    const id = setInterval(() => {
      setStatus(prev => ({
        ...prev,
        secondsRemaining: prev.secondsRemaining !== null
          ? Math.max(0, prev.secondsRemaining - 1)
          : null,
      }))
    }, 1000)
    return () => clearInterval(id)
  }, [status.isDemo, status.secondsRemaining !== null])

  return (
    <DemoContext.Provider value={{
      isDemo: status.isDemo,
      demoSets: status.demoSets,
      secondsRemaining: status.secondsRemaining,
    }}>
      {children}
    </DemoContext.Provider>
  )
}

export function useDemo() {
  return useContext(DemoContext)
}
