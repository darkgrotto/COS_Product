import { MetricsDashboard } from '../Metrics/MetricsDashboard';
import { SetCompletion } from '../Collection/SetCompletion';

export function CollectionDashboard() {
  return (
    <div>
      <h1>Collection Overview</h1>
      <MetricsDashboard />
      <SetCompletion />
    </div>
  );
}
