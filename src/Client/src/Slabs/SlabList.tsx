import { useEffect, useState } from 'react';
import { SlabEntry } from '../types/collection';
import { slabsApi } from '../api/slabs';
import { useReservedList } from '../hooks/useReservedList';
import { ReservedBadge } from '../components/ReservedBadge';

interface Props {
  adminUserId?: string;
}

export function SlabList({ adminUserId }: Props) {
  const [entries, setEntries] = useState<SlabEntry[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const reservedSet = useReservedList();

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    slabsApi.getAll(adminUserId)
      .then((data) => { if (!cancelled) { setEntries(data); setLoading(false); } })
      .catch(() => { if (!cancelled) { setError('Failed to load slabs'); setLoading(false); } });
    return () => { cancelled = true; };
  }, [adminUserId]);

  const handleDelete = async (id: string) => {
    await slabsApi.delete(id);
    setEntries((prev) => prev.filter((e) => e.id !== id));
  };

  if (error) return <div role="alert">{error}</div>;

  return (
    <div>
      <h2>Slabs</h2>
      {loading ? (
        <p>Loading...</p>
      ) : entries.length === 0 ? (
        <p>No slabs found.</p>
      ) : (
        <table aria-label="Slab entries">
          <thead>
            <tr>
              <th>Card</th>
              <th>Treatment</th>
              <th>Agency</th>
              <th>Grade</th>
              <th>Certificate</th>
              <th>Serial #</th>
              <th>Print run</th>
              <th>Acquired</th>
              <th>Acquisition price</th>
              <th>Market value</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {entries.map((e) => (
              <tr key={e.id}>
                <td>
                  {e.cardIdentifier.toUpperCase()}
                  {reservedSet.has(e.cardIdentifier.toLowerCase()) && <ReservedBadge />}
                </td>
                <td>{e.treatment}</td>
                <td>{e.gradingAgencyCode}</td>
                <td>{e.grade}</td>
                <td>{e.certificateNumber}</td>
                <td>{e.serialNumber ?? '--'}</td>
                <td>{e.printRunTotal ?? '--'}</td>
                <td>{e.acquisitionDate}</td>
                <td>${e.acquisitionPrice.toFixed(2)}</td>
                <td>{e.currentMarketValue !== null ? `$${e.currentMarketValue.toFixed(2)}` : '--'}</td>
                <td>
                  {!adminUserId && (
                    <button
                      type="button"
                      onClick={() => handleDelete(e.id)}
                      aria-label={`Delete slab ${e.cardIdentifier.toUpperCase()}`}
                    >
                      Delete
                    </button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}
