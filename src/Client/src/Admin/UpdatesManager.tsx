import { useEffect, useState } from 'react';
import { updatesApi, UpdateStatus, AdminNotification } from '../api/updates';

export function UpdatesManager() {
  const [status, setStatus] = useState<UpdateStatus | null>(null);
  const [notifications, setNotifications] = useState<AdminNotification[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [checking, setChecking] = useState(false);
  const [checkResult, setCheckResult] = useState<string | null>(null);
  const [approving, setApproving] = useState(false);
  const [approveError, setApproveError] = useState<string | null>(null);

  const reload = async () => {
    const [s, n] = await Promise.all([
      updatesApi.getStatus(),
      updatesApi.getNotifications(),
    ]);
    setStatus(s);
    setNotifications(n);
  };

  useEffect(() => {
    reload()
      .then(() => setLoading(false))
      .catch(() => {
        setLoadError('Failed to load update information');
        setLoading(false);
      });
  }, []);

  const triggerCheck = async () => {
    setChecking(true);
    setCheckResult(null);
    try {
      await updatesApi.triggerCheck();
      await reload();
      setCheckResult('Check complete.');
    } catch {
      setCheckResult('Check failed.');
    } finally {
      setChecking(false);
    }
  };

  const approveSchema = async (id: number) => {
    if (!window.confirm(
      'Apply this schema update? A backup will be taken automatically before proceeding.',
    )) return;
    setApproving(true);
    setApproveError(null);
    try {
      await updatesApi.approveSchemaUpdate(id);
      await reload();
    } catch {
      setApproveError('Schema update failed. Check notifications for details.');
    } finally {
      setApproving(false);
    }
  };

  const markRead = async (id: number) => {
    try {
      await updatesApi.markNotificationRead(id);
      setNotifications((prev) => prev.filter((n) => n.id !== id));
    } catch {
      // non-critical - leave notification in list
    }
  };

  if (loading) return <p>Loading...</p>;
  if (loadError) return <div role="alert">{loadError}</div>;

  return (
    <div>
      <h2>Updates</h2>

      <section aria-labelledby="update-status-heading">
        <h3 id="update-status-heading">Status</h3>
        <dl>
          <dt>Content version</dt>
          <dd>{status?.currentContentVersion ?? 'None'}</dd>
          <dt>Application version</dt>
          <dd>
            {status?.latestApplicationVersion
              ? status.applicationUpdatePending
                ? `Update available: ${status.latestApplicationVersion}`
                : `Up to date (${status.latestApplicationVersion})`
              : 'Unknown'}
          </dd>
        </dl>
        <button type="button" onClick={triggerCheck} disabled={checking}>
          {checking ? 'Checking...' : 'Check for updates now'}
        </button>
        {checkResult && <div role="status">{checkResult}</div>}
      </section>

      {status?.pendingSchemaUpdate && (
        <section aria-labelledby="schema-update-heading">
          <h3 id="schema-update-heading">Pending Schema Update</h3>
          <p>
            A schema update to version{' '}
            <strong>{status.pendingSchemaUpdate.schemaVersion}</strong> is awaiting
            your approval.
          </p>
          <p>{status.pendingSchemaUpdate.description}</p>
          <p>
            Detected: {new Date(status.pendingSchemaUpdate.detectedAt).toLocaleString()}
          </p>
          <p>
            A backup will be taken automatically before the update is applied. If the
            update fails, the backup will be restored automatically.
          </p>
          {approveError && <div role="alert">{approveError}</div>}
          <button
            type="button"
            onClick={() => approveSchema(status.pendingSchemaUpdate!.id)}
            disabled={approving}
          >
            {approving ? 'Applying update...' : 'Approve and apply schema update'}
          </button>
        </section>
      )}

      <section aria-labelledby="notifications-heading">
        <h3 id="notifications-heading">Unread Notifications</h3>
        {notifications.length === 0 ? (
          <p>No unread notifications.</p>
        ) : (
          <ul>
            {notifications.map((n) => (
              <li key={n.id}>
                <span>[{n.category}]</span>
                {' '}
                <span>{n.message}</span>
                {' '}
                <span>{new Date(n.createdAt).toLocaleString()}</span>
                {' '}
                <button type="button" onClick={() => markRead(n.id)}>
                  Dismiss
                </button>
              </li>
            ))}
          </ul>
        )}
      </section>
    </div>
  );
}
