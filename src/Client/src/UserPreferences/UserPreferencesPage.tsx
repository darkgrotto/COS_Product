import { useEffect, useState } from 'react';
import { usersApi, UserPreferences } from '../api/users';

const PAGE_OPTIONS: { label: string; value: string }[] = [
  { label: 'Dashboard', value: '/' },
  { label: 'Collection', value: '/collection' },
  { label: 'Reserved List', value: '/collection/reserved-list' },
  { label: 'Set Completion', value: '/collection/completion' },
  { label: 'Serialized', value: '/serialized' },
  { label: 'Slabs', value: '/slabs' },
  { label: 'Sealed Product', value: '/sealed' },
  { label: 'Wishlist', value: '/wishlist' },
  { label: 'Metrics', value: '/metrics' },
];

export function UserPreferencesPage() {
  const [prefs, setPrefs] = useState<UserPreferences | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [saved, setSaved] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    usersApi.getPreferences()
      .then((p) => { setPrefs(p); setLoading(false); })
      .catch(() => { setError('Failed to load preferences'); setLoading(false); });
  }, []);

  const handleSave = async () => {
    if (!prefs) return;
    setSaving(true);
    setSaved(false);
    setError(null);
    try {
      await usersApi.patchPreferences(prefs);
      setSaved(true);
    } catch {
      setError('Failed to save preferences');
    } finally {
      setSaving(false);
    }
  };

  if (loading) return <p>Loading...</p>;
  if (!prefs) return <div role="alert">{error}</div>;

  return (
    <div>
      <h2>Preferences</h2>
      {error && <div role="alert">{error}</div>}

      <section>
        <h3>Default landing page</h3>
        <select
          value={prefs.defaultPage ?? '/'}
          onChange={(e) =>
            setPrefs({ ...prefs, defaultPage: e.target.value === '/' ? null : e.target.value })
          }
        >
          {PAGE_OPTIONS.map((opt) => (
            <option key={opt.value} value={opt.value}>
              {opt.label}
            </option>
          ))}
        </select>
      </section>

      <section>
        <h3>Set completion</h3>
        <label>
          <input
            type="checkbox"
            checked={prefs.setCompletionRegularOnly}
            onChange={(e) => setPrefs({ ...prefs, setCompletionRegularOnly: e.target.checked })}
          />
          Count regular/non-foil only
        </label>
      </section>

      <button type="button" onClick={handleSave} disabled={saving}>
        {saving ? 'Saving...' : 'Save preferences'}
      </button>
      {saved && <span role="status"> Saved.</span>}
    </div>
  );
}
