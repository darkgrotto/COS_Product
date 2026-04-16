import { NavLink, Outlet, Navigate, useLocation } from 'react-router-dom'
import { cn } from '@/lib/utils'

const tabs = [
  { to: '/admin/dashboard', label: 'Dashboard' },
  { to: '/admin/content', label: 'Content Browser' },
  { to: '/admin/operations', label: 'Operations' },
  { to: '/admin/administration', label: 'Administration' },
]

export function AdminLayout() {
  const { pathname } = useLocation()

  if (pathname === '/admin') {
    return <Navigate to="/admin/dashboard" replace />
  }

  return (
    <div className="flex flex-col h-full">
      <div className="flex gap-1 border-b mb-6">
        {tabs.map(tab => (
          <NavLink
            key={tab.to}
            to={tab.to}
            end={false}
            className={cn(
              'px-4 py-2 text-sm font-medium -mb-px border-b-2 transition-colors',
              pathname.startsWith(tab.to)
                ? 'border-primary text-foreground'
                : 'border-transparent text-muted-foreground hover:text-foreground'
            )}
          >
            {tab.label}
          </NavLink>
        ))}
      </div>
      <div className="flex-1">
        <Outlet />
      </div>
    </div>
  )
}
