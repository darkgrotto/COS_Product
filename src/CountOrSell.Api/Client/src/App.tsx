import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { DemoProvider } from '@/contexts/DemoContext'
import { AuthProvider } from '@/contexts/AuthContext'
import { ProtectedRoute } from '@/components/ProtectedRoute'
import { AppShell } from '@/components/layout/AppShell'
import { LoginPage } from '@/pages/Login'
import { DashboardPage } from '@/pages/Dashboard'
import { AboutPage } from '@/pages/About'

function App() {
  return (
    <DemoProvider>
      <AuthProvider>
        <BrowserRouter>
          <Routes>
            <Route path="/login" element={<LoginPage />} />
            <Route element={<ProtectedRoute />}>
              <Route element={<AppShell />}>
                <Route index element={<Navigate to="/dashboard" replace />} />
                <Route path="/dashboard" element={<DashboardPage />} />
                <Route path="/about" element={<AboutPage />} />
                {/* Additional routes added as views are built */}
              </Route>
            </Route>
          </Routes>
        </BrowserRouter>
      </AuthProvider>
    </DemoProvider>
  )
}

export default App
