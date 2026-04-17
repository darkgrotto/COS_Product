import { NavLink, Outlet, Navigate, useLocation } from 'react-router-dom';

const TABS = [
  { to: '/admin/content/cards', label: 'Sets & Cards' },
  { to: '/admin/content/sealed', label: 'Sealed Products' },
  { to: '/admin/content/users', label: 'User Collections' },
];

export function ContentBrowser() {
  const { pathname } = useLocation();

  if (pathname === '/admin/content' || pathname === '/admin/content/') {
    return <Navigate to="/admin/content/cards" replace />;
  }

  return (
    <div>
      <h2>Content Browser</h2>
      <nav aria-label="Content browser sections">
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
