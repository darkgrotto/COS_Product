import { useEffect, useState } from 'react';
import { backupApi, BackupStatus, BackupRecord, BackupHistoryPage } from '../api/backup';
import { settingsApi, BackupSettings } from '../api/settings';

export function BackupManager() {
  const [status, setStatus] = useState<BackupStatus | null>(null);
  const [settings, setSettings] = useState<BackupSettings | null>(null);
  const [history, setHistory] = useState<BackupHistoryPage | null>(null);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);

  const [scheduleInput, setScheduleInput] = useState('weekly');
  const [retentionScheduledInput, setRetentionScheduledInput] = useState('4');
  const [retentionPreUpdateInput, setRetentionPreUpdateInput] = useState('4');
  const [settingsSaving, setSettingsSaving] = useState(false);
  const [settingsError, setSettingsError] = useState<string | null>(null);
  const [settingsSuccess, setSettingsSuccess] = useState(false);

  const [triggering, setTriggering] = useState(false);
  const [triggerError, setTriggerError] = useState<string | null>(null);
  const [triggerSuccess, setTriggerSuccess] = useState(false);

  const [destType, setDestType] = useState('local');
  const [destLabel, setDestLabel] = useState('');
  const [destConfig, setDestConfig] = useState('{}');
  const [addingDest, setAddingDest] = useState(false);
  const [destError, setDestError] = useState<string | null>(null);
  const [testingDest, setTestingDest] = useState<string | null>(null);
  const [testResults, setTestResults] = useState<Record<string, boolean>>({});

  const [restoreFile, setRestoreFile] = useState<File | null>(null);
  const [restoring, setRestoring] = useState(false);
  const [restoreError, setRestoreError] = useState<string | null>(null);
  const [restoreSuccess, setRestoreSuccess] = useState<string | null>(null);

  const reloadStatus = async () => {
    const [s, hist] = await Promise.all([
      backupApi.getStatus(),
      backupApi.getHistory(1, 10),
    ]);
    setStatus(s);
    setHistory(hist);
  };

  useEffect(() => {
    Promise.all([
      backupApi.getStatus(),
      settingsApi.getBackup(),
      backupApi.getHistory(1, 10),
    ])
      .then(([s, cfg, hist]) => {
        setStatus(s);
        setSettings(cfg);
        setHistory(hist);
        setScheduleInput(cfg.schedule);
        setRetentionScheduledInput(String(cfg.retentionScheduled));
        setRetentionPreUpdateInput(String(cfg.retentionPreUpdate));
        setLoading(false);
      })
      .catch(() => {
        setLoadError('Failed to load backup information');
        setLoading(false);
      });
  }, []);

  const saveSettings = async () => {
    setSettingsSaving(true);
    setSettingsError(null);
    setSettingsSuccess(false);
    try {
      await settingsApi.updateBackup({
        schedule: scheduleInput,
        retentionScheduled: parseInt(retentionScheduledInput, 10) || 4,
        retentionPreUpdate: parseInt(retentionPreUpdateInput, 10) || 4,
      });
      setSettingsSuccess(true);
    } catch {
      setSettingsError('Failed to save backup settings');
    } finally {
      setSettingsSaving(false);
    }
  };

  const triggerBackup = async () => {
    setTriggering(true);
    setTriggerError(null);
    setTriggerSuccess(false);
    try {
      await backupApi.trigger();
      await reloadStatus();
      setTriggerSuccess(true);
    } catch {
      setTriggerError('Backup failed');
    } finally {
      setTriggering(false);
    }
  };

  const addDestination = async () => {
    setAddingDest(true);
    setDestError(null);
    try {
      await backupApi.addDestination({
        destinationType: destType,
        label: destLabel.trim(),
        configurationJson: destConfig,
      });
      await reloadStatus();
      setDestLabel('');
      setDestConfig('{}');
    } catch {
      setDestError('Failed to add destination');
    } finally {
      setAddingDest(false);
    }
  };

  const removeDestination = async (id: string) => {
    if (!window.confirm('Remove this backup destination?')) return;
    try {
      await backupApi.removeDestination(id);
      setStatus((prev) =>
        prev ? { ...prev, destinations: prev.destinations.filter((d) => d.id !== id) } : prev,
      );
    } catch {
      setDestError('Failed to remove destination');
    }
  };

  const testDestination = async (id: string) => {
    setTestingDest(id);
    try {
      const result = await backupApi.testDestination(id);
      setTestResults((prev) => ({ ...prev, [id]: result.success }));
    } catch {
      setTestResults((prev) => ({ ...prev, [id]: false }));
    } finally {
      setTestingDest(null);
    }
  };

  const restoreFromFile = async () => {
    if (!restoreFile) return;
    if (!window.confirm('Restore from this backup file? All current data will be replaced.')) return;
    setRestoring(true);
    setRestoreError(null);
    setRestoreSuccess(null);
    try {
      const result = await backupApi.restore(restoreFile);
      setRestoreSuccess(`Restore complete. Schema version: ${result.restoredSchemaVersion}`);
    } catch (err) {
      setRestoreError(err instanceof Error ? err.message : 'Restore failed');
    } finally {
      setRestoring(false);
    }
  };

  const restoreFromRecord = async (record: BackupRecord) => {
    if (!window.confirm(`Restore from backup "${record.label}"? All current data will be replaced.`)) return;
    setRestoring(true);
    setRestoreError(null);
    setRestoreSuccess(null);
    try {
      const result = await backupApi.restoreFromRecord(record.id);
      setRestoreSuccess(`Restore complete. Schema version: ${result.restoredSchemaVersion}`);
    } catch (err) {
      setRestoreError(err instanceof Error ? err.message : 'Restore failed');
    } finally {
      setRestoring(false);
    }
  };

  if (loading) return <p>Loading...</p>;
  if (loadError) return <div role="alert">{loadError}</div>;

  return (
    <div>
      <h2>Backup and Restore</h2>

      <section aria-labelledby="backup-status-heading">
        <h3 id="backup-status-heading">Status</h3>
        <dl>
          <dt>Last scheduled backup</dt>
          <dd>
            {status?.lastScheduledBackup
              ? `${status.lastScheduledBackup.label} (${new Date(status.lastScheduledBackup.createdAt).toLocaleString()})`
              : 'None'}
          </dd>
          <dt>Last pre-update backup</dt>
          <dd>
            {status?.lastPreUpdateBackup
              ? `${status.lastPreUpdateBackup.label} (${new Date(status.lastPreUpdateBackup.createdAt).toLocaleString()})`
              : 'None'}
          </dd>
          <dt>Next scheduled backup</dt>
          <dd>
            {status?.nextScheduledBackup
              ? new Date(status.nextScheduledBackup).toLocaleString()
              : 'Unknown'}
          </dd>
        </dl>
        <button type="button" onClick={triggerBackup} disabled={triggering}>
          {triggering ? 'Running backup...' : 'Run backup now'}
        </button>
        {triggerError && <div role="alert">{triggerError}</div>}
        {triggerSuccess && <div role="status">Backup complete.</div>}
      </section>

      <section aria-labelledby="backup-settings-heading">
        <h3 id="backup-settings-heading">Schedule and Retention</h3>
        <label htmlFor="backup-schedule">Schedule</label>
        <select
          id="backup-schedule"
          value={scheduleInput}
          onChange={(e) => setScheduleInput(e.target.value)}
          disabled={settingsSaving}
        >
          <option value="daily">Daily</option>
          <option value="weekly">Weekly</option>
        </select>
        <label htmlFor="retention-scheduled">Retain scheduled backups (count)</label>
        <input
          id="retention-scheduled"
          type="number"
          min="1"
          max="99"
          value={retentionScheduledInput}
          onChange={(e) => setRetentionScheduledInput(e.target.value)}
          disabled={settingsSaving}
        />
        <label htmlFor="retention-pre-update">Retain pre-update backups (count)</label>
        <input
          id="retention-pre-update"
          type="number"
          min="1"
          max="99"
          value={retentionPreUpdateInput}
          onChange={(e) => setRetentionPreUpdateInput(e.target.value)}
          disabled={settingsSaving}
        />
        <button type="button" onClick={saveSettings} disabled={settingsSaving}>
          {settingsSaving ? 'Saving...' : 'Save settings'}
        </button>
        {settingsError && <div role="alert">{settingsError}</div>}
        {settingsSuccess && <div role="status">Settings saved.</div>}
      </section>

      <section aria-labelledby="destinations-heading">
        <h3 id="destinations-heading">Backup Destinations</h3>
        {destError && <div role="alert">{destError}</div>}
        {!status?.destinations.length ? (
          <p>No backup destinations configured.</p>
        ) : (
          <table aria-label="Backup destinations">
            <thead>
              <tr>
                <th>Label</th>
                <th>Type</th>
                <th>Active</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {status.destinations.map((d) => (
                <tr key={d.id}>
                  <td>{d.label}</td>
                  <td>{d.type}</td>
                  <td>{d.isActive ? 'Yes' : 'No'}</td>
                  <td>
                    <button
                      type="button"
                      onClick={() => testDestination(d.id)}
                      disabled={testingDest === d.id}
                    >
                      {testingDest === d.id ? 'Testing...' : 'Test'}
                    </button>
                    {d.id in testResults && (
                      <span role="status">
                        {testResults[d.id] ? ' OK' : ' Failed'}
                      </span>
                    )}
                    {' '}
                    <button type="button" onClick={() => removeDestination(d.id)}>
                      Remove
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}

        <h4>Add destination</h4>
        <label htmlFor="dest-type">Type</label>
        <select
          id="dest-type"
          value={destType}
          onChange={(e) => setDestType(e.target.value)}
          disabled={addingDest}
        >
          <option value="local">Local file</option>
          <option value="azure-blob">Azure Blob Storage</option>
          <option value="aws-s3">AWS S3</option>
          <option value="gcp-storage">GCP Storage</option>
        </select>
        <label htmlFor="dest-label">Label</label>
        <input
          id="dest-label"
          type="text"
          value={destLabel}
          onChange={(e) => setDestLabel(e.target.value)}
          disabled={addingDest}
        />
        <label htmlFor="dest-config">Configuration (JSON)</label>
        <textarea
          id="dest-config"
          value={destConfig}
          onChange={(e) => setDestConfig(e.target.value)}
          rows={3}
          disabled={addingDest}
        />
        <button
          type="button"
          onClick={addDestination}
          disabled={addingDest || !destLabel.trim()}
        >
          {addingDest ? 'Adding...' : 'Add destination'}
        </button>
      </section>

      <section aria-labelledby="history-heading">
        <h3 id="history-heading">Backup History</h3>
        {!history?.records.length ? (
          <p>No backups yet.</p>
        ) : (
          <table aria-label="Backup history">
            <thead>
              <tr>
                <th>Label</th>
                <th>Type</th>
                <th>Schema version</th>
                <th>Created</th>
                <th>Size</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {history.records.map((r) => (
                <tr key={r.id}>
                  <td>{r.label}</td>
                  <td>{r.backupType}</td>
                  <td>{r.schemaVersion}</td>
                  <td>{new Date(r.createdAt).toLocaleString()}</td>
                  <td>{r.fileSizeBytes.toLocaleString()} bytes</td>
                  <td>
                    {r.isAvailable && (
                      <>
                        <a href={backupApi.downloadUrl(r.id)} download>
                          Download
                        </a>
                        {' '}
                        <button
                          type="button"
                          onClick={() => restoreFromRecord(r)}
                          disabled={restoring}
                        >
                          Restore
                        </button>
                      </>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
        {history && history.total > history.pageSize && (
          <p>{history.total} total backups. Showing most recent {history.pageSize}.</p>
        )}
      </section>

      <section aria-labelledby="restore-heading">
        <h3 id="restore-heading">Restore from File</h3>
        <p>Upload a backup file to restore. All current data will be replaced.</p>
        {restoreError && <div role="alert">{restoreError}</div>}
        {restoreSuccess && <div role="status">{restoreSuccess}</div>}
        <label htmlFor="restore-file">Backup file (.zip)</label>
        <input
          id="restore-file"
          type="file"
          accept=".zip"
          onChange={(e) => setRestoreFile(e.target.files?.[0] ?? null)}
          disabled={restoring}
        />
        <button
          type="button"
          onClick={restoreFromFile}
          disabled={restoring || !restoreFile}
        >
          {restoring ? 'Restoring...' : 'Restore from file'}
        </button>
      </section>
    </div>
  );
}
