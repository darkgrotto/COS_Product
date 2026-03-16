import { useState, useRef, useCallback } from 'react';
import { cardsApi, CardSummary } from '../api/cards';

interface Props {
  value: string;
  onChange: (identifier: string) => void;
  id?: string;
  required?: boolean;
  disabled?: boolean;
}

export function CardLookup({ value, onChange, id, required, disabled }: Props) {
  const [query, setQuery] = useState('');
  const [results, setResults] = useState<CardSummary[]>([]);
  const [searching, setSearching] = useState(false);
  const [selectedName, setSelectedName] = useState<string>('');
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const handleSearch = useCallback((q: string) => {
    setQuery(q);
    if (debounceRef.current) clearTimeout(debounceRef.current);
    if (q.length < 2) {
      setResults([]);
      return;
    }
    debounceRef.current = setTimeout(async () => {
      setSearching(true);
      try {
        const data = await cardsApi.search(q);
        setResults(data);
      } catch {
        setResults([]);
      } finally {
        setSearching(false);
      }
    }, 300);
  }, []);

  const handleSelect = (card: CardSummary) => {
    onChange(card.identifier);
    setSelectedName(`${card.identifier.toUpperCase()} - ${card.name}`);
    setQuery('');
    setResults([]);
  };

  return (
    <div>
      {value && (
        <div aria-label="Selected card">
          {selectedName || value.toUpperCase()}
          <button
            type="button"
            onClick={() => { onChange(''); setSelectedName(''); }}
            aria-label="Clear card selection"
          >
            Clear
          </button>
        </div>
      )}
      {!value && (
        <>
          <input
            id={id}
            type="text"
            value={query}
            onChange={(e) => handleSearch(e.target.value)}
            placeholder="Search by name or identifier..."
            required={required && !value}
            disabled={disabled}
            aria-label="Card search"
            autoComplete="off"
          />
          {searching && <span>Searching...</span>}
          {results.length > 0 && (
            <ul role="listbox" aria-label="Card search results">
              {results.map((card) => (
                <li
                  key={card.identifier}
                  role="option"
                  aria-selected={false}
                  onClick={() => handleSelect(card)}
                  style={{ cursor: 'pointer' }}
                >
                  {card.identifier.toUpperCase()} - {card.name}
                </li>
              ))}
            </ul>
          )}
        </>
      )}
    </div>
  );
}
