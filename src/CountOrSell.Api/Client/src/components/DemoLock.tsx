import React from 'react'
import { useDemo } from '@/contexts/DemoContext'
import { Tooltip, TooltipContent, TooltipTrigger } from '@/components/ui/tooltip'

interface DemoLockProps {
  children: React.ReactNode
}

export function DemoLock({ children }: DemoLockProps) {
  const { isDemo } = useDemo()

  if (!isDemo) return <>{children}</>

  return (
    <Tooltip>
      <TooltipTrigger asChild>
        <div className="opacity-50 pointer-events-none select-none" aria-disabled="true">
          {children}
        </div>
      </TooltipTrigger>
      <TooltipContent>
        This action is not available in demo mode.
      </TooltipContent>
    </Tooltip>
  )
}
