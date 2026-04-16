import { useEffect, useState, useCallback } from 'react';
import { auditApi, AuditLogEntry } from '../api/audit';

const LIMIT_OPTIONS = [50, 100, 250, 500];

function formatTimestamp(ts: string): string {
  const d = new Date(ts);
  return d.toLocaleString(undefined, {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
  });
}

export function LogViewer() {
  const [entries, setEntries] = useState<AuditLogEntry[]>([]);
  const [actionTypes, setActionTypes] = useState<string[]>([]);
  const [limit, setLimit] = useState(100);
  const [actionTypeFilter, setActionTypeFilter] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await auditApi.getLogs(limit, actionTypeFilter || undefined);
      setEntries(data);
    } catch {
      setError('Failed to load audit logs.');
    } finally {
      setLoading(false);
    }
  }, [limit, actionTypeFilter]);

  useEffect(() => {
    auditApi.getActionTypes().then(setActionTypes).catch(() => {});
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  return (
    <div>
      <h3>Audit Log</h3>

      <div style={{ display: 'flex', gap: '1rem', alignItems: 'center', marginBottom: '0.75rem', flexWrap: 'wrap' }}>
        <label>
          Limit:{' '}
          <select value={limit} onChange={e => setLimit(Number(e.target.value))}>
            {LIMIT_OPTIONS.map(n => (
              <option key={n} value={n}>{n}</option>
            ))}
          </select>
        </label>

        <label>
          Action type:{' '}
          <select value={actionTypeFilter} onChange={e => setActionTypeFilter(e.target.value)}>
            <option value="">All</option>
            {actionTypes.map(t => (
              <option key={t} value={t}>{t}</option>
            ))}
          </select>
        </label>

        <button onClick={load} disabled={loading}>
          {loading ? 'Loading...' : 'Refresh'}
        </button>
      </div>

      {error && <p style={{ color: 'red' }}>{error}</p>}

      {entries.length === 0 && !loading && !error && (
        <p>No audit log entries found.</p>
      )}

      {entries.length > 0 && (
        <div style={{ overflowX: 'auto' }}>
          <table>
            <thead>
              <tr>
                <th>Timestamp</th>
                <th>Actor</th>
                <th>Action</th>
                <th>Target</th>
                <th>Result</th>
                <th>IP Address</th>
              </tr>
            </thead>
            <tbody>
              {entries.map(e => (
                <tr key={e.id}>
                  <td style={{ whiteSpace: 'nowrap' }}>{formatTimestamp(e.timestamp)}</td>
                  <td title={e.actor}>{e.actorDisplayName}</td>
                  <td><code>{e.actionType}</code></td>
                  <td>{e.target ?? '-'}</td>
                  <td>{e.result}</td>
                  <td>{e.ipAddress ?? '-'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
