import { useEffect, useState } from 'react';
import { SetCompletionResult } from '../types/metrics';
import { CARD_CONDITIONS } from '../types/filters';
import { Treatment } from '../types/treatments';
import { metricsApi } from '../api/metrics';
import { usersApi } from '../api/users';
import { collectionApi } from '../api/collection';
import { treatmentsApi } from '../api/treatments';
import { SetSymbol } from '../components/SetSymbol';

interface BulkAddForm {
  treatment: string;
  condition: string;
  acquisitionDate: string;
  acquisitionPrice: string;
}

const today = () => new Date().toISOString().split('T')[0];

export function SetCompletion() {
  const [results, setResults] = useState<SetCompletionResult[]>([]);
  // null = preference not yet loaded
  const [regularOnly, setRegularOnly] = useState<boolean | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [treatments, setTreatments] = useState<Treatment[]>([]);
  const [bulkAddSet, setBulkAddSet] = useState<string | null>(null);
  const [bulkForm, setBulkForm] = useState<BulkAddForm>({
    treatment: '',
    condition: 'NM',
    acquisitionDate: today(),
    acquisitionPrice: '',
  });
  const [bulkResult, setBulkResult] = useState<{ added: number; skipped: number } | null>(null);
  const [bulkError, setBulkError] = useState<string | null>(null);
  const [bulkSubmitting, setBulkSubmitting] = useState(false);

  useEffect(() => {
    usersApi.getPreferences()
      .then((prefs) => setRegularOnly(prefs.setCompletionRegularOnly))
      .catch(() => setRegularOnly(false));
    treatmentsApi.getAll().then((ts) => {
      setTreatments(ts);
      if (ts.length > 0) setBulkForm((f) => ({ ...f, treatment: ts[0].key }));
    }).catch(() => {});
  }, []);

  useEffect(() => {
    if (regularOnly === null) return;
    let cancelled = false;
    setLoading(true);
    metricsApi.getSetCompletion(regularOnly)
      .then((data) => { if (!cancelled) { setResults(data); setLoading(false); } })
      .catch(() => { if (!cancelled) { setError('Failed to load set completion'); setLoading(false); } });
    return () => { cancelled = true; };
  }, [regularOnly]);

  const handleRegularOnlyChange = (checked: boolean) => {
    setRegularOnly(checked);
    usersApi.patchPreferences({ setCompletionRegularOnly: checked }).catch(() => {});
  };

  const openBulkAdd = (setCode: string) => {
    setBulkAddSet(setCode);
    setBulkResult(null);
    setBulkError(null);
    setBulkForm((f) => ({ ...f, acquisitionDate: today(), acquisitionPrice: '' }));
  };

  const handleBulkSubmit = async (setCode: string) => {
    setBulkSubmitting(true);
    setBulkError(null);
    setBulkResult(null);
    try {
      const price = bulkForm.acquisitionPrice ? parseFloat(bulkForm.acquisitionPrice) : undefined;
      const result = await collectionApi.bulkAddSet({
        setCode,
        treatment: bulkForm.treatment,
        condition: bulkForm.condition,
        acquisitionDate: bulkForm.acquisitionDate,
        acquisitionPrice: price,
      });
      setBulkResult(result);
      if (result.added > 0 && regularOnly !== null) {
        metricsApi.getSetCompletion(regularOnly).then(setResults).catch(() => {});
      }
    } catch {
      setBulkError('Failed to add cards');
    } finally {
      setBulkSubmitting(false);
    }
  };

  return (
    <div>
      <h2>Set Completion</h2>
      <label>
        <input
          type="checkbox"
          checked={regularOnly ?? false}
          onChange={(e) => handleRegularOnlyChange(e.target.checked)}
        />
        Count regular/non-foil only
      </label>
      {error && <div role="alert">{error}</div>}
      {loading ? (
        <p>Loading...</p>
      ) : results.length === 0 ? (
        <p>No sets found.</p>
      ) : (
        <table aria-label="Set completion">
          <thead>
            <tr>
              <th>Set</th>
              <th>Name</th>
              <th>Owned</th>
              <th>Total</th>
              <th>Completion</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {results.map((r) => (
              <>
                <tr key={r.setCode}>
                  <td>
                    <SetSymbol setCode={r.setCode} />
                    {' '}
                    {r.setCode.toUpperCase()}
                  </td>
                  <td>{r.setName}</td>
                  <td>{r.ownedCount}</td>
                  <td>{r.totalCards}</td>
                  <td>{r.percentage.toFixed(1)}%</td>
                  <td>
                    <button
                      type="button"
                      onClick={() => bulkAddSet === r.setCode ? setBulkAddSet(null) : openBulkAdd(r.setCode)}
                    >
                      {bulkAddSet === r.setCode ? 'Cancel' : 'Mark as owned'}
                    </button>
                  </td>
                </tr>
                {bulkAddSet === r.setCode && (
                  <tr key={`${r.setCode}-bulk`}>
                    <td colSpan={6}>
                      <div>
                        <label>
                          Treatment
                          <select
                            value={bulkForm.treatment}
                            onChange={(e) => setBulkForm((f) => ({ ...f, treatment: e.target.value }))}
                            aria-label="Treatment for bulk add"
                          >
                            {treatments.map((t) => (
                              <option key={t.key} value={t.key}>{t.displayName}</option>
                            ))}
                          </select>
                        </label>
                        {' '}
                        <label>
                          Condition
                          <select
                            value={bulkForm.condition}
                            onChange={(e) => setBulkForm((f) => ({ ...f, condition: e.target.value }))}
                            aria-label="Condition for bulk add"
                          >
                            {CARD_CONDITIONS.map((c) => (
                              <option key={c} value={c}>{c}</option>
                            ))}
                          </select>
                        </label>
                        {' '}
                        <label>
                          Acquisition date
                          <input
                            type="date"
                            value={bulkForm.acquisitionDate}
                            onChange={(e) => setBulkForm((f) => ({ ...f, acquisitionDate: e.target.value }))}
                            aria-label="Acquisition date for bulk add"
                          />
                        </label>
                        {' '}
                        <label>
                          Acquisition price (optional)
                          <input
                            type="number"
                            min="0"
                            step="0.01"
                            value={bulkForm.acquisitionPrice}
                            onChange={(e) => setBulkForm((f) => ({ ...f, acquisitionPrice: e.target.value }))}
                            placeholder="Leave blank to use market value"
                            aria-label="Acquisition price for bulk add"
                          />
                        </label>
                        {' '}
                        <button
                          type="button"
                          onClick={() => handleBulkSubmit(r.setCode)}
                          disabled={bulkSubmitting || !bulkForm.treatment || !bulkForm.condition}
                        >
                          {bulkSubmitting ? 'Adding...' : `Add unowned cards from ${r.setCode.toUpperCase()}`}
                        </button>
                        {bulkResult && (
                          <span>
                            {' '}{bulkResult.added} card{bulkResult.added !== 1 ? 's' : ''} added,{' '}
                            {bulkResult.skipped} already owned
                          </span>
                        )}
                        {bulkError && <span role="alert">{' '}{bulkError}</span>}
                      </div>
                    </td>
                  </tr>
                )}
              </>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}
