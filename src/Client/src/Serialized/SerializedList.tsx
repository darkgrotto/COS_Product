import { useEffect, useState } from 'react';
import { SerializedEntry } from '../types/collection';
import { CollectionFilter } from '../types/filters';
import { serializedApi, SerializedEntryRequest } from '../api/serialized';
import { collectionApi } from '../api/collection';
import { UniversalFilter } from '../components/UniversalFilter';
import { useReservedList } from '../hooks/useReservedList';
import { ReservedBadge } from '../components/ReservedBadge';
import { useTcgPlayerConfigured } from '../context/TcgPlayerContext';

interface Props {
  adminUserId?: string;
}

interface EditForm {
  acquisitionDate: string;
  acquisitionPrice: string;
  notes: string;
}

export function SerializedList({ adminUserId }: Props) {
  const [entries, setEntries] = useState<SerializedEntry[]>([]);
  const [filter, setFilter] = useState<CollectionFilter>({});
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editForm, setEditForm] = useState<EditForm>({ acquisitionDate: '', acquisitionPrice: '', notes: '' });
  const [saving, setSaving] = useState(false);
  const [refreshingId, setRefreshingId] = useState<string | null>(null);
  const reservedSet = useReservedList();
  const tcgConfigured = useTcgPlayerConfigured();

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    serializedApi.getAll(adminUserId, {
      setCode: filter.setCode,
      treatment: filter.treatment,
      condition: filter.condition,
      autographed: filter.autographed,
    })
      .then((data) => { if (!cancelled) { setEntries(data); setLoading(false); } })
      .catch(() => { if (!cancelled) { setError('Failed to load serialized cards'); setLoading(false); } });
    return () => { cancelled = true; };
  }, [adminUserId, filter.setCode, filter.treatment, filter.condition, filter.autographed]);

  const handleDelete = async (id: string) => {
    await serializedApi.delete(id);
    setEntries((prev) => prev.filter((e) => e.id !== id));
  };

  const startEdit = (e: SerializedEntry) => {
    setEditingId(e.id);
    setEditForm({
      acquisitionDate: e.acquisitionDate,
      acquisitionPrice: String(e.acquisitionPrice),
      notes: e.notes ?? '',
    });
  };

  const cancelEdit = () => setEditingId(null);

  const saveEdit = async (e: SerializedEntry) => {
    setSaving(true);
    try {
      const request: SerializedEntryRequest = {
        cardIdentifier: e.cardIdentifier,
        treatment: e.treatment,
        serialNumber: e.serialNumber,
        printRunTotal: e.printRunTotal,
        condition: e.condition,
        autographed: e.autographed,
        acquisitionDate: editForm.acquisitionDate,
        acquisitionPrice: parseFloat(editForm.acquisitionPrice),
        notes: editForm.notes.trim() || undefined,
      };
      const updated = await serializedApi.update(e.id, request);
      setEntries((prev) => prev.map((x) => x.id === e.id ? updated : x));
      setEditingId(null);
    } catch {
      // leave edit form open on error
    } finally {
      setSaving(false);
    }
  };

  const refreshPrice = async (identifier: string) => {
    setRefreshingId(identifier.toLowerCase());
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

  if (error) return <div role="alert">{error}</div>;

  return (
    <div>
      <h2>Serialized Cards</h2>
      <UniversalFilter
        filter={filter}
        onChange={setFilter}
        hideFields={['color', 'cardType', 'slabbed', 'sealedProduct', 'sealedCategorySlug', 'sealedSubTypeSlug', 'gradingAgency', 'serialized']}
      />
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
            {entries.map((e) => {
              if (editingId === e.id) {
                return (
                  <tr key={e.id}>
                    <td>
                      {e.cardIdentifier.toUpperCase()}
                      {reservedSet.has(e.cardIdentifier.toLowerCase()) && <ReservedBadge />}
                    </td>
                    <td>{e.treatment}</td>
                    <td>{e.serialNumber}</td>
                    <td>{e.printRunTotal}</td>
                    <td>{e.condition}{e.autographed ? ' - Autographed' : ''}</td>
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
                    <td>
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
                    {e.cardIdentifier.toUpperCase()}
                    {reservedSet.has(e.cardIdentifier.toLowerCase()) && <ReservedBadge />}
                  </td>
                  <td>{e.treatment}</td>
                  <td>{e.serialNumber}</td>
                  <td>{e.printRunTotal}</td>
                  <td>{e.condition}{e.autographed ? ' - Autographed' : ''}</td>
                  <td>{e.acquisitionDate}</td>
                  <td>${e.acquisitionPrice.toFixed(2)}</td>
                  <td>{e.currentMarketValue !== null ? `$${e.currentMarketValue.toFixed(2)}` : '--'}</td>
                  <td>
                    {!adminUserId && (
                      <>
                        <button
                          type="button"
                          onClick={() => startEdit(e)}
                          aria-label={`Edit serialized ${e.cardIdentifier.toUpperCase()}`}
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
                          aria-label={`Delete serialized ${e.cardIdentifier.toUpperCase()}`}
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
