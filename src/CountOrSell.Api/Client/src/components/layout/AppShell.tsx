import { Outlet } from 'react-router-dom'
import { DemoBanner } from '@/components/DemoBanner'
import { Sidebar } from './Sidebar'
import { TopNav } from './TopNav'
import { TooltipProvider } from '@/components/ui/tooltip'
import { usePreferences } from '@/contexts/PreferencesContext'

export function AppShell() {
  const { prefs } = usePreferences()
  const isTop = prefs.navLayout === 'top'

  return (
    <TooltipProvider>
      <div className="flex flex-col h-screen">
        <DemoBanner />
        {isTop ? (
          <>
            <TopNav />
            <main className="flex-1 overflow-auto p-6">
              <Outlet />
            </main>
          </>
        ) : (
          <div className="flex flex-1 overflow-hidden">
            <Sidebar />
            <main className="flex-1 overflow-auto p-6">
              <Outlet />
            </main>
          </div>
        )}
      </div>
    </TooltipProvider>
  )
}
