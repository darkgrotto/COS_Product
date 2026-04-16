import { useEffect, useRef, useState, FormEvent } from 'react';
import { setsApi, SetSummary } from '../api/sets';
import { collectionApi } from '../api/collection';
import { TreatmentSelector } from '../components/TreatmentSelector';
import { ConditionSelector } from '../components/ConditionSelector';
import { CardCondition } from '../types/filters';
import { SetSymbol } from '../components/SetSymbol';

interface SetResult {
  setCode: string;
  added: number;
  skipped: number;
  notFound: boolean;
}

interface Props {
  onClose: () => void;
  onComplete: () => void;
}

export function BulkAddSetsDialog({ onClose, onComplete }: Props) {
  const today = new Date().toISOString().split('T')[0];

  const [allSets, setAllSets] = useState<SetSummary[]>([]);
  const [setsLoading, setSetsLoading] = useState(true);
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedSets, setSelectedSets] = useState<SetSummary[]>([]);

  const [treatment, setTreatment] = useState('');
  const [condition, setCondition] = useState<CardCondition | ''>('');
  const [acquisitionDate, setAcquisitionDate] = useState(today);
  const [acquisitionPrice, setAcquisitionPrice] = useState('');

  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [results, setResults] = useState<SetResult[] | null>(null);
  const [totalAdded, setTotalAdded] = useState(0);
  const [totalSkipped, setTotalSkipped] = useState(0);

  const searchRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    setsApi.getAll()
      .then((data) => { setAllSets(data); setSetsLoading(false); })
      .catch(() => setSetsLoading(false));
  }, []);

  useEffect(() => {
    searchRef.current?.focus();
  }, []);

  const filteredSets = searchQuery.trim().length === 0
    ? []
    : allSets.filter((s) => {
        const q = searchQuery.trim().toLowerCase();
        return (
          s.code.toLowerCase().includes(q) ||
          s.name.toLowerCase().includes(q)
        ) && !selectedSets.some((sel) => sel.code === s.code);
      }).slice(0, 10);

  const addSet = (set: SetSummary) => {
    setSelectedSets((prev) => [...prev, set]);
    setSearchQuery('');
    searchRef.current?.focus();
  };

  const removeSet = (code: string) => {
    setSelectedSets((prev) => prev.filter((s) => s.code !== code));
  };

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setError(null);

    if (selectedSets.length === 0) { setError('Select at least one set.'); return; }
    if (!treatment) { setError('Treatment is required.'); return; }
    if (!condition) { setError('Condition is required.'); return; }
    if (!acquisitionDate) { setError('Acquisition date is required.'); return; }

    setSubmitting(true);
    try {
      const result = await collectionApi.bulkAddSets({
        setCodes: selectedSets.map((s) => s.code),
        treatment,
        condition,
        acquisitionDate,
        acquisitionPrice: acquisitionPrice ? parseFloat(acquisitionPrice) : undefined,
      });
      setResults(result.bySet);
      setTotalAdded(result.totalAdded);
      setTotalSkipped(result.totalSkipped);
      onComplete();
    } catch {
      setError('Failed to add sets. Please try again.');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div role="dialog" aria-modal="true" aria-labelledby="bulk-add-sets-title">
      <h2 id="bulk-add-sets-title">Add Complete Set(s)</h2>

      {results ? (
        <div>
          <p>
            Done - {totalAdded} card{totalAdded !== 1 ? 's' : ''} added,{' '}
            {totalSkipped} already owned.
          </p>
          <table aria-label="Results by set">
            <thead>
              <tr>
                <th>Set</th>
                <th>Added</th>
                <th>Skipped</th>
              </tr>
            </thead>
            <tbody>
              {results.map((r) => (
                <tr key={r.setCode}>
                  <td>
                    <SetSymbol setCode={r.setCode.toLowerCase()} />
                    {' '}{r.setCode}
                    {r.notFound && ' (not found)'}
                  </td>
                  <td>{r.added}</td>
                  <td>{r.skipped}</td>
                </tr>
              ))}
            </tbody>
          </table>
          <button type="button" onClick={onClose}>Close</button>
        </div>
      ) : (
        <form onSubmit={handleSubmit} aria-label="Bulk add sets form" noValidate>
          {error && <div role="alert">{error}</div>}

          <fieldset>
            <legend>Sets to add</legend>

            {selectedSets.length > 0 && (
              <ul aria-label="Selected sets">
                {selectedSets.map((s) => (
                  <li key={s.code}>
                    <SetSymbol setCode={s.code.toLowerCase()} />
                    {' '}{s.code} - {s.name} ({s.totalCards} cards)
                    {' '}
                    <button
                      type="button"
                      onClick={() => removeSet(s.code)}
                      aria-label={`Remove ${s.name}`}
                      disabled={submitting}
                    >
                      Remove
                    </button>
                  </li>
                ))}
              </ul>
            )}

            <label htmlFor="set-search">Search sets</label>
            <input
              id="set-search"
              ref={searchRef}
              type="text"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              placeholder={setsLoading ? 'Loading sets...' : 'Set name or code...'}
              disabled={submitting || setsLoading}
              autoComplete="off"
              aria-expanded={filteredSets.length > 0}
              aria-autocomplete="list"
              aria-controls="set-search-results"
            />
            {filteredSets.length > 0 && (
              <ul id="set-search-results" role="listbox" aria-label="Set search results">
                {filteredSets.map((s) => (
                  <li key={s.code} role="option" aria-selected={false}>
                    <button
                      type="button"
                      onClick={() => addSet(s)}
                      disabled={submitting}
                    >
                      <SetSymbol setCode={s.code.toLowerCase()} />
                      {' '}{s.code} - {s.name} ({s.totalCards} cards)
                    </button>
                  </li>
                ))}
              </ul>
            )}
          </fieldset>

          <label htmlFor="bulk-treatment">Treatment</label>
          <TreatmentSelector
            id="bulk-treatment"
            value={treatment}
            onChange={setTreatment}
            required
            disabled={submitting}
          />

          <label htmlFor="bulk-condition">Condition</label>
          <ConditionSelector
            id="bulk-condition"
            value={condition}
            onChange={setCondition}
            required
            disabled={submitting}
          />

          <label htmlFor="bulk-acq-date">Acquisition date</label>
          <input
            id="bulk-acq-date"
            type="date"
            value={acquisitionDate}
            onChange={(e) => setAcquisitionDate(e.target.value)}
            required
            disabled={submitting}
          />

          <label htmlFor="bulk-acq-price">
            Acquisition price per card (optional - uses market value if blank)
          </label>
          <input
            id="bulk-acq-price"
            type="number"
            value={acquisitionPrice}
            onChange={(e) => setAcquisitionPrice(e.target.value)}
            min={0}
            step="0.01"
            placeholder="Market value"
            disabled={submitting}
          />

          <div>
            <button
              type="submit"
              disabled={submitting || selectedSets.length === 0}
            >
              {submitting
                ? 'Adding...'
                : `Add ${selectedSets.length > 0
                    ? `${selectedSets.length} set${selectedSets.length !== 1 ? 's' : ''}`
                    : 'sets'}`}
            </button>
            {' '}
            <button type="button" onClick={onClose} disabled={submitting}>
              Cancel
            </button>
          </div>
        </form>
      )}
    </div>
  );
}
