import { useEffect, useState } from 'react';
import { adminApi, AdminDashboardStats } from '../api/admin';

interface StatCardProps {
  label: string;
  value: number | null;
}

function StatCard({ label, value }: StatCardProps) {
  return (
    <div>
      <dt>{label}</dt>
      <dd>{value !== null ? value.toLocaleString() : '--'}</dd>
    </div>
  );
}

export function AdminDashboard() {
  const [stats, setStats] = useState<AdminDashboardStats | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    adminApi.getDashboard()
      .then((data) => { setStats(data); setLoading(false); })
      .catch(() => { setError('Failed to load dashboard stats'); setLoading(false); });
  }, []);

  if (loading) return <p>Loading...</p>;
  if (error) return <div role="alert">{error}</div>;

  return (
    <div>
      <h2>Dashboard</h2>
      <dl aria-label="Instance statistics">
        <StatCard label="Users" value={stats?.userCount ?? null} />
        <StatCard label="Sets" value={stats?.setCount ?? null} />
        <StatCard label="Cards" value={stats?.cardCount ?? null} />
        <StatCard label="Card images" value={stats?.cardImageCount ?? null} />
        <StatCard label="Sealed products" value={stats?.sealedProductCount ?? null} />
        <StatCard label="Sealed product images" value={stats?.sealedImageCount ?? null} />
        <StatCard label="Reserved List cards" value={stats?.reservedListCount ?? null} />
      </dl>
    </div>
  );
}
