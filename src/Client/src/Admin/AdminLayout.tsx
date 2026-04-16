import { NavLink, Outlet, Navigate, useLocation } from 'react-router-dom';

const TABS = [
  { to: '/admin/dashboard', label: 'Dashboard' },
  { to: '/admin/content', label: 'Content Browser' },
  { to: '/admin/operations', label: 'Operations' },
  { to: '/admin/administration', label: 'Administration' },
];

export function AdminLayout() {
  const { pathname } = useLocation();

  // Redirect bare /admin to dashboard
  if (pathname === '/admin' || pathname === '/admin/') {
    return <Navigate to="/admin/dashboard" replace />;
  }

  return (
    <div>
      <nav aria-label="Admin sections">
        {TABS.map((tab) => (
          <NavLink
            key={tab.to}
            to={tab.to}
            // Use prefix match so sub-routes keep the parent tab highlighted
            className={pathname.startsWith(tab.to) ? 'active' : undefined}
            end={false}
          >
            {tab.label}
          </NavLink>
        ))}
      </nav>
      <Outlet />
    </div>
  );
}
