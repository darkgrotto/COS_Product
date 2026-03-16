import { useEffect, useState } from 'react';
import { SealedInventoryEntry } from '../types/collection';
import { sealedInventoryApi } from '../api/sealedInventory';

interface Props {
  adminUserId?: string;
}

export function SealedInventoryList({ adminUserId }: Props) {
  const [entries, setEntries] = useState<SealedInventoryEntry[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    sealedInventoryApi.getAll(adminUserId)
      .then((data) => { if (!cancelled) { setEntries(data); setLoading(false); } })
      .catch(() => { if (!cancelled) { setError('Failed to load sealed inventory'); setLoading(false); } });
    return () => { cancelled = true; };
  }, [adminUserId]);

  const handleDelete = async (id: string) => {
    await sealedInventoryApi.delete(id);
    setEntries((prev) => prev.filter((e) => e.id !== id));
  };

  if (error) return <div role="alert">{error}</div>;

  return (
    <div>
      <h2>Sealed Product Inventory</h2>
      {loading ? (
        <p>Loading...</p>
      ) : entries.length === 0 ? (
        <p>No sealed inventory found.</p>
      ) : (
        <table aria-label="Sealed inventory entries">
          <thead>
            <tr>
              <th>Product</th>
              <th>Qty</th>
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
                <td>{e.productIdentifier}</td>
                <td>{e.quantity}</td>
                <td>{e.condition}</td>
                <td>{e.acquisitionDate}</td>
                <td>${e.acquisitionPrice.toFixed(2)}</td>
                <td>{e.currentMarketValue !== null ? `$${e.currentMarketValue.toFixed(2)}` : '--'}</td>
                <td>
                  {!adminUserId && (
                    <button
                      type="button"
                      onClick={() => handleDelete(e.id)}
                      aria-label={`Delete sealed product ${e.productIdentifier}`}
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
