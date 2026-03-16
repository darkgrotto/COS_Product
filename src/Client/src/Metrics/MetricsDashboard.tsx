import { useEffect, useState } from 'react';
import { MetricsResult } from '../types/metrics';
import { CollectionFilter } from '../types/filters';
import { metricsApi } from '../api/metrics';
import { UniversalFilter } from '../components/UniversalFilter';

interface Props {
  adminUserId?: string;
}

export function MetricsDashboard({ adminUserId }: Props) {
  const [metrics, setMetrics] = useState<MetricsResult | null>(null);
  const [filter, setFilter] = useState<CollectionFilter>({});
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    metricsApi.getMetrics(filter, adminUserId)
      .then((data) => { if (!cancelled) { setMetrics(data); setLoading(false); } })
      .catch(() => { if (!cancelled) { setError('Failed to load metrics'); setLoading(false); } });
    return () => { cancelled = true; };
  }, [filter, adminUserId]);

  return (
    <div>
      <h2>Collection Metrics</h2>
      <UniversalFilter filter={filter} onChange={setFilter} />
      {error && <div role="alert">{error}</div>}
      {loading ? (
        <p>Loading...</p>
      ) : metrics ? (
        <div>
          <dl>
            <dt>Total value</dt>
            <dd>${metrics.totalValue.toFixed(2)}</dd>
            <dt>Profit / loss</dt>
            <dd>${metrics.totalProfitLoss.toFixed(2)}</dd>
            <dt>Total cards</dt>
            <dd>{metrics.totalCardCount}</dd>
            <dt>Serialized cards</dt>
            <dd>{metrics.serializedCount}</dd>
            <dt>Slabs</dt>
            <dd>{metrics.slabCount}</dd>
            <dt>Sealed products</dt>
            <dd>{metrics.sealedProductCount}</dd>
            <dt>Sealed product value</dt>
            <dd>${metrics.sealedProductValue.toFixed(2)}</dd>
          </dl>
          {metrics.byContentType.length > 0 && (
            <table aria-label="Metrics by content type">
              <thead>
                <tr>
                  <th>Content type</th>
                  <th>Count</th>
                  <th>Total value</th>
                  <th>Profit / loss</th>
                </tr>
              </thead>
              <tbody>
                {metrics.byContentType.map((row) => (
                  <tr key={row.contentType}>
                    <td>{row.contentType}</td>
                    <td>{row.count}</td>
                    <td>${row.totalValue.toFixed(2)}</td>
                    <td>${row.totalProfitLoss.toFixed(2)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      ) : null}
    </div>
  );
}
