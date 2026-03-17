import { useEffect, useState } from 'react';
import { cardsApi } from '../api/cards';

// Returns a Set of lowercase card identifiers that are on the Reserved List.
// Loaded once per session; empty set while loading or on error.
let cachedIdentifiers: Set<string> | null = null;
let pendingPromise: Promise<Set<string>> | null = null;

function loadReservedIdentifiers(): Promise<Set<string>> {
  if (cachedIdentifiers) return Promise.resolve(cachedIdentifiers);
  if (pendingPromise) return pendingPromise;
  pendingPromise = cardsApi.getReservedIdentifiers()
    .then((ids) => {
      cachedIdentifiers = new Set(ids.map((id) => id.toLowerCase()));
      return cachedIdentifiers;
    })
    .catch(() => {
      pendingPromise = null;
      return new Set<string>();
    });
  return pendingPromise;
}

export function useReservedList(): Set<string> {
  const [reserved, setReserved] = useState<Set<string>>(cachedIdentifiers ?? new Set());

  useEffect(() => {
    if (cachedIdentifiers) {
      setReserved(cachedIdentifiers);
      return;
    }
    loadReservedIdentifiers().then(setReserved);
  }, []);

  return reserved;
}
