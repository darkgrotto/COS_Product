import { useEffect, useState } from 'react';
import { SetCompletionResult } from '../types/metrics';
import { metricsApi } from '../api/metrics';
import { usersApi } from '../api/users';

export function CollectionValueBySet() {
  const [results, setResults] = useState<SetCompletionResult[]>([]);
  const [regularOnly, setRegularOnly] = useState<boolean | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    usersApi.getPreferences()
      .then((prefs) => setRegularOnly(prefs.setCompletionRegularOnly))
      .catch(() => setRegularOnly(false));
  }, []);

  useEffect(() => {
    if (regularOnly === null) return;
    let cancelled = false;
    setLoading(true);
    metricsApi.getSetCompletion(regularOnly)
      .then((data) => { if (!cancelled) { setResults(data); setLoading(false); } })
      .catch(() => { if (!cancelled) { setError('Failed to load set data'); setLoading(false); } });
    return () => { cancelled = true; };
  }, [regularOnly]);

  if (error) return <div role="alert">{error}</div>;

  return (
    <div>
      <h2>Collection by Set</h2>
      {loading ? (
        <p>Loading...</p>
      ) : results.length === 0 ? (
        <p>No sets found.</p>
      ) : (
        <table aria-label="Collection by set">
          <thead>
            <tr>
              <th>Set</th>
              <th>Name</th>
              <th>Owned</th>
              <th>Total</th>
              <th>Completion</th>
              <th>Collection value</th>
              <th>Profit / loss</th>
            </tr>
          </thead>
          <tbody>
            {results.map((r) => (
              <tr key={r.setCode}>
                <td>{r.setCode.toUpperCase()}</td>
                <td>{r.setName}</td>
                <td>{r.ownedCount}</td>
                <td>{r.totalCards}</td>
                <td>{r.percentage.toFixed(1)}%</td>
                <td>{r.totalValue !== null ? `$${r.totalValue.toFixed(2)}` : '--'}</td>
                <td>{r.totalProfitLoss !== null ? `$${r.totalProfitLoss.toFixed(2)}` : '--'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}
