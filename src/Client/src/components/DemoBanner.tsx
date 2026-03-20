import { useDemo } from '../context/DemoContext';

function formatCountdown(seconds: number): string {
  if (seconds <= 0) return 'Expired';
  const h = Math.floor(seconds / 3600);
  const m = Math.floor((seconds % 3600) / 60);
  const s = seconds % 60;
  if (h > 0) return `${h}h ${m}m ${s}s`;
  if (m > 0) return `${m}m ${s}s`;
  return `${s}s`;
}

export function DemoBanner() {
  const { isDemo, secondsRemaining } = useDemo();
  if (!isDemo) return null;

  return (
    <div role="banner" aria-label="Demo mode notice">
      <strong>Demo Mode</strong>
      {' - '}
      This is a demonstration environment. Some actions are disabled.
      {secondsRemaining > 0 && (
        <> Session expires in <strong>{formatCountdown(secondsRemaining)}</strong>.</>
      )}
    </div>
  );
}
