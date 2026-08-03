import { lazy, Suspense } from 'react'
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { BrandingProvider } from '@/contexts/BrandingContext'
import { DemoProvider } from '@/contexts/DemoContext'
import { AuthProvider } from '@/contexts/AuthContext'
import { PreferencesProvider } from '@/contexts/PreferencesContext'
import { ToastProvider } from '@/contexts/ToastContext'
import { ErrorBoundary } from '@/components/ErrorBoundary'
import { SetupGuard } from '@/components/SetupGuard'
import { ProtectedRoute } from '@/components/ProtectedRoute'
import { AdminRoute } from '@/components/AdminRoute'
import { AppShell } from '@/components/layout/AppShell'

// Core general-user pages are eagerly imported (they are the primary experience).
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

// First-run setup and the entire admin subtree are code-split: general users never
// reach these routes, so they are not pulled into the main bundle. Named exports are
// mapped to the default export React.lazy expects.
const SetupPage = lazy(() => import('@/pages/Setup').then(m => ({ default: m.SetupPage })))
const UpdatesPage = lazy(() => import('@/pages/admin/Updates').then(m => ({ default: m.UpdatesPage })))
const UsersPage = lazy(() => import('@/pages/admin/Users').then(m => ({ default: m.UsersPage })))
const BackupsPage = lazy(() => import('@/pages/admin/Backups').then(m => ({ default: m.BackupsPage })))
const SettingsPage = lazy(() => import('@/pages/admin/Settings').then(m => ({ default: m.SettingsPage })))
const AdminLayout = lazy(() => import('@/pages/admin/AdminLayout').then(m => ({ default: m.AdminLayout })))
const AdminDashboard = lazy(() => import('@/pages/admin/AdminDashboard').then(m => ({ default: m.AdminDashboard })))
const ContentBrowser = lazy(() => import('@/pages/admin/ContentBrowser').then(m => ({ default: m.ContentBrowser })))
const AdminContentCards = lazy(() => import('@/pages/admin/AdminContentCards').then(m => ({ default: m.AdminContentCards })))
const AdminContentSealed = lazy(() => import('@/pages/admin/AdminContentSealed').then(m => ({ default: m.AdminContentSealed })))
const AdminContentUsers = lazy(() => import('@/pages/admin/AdminContentUsers').then(m => ({ default: m.AdminContentUsers })))
const OperationsHub = lazy(() => import('@/pages/admin/OperationsHub').then(m => ({ default: m.OperationsHub })))
const NotificationsPanel = lazy(() => import('@/pages/admin/NotificationsPanel').then(m => ({ default: m.NotificationsPanel })))
const LogViewer = lazy(() => import('@/pages/admin/LogViewer').then(m => ({ default: m.LogViewer })))
const AdministrationHub = lazy(() => import('@/pages/admin/AdministrationHub').then(m => ({ default: m.AdministrationHub })))
const LogForwarding = lazy(() => import('@/pages/admin/LogForwarding').then(m => ({ default: m.LogForwarding })))
const DataManagementPage = lazy(() => import('@/pages/admin/DataManagement').then(m => ({ default: m.DataManagementPage })))

function RouteFallback() {
  return (
    <div className="min-h-screen flex items-center justify-center">
      <p className="text-muted-foreground text-sm">Loading...</p>
    </div>
  )
}

function App() {
  return (
    <BrandingProvider>
    <DemoProvider>
      <AuthProvider>
        <PreferencesProvider>
        <ToastProvider>
        <BrowserRouter>
          <ErrorBoundary>
          <SetupGuard>
          <Suspense fallback={<RouteFallback />}>
          <Routes>
            {/* Public routes */}
            <Route path="/setup" element={<SetupPage />} />
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
                      <Route path="data" element={<DataManagementPage />} />
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
          </Suspense>
          </SetupGuard>
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
