import { useEffect, useState } from 'react';
import { GradingAgency } from '../types/gradingAgency';
import { gradingAgenciesApi, GradingAgencyCreateRequest, GradingAgencyDeleteConflict } from '../api/gradingAgencies';

export function GradingAgencyManager() {
  const [agencies, setAgencies] = useState<GradingAgency[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [newCode, setNewCode] = useState('');
  const [newFullName, setNewFullName] = useState('');
  const [newUrl, setNewUrl] = useState('');
  const [newDirectLookup, setNewDirectLookup] = useState(true);
  const [creating, setCreating] = useState(false);
  const [createError, setCreateError] = useState<string | null>(null);

  const [deleteConflict, setDeleteConflict] = useState<{ code: string; conflict: GradingAgencyDeleteConflict } | null>(null);
  const [replacementCode, setReplacementCode] = useState('');

  useEffect(() => {
    gradingAgenciesApi.getAll()
      .then((data) => { setAgencies(data); setLoading(false); })
      .catch(() => { setError('Failed to load grading agencies'); setLoading(false); });
  }, []);

  const handleCreate = async () => {
    setCreateError(null);
    if (!newCode.trim() || !newFullName.trim() || !newUrl.trim()) {
      setCreateError('All fields are required');
      return;
    }
    setCreating(true);
    try {
      const created = await gradingAgenciesApi.create({
        code: newCode.trim().toUpperCase(),
        fullName: newFullName.trim(),
        validationUrlTemplate: newUrl.trim(),
        supportsDirectLookup: newDirectLookup,
      });
      setAgencies((prev) => [...prev, created]);
      setNewCode('');
      setNewFullName('');
      setNewUrl('');
      setNewDirectLookup(true);
    } catch {
      setCreateError('Failed to create agency');
    } finally {
      setCreating(false);
    }
  };

  const handleDelete = async (code: string, replacement?: string) => {
    try {
      await gradingAgenciesApi.delete(code, replacement ? { replacementCode: replacement } : undefined);
      setAgencies((prev) => prev.filter((a) => a.code !== code));
      setDeleteConflict(null);
      setReplacementCode('');
    } catch (err: unknown) {
      if (err && typeof err === 'object' && 'status' in err && (err as { status: number }).status === 409) {
        const body = (err as { body: string }).body;
        try {
          const conflict = JSON.parse(body) as GradingAgencyDeleteConflict;
          setDeleteConflict({ code, conflict });
        } catch {
          setError('Delete failed with conflict');
        }
      } else {
        setError('Failed to delete agency');
      }
    }
  };

  if (loading) return <p>Loading...</p>;
  if (error) return <div role="alert">{error}</div>;

  const localAgencies = agencies.filter((a) => a.source === 'Local');
  const canonicalAgencies = agencies.filter((a) => a.source === 'Canonical');

  return (
    <div>
      <h2>Grading Agencies</h2>

      <h3>Canonical agencies</h3>
      <table aria-label="Canonical grading agencies">
        <thead>
          <tr><th>Code</th><th>Name</th><th>Direct lookup</th><th>Active</th></tr>
        </thead>
        <tbody>
          {canonicalAgencies.map((a) => (
            <tr key={a.code}>
              <td>{a.code}</td>
              <td>{a.fullName}</td>
              <td>{a.supportsDirectLookup ? 'Yes' : 'No'}</td>
              <td>{a.active ? 'Yes' : 'No'}</td>
            </tr>
          ))}
        </tbody>
      </table>

      <h3>Local agencies</h3>
      {localAgencies.length === 0 ? (
        <p>No local agencies configured.</p>
      ) : (
        <table aria-label="Local grading agencies">
          <thead>
            <tr><th>Code</th><th>Name</th><th>Direct lookup</th><th>Active</th><th>Actions</th></tr>
          </thead>
          <tbody>
            {localAgencies.map((a) => (
              <tr key={a.code}>
                <td>{a.code}</td>
                <td>{a.fullName}</td>
                <td>{a.supportsDirectLookup ? 'Yes' : 'No'}</td>
                <td>{a.active ? 'Yes' : 'No'}</td>
                <td>
                  <button
                    type="button"
                    onClick={() => handleDelete(a.code)}
                    aria-label={`Delete agency ${a.code}`}
                  >
                    Delete
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      {deleteConflict && (
        <div role="dialog" aria-label="Replacement required">
          <p>
            Agency <strong>{deleteConflict.code}</strong> is referenced by{' '}
            {deleteConflict.conflict.recordCount} slab record(s). Select a replacement agency:
          </p>
          <select
            value={replacementCode}
            onChange={(e) => setReplacementCode(e.target.value)}
            aria-label="Replacement agency"
          >
            <option value="">Select replacement...</option>
            {agencies
              .filter((a) => a.code !== deleteConflict.code && a.active)
              .map((a) => (
                <option key={a.code} value={a.code}>
                  {a.code} - {a.fullName}
                </option>
              ))}
          </select>
          <button
            type="button"
            onClick={() => handleDelete(deleteConflict.code, replacementCode)}
            disabled={!replacementCode}
          >
            Confirm delete and remap
          </button>
          <button type="button" onClick={() => { setDeleteConflict(null); setReplacementCode(''); }}>
            Cancel
          </button>
        </div>
      )}

      <h3>Add local agency</h3>
      {createError && <div role="alert">{createError}</div>}
      <div>
        <label htmlFor="new-agency-code">Code</label>
        <input
          id="new-agency-code"
          type="text"
          value={newCode}
          onChange={(e) => setNewCode(e.target.value)}
          disabled={creating}
        />
        <label htmlFor="new-agency-name">Full name</label>
        <input
          id="new-agency-name"
          type="text"
          value={newFullName}
          onChange={(e) => setNewFullName(e.target.value)}
          disabled={creating}
        />
        <label htmlFor="new-agency-url">Validation URL template</label>
        <input
          id="new-agency-url"
          type="text"
          value={newUrl}
          onChange={(e) => setNewUrl(e.target.value)}
          disabled={creating}
        />
        <label>
          <input
            type="checkbox"
            checked={newDirectLookup}
            onChange={(e) => setNewDirectLookup(e.target.checked)}
            disabled={creating}
          />
          Supports direct certificate lookup
        </label>
        <button type="button" onClick={handleCreate} disabled={creating}>
          {creating ? 'Creating...' : 'Add agency'}
        </button>
      </div>
    </div>
  );
}
