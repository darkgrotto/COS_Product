import { useEffect, useState } from 'react';
import { CollectionEntry } from '../types/collection';
import { CollectionFilter } from '../types/filters';
import { collectionApi, CollectionEntryRequest } from '../api/collection';
import { UniversalFilter } from '../components/UniversalFilter';
import { useReservedList } from '../hooks/useReservedList';
import { ReservedBadge } from '../components/ReservedBadge';
import { SetSymbol } from '../components/SetSymbol';
import { useTcgPlayerConfigured } from '../context/TcgPlayerContext';

interface Props {
  adminUserId?: string;
}

type SortField = 'marketValue' | 'profitLoss';
type SortDir = 'asc' | 'desc';

interface EditForm {
  acquisitionDate: string;
  acquisitionPrice: string;
  notes: string;
}

function extractSetCode(identifier: string): string {
  const match = identifier.toLowerCase().match(/^([a-z0-9]{3,4})\d{3,4}$/);
  return match ? match[1] : '';
}

export function CollectionList({ adminUserId }: Props) {
  const [entries, setEntries] = useState<CollectionEntry[]>([]);
  const [filter, setFilter] = useState<CollectionFilter>({});
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editForm, setEditForm] = useState<EditForm>({ acquisitionDate: '', acquisitionPrice: '', notes: '' });
  const [saving, setSaving] = useState(false);
  const [sortField, setSortField] = useState<SortField | null>(null);
  const [sortDir, setSortDir] = useState<SortDir>('desc');
  const [refreshingId, setRefreshingId] = useState<string | null>(null);
  const reservedSet = useReservedList();
  const tcgConfigured = useTcgPlayerConfigured();

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    collectionApi.getAll(filter, adminUserId)
      .then((data) => { if (!cancelled) { setEntries(data); setLoading(false); } })
      .catch(() => { if (!cancelled) { setError('Failed to load collection'); setLoading(false); } });
    return () => { cancelled = true; };
  }, [filter, adminUserId]);

  const handleDelete = async (id: string) => {
    await collectionApi.delete(id);
    setEntries((prev) => prev.filter((e) => e.id !== id));
  };

  const startEdit = (e: CollectionEntry) => {
    setEditingId(e.id);
    setEditForm({
      acquisitionDate: e.acquisitionDate,
      acquisitionPrice: String(e.acquisitionPrice),
      notes: e.notes ?? '',
    });
  };

  const cancelEdit = () => setEditingId(null);

  const saveEdit = async (e: CollectionEntry) => {
    setSaving(true);
    try {
      const request: CollectionEntryRequest = {
        cardIdentifier: e.cardIdentifier,
        treatment: e.treatment,
        quantity: e.quantity,
        condition: e.condition,
        autographed: e.autographed,
        acquisitionDate: editForm.acquisitionDate,
        acquisitionPrice: parseFloat(editForm.acquisitionPrice),
        notes: editForm.notes.trim() || undefined,
      };
      const updated = await collectionApi.update(e.id, request);
      setEntries((prev) => prev.map((x) => x.id === e.id ? updated : x));
      setEditingId(null);
    } catch {
      // leave edit form open on error
    } finally {
      setSaving(false);
    }
  };

  const refreshPrice = async (identifier: string) => {
    setRefreshingId(identifier);
    try {
      const result = await collectionApi.refreshPrice(identifier.toLowerCase());
      setEntries((prev) => prev.map((e) =>
        e.cardIdentifier.toLowerCase() === result.cardIdentifier.toLowerCase()
          ? { ...e, currentMarketValue: result.marketValue }
          : e
      ));
    } catch {
      // silently ignore
    } finally {
      setRefreshingId(null);
    }
  };

  const toggleSort = (field: SortField) => {
    if (sortField === field) {
      setSortDir((d) => d === 'asc' ? 'desc' : 'asc');
    } else {
      setSortField(field);
      setSortDir('desc');
    }
  };

  const sortedEntries = sortField === null ? entries : [...entries].sort((a, b) => {
    const aVal = sortField === 'marketValue'
      ? (a.currentMarketValue ?? -Infinity)
      : (a.currentMarketValue !== null ? (a.currentMarketValue - a.acquisitionPrice) * a.quantity : -Infinity);
    const bVal = sortField === 'marketValue'
      ? (b.currentMarketValue ?? -Infinity)
      : (b.currentMarketValue !== null ? (b.currentMarketValue - b.acquisitionPrice) * b.quantity : -Infinity);
    return sortDir === 'asc' ? aVal - bVal : bVal - aVal;
  });

  const sortIndicator = (field: SortField) => {
    if (sortField !== field) return null;
    return sortDir === 'asc' ? ' \u25b2' : ' \u25bc';
  };

  if (error) return <div role="alert">{error}</div>;

  return (
    <div>
      <UniversalFilter filter={filter} onChange={setFilter} />
      {loading ? (
        <p>Loading...</p>
      ) : entries.length === 0 ? (
        <p>No entries found.</p>
      ) : (
        <table aria-label="Collection entries">
          <thead>
            <tr>
              <th>Card</th>
              <th>Treatment</th>
              <th>Qty</th>
              <th>Condition</th>
              <th>Autographed</th>
              <th>Acquired</th>
              <th>Acquisition price</th>
              <th
                style={{ cursor: 'pointer' }}
                onClick={() => toggleSort('marketValue')}
              >
                Market value{sortIndicator('marketValue')}
              </th>
              <th
                style={{ cursor: 'pointer' }}
                onClick={() => toggleSort('profitLoss')}
              >
                Profit / loss{sortIndicator('profitLoss')}
              </th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {sortedEntries.map((e) => {
              const setCode = extractSetCode(e.cardIdentifier);
              const pl = e.currentMarketValue !== null
                ? (e.currentMarketValue - e.acquisitionPrice) * e.quantity
                : null;
              if (editingId === e.id) {
                return (
                  <tr key={e.id}>
                    <td>
                      {setCode && <SetSymbol setCode={setCode} />}
                      {' '}{e.cardIdentifier.toUpperCase()}
                      {reservedSet.has(e.cardIdentifier.toLowerCase()) && <ReservedBadge />}
                    </td>
                    <td>{e.treatment}</td>
                    <td>{e.quantity}</td>
                    <td>{e.condition}{e.autographed ? ' - Autographed' : ''}</td>
                    <td>{e.autographed ? 'Yes' : 'No'}</td>
                    <td>
                      <input
                        type="date"
                        value={editForm.acquisitionDate}
                        onChange={(ev) => setEditForm((f) => ({ ...f, acquisitionDate: ev.target.value }))}
                        disabled={saving}
                        aria-label="Acquisition date"
                      />
                    </td>
                    <td>
                      <input
                        type="number"
                        min={0}
                        step="0.01"
                        value={editForm.acquisitionPrice}
                        onChange={(ev) => setEditForm((f) => ({ ...f, acquisitionPrice: ev.target.value }))}
                        disabled={saving}
                        aria-label="Acquisition price"
                      />
                    </td>
                    <td colSpan={2}>
                      <input
                        type="text"
                        value={editForm.notes}
                        onChange={(ev) => setEditForm((f) => ({ ...f, notes: ev.target.value }))}
                        disabled={saving}
                        placeholder="Notes"
                        aria-label="Notes"
                      />
                    </td>
                    <td>
                      <button type="button" onClick={() => saveEdit(e)} disabled={saving}>
                        {saving ? 'Saving...' : 'Save'}
                      </button>
                      {' '}
                      <button type="button" onClick={cancelEdit} disabled={saving}>Cancel</button>
                    </td>
                  </tr>
                );
              }
              return (
                <tr key={e.id}>
                  <td>
                    {setCode && <SetSymbol setCode={setCode} />}
                    {' '}
                    {e.cardIdentifier.toUpperCase()}
                    {reservedSet.has(e.cardIdentifier.toLowerCase()) && <ReservedBadge />}
                    {e.oracleRulingUrl && (
                      <>
                        {' '}
                        <a href={e.oracleRulingUrl} target="_blank" rel="noopener noreferrer">
                          Oracle
                        </a>
                      </>
                    )}
                  </td>
                  <td>{e.treatment}</td>
                  <td>{e.quantity}</td>
                  <td>{e.condition}{e.autographed ? ' - Autographed' : ''}</td>
                  <td>{e.autographed ? 'Yes' : 'No'}</td>
                  <td>{e.acquisitionDate}</td>
                  <td>${e.acquisitionPrice.toFixed(2)}</td>
                  <td>{e.currentMarketValue !== null ? `$${e.currentMarketValue.toFixed(2)}` : '--'}</td>
                  <td>{pl !== null ? `$${pl.toFixed(2)}` : '--'}</td>
                  <td>
                    {!adminUserId && (
                      <>
                        <button
                          type="button"
                          onClick={() => startEdit(e)}
                          aria-label={`Edit ${e.cardIdentifier.toUpperCase()}`}
                        >
                          Edit
                        </button>
                        {' '}
                        {tcgConfigured && (
                          <>
                            <button
                              type="button"
                              onClick={() => refreshPrice(e.cardIdentifier)}
                              disabled={refreshingId === e.cardIdentifier.toLowerCase()}
                              aria-label={`Refresh price for ${e.cardIdentifier.toUpperCase()}`}
                            >
                              {refreshingId === e.cardIdentifier.toLowerCase() ? 'Refreshing...' : 'Refresh price'}
                            </button>
                            {' '}
                          </>
                        )}
                        <button
                          type="button"
                          onClick={() => handleDelete(e.id)}
                          aria-label={`Delete ${e.cardIdentifier.toUpperCase()}`}
                        >
                          Delete
                        </button>
                      </>
                    )}
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      )}
    </div>
  );
}
