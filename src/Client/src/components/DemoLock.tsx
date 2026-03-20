import { useDemo } from '../context/DemoContext';

interface Props {
  children: React.ReactNode;
}

export function DemoLock({ children }: Props) {
  const { isDemo } = useDemo();
  if (!isDemo) return <>{children}</>;

  return (
    <span title="Not available in demo mode" aria-disabled="true">
      <span style={{ pointerEvents: 'none', opacity: 0.5 }}>
        {children}
      </span>
    </span>
  );
}
