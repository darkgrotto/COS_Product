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

function DetailPanel({ entry, onClose }: { entry: AuditLogEntry; onClose: () => void }) {
  return (
    <div role="region" aria-label="Audit log entry detail" style={{ border: '1px solid', padding: '1rem', marginTop: '1rem' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.75rem' }}>
        <strong>Entry Detail</strong>
        <button type="button" onClick={onClose} aria-label="Close detail panel">Close</button>
      </div>
      <dl style={{ display: 'grid', gridTemplateColumns: 'max-content 1fr', gap: '0.25rem 1rem' }}>
        <dt>ID</dt>
        <dd><code style={{ wordBreak: 'break-all' }}>{entry.id}</code></dd>

        <dt>Timestamp</dt>
        <dd>{formatTimestamp(entry.timestamp)}</dd>

        <dt>Actor</dt>
        <dd>{entry.actorDisplayName} <span style={{ opacity: 0.7 }}>({entry.actor})</span></dd>

        <dt>Action</dt>
        <dd><code>{entry.actionType}</code></dd>

        <dt>Target</dt>
        <dd>{entry.target ?? <em>none</em>}</dd>

        <dt>Result</dt>
        <dd style={{ wordBreak: 'break-word' }}>{entry.result}</dd>

        <dt>IP Address</dt>
        <dd>{entry.ipAddress ?? <em>none</em>}</dd>

        <dt>Session ID</dt>
        <dd>{entry.sessionId ? <code>{entry.sessionId}</code> : <em>none</em>}</dd>
      </dl>
    </div>
  );
}

export function LogViewer() {
  const [entries, setEntries] = useState<AuditLogEntry[]>([]);
  const [actionTypes, setActionTypes] = useState<string[]>([]);
  const [limit, setLimit] = useState(100);
  const [actionTypeFilter, setActionTypeFilter] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [selected, setSelected] = useState<AuditLogEntry | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    setSelected(null);
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
                <tr
                  key={e.id}
                  onClick={() => setSelected(prev => prev?.id === e.id ? null : e)}
                  style={{ cursor: 'pointer', background: selected?.id === e.id ? 'var(--color-bg-subtle, #f0f0f0)' : undefined }}
                  aria-selected={selected?.id === e.id}
                >
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

      {selected && (
        <DetailPanel entry={selected} onClose={() => setSelected(null)} />
      )}
    </div>
  );
}
