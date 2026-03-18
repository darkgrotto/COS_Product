import { BrowserRouter, Routes, Route, NavLink } from 'react-router-dom';
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

export function App() {
  return (
    <BrowserRouter>
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
        <NavLink to="/about">About</NavLink>
      </nav>
      <main>
        <Routes>
          <Route path="/" element={<CollectionDashboard />} />
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
          <Route path="/about" element={<AboutView />} />
        </Routes>
      </main>
    </BrowserRouter>
  );
}

export default App;
