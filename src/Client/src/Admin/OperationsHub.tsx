import { NavLink, Outlet, Navigate, useLocation } from 'react-router-dom';

const TABS = [
  { to: '/admin/operations/updates', label: 'Content Updates' },
  { to: '/admin/operations/notifications', label: 'Notifications' },
  { to: '/admin/operations/logs', label: 'Logs' },
];

export function OperationsHub() {
  const { pathname } = useLocation();

  if (pathname === '/admin/operations' || pathname === '/admin/operations/') {
    return <Navigate to="/admin/operations/updates" replace />;
  }

  return (
    <div>
      <h2>Operations</h2>
      <nav aria-label="Operations sections">
        {TABS.map((tab) => (
          <NavLink key={tab.to} to={tab.to}>
            {tab.label}
          </NavLink>
        ))}
      </nav>
      <Outlet />
    </div>
  );
}
