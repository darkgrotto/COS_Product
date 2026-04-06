import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from '@/contexts/AuthContext'

export function AdminRoute() {
  const { user } = useAuth()
  if (user?.role !== 'Admin') return <Navigate to="/dashboard" replace />
  return <Outlet />
}
