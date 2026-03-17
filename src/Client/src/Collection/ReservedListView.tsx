import { useEffect, useState, useMemo } from 'react';
import { collectionApi, ReservedCollectionEntry } from '../api/collection';
import { ReservedBadge } from '../components/ReservedBadge';

type SortField = 'cardIdentifier' | 'cardName' | 'setCode' | 'cardType' | 'treatment' | 'quantity' | 'condition' | 'acquisitionPrice' | 'marketValue' | 'profitLoss';
type SortDir = 'asc' | 'desc';

interface Props {
  adminUserId?: string;
}

export function ReservedListView({ adminUserId }: Props) {
  const [entries, setEntries] = useState<ReservedCollectionEntry[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [filter, setFilter] = useState('');
  const [sortField, setSortField] = useState<SortField>('setCode');
  const [sortDir, setSortDir] = useState<SortDir>('asc');

  useEffect(() => {
    collectionApi.getReserved(adminUserId)
      .then(setEntries)
      .catch(() => setError('Failed to load reserved list collection'));
  }, [adminUserId]);

  function handleSort(field: SortField) {
    if (sortField === field) {
      setSortDir((d) => (d === 'asc' ? 'desc' : 'asc'));
    } else {
      setSortField(field);
      setSortDir('asc');
    }
  }

  const visible = useMemo(() => {
    if (!entries) return [];
    let result = entries;

    if (filter.trim()) {
      const q = filter.trim().toLowerCase();
      result = result.filter(
        (e) =>
          e.cardName.toLowerCase().includes(q) ||
          e.cardIdentifier.toLowerCase().includes(q) ||
          e.setCode.toLowerCase().includes(q)
      );
    }

    return [...result].sort((a, b) => {
      let cmp = 0;
      if (sortField === 'cardIdentifier') cmp = a.cardIdentifier.localeCompare(b.cardIdentifier);
      else if (sortField === 'cardName') cmp = a.cardName.localeCompare(b.cardName);
      else if (sortField === 'setCode') cmp = a.setCode.localeCompare(b.setCode) || a.cardIdentifier.localeCompare(b.cardIdentifier);
      else if (sortField === 'cardType') cmp = (a.cardType ?? '').localeCompare(b.cardType ?? '');
      else if (sortField === 'treatment') cmp = a.treatment.localeCompare(b.treatment);
      else if (sortField === 'quantity') cmp = a.quantity - b.quantity;
      else if (sortField === 'condition') cmp = a.condition.localeCompare(b.condition);
      else if (sortField === 'acquisitionPrice') cmp = a.acquisitionPrice - b.acquisitionPrice;
      else if (sortField === 'marketValue') cmp = (a.marketValue ?? 0) - (b.marketValue ?? 0);
      else if (sortField === 'profitLoss') {
        const plA = (a.marketValue ?? 0) - a.acquisitionPrice;
        const plB = (b.marketValue ?? 0) - b.acquisitionPrice;
        cmp = plA - plB;
      }
      return sortDir === 'asc' ? cmp : -cmp;
    });
  }, [entries, filter, sortField, sortDir]);

  function SortIcon({ field }: { field: SortField }) {
    if (sortField !== field) return <span aria-hidden="true" style={{ color: 'var(--text-muted, #888)', fontSize: '10px', marginLeft: '4px' }}>&#8597;</span>;
    return <span aria-hidden="true" style={{ fontSize: '10px', marginLeft: '4px' }}>{sortDir === 'asc' ? '▲' : '▼'}</span>;
  }

  if (error) return <div role="alert">{error}</div>;
  if (!entries) return <p>Loading...</p>;

  const totalValue = visible.reduce((sum, e) => sum + (e.marketValue ?? 0) * e.quantity, 0);
  const totalCost = visible.reduce((sum, e) => sum + e.acquisitionPrice * e.quantity, 0);
  const totalPL = totalValue - totalCost;

  return (
    <div>
      <div style={{ marginBottom: '12px', display: 'flex', alignItems: 'center', gap: '12px', flexWrap: 'wrap' }}>
        <ReservedBadge inline={false} />
        <span style={{ fontSize: '14px' }}>
          {visible.length} {visible.length === 1 ? 'entry' : 'entries'}
          {filter && entries.length !== visible.length ? ` (filtered from ${entries.length})` : ''}
        </span>
        <span style={{ fontSize: '14px' }}>
          Value: <strong>${totalValue.toFixed(2)}</strong>
          {' - '}
          P/L:{' '}
          <strong style={{ color: totalPL >= 0 ? 'green' : 'red' }}>
            {totalPL >= 0 ? '+' : ''}{totalPL.toFixed(2)}
          </strong>
        </span>
      </div>

      <div style={{ marginBottom: '12px' }}>
        <input
          type="search"
          placeholder="Search by name, identifier, or set..."
          value={filter}
          onChange={(e) => setFilter(e.target.value)}
          style={{ width: '280px' }}
          aria-label="Search reserved list cards"
        />
      </div>

      {visible.length === 0 ? (
        <p>{filter ? 'No matching cards.' : 'No Reserved List cards in your collection.'}</p>
      ) : (
        <table aria-label="Reserved list collection">
          <thead>
            <tr>
              <th>
                <button type="button" onClick={() => handleSort('cardName')} style={{ background: 'none', border: 'none', cursor: 'pointer', fontWeight: 'bold', padding: 0 }}>
                  Card <SortIcon field="cardName" />
                </button>
              </th>
              <th>
                <button type="button" onClick={() => handleSort('setCode')} style={{ background: 'none', border: 'none', cursor: 'pointer', fontWeight: 'bold', padding: 0 }}>
                  Set <SortIcon field="setCode" />
                </button>
              </th>
              <th>
                <button type="button" onClick={() => handleSort('cardType')} style={{ background: 'none', border: 'none', cursor: 'pointer', fontWeight: 'bold', padding: 0 }}>
                  Type <SortIcon field="cardType" />
                </button>
              </th>
              <th>
                <button type="button" onClick={() => handleSort('treatment')} style={{ background: 'none', border: 'none', cursor: 'pointer', fontWeight: 'bold', padding: 0 }}>
                  Treatment <SortIcon field="treatment" />
                </button>
              </th>
              <th>
                <button type="button" onClick={() => handleSort('quantity')} style={{ background: 'none', border: 'none', cursor: 'pointer', fontWeight: 'bold', padding: 0 }}>
                  Qty <SortIcon field="quantity" />
                </button>
              </th>
              <th>
                <button type="button" onClick={() => handleSort('condition')} style={{ background: 'none', border: 'none', cursor: 'pointer', fontWeight: 'bold', padding: 0 }}>
                  Condition <SortIcon field="condition" />
                </button>
              </th>
              <th>
                <button type="button" onClick={() => handleSort('acquisitionPrice')} style={{ background: 'none', border: 'none', cursor: 'pointer', fontWeight: 'bold', padding: 0 }}>
                  Acq. Price <SortIcon field="acquisitionPrice" />
                </button>
              </th>
              <th>
                <button type="button" onClick={() => handleSort('marketValue')} style={{ background: 'none', border: 'none', cursor: 'pointer', fontWeight: 'bold', padding: 0 }}>
                  Market Value <SortIcon field="marketValue" />
                </button>
              </th>
              <th>
                <button type="button" onClick={() => handleSort('profitLoss')} style={{ background: 'none', border: 'none', cursor: 'pointer', fontWeight: 'bold', padding: 0 }}>
                  P/L <SortIcon field="profitLoss" />
                </button>
              </th>
            </tr>
          </thead>
          <tbody>
            {visible.map((e) => {
              const pl = ((e.marketValue ?? 0) - e.acquisitionPrice) * e.quantity;
              return (
                <tr key={e.entryId}>
                  <td>
                    <span>{e.cardName}</span>
                    {' '}
                    <span style={{ color: 'var(--text-muted, #888)', fontSize: '12px' }}>
                      {e.cardIdentifier}
                    </span>
                    <ReservedBadge />
                    {e.autographed && (
                      <span style={{ marginLeft: '4px', fontSize: '10px', color: '#6b7280' }} title="Autographed">
                        Autographed
                      </span>
                    )}
                  </td>
                  <td>{e.setCode}</td>
                  <td style={{ color: 'var(--text-muted, #888)', fontSize: '13px' }}>{e.cardType ?? '-'}</td>
                  <td>{e.treatment}</td>
                  <td>{e.quantity}</td>
                  <td>{e.condition}</td>
                  <td>${(e.acquisitionPrice * e.quantity).toFixed(2)}</td>
                  <td>{e.marketValue != null ? `$${(e.marketValue * e.quantity).toFixed(2)}` : '-'}</td>
                  <td style={{ color: pl >= 0 ? 'green' : 'red', fontWeight: 500 }}>
                    {e.marketValue != null ? `${pl >= 0 ? '+' : ''}$${pl.toFixed(2)}` : '-'}
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
