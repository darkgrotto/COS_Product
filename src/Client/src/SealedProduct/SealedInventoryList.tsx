import { useEffect, useState } from 'react';
import { SealedInventoryEntry } from '../types/collection';
import { CollectionFilter } from '../types/filters';
import { sealedInventoryApi } from '../api/sealedInventory';
import { UniversalFilter } from '../components/UniversalFilter';

interface Props {
  adminUserId?: string;
}

export function SealedInventoryList({ adminUserId }: Props) {
  const [entries, setEntries] = useState<SealedInventoryEntry[]>([]);
  const [filter, setFilter] = useState<CollectionFilter>({});
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    sealedInventoryApi.getAll(adminUserId, filter.sealedCategorySlug, filter.sealedSubTypeSlug)
      .then((data) => { if (!cancelled) { setEntries(data); setLoading(false); } })
      .catch(() => { if (!cancelled) { setError('Failed to load sealed inventory'); setLoading(false); } });
    return () => { cancelled = true; };
  }, [adminUserId, filter.sealedCategorySlug, filter.sealedSubTypeSlug]);

  const handleDelete = async (id: string) => {
    await sealedInventoryApi.delete(id);
    setEntries((prev) => prev.filter((e) => e.id !== id));
  };

  if (error) return <div role="alert">{error}</div>;

  return (
    <div>
      <h2>Sealed Product Inventory</h2>
      <UniversalFilter
        filter={filter}
        onChange={setFilter}
        hideFields={['setCode', 'color', 'cardType', 'treatment', 'autographed', 'serialized', 'slabbed', 'sealedProduct', 'gradingAgency']}
      />
      {loading ? (
        <p>Loading...</p>
      ) : entries.length === 0 ? (
        <p>No sealed inventory found.</p>
      ) : (
        <table aria-label="Sealed inventory entries">
          <thead>
            <tr>
              <th>Product</th>
              <th>Category</th>
              <th>Sub-type</th>
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
                <td>{e.productName ?? e.productIdentifier}</td>
                <td>{e.categoryDisplayName ?? ''}</td>
                <td>{e.subTypeDisplayName ?? ''}</td>
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
                      aria-label={`Delete sealed product ${e.productName ?? e.productIdentifier}`}
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
