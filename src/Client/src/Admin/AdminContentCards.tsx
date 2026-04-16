import { useEffect, useState } from 'react';
import { setsApi, SetSummary } from '../api/sets';
import { api } from '../api/client';
import { SetSymbol } from '../components/SetSymbol';

interface CardEntry {
  identifier: string;
  name: string;
  color: string | null;
  cardType: string | null;
  rarity: string | null;
  currentMarketValue: number | null;
  isReserved: boolean;
  validTreatments: string[];
}

export function AdminContentCards() {
  const [sets, setSets] = useState<SetSummary[]>([]);
  const [setsLoading, setSetsLoading] = useState(true);
  const [setSearch, setSetSearch] = useState('');
  const [selectedSet, setSelectedSet] = useState<SetSummary | null>(null);
  const [cards, setCards] = useState<CardEntry[]>([]);
  const [cardsLoading, setCardsLoading] = useState(false);
  const [cardsError, setCardsError] = useState<string | null>(null);

  useEffect(() => {
    setsApi.getAll()
      .then((data) => { setSets(data); setSetsLoading(false); })
      .catch(() => setSetsLoading(false));
  }, []);

  const selectSet = async (set: SetSummary) => {
    setSelectedSet(set);
    setCards([]);
    setCardsError(null);
    setCardsLoading(true);
    try {
      const data = await api.get<CardEntry[]>(`/api/sets/${set.code.toLowerCase()}/cards`);
      setCards(data);
    } catch {
      setCardsError('Failed to load cards for this set');
    } finally {
      setCardsLoading(false);
    }
  };

  const filteredSets = sets.filter((s) => {
    const q = setSearch.toLowerCase();
    return !q || s.code.toLowerCase().includes(q) || s.name.toLowerCase().includes(q);
  });

  return (
    <div>
      <div style={{ display: 'flex', gap: '1rem' }}>
        <section aria-label="Set list" style={{ minWidth: '220px' }}>
          <h4>Sets ({sets.length})</h4>
          <input
            type="search"
            placeholder="Filter sets..."
            value={setSearch}
            onChange={(e) => setSetSearch(e.target.value)}
            aria-label="Filter sets"
          />
          {setsLoading ? (
            <p>Loading...</p>
          ) : (
            <ul style={{ listStyle: 'none', padding: 0, maxHeight: '70vh', overflowY: 'auto' }}>
              {filteredSets.map((s) => (
                <li key={s.code}>
                  <button
                    type="button"
                    onClick={() => selectSet(s)}
                    aria-pressed={selectedSet?.code === s.code}
                    style={{ width: '100%', textAlign: 'left' }}
                  >
                    <SetSymbol setCode={s.code.toLowerCase()} />
                    {' '}{s.code} - {s.name}
                    {' '}
                    <small>({s.totalCards})</small>
                  </button>
                </li>
              ))}
            </ul>
          )}
        </section>

        <section aria-label="Card list" style={{ flex: 1 }}>
          {selectedSet ? (
            <>
              <h4>
                <SetSymbol setCode={selectedSet.code.toLowerCase()} />
                {' '}{selectedSet.code} - {selectedSet.name}
                {' '}
                <small>({selectedSet.totalCards} cards)</small>
              </h4>
              {cardsLoading ? (
                <p>Loading...</p>
              ) : cardsError ? (
                <div role="alert">{cardsError}</div>
              ) : (
                <table aria-label={`Cards in ${selectedSet.name}`}>
                  <thead>
                    <tr>
                      <th>Identifier</th>
                      <th>Name</th>
                      <th>Type</th>
                      <th>Rarity</th>
                      <th>Color</th>
                      <th>Market value</th>
                      <th>Treatments</th>
                    </tr>
                  </thead>
                  <tbody>
                    {cards.map((c) => (
                      <tr key={c.identifier}>
                        <td>{c.identifier.toUpperCase()}</td>
                        <td>
                          {c.name}
                          {c.isReserved && <> <abbr title="Reserved List">RL</abbr></>}
                        </td>
                        <td>{c.cardType ?? '--'}</td>
                        <td>{c.rarity ?? '--'}</td>
                        <td>{c.color ?? '--'}</td>
                        <td>
                          {c.currentMarketValue !== null
                            ? `$${c.currentMarketValue.toFixed(2)}`
                            : '--'}
                        </td>
                        <td>{c.validTreatments.join(', ') || '--'}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </>
          ) : (
            <p>Select a set to browse its cards.</p>
          )}
        </section>
      </div>
    </div>
  );
}
