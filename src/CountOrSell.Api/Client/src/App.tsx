import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { DemoProvider } from '@/contexts/DemoContext'
import { AuthProvider } from '@/contexts/AuthContext'
import { ProtectedRoute } from '@/components/ProtectedRoute'
import { AdminRoute } from '@/components/AdminRoute'
import { AppShell } from '@/components/layout/AppShell'
import { LoginPage } from '@/pages/Login'
import { InviteAcceptPage } from '@/pages/InviteAccept'
import { DashboardPage } from '@/pages/Dashboard'
import { AboutPage } from '@/pages/About'
import { UpdatesPage } from '@/pages/admin/Updates'
import { UsersPage } from '@/pages/admin/Users'

function App() {
  return (
    <DemoProvider>
      <AuthProvider>
        <BrowserRouter>
          <Routes>
            {/* Public routes */}
            <Route path="/login" element={<LoginPage />} />
            <Route path="/invite/:token" element={<InviteAcceptPage />} />

            {/* Protected routes */}
            <Route element={<ProtectedRoute />}>
              <Route element={<AppShell />}>
                <Route index element={<Navigate to="/dashboard" replace />} />
                <Route path="/dashboard" element={<DashboardPage />} />
                <Route path="/about" element={<AboutPage />} />

                {/* Admin-only routes */}
                <Route element={<AdminRoute />}>
                  <Route path="/admin/updates" element={<UpdatesPage />} />
                  <Route path="/admin/users" element={<UsersPage />} />
                </Route>
              </Route>
            </Route>
          </Routes>
        </BrowserRouter>
      </AuthProvider>
    </DemoProvider>
  )
}

export default App
