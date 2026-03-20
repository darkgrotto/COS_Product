import { useEffect, useState } from 'react';
import {
  settingsApi,
  TcgPlayerSettings,
  SelfEnrollmentSettings,
  OAuthProviderConfig,
} from '../api/settings';
import { DemoLock } from '../components/DemoLock';

const OAUTH_PROVIDER_LABELS: Record<string, string> = {
  google: 'Google',
  microsoft: 'Microsoft',
  github: 'GitHub',
};

export function AdminSettings() {
  const [instanceNameInput, setInstanceNameInput] = useState('');
  const [instanceSaving, setInstanceSaving] = useState(false);
  const [instanceError, setInstanceError] = useState<string | null>(null);
  const [instanceSuccess, setInstanceSuccess] = useState(false);

  const [tcgPlayer, setTcgPlayer] = useState<TcgPlayerSettings | null>(null);
  const [tcgKeyInput, setTcgKeyInput] = useState('');
  const [tcgSaving, setTcgSaving] = useState(false);
  const [tcgError, setTcgError] = useState<string | null>(null);
  const [tcgSuccess, setTcgSuccess] = useState(false);

  const [selfEnrollment, setSelfEnrollment] = useState<SelfEnrollmentSettings | null>(null);
  const [enrollmentSaving, setEnrollmentSaving] = useState(false);
  const [enrollmentError, setEnrollmentError] = useState<string | null>(null);

  const [oauthProviders, setOauthProviders] = useState<OAuthProviderConfig[]>([]);
  const [oauthInputs, setOauthInputs] = useState<Record<string, { clientId: string; clientSecret: string }>>({});
  const [oauthSaving, setOauthSaving] = useState<Record<string, boolean>>({});
  const [oauthError, setOauthError] = useState<Record<string, string | null>>({});
  const [oauthSuccess, setOauthSuccess] = useState<Record<string, boolean>>({});

  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);

  useEffect(() => {
    Promise.all([
      settingsApi.getInstance(),
      settingsApi.getTcgPlayer(),
      settingsApi.getSelfEnrollment(),
      settingsApi.getOAuth(),
    ])
      .then(([inst, tcg, enroll, oauth]) => {
        setInstanceNameInput(inst.instanceName);
        setTcgPlayer(tcg);
        setSelfEnrollment(enroll);
        setOauthProviders(oauth);
        const inputs: Record<string, { clientId: string; clientSecret: string }> = {};
        oauth.forEach((p) => {
          inputs[p.provider] = { clientId: p.clientId ?? '', clientSecret: '' };
        });
        setOauthInputs(inputs);
        setLoading(false);
      })
      .catch(() => {
        setLoadError('Failed to load settings');
        setLoading(false);
      });
  }, []);

  const saveInstanceName = async () => {
    setInstanceSaving(true);
    setInstanceError(null);
    setInstanceSuccess(false);
    try {
      await settingsApi.updateInstance(instanceNameInput);
      setInstanceSuccess(true);
    } catch {
      setInstanceError('Failed to save instance name');
    } finally {
      setInstanceSaving(false);
    }
  };

  const saveTcgKey = async () => {
    setTcgSaving(true);
    setTcgError(null);
    setTcgSuccess(false);
    try {
      await settingsApi.setTcgPlayerKey(tcgKeyInput);
      const updated = await settingsApi.getTcgPlayer();
      setTcgPlayer(updated);
      setTcgKeyInput('');
      setTcgSuccess(true);
    } catch {
      setTcgError('Failed to save TCGPlayer API key');
    } finally {
      setTcgSaving(false);
    }
  };

  const clearTcgKey = async () => {
    if (!window.confirm('Clear the TCGPlayer API key? Direct price refresh will be unavailable until a new key is set.')) return;
    setTcgSaving(true);
    setTcgError(null);
    try {
      await settingsApi.clearTcgPlayerKey();
      setTcgPlayer({ configured: false, maskedKey: null });
      setTcgSuccess(false);
    } catch {
      setTcgError('Failed to clear TCGPlayer API key');
    } finally {
      setTcgSaving(false);
    }
  };

  const toggleSelfEnrollment = async () => {
    if (!selfEnrollment) return;
    setEnrollmentSaving(true);
    setEnrollmentError(null);
    try {
      const next = !selfEnrollment.enabled;
      await settingsApi.updateSelfEnrollment(next);
      setSelfEnrollment({ enabled: next });
    } catch {
      setEnrollmentError('Failed to update self-enrollment setting');
    } finally {
      setEnrollmentSaving(false);
    }
  };

  const saveOAuthProvider = async (provider: string) => {
    const input = oauthInputs[provider];
    if (!input) return;
    setOauthSaving((prev) => ({ ...prev, [provider]: true }));
    setOauthError((prev) => ({ ...prev, [provider]: null }));
    setOauthSuccess((prev) => ({ ...prev, [provider]: false }));
    try {
      await settingsApi.updateOAuthProvider(provider, input.clientId, input.clientSecret);
      const updated = await settingsApi.getOAuth();
      setOauthProviders(updated);
      const updatedProvider = updated.find((p) => p.provider === provider);
      setOauthInputs((prev) => ({
        ...prev,
        [provider]: { clientId: updatedProvider?.clientId ?? '', clientSecret: '' },
      }));
      setOauthSuccess((prev) => ({ ...prev, [provider]: true }));
    } catch {
      setOauthError((prev) => ({
        ...prev,
        [provider]: `Failed to save ${OAUTH_PROVIDER_LABELS[provider] ?? provider} configuration`,
      }));
    } finally {
      setOauthSaving((prev) => ({ ...prev, [provider]: false }));
    }
  };

  const clearOAuthProvider = async (provider: string) => {
    const label = OAUTH_PROVIDER_LABELS[provider] ?? provider;
    if (!window.confirm(`Clear the ${label} OAuth configuration? Users will no longer be able to sign in with ${label}.`)) return;
    setOauthSaving((prev) => ({ ...prev, [provider]: true }));
    setOauthError((prev) => ({ ...prev, [provider]: null }));
    setOauthSuccess((prev) => ({ ...prev, [provider]: false }));
    try {
      await settingsApi.clearOAuthProvider(provider);
      setOauthProviders((prev) =>
        prev.map((p) =>
          p.provider === provider ? { ...p, clientId: null, secretConfigured: false } : p,
        ),
      );
      setOauthInputs((prev) => ({ ...prev, [provider]: { clientId: '', clientSecret: '' } }));
    } catch {
      setOauthError((prev) => ({
        ...prev,
        [provider]: `Failed to clear ${label} configuration`,
      }));
    } finally {
      setOauthSaving((prev) => ({ ...prev, [provider]: false }));
    }
  };

  if (loading) return <p>Loading...</p>;
  if (loadError) return <div role="alert">{loadError}</div>;

  return (
    <div>
      <h2>Admin Settings</h2>

      <section aria-labelledby="instance-heading">
        <h3 id="instance-heading">Instance Branding</h3>
        <p>The instance name appears in the page title, header, and browser tab.</p>
        <label htmlFor="instance-name">Instance name</label>
        <input
          id="instance-name"
          type="text"
          value={instanceNameInput}
          onChange={(e) => setInstanceNameInput(e.target.value)}
          disabled={instanceSaving}
        />
        <DemoLock>
          <button
            type="button"
            onClick={saveInstanceName}
            disabled={instanceSaving || !instanceNameInput.trim()}
          >
            {instanceSaving ? 'Saving...' : 'Save'}
          </button>
        </DemoLock>
        {instanceError && <div role="alert">{instanceError}</div>}
        {instanceSuccess && <div role="status">Instance name saved.</div>}
      </section>

      <section aria-labelledby="tcgplayer-heading">
        <h3 id="tcgplayer-heading">TCGPlayer API Key</h3>
        <p>
          Required for direct per-card market value refresh. Requests go directly from your
          browser to TCGPlayer - CountOrSell does not proxy this connection.
        </p>
        {tcgPlayer?.configured ? (
          <p>
            Current key: <code>{tcgPlayer.maskedKey}</code>
            {' '}
            <button type="button" onClick={clearTcgKey} disabled={tcgSaving}>
              Clear key
            </button>
          </p>
        ) : (
          <p>No TCGPlayer API key configured.</p>
        )}
        <label htmlFor="tcg-key">
          {tcgPlayer?.configured ? 'Replace key' : 'Set key'}
        </label>
        <input
          id="tcg-key"
          type="password"
          value={tcgKeyInput}
          onChange={(e) => setTcgKeyInput(e.target.value)}
          placeholder="Paste API key here"
          disabled={tcgSaving}
          autoComplete="off"
        />
        <button type="button" onClick={saveTcgKey} disabled={tcgSaving || !tcgKeyInput.trim()}>
          {tcgSaving ? 'Saving...' : 'Save key'}
        </button>
        {tcgError && <div role="alert">{tcgError}</div>}
        {tcgSuccess && <div role="status">TCGPlayer API key saved.</div>}
      </section>

      <section aria-labelledby="enrollment-heading">
        <h3 id="enrollment-heading">Self-Enrollment</h3>
        <p>
          When enabled, new users can create accounts without admin involvement.
          Off by default.
        </p>
        <p>
          Status: <strong>{selfEnrollment?.enabled ? 'Enabled' : 'Disabled'}</strong>
        </p>
        <button
          type="button"
          onClick={toggleSelfEnrollment}
          disabled={enrollmentSaving}
        >
          {enrollmentSaving
            ? 'Saving...'
            : selfEnrollment?.enabled
              ? 'Disable self-enrollment'
              : 'Enable self-enrollment'}
        </button>
        {enrollmentError && <div role="alert">{enrollmentError}</div>}
      </section>

      <section aria-labelledby="oauth-heading">
        <h3 id="oauth-heading">OAuth Configuration</h3>
        <p>
          Configure OAuth providers for user sign-in. Changes are stored and take effect
          after the next application restart.
        </p>
        {oauthProviders.map((provider) => {
          const label = OAUTH_PROVIDER_LABELS[provider.provider] ?? provider.provider;
          const isConfigured = !!(provider.clientId && provider.secretConfigured);
          const input = oauthInputs[provider.provider] ?? { clientId: '', clientSecret: '' };
          const saving = oauthSaving[provider.provider] ?? false;
          const canSave = !saving && !!(input.clientId.trim() || input.clientSecret.trim());
          return (
            <div key={provider.provider}>
              <h4>{label}</h4>
              <p>
                Status: <strong>{isConfigured ? 'Configured' : 'Not configured'}</strong>
              </p>
              {provider.clientId && (
                <p>Client ID: <code>{provider.clientId}</code></p>
              )}
              {provider.secretConfigured && (
                <p>Client secret: configured</p>
              )}
              <label htmlFor={`oauth-${provider.provider}-client-id`}>Client ID</label>
              <input
                id={`oauth-${provider.provider}-client-id`}
                type="text"
                value={input.clientId}
                onChange={(e) =>
                  setOauthInputs((prev) => ({
                    ...prev,
                    [provider.provider]: { ...prev[provider.provider], clientId: e.target.value },
                  }))
                }
                disabled={saving}
              />
              <label htmlFor={`oauth-${provider.provider}-secret`}>Client secret</label>
              <input
                id={`oauth-${provider.provider}-secret`}
                type="password"
                value={input.clientSecret}
                onChange={(e) =>
                  setOauthInputs((prev) => ({
                    ...prev,
                    [provider.provider]: { ...prev[provider.provider], clientSecret: e.target.value },
                  }))
                }
                placeholder={provider.secretConfigured ? 'Leave blank to keep existing secret' : ''}
                disabled={saving}
                autoComplete="off"
              />
              <DemoLock>
                <button
                  type="button"
                  onClick={() => saveOAuthProvider(provider.provider)}
                  disabled={!canSave}
                >
                  {saving ? 'Saving...' : 'Save'}
                </button>
              </DemoLock>
              {isConfigured && (
                <DemoLock>
                  <button
                    type="button"
                    onClick={() => clearOAuthProvider(provider.provider)}
                    disabled={saving}
                  >
                    Clear configuration
                  </button>
                </DemoLock>
              )}
              {oauthError[provider.provider] && (
                <div role="alert">{oauthError[provider.provider]}</div>
              )}
              {oauthSuccess[provider.provider] && (
                <div role="status">
                  {label} configuration saved. Restart the application to apply changes.
                </div>
              )}
            </div>
          );
        })}
      </section>
    </div>
  );
}
