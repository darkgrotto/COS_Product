import { BrowserRouter, Routes, Route, NavLink } from 'react-router-dom';
import { CollectionDashboard } from './Dashboard/CollectionDashboard';
import { CollectionList } from './Collection/CollectionList';
import { SetCompletion } from './Collection/SetCompletion';
import { SerializedList } from './Serialized/SerializedList';
import { SlabList } from './Slabs/SlabList';
import { SealedInventoryList } from './SealedProduct/SealedInventoryList';
import { WishlistView } from './Wishlist/WishlistView';
import { MetricsDashboard } from './Metrics/MetricsDashboard';
import { GradingAgencyManager } from './Admin/GradingAgencyManager';
import { UserManagement } from './Admin/UserManagement';

export function App() {
  return (
    <BrowserRouter>
      <nav aria-label="Main navigation">
        <NavLink to="/">Dashboard</NavLink>
        <NavLink to="/collection">Collection</NavLink>
        <NavLink to="/serialized">Serialized</NavLink>
        <NavLink to="/slabs">Slabs</NavLink>
        <NavLink to="/sealed">Sealed Product</NavLink>
        <NavLink to="/wishlist">Wishlist</NavLink>
        <NavLink to="/metrics">Metrics</NavLink>
        <NavLink to="/admin/agencies">Agencies</NavLink>
        <NavLink to="/admin/users">Users</NavLink>
      </nav>
      <main>
        <Routes>
          <Route path="/" element={<CollectionDashboard />} />
          <Route path="/collection" element={<CollectionList />} />
          <Route path="/collection/completion" element={<SetCompletion />} />
          <Route path="/serialized" element={<SerializedList />} />
          <Route path="/slabs" element={<SlabList />} />
          <Route path="/sealed" element={<SealedInventoryList />} />
          <Route path="/wishlist" element={<WishlistView />} />
          <Route path="/metrics" element={<MetricsDashboard />} />
          <Route path="/admin/agencies" element={<GradingAgencyManager />} />
          <Route path="/admin/users" element={<UserManagement />} />
        </Routes>
      </main>
    </BrowserRouter>
  );
}

export default App;
