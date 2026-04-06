import { Outlet } from 'react-router-dom'
import { DemoBanner } from '@/components/DemoBanner'
import { Sidebar } from './Sidebar'
import { TooltipProvider } from '@/components/ui/tooltip'

export function AppShell() {
  return (
    <TooltipProvider>
      <div className="flex flex-col min-h-screen">
        <DemoBanner />
        <div className="flex flex-1">
          <Sidebar />
          <main className="flex-1 overflow-auto p-6">
            <Outlet />
          </main>
        </div>
      </div>
    </TooltipProvider>
  )
}
