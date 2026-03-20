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
import { WishlistView } from './Wishlist/WishlistView';
import { MetricsDashboard } from './Metrics/MetricsDashboard';
import { GradingAgencyManager } from './Admin/GradingAgencyManager';
import { UserManagement } from './Admin/UserManagement';
import { UserCollectionView } from './Admin/UserCollectionView';
import { AdminSettings } from './Admin/AdminSettings';
import { BackupManager } from './Admin/BackupManager';
import { UpdatesManager } from './Admin/UpdatesManager';
import { AboutView } from './About/AboutView';
import { UserPreferencesPage } from './UserPreferences/UserPreferencesPage';
import { usersApi } from './api/users';
import { TcgPlayerProvider } from './context/TcgPlayerContext';
import { DemoProvider } from './context/DemoContext';
import { DemoBanner } from './components/DemoBanner';

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
        <NavLink to="/sealed">Sealed Product</NavLink>
        <NavLink to="/wishlist">Wishlist</NavLink>
        <NavLink to="/metrics">Metrics</NavLink>
        <NavLink to="/admin/users">Users</NavLink>
        <NavLink to="/admin/agencies">Agencies</NavLink>
        <NavLink to="/admin/settings">Settings</NavLink>
        <NavLink to="/admin/backup">Backup</NavLink>
        <NavLink to="/admin/updates">Updates</NavLink>
        <NavLink to="/preferences">Preferences</NavLink>
        <NavLink to="/about">About</NavLink>
      </nav>
      <main>
        <Routes>
          <Route path="/" element={<DefaultPage />} />
          <Route path="/collection" element={<CollectionList />} />
          <Route path="/collection/reserved-list" element={<ReservedListView />} />
          <Route path="/collection/completion" element={<SetCompletion />} />
          <Route path="/serialized" element={<SerializedList />} />
          <Route path="/slabs" element={<SlabList />} />
          <Route path="/sealed" element={<SealedInventoryList />} />
          <Route path="/wishlist" element={<WishlistView />} />
          <Route path="/metrics" element={<MetricsDashboard />} />
          <Route path="/admin/users" element={<UserManagement />} />
          <Route path="/admin/users/:userId/collection" element={<UserCollectionView />} />
          <Route path="/admin/agencies" element={<GradingAgencyManager />} />
          <Route path="/admin/settings" element={<AdminSettings />} />
          <Route path="/admin/backup" element={<BackupManager />} />
          <Route path="/admin/updates" element={<UpdatesManager />} />
          <Route path="/preferences" element={<UserPreferencesPage />} />
          <Route path="/about" element={<AboutView />} />
        </Routes>
      </main>
    </BrowserRouter>
    </TcgPlayerProvider>
    </DemoProvider>
  );
}

export default App;
