import { useEffect, useState } from 'react';
import { WishlistEntry } from '../types/collection';
import { CollectionFilter } from '../types/filters';
import { wishlistApi } from '../api/wishlist';
import { UniversalFilter } from '../components/UniversalFilter';
import { DemoLock } from '../components/DemoLock';

export function WishlistView() {
  const [entries, setEntries] = useState<WishlistEntry[]>([]);
  const [totalValue, setTotalValue] = useState<number | null>(null);
  const [filter, setFilter] = useState<CollectionFilter>({});
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [addIdentifier, setAddIdentifier] = useState('');
  const [adding, setAdding] = useState(false);
  const [addError, setAddError] = useState<string | null>(null);
  const [exportBusy, setExportBusy] = useState(false);
  const [copyNotice, setCopyNotice] = useState(false);

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
    const normalized = addIdentifier.trim().toLowerCase();
    if (!/^[a-z0-9]{3,4}[0-9]{3,4}$/.test(normalized) || /^[a-z0-9]{3,4}0[0-9]{3}$/.test(normalized)) {
      setAddError('Invalid card identifier format. Expected set code (3-4 alphanumeric) followed by card number (3 digits, or 4 digits >= 1000), e.g. EOE019 or EOE1234.');
      return;
    }
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

  const handleDownload = async () => {
    setExportBusy(true);
    try {
      const text = await wishlistApi.exportTcgPlayer();
      const blob = new Blob([text], { type: 'text/plain' });
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = 'wishlist-tcgplayer.txt';
      a.click();
      URL.revokeObjectURL(url);
    } catch {
      // silently ignore
    } finally {
      setExportBusy(false);
    }
  };

  const handleOpenTcgPlayer = async () => {
    setExportBusy(true);
    try {
      const text = await wishlistApi.exportTcgPlayer();
      await navigator.clipboard.writeText(text);
      setCopyNotice(true);
      setTimeout(() => setCopyNotice(false), 5000);
    } catch {
      // silently ignore
    } finally {
      setExportBusy(false);
    }
    window.open('https://www.tcgplayer.com/massentry', '_blank', 'noopener,noreferrer');
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

      {!loading && entries.length > 0 && (
        <div>
          <button type="button" onClick={handleDownload} disabled={exportBusy}>
            {exportBusy ? 'Exporting...' : 'Download for TCGPlayer'}
          </button>
          {' '}
          <DemoLock>
            <button type="button" onClick={handleOpenTcgPlayer} disabled={exportBusy}>
              {exportBusy ? 'Exporting...' : 'Open TCGPlayer Mass Entry'}
            </button>
          </DemoLock>
          {copyNotice && (
            <span role="status"> List copied to clipboard - paste it into the Mass Entry field</span>
          )}
        </div>
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
