import { useEffect, useState } from 'react';
import { SetCompletionResult } from '../types/metrics';
import { metricsApi } from '../api/metrics';

export function SetCompletion() {
  const [results, setResults] = useState<SetCompletionResult[]>([]);
  const [regularOnly, setRegularOnly] = useState(false);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    metricsApi.getSetCompletion(regularOnly)
      .then((data) => { if (!cancelled) { setResults(data); setLoading(false); } })
      .catch(() => { if (!cancelled) { setError('Failed to load set completion'); setLoading(false); } });
    return () => { cancelled = true; };
  }, [regularOnly]);

  return (
    <div>
      <h2>Set Completion</h2>
      <label>
        <input
          type="checkbox"
          checked={regularOnly}
          onChange={(e) => setRegularOnly(e.target.checked)}
        />
        Count regular/non-foil only
      </label>
      {error && <div role="alert">{error}</div>}
      {loading ? (
        <p>Loading...</p>
      ) : results.length === 0 ? (
        <p>No sets found.</p>
      ) : (
        <table aria-label="Set completion">
          <thead>
            <tr>
              <th>Set</th>
              <th>Name</th>
              <th>Owned</th>
              <th>Total</th>
              <th>Completion</th>
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
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}
