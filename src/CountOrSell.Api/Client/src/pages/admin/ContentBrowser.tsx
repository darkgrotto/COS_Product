import { NavLink, Outlet, Navigate, useLocation } from 'react-router-dom'
import { cn } from '@/lib/utils'

const tabs = [
  { to: '/admin/content/cards', label: 'Cards & Sets' },
  { to: '/admin/content/users', label: 'User Collections' },
]

function SubNav() {
  const { pathname } = useLocation()
  return (
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
  )
}

export function ContentBrowser() {
  const { pathname } = useLocation()

  if (pathname === '/admin/content') {
    return <Navigate to="/admin/content/cards" replace />
  }

  return (
    <div>
      <SubNav />
      <Outlet />
    </div>
  )
}
