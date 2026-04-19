import { useEffect, useState } from 'react';
import { logForwardingApi, LOG_LEVELS, LogLevel, SaveLogForwardingConfig } from '../api/logForwarding';

const AUTH_PLACEHOLDER = '\u2022\u2022\u2022\u2022\u2022\u2022\u2022\u2022'; // bullet placeholder when already set

export function LogForwarding() {
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);

  const [enabled, setEnabled] = useState(false);
  const [destinationUrl, setDestinationUrl] = useState('');
  const [authHeader, setAuthHeader] = useState('');
  const [authHeaderSet, setAuthHeaderSet] = useState(false);
  const [authHeaderChanged, setAuthHeaderChanged] = useState(false);
  const [minLevel, setMinLevel] = useState<LogLevel>('Warning');

  const [saving, setSaving] = useState(false);
  const [saveResult, setSaveResult] = useState<string | null>(null);
  const [saveError, setSaveError] = useState<string | null>(null);
  const [testing, setTesting] = useState(false);
  const [testResult, setTestResult] = useState<string | null>(null);

  useEffect(() => {
    logForwardingApi.getConfig()
      .then(cfg => {
        setEnabled(cfg.enabled);
        setDestinationUrl(cfg.destinationUrl ?? '');
        setAuthHeaderSet(cfg.authHeaderSet);
        setMinLevel(cfg.minLevel);
        setLoading(false);
      })
      .catch(() => {
        setLoadError('Failed to load log forwarding configuration.');
        setLoading(false);
      });
  }, []);

  const handleSave = async () => {
    setSaving(true);
    setSaveResult(null);
    setSaveError(null);
    try {
      const payload: SaveLogForwardingConfig = {
        enabled,
        destinationUrl: destinationUrl.trim() || null,
        // null = keep existing; '' = clear; actual value = update
        authHeader: authHeaderChanged ? authHeader : null,
        minLevel,
      };
      await logForwardingApi.saveConfig(payload);
      setAuthHeaderSet(!!authHeader || (!authHeaderChanged && authHeaderSet));
      setAuthHeaderChanged(false);
      setAuthHeader('');
      setSaveResult('Configuration saved.');
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : String(err);
      setSaveError(msg.includes('400') ? 'Invalid configuration. Check the URL and try again.' : 'Failed to save configuration.');
    } finally {
      setSaving(false);
    }
  };

  const handleTest = async () => {
    setTesting(true);
    setTestResult(null);
    try {
      const result = await logForwardingApi.sendTest();
      setTestResult(result.message);
    } catch {
      setTestResult('Failed to queue test entry.');
    } finally {
      setTesting(false);
    }
  };

  const handleAuthHeaderChange = (value: string) => {
    setAuthHeader(value);
    setAuthHeaderChanged(true);
    setSaveResult(null);
  };

  if (loading) return <p>Loading...</p>;
  if (loadError) return <div role="alert">{loadError}</div>;

  const canTest = enabled && !!destinationUrl.trim();

  return (
    <div>
      <h3>Log Forwarding</h3>
      <p>
        Forward application log entries to an HTTP endpoint via POST. Each batch is sent as a
        JSON array. The endpoint must accept <code>POST</code> with{' '}
        <code>Content-Type: application/json</code>.
      </p>

      <div style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem', maxWidth: '480px' }}>
        <label style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
          <input
            type="checkbox"
            checked={enabled}
            onChange={e => { setEnabled(e.target.checked); setSaveResult(null); }}
          />
          Enable log forwarding
        </label>

        <label>
          Destination URL
          <input
            type="url"
            value={destinationUrl}
            onChange={e => { setDestinationUrl(e.target.value); setSaveResult(null); }}
            placeholder="https://logs.example.com/ingest"
            disabled={!enabled}
            style={{ display: 'block', width: '100%', marginTop: '0.25rem' }}
          />
        </label>

        <label>
          Authorization header{' '}
          <span style={{ opacity: 0.7 }}>(optional)</span>
          <input
            type="password"
            value={authHeaderChanged ? authHeader : (authHeaderSet ? AUTH_PLACEHOLDER : '')}
            onChange={e => handleAuthHeaderChange(e.target.value)}
            onFocus={() => { if (!authHeaderChanged) { setAuthHeader(''); setAuthHeaderChanged(true); } }}
            placeholder={authHeaderSet ? 'Leave blank to keep existing' : 'Bearer your-token-here'}
            disabled={!enabled}
            style={{ display: 'block', width: '100%', marginTop: '0.25rem' }}
          />
          {authHeaderSet && !authHeaderChanged && (
            <small style={{ opacity: 0.7 }}>An authorization header is currently set.</small>
          )}
          {authHeaderChanged && !authHeader && authHeaderSet && (
            <small style={{ opacity: 0.7 }}>Saving with blank will clear the existing header.</small>
          )}
        </label>

        <label>
          Minimum log level
          <select
            value={minLevel}
            onChange={e => { setMinLevel(e.target.value as LogLevel); setSaveResult(null); }}
            disabled={!enabled}
            style={{ display: 'block', marginTop: '0.25rem' }}
          >
            {LOG_LEVELS.map(l => (
              <option key={l} value={l}>{l}</option>
            ))}
          </select>
        </label>

        <div style={{ display: 'flex', gap: '0.5rem', alignItems: 'center', flexWrap: 'wrap' }}>
          <button type="button" onClick={handleSave} disabled={saving}>
            {saving ? 'Saving...' : 'Save'}
          </button>
          <button type="button" onClick={handleTest} disabled={testing || !canTest}>
            {testing ? 'Sending...' : 'Send test entry'}
          </button>
        </div>

        {saveResult && <div role="status" style={{ color: 'green' }}>{saveResult}</div>}
        {saveError && <div role="alert" style={{ color: 'red' }}>{saveError}</div>}
        {testResult && <div role="status">{testResult}</div>}
      </div>

      <details style={{ marginTop: '1.5rem' }}>
        <summary>Payload format</summary>
        <pre style={{ fontSize: '0.85em', overflowX: 'auto' }}>{`POST {destination_url}
Authorization: {header_value}
Content-Type: application/json

[
  {
    "timestamp": "2026-04-19T10:00:00.000Z",
    "level": "Warning",
    "category": "CountOrSell.Api.Services.ContentUpdateApplicator",
    "message": "Checksum mismatch for image ...",
    "exception": null
  }
]`}</pre>
        <p>
          Entries are batched (up to 100 per request) and flushed every 10 seconds.
          Forwarding is best-effort - delivery failures are not retried.
        </p>
      </details>
    </div>
  );
}
