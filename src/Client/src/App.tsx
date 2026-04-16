import { useEffect, useState } from 'react';
import { BrowserRouter, Routes, Route, NavLink, Navigate } from 'react-router-dom';
import 'keyrune/css/keyrune.css';
import { CollectionDashboard } from './Dashboard/CollectionDashboard';
import { CollectionList } from './Collection/CollectionList';
import { ReservedListView } from './Collection/ReservedListView';
import { SetCompletion } from './Collection/SetCompletion';
import { SerializedList } from './Serialized/SerializedList';
import { SlabList } from './Slabs/SlabList';
import { SealedInventoryList } from './SealedProduct/SealedInventoryList';
import { SealedProductBrowse } from './SealedProduct/SealedProductBrowse';
import { WishlistView } from './Wishlist/WishlistView';
import { MetricsDashboard } from './Metrics/MetricsDashboard';
import { AdminLayout } from './Admin/AdminLayout';
import { AdminDashboard } from './Admin/AdminDashboard';
import { ContentBrowser } from './Admin/ContentBrowser';
import { AdminContentCards } from './Admin/AdminContentCards';
import { AdminContentUsers } from './Admin/AdminContentUsers';
import { OperationsHub } from './Admin/OperationsHub';
import { UpdatesManager } from './Admin/UpdatesManager';
import { NotificationsPanel } from './Admin/NotificationsPanel';
import { LogViewer } from './Admin/LogViewer';
import { AdministrationHub } from './Admin/AdministrationHub';
import { UserManagement } from './Admin/UserManagement';
import { UserCollectionView } from './Admin/UserCollectionView';
import { BackupManager } from './Admin/BackupManager';
import { InstanceSettings } from './Admin/InstanceSettings';
import { LogForwarding } from './Admin/LogForwarding';
import { InviteAccept } from './Admin/InviteAccept';
import { AboutView } from './About/AboutView';
import { UserPreferencesPage } from './UserPreferences/UserPreferencesPage';
import { usersApi } from './api/users';
import { TcgPlayerProvider } from './context/TcgPlayerContext';
import { DemoProvider } from './context/DemoContext';
import { DemoBanner } from './components/DemoBanner';
import { FlavorButton } from './components/FlavorButton';
import { CardDetail } from './Cards/CardDetail';
import { AppFooter } from './components/AppFooter';

function DefaultPage() {
  const [defaultPage, setDefaultPage] = useState<string | null | undefined>(undefined);

  useEffect(() => {
    usersApi.getPreferences()
      .then((prefs) => setDefaultPage(prefs.defaultPage))
      .catch(() => setDefaultPage(null));
  }, []);

  if (defaultPage === undefined) return null;
  if (defaultPage && defaultPage !== '/') return <Navigate to={defaultPage} replace />;
  return <CollectionDashboard />;
}

export function App() {
  return (
    <DemoProvider>
    <TcgPlayerProvider>
    <BrowserRouter>
      <DemoBanner />
      <nav aria-label="Main navigation">
        <NavLink to="/">Dashboard</NavLink>
        <NavLink to="/collection">Collection</NavLink>
        <NavLink to="/collection/reserved-list">Reserved List</NavLink>
        <NavLink to="/serialized">Serialized</NavLink>
        <NavLink to="/slabs">Slabs</NavLink>
        <NavLink to="/sealed/browse">Sealed Catalog</NavLink>
        <NavLink to="/sealed">Sealed Inventory</NavLink>
        <NavLink to="/wishlist">Wishlist</NavLink>
        <NavLink to="/metrics">Metrics</NavLink>
        <NavLink to="/admin">Admin</NavLink>
        <NavLink to="/preferences">Preferences</NavLink>
        <NavLink to="/about">About</NavLink>
        <FlavorButton />
      </nav>
      <main style={{ paddingBottom: '2.5rem' }}>
        <Routes>
          <Route path="/" element={<DefaultPage />} />
          <Route path="/collection" element={<CollectionList />} />
          <Route path="/collection/reserved-list" element={<ReservedListView />} />
          <Route path="/collection/completion" element={<SetCompletion />} />
          <Route path="/serialized" element={<SerializedList />} />
          <Route path="/slabs" element={<SlabList />} />
          <Route path="/sealed" element={<SealedInventoryList />} />
          <Route path="/sealed/browse" element={<SealedProductBrowse />} />
          <Route path="/wishlist" element={<WishlistView />} />
          <Route path="/metrics" element={<MetricsDashboard />} />

          {/* Admin section - nested routes under AdminLayout */}
          <Route path="/admin" element={<AdminLayout />}>
            <Route path="dashboard" element={<AdminDashboard />} />

            <Route path="content" element={<ContentBrowser />}>
              <Route path="cards" element={<AdminContentCards />} />
              <Route path="sealed" element={<SealedProductBrowse />} />
              <Route path="users" element={<AdminContentUsers />} />
            </Route>

            <Route path="operations" element={<OperationsHub />}>
              <Route path="updates" element={<UpdatesManager />} />
              <Route path="notifications" element={<NotificationsPanel />} />
              <Route path="logs" element={<LogViewer />} />
            </Route>

            <Route path="administration" element={<AdministrationHub />}>
              <Route path="users" element={<UserManagement />} />
              <Route path="backup" element={<BackupManager />} />
              <Route path="config" element={<InstanceSettings />} />
              <Route path="log-forwarding" element={<LogForwarding />} />
            </Route>
          </Route>

          {/* Legacy admin routes - redirect to new locations */}
          <Route path="/admin/users" element={<Navigate to="/admin/administration/users" replace />} />
          <Route path="/admin/users/:userId/collection" element={<UserCollectionView />} />
          <Route path="/admin/agencies" element={<Navigate to="/admin/administration/config" replace />} />
          <Route path="/admin/settings" element={<Navigate to="/admin/administration/config" replace />} />
          <Route path="/admin/backup" element={<Navigate to="/admin/administration/backup" replace />} />
          <Route path="/admin/updates" element={<Navigate to="/admin/operations/updates" replace />} />

          <Route path="/preferences" element={<UserPreferencesPage />} />
          <Route path="/about" element={<AboutView />} />
          <Route path="/invite/:token" element={<InviteAccept />} />
          <Route path="/cards/:identifier" element={<CardDetail />} />
        </Routes>
      </main>
      <AppFooter />
    </BrowserRouter>
    </TcgPlayerProvider>
    </DemoProvider>
  );
}

export default App;
