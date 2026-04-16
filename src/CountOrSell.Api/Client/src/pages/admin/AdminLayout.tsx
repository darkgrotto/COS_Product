import { Outlet, Navigate, useLocation } from 'react-router-dom'

export function AdminLayout() {
  const { pathname } = useLocation()

  if (pathname === '/admin' || pathname === '/admin/') {
    return <Navigate to="/admin/dashboard" replace />
  }

  return <Outlet />
}
