import { useEffect, useState } from 'react';
import { CollectionEntry } from '../types/collection';
import { CollectionFilter } from '../types/filters';
import { collectionApi } from '../api/collection';
import { UniversalFilter } from '../components/UniversalFilter';
import { useReservedList } from '../hooks/useReservedList';
import { ReservedBadge } from '../components/ReservedBadge';
import { SetSymbol } from '../components/SetSymbol';

interface Props {
  adminUserId?: string;
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
  const reservedSet = useReservedList();

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
              <th>Market value</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {entries.map((e) => {
              const setCode = extractSetCode(e.cardIdentifier);
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
                  <td>
                    {!adminUserId && (
                      <button
                        type="button"
                        onClick={() => handleDelete(e.id)}
                        aria-label={`Delete ${e.cardIdentifier.toUpperCase()}`}
                      >
                        Delete
                      </button>
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
