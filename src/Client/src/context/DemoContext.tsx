import { createContext, useContext, useEffect, useRef, useState } from 'react';
import { demoApi } from '../api/demo';

interface DemoState {
  isDemo: boolean;
  demoSets: string[];
  secondsRemaining: number;
}

const DemoContext = createContext<DemoState>({ isDemo: false, demoSets: [], secondsRemaining: 0 });

export function DemoProvider({ children }: { children: React.ReactNode }) {
  const [state, setState] = useState<DemoState>({ isDemo: false, demoSets: [], secondsRemaining: 0 });
  const intervalRef = useRef<ReturnType<typeof setInterval> | null>(null);

  useEffect(() => {
    demoApi.getStatus()
      .then((status) => {
        if (!status) return;
        setState({
          isDemo: true,
          demoSets: status.demoSets,
          secondsRemaining: status.secondsRemaining,
        });
      })
      .catch(() => {});
  }, []);

  useEffect(() => {
    if (!state.isDemo) return;
    intervalRef.current = setInterval(() => {
      setState((prev) => ({
        ...prev,
        secondsRemaining: Math.max(0, prev.secondsRemaining - 1),
      }));
    }, 1000);
    return () => {
      if (intervalRef.current) clearInterval(intervalRef.current);
    };
  }, [state.isDemo]);

  return <DemoContext.Provider value={state}>{children}</DemoContext.Provider>;
}

export function useDemo() {
  return useContext(DemoContext);
}
