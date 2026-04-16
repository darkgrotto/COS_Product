import { AdminSettings } from './AdminSettings';
import { GradingAgencyManager } from './GradingAgencyManager';

export function InstanceSettings() {
  return (
    <div>
      <AdminSettings />
      <hr />
      <GradingAgencyManager />
    </div>
  );
}
