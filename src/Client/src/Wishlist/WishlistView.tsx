import { useEffect, useState } from 'react';
import { WishlistEntry } from '../types/collection';
import { CollectionFilter } from '../types/filters';
import { wishlistApi } from '../api/wishlist';
import { UniversalFilter } from '../components/UniversalFilter';

export function WishlistView() {
  const [entries, setEntries] = useState<WishlistEntry[]>([]);
  const [totalValue, setTotalValue] = useState<number | null>(null);
  const [filter, setFilter] = useState<CollectionFilter>({});
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [addIdentifier, setAddIdentifier] = useState('');
  const [adding, setAdding] = useState(false);
  const [addError, setAddError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    wishlistApi.getAll(filter)
      .then((data) => {
        if (!cancelled) {
          setEntries(data.entries);
          setTotalValue(data.totalValue);
          setLoading(false);
        }
      })
      .catch(() => { if (!cancelled) { setError('Failed to load wishlist'); setLoading(false); } });
    return () => { cancelled = true; };
  }, [filter]);

  const handleAdd = async () => {
    setAddError(null);
    if (!addIdentifier.trim()) { setAddError('Card identifier is required'); return; }
    setAdding(true);
    try {
      const entry = await wishlistApi.add(addIdentifier.trim().toLowerCase());
      setEntries((prev) => [...prev, entry]);
      setAddIdentifier('');
    } catch {
      setAddError('Failed to add card to wishlist');
    } finally {
      setAdding(false);
    }
  };

  const handleRemove = async (id: string) => {
    await wishlistApi.remove(id);
    setEntries((prev) => prev.filter((e) => e.id !== id));
  };

  if (error) return <div role="alert">{error}</div>;

  return (
    <div>
      <h2>Wishlist</h2>
      <UniversalFilter
        filter={filter}
        onChange={setFilter}
        hideFields={['condition', 'treatment', 'autographed', 'serialized', 'slabbed', 'sealedProduct', 'sealedCategorySlug', 'sealedSubTypeSlug', 'gradingAgency']}
      />
      {totalValue !== null && (
        <p>Total wishlist value: <strong>${totalValue.toFixed(2)}</strong></p>
      )}

      <div>
        <label htmlFor="wishlist-add">Add card identifier</label>
        <input
          id="wishlist-add"
          type="text"
          value={addIdentifier}
          onChange={(e) => setAddIdentifier(e.target.value)}
          placeholder="e.g. EOE019"
          disabled={adding}
        />
        <button type="button" onClick={handleAdd} disabled={adding}>
          {adding ? 'Adding...' : 'Add to wishlist'}
        </button>
        {addError && <span role="alert">{addError}</span>}
      </div>

      {loading ? (
        <p>Loading...</p>
      ) : entries.length === 0 ? (
        <p>Your wishlist is empty.</p>
      ) : (
        <table aria-label="Wishlist entries">
          <thead>
            <tr>
              <th>Card</th>
              <th>Market value</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {entries.map((e) => (
              <tr key={e.id}>
                <td>{e.cardIdentifier.toUpperCase()}</td>
                <td>{e.currentMarketValue !== null ? `$${e.currentMarketValue.toFixed(2)}` : '--'}</td>
                <td>
                  <button
                    type="button"
                    onClick={() => handleRemove(e.id)}
                    aria-label={`Remove ${e.cardIdentifier.toUpperCase()} from wishlist`}
                  >
                    Remove
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}
