import { useEffect, useState } from 'react';
import { SerializedEntry } from '../types/collection';
import { serializedApi } from '../api/serialized';

interface Props {
  adminUserId?: string;
}

export function SerializedList({ adminUserId }: Props) {
  const [entries, setEntries] = useState<SerializedEntry[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    serializedApi.getAll(adminUserId)
      .then((data) => { if (!cancelled) { setEntries(data); setLoading(false); } })
      .catch(() => { if (!cancelled) { setError('Failed to load serialized cards'); setLoading(false); } });
    return () => { cancelled = true; };
  }, [adminUserId]);

  const handleDelete = async (id: string) => {
    await serializedApi.delete(id);
    setEntries((prev) => prev.filter((e) => e.id !== id));
  };

  if (error) return <div role="alert">{error}</div>;

  return (
    <div>
      <h2>Serialized Cards</h2>
      {loading ? (
        <p>Loading...</p>
      ) : entries.length === 0 ? (
        <p>No serialized cards found.</p>
      ) : (
        <table aria-label="Serialized card entries">
          <thead>
            <tr>
              <th>Card</th>
              <th>Treatment</th>
              <th>Serial #</th>
              <th>Print run</th>
              <th>Condition</th>
              <th>Acquired</th>
              <th>Acquisition price</th>
              <th>Market value</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {entries.map((e) => (
              <tr key={e.id}>
                <td>{e.cardIdentifier.toUpperCase()}</td>
                <td>{e.treatment}</td>
                <td>{e.serialNumber}</td>
                <td>{e.printRunTotal}</td>
                <td>{e.condition}{e.autographed ? ' - Autographed' : ''}</td>
                <td>{e.acquisitionDate}</td>
                <td>${e.acquisitionPrice.toFixed(2)}</td>
                <td>{e.currentMarketValue !== null ? `$${e.currentMarketValue.toFixed(2)}` : '--'}</td>
                <td>
                  {!adminUserId && (
                    <button
                      type="button"
                      onClick={() => handleDelete(e.id)}
                      aria-label={`Delete serialized ${e.cardIdentifier.toUpperCase()}`}
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
