import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { BrandingProvider } from '@/contexts/BrandingContext'
import { DemoProvider } from '@/contexts/DemoContext'
import { AuthProvider } from '@/contexts/AuthContext'
import { PreferencesProvider } from '@/contexts/PreferencesContext'
import { ProtectedRoute } from '@/components/ProtectedRoute'
import { AdminRoute } from '@/components/AdminRoute'
import { AppShell } from '@/components/layout/AppShell'
import { LoginPage } from '@/pages/Login'
import { InviteAcceptPage } from '@/pages/InviteAccept'
import { DashboardPage } from '@/pages/Dashboard'
import { BrowsePage } from '@/pages/Browse'
import { CollectionPage } from '@/pages/Collection'
import { SerializedPage } from '@/pages/Serialized'
import { SlabsPage } from '@/pages/Slabs'
import { SealedProductPage } from '@/pages/SealedProduct'
import { WishlistPage } from '@/pages/Wishlist'
import { ReservedListPage } from '@/pages/ReservedList'
import { MetricsPage } from '@/pages/Metrics'
import { AboutPage } from '@/pages/About'
import { UpdatesPage } from '@/pages/admin/Updates'
import { UsersPage } from '@/pages/admin/Users'
import { BackupsPage } from '@/pages/admin/Backups'
import { SettingsPage } from '@/pages/admin/Settings'

function App() {
  return (
    <BrandingProvider>
    <DemoProvider>
      <AuthProvider>
        <PreferencesProvider>
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
                <Route path="/browse" element={<BrowsePage />} />
                <Route path="/collection" element={<CollectionPage />} />
                <Route path="/serialized" element={<SerializedPage />} />
                <Route path="/slabs" element={<SlabsPage />} />
                <Route path="/sealed" element={<SealedProductPage />} />
                <Route path="/wishlist" element={<WishlistPage />} />
                <Route path="/reserved" element={<ReservedListPage />} />
                <Route path="/metrics" element={<MetricsPage />} />
                <Route path="/about" element={<AboutPage />} />

                {/* Admin-only routes */}
                <Route element={<AdminRoute />}>
                  <Route path="/admin/users" element={<UsersPage />} />
                  <Route path="/admin/updates" element={<UpdatesPage />} />
                  <Route path="/admin/backups" element={<BackupsPage />} />
                  <Route path="/admin/settings" element={<SettingsPage />} />
                </Route>
              </Route>
            </Route>
          </Routes>
        </BrowserRouter>
        </PreferencesProvider>
      </AuthProvider>
    </DemoProvider>
    </BrandingProvider>
  )
}

export default App
