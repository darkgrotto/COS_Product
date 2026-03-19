import { createContext, useContext, useEffect, useState } from 'react';
import { settingsApi } from '../api/settings';

const TcgPlayerContext = createContext(false);

export function TcgPlayerProvider({ children }: { children: React.ReactNode }) {
  const [configured, setConfigured] = useState(false);

  useEffect(() => {
    settingsApi.getTcgPlayer()
      .then((s) => setConfigured(s.configured))
      .catch(() => {});
  }, []);

  return (
    <TcgPlayerContext.Provider value={configured}>
      {children}
    </TcgPlayerContext.Provider>
  );
}

export function useTcgPlayerConfigured() {
  return useContext(TcgPlayerContext);
}
