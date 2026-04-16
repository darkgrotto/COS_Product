import { NavLink, Outlet, Navigate, useLocation } from 'react-router-dom';

const TABS = [
  { to: '/admin/administration/users', label: 'User Management' },
  { to: '/admin/administration/backup', label: 'Backup & Restore' },
  { to: '/admin/administration/config', label: 'Instance Settings' },
  { to: '/admin/administration/log-forwarding', label: 'Log Forwarding' },
];

export function AdministrationHub() {
  const { pathname } = useLocation();

  if (pathname === '/admin/administration' || pathname === '/admin/administration/') {
    return <Navigate to="/admin/administration/users" replace />;
  }

  return (
    <div>
      <h2>Administration</h2>
      <nav aria-label="Administration sections">
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
