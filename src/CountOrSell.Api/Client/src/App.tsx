import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { BrandingProvider } from '@/contexts/BrandingContext'
import { DemoProvider } from '@/contexts/DemoContext'
import { AuthProvider } from '@/contexts/AuthContext'
import { PreferencesProvider } from '@/contexts/PreferencesContext'
import { ToastProvider } from '@/contexts/ToastContext'
import { ErrorBoundary } from '@/components/ErrorBoundary'
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
import { AdminLayout } from '@/pages/admin/AdminLayout'
import { AdminDashboard } from '@/pages/admin/AdminDashboard'
import { ContentBrowser } from '@/pages/admin/ContentBrowser'
import { AdminContentCards } from '@/pages/admin/AdminContentCards'
import { AdminContentSealed } from '@/pages/admin/AdminContentSealed'
import { AdminContentUsers } from '@/pages/admin/AdminContentUsers'
import { OperationsHub } from '@/pages/admin/OperationsHub'
import { NotificationsPanel } from '@/pages/admin/NotificationsPanel'
import { LogViewer } from '@/pages/admin/LogViewer'
import { AdministrationHub } from '@/pages/admin/AdministrationHub'
import { LogForwarding } from '@/pages/admin/LogForwarding'

function App() {
  return (
    <BrandingProvider>
    <DemoProvider>
      <AuthProvider>
        <PreferencesProvider>
        <ToastProvider>
        <BrowserRouter>
          <ErrorBoundary>
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
                  <Route path="/admin" element={<AdminLayout />}>
                    <Route path="dashboard" element={<AdminDashboard />} />
                    <Route path="content" element={<ContentBrowser />}>
                      <Route path="cards" element={<AdminContentCards />} />
                      <Route path="sealed" element={<AdminContentSealed />} />
                      <Route path="users" element={<AdminContentUsers />} />
                    </Route>
                    <Route path="operations" element={<OperationsHub />}>
                      <Route path="updates" element={<UpdatesPage />} />
                      <Route path="notifications" element={<NotificationsPanel />} />
                      <Route path="logs" element={<LogViewer />} />
                    </Route>
                    <Route path="administration" element={<AdministrationHub />}>
                      <Route path="users" element={<UsersPage />} />
                      <Route path="backup" element={<BackupsPage />} />
                      <Route path="config" element={<SettingsPage />} />
                      <Route path="log-forwarding" element={<LogForwarding />} />
                    </Route>
                  </Route>
                  {/* Legacy redirects */}
                  <Route path="/admin/users" element={<Navigate to="/admin/administration/users" replace />} />
                  <Route path="/admin/updates" element={<Navigate to="/admin/operations/updates" replace />} />
                  <Route path="/admin/backups" element={<Navigate to="/admin/administration/backup" replace />} />
                  <Route path="/admin/settings" element={<Navigate to="/admin/administration/config" replace />} />
                </Route>
              </Route>
            </Route>
          </Routes>
          </ErrorBoundary>
        </BrowserRouter>
        </ToastProvider>
        </PreferencesProvider>
      </AuthProvider>
    </DemoProvider>
    </BrandingProvider>
  )
}

export default App
