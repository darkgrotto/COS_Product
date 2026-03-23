import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { DemoLock } from '../components/DemoLock';

interface UserSummary {
  id: string;
  username: string;
  displayName: string | null;
  role: string;
  state: string;
  authType: string;
  isBuiltinAdmin: boolean;
}

interface ExportFile {
  id: string;
  username: string;
  removedAt: string;
  fileSizeBytes: number;
  createdAt: string;
}

interface PendingInvitation {
  id: string;
  email: string;
  role: string;
  createdAt: string;
  expiresAt: string;
}

export function UserManagement() {
  const [users, setUsers] = useState<UserSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [exportFiles, setExportFiles] = useState<ExportFile[]>([]);
  const [exportLoading, setExportLoading] = useState(false);
  const [selectedExportUserId, setSelectedExportUserId] = useState<string | null>(null);

  const [pendingInvitations, setPendingInvitations] = useState<PendingInvitation[]>([]);
  const [inviteEmail, setInviteEmail] = useState('');
  const [inviteRole, setInviteRole] = useState('GeneralUser');
  const [inviteLoading, setInviteLoading] = useState(false);
  const [inviteLink, setInviteLink] = useState<string | null>(null);
  const [inviteError, setInviteError] = useState<string | null>(null);

  useEffect(() => {
    fetch('/api/users', { credentials: 'include' })
      .then((r) => r.json() as Promise<UserSummary[]>)
      .then((data) => { setUsers(data); setLoading(false); })
      .catch(() => { setError('Failed to load users'); setLoading(false); });

    fetch('/api/invitations', { credentials: 'include' })
      .then((r) => r.json() as Promise<PendingInvitation[]>)
      .then(setPendingInvitations)
      .catch(() => {});
  }, []);

  const loadExports = async (userId: string) => {
    setExportLoading(true);
    setSelectedExportUserId(userId);
    try {
      const r = await fetch(`/api/users/${userId}/exports`, { credentials: 'include' });
      const data = await r.json() as ExportFile[];
      setExportFiles(data);
    } catch {
      setError('Failed to load export files');
    } finally {
      setExportLoading(false);
    }
  };

  const userAction = async (
    userId: string,
    path: string,
    method: string,
    nextState?: Partial<UserSummary>,
  ) => {
    try {
      const r = await fetch(`/api/users/${userId}/${path}`, { method, credentials: 'include' });
      if (!r.ok) {
        const body = await r.json().catch(() => ({})) as { error?: string };
        setError(body.error ?? `Action failed (${r.status})`);
        return;
      }
      if (nextState) {
        setUsers((prev) => prev.map((u) => u.id === userId ? { ...u, ...nextState } : u));
      }
    } catch {
      setError('Request failed');
    }
  };

  const handleRemove = async (userId: string) => {
    if (!window.confirm('Remove this user? Their data will be exported before deletion.')) return;
    await userAction(userId, 'remove', 'POST', { state: 'Removed' });
  };

  const handleDisable = (userId: string) =>
    userAction(userId, 'disable', 'POST', { state: 'Disabled' });

  const handleEnable = (userId: string) =>
    userAction(userId, 'reenable', 'POST', { state: 'Active' });

  const handlePromote = (userId: string) =>
    userAction(userId, 'promote', 'POST', { role: 'Admin' });

  const handleDemote = (userId: string) =>
    userAction(userId, 'demote', 'POST', { role: 'GeneralUser' });

  const handleDeleteExport = async (userId: string, exportId: string) => {
    try {
      await fetch(`/api/users/${userId}/exports/${exportId}`, {
        method: 'DELETE',
        credentials: 'include',
      });
      setExportFiles((prev) => prev.filter((e) => e.id !== exportId));
    } catch {
      setError('Failed to delete export file');
    }
  };

  const handleSendInvite = async (e: React.FormEvent) => {
    e.preventDefault();
    setInviteError(null);
    setInviteLink(null);
    if (!inviteEmail.trim()) {
      setInviteError('Email is required.');
      return;
    }
    setInviteLoading(true);
    try {
      const r = await fetch('/api/invitations', {
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email: inviteEmail.trim(), role: inviteRole }),
      });
      if (!r.ok) {
        const body = await r.json().catch(() => ({})) as { error?: string };
        setInviteError(body.error ?? `Failed (${r.status})`);
        return;
      }
      const data = await r.json() as { id: string; email: string; role: string; expiresAt: string; inviteUrl: string };
      setInviteLink(data.inviteUrl);
      setInviteEmail('');
      setPendingInvitations((prev) => [
        { id: data.id, email: data.email, role: data.role, createdAt: new Date().toISOString(), expiresAt: data.expiresAt },
        ...prev,
      ]);
    } catch {
      setInviteError('Request failed');
    } finally {
      setInviteLoading(false);
    }
  };

  const handleRevokeInvitation = async (id: string) => {
    try {
      await fetch(`/api/invitations/${id}`, { method: 'DELETE', credentials: 'include' });
      setPendingInvitations((prev) => prev.filter((i) => i.id !== id));
    } catch {
      setError('Failed to revoke invitation');
    }
  };

  if (loading) return <p>Loading...</p>;
  if (error) return <div role="alert">{error}</div>;

  return (
    <div>
      <h2>User Management</h2>
      <table aria-label="Users">
        <thead>
          <tr>
            <th>Username</th>
            <th>Role</th>
            <th>Status</th>
            <th>Auth</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          {users.map((u) => (
            <tr key={u.id}>
              <td>{u.username}</td>
              <td>{u.role === 'GeneralUser' ? 'General user' : u.role}</td>
              <td>{u.state}</td>
              <td>{u.authType}</td>
              <td>
                {!u.isBuiltinAdmin && (
                  <>
                    {u.state === 'Active' && (
                      <>
                        <button type="button" onClick={() => handleDisable(u.id)}>
                          Disable
                        </button>
                        {' '}
                      </>
                    )}
                    {u.state === 'Disabled' && (
                      <>
                        <button type="button" onClick={() => handleEnable(u.id)}>
                          Enable
                        </button>
                        {' '}
                      </>
                    )}
                    {u.state !== 'Removed' && (
                      <>
                        {u.role === 'GeneralUser' && (
                          <>
                            <button type="button" onClick={() => handlePromote(u.id)}>
                              Promote to admin
                            </button>
                            {' '}
                          </>
                        )}
                        {u.role === 'Admin' && (
                          <>
                            <button type="button" onClick={() => handleDemote(u.id)}>
                              Demote
                            </button>
                            {' '}
                          </>
                        )}
                        <DemoLock>
                          <button type="button" onClick={() => handleRemove(u.id)}>
                            Remove
                          </button>
                        </DemoLock>
                        {' '}
                      </>
                    )}
                    {u.state === 'Removed' && (
                      <button type="button" onClick={() => loadExports(u.id)}>
                        View exports
                      </button>
                    )}
                    {u.role === 'GeneralUser' && u.state !== 'Removed' && (
                      <>
                        {' '}
                        <Link to={`/admin/users/${u.id}/collection`}>
                          View collection
                        </Link>
                      </>
                    )}
                  </>
                )}
              </td>
            </tr>
          ))}
        </tbody>
      </table>

      {selectedExportUserId && (
        <div>
          <h3>Export files</h3>
          {exportLoading ? (
            <p>Loading...</p>
          ) : exportFiles.length === 0 ? (
            <p>No export files.</p>
          ) : (
            <table aria-label="Export files">
              <thead>
                <tr>
                  <th>Username</th>
                  <th>Removed at</th>
                  <th>Size</th>
                  <th>Created at</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {exportFiles.map((f) => (
                  <tr key={f.id}>
                    <td>{f.username}</td>
                    <td>{f.removedAt}</td>
                    <td>{f.fileSizeBytes.toLocaleString()} bytes</td>
                    <td>{f.createdAt}</td>
                    <td>
                      <a
                        href={`/api/users/${selectedExportUserId}/exports/${f.id}/download`}
                        download
                      >
                        Download
                      </a>
                      {' '}
                      <button
                        type="button"
                        onClick={() => handleDeleteExport(selectedExportUserId, f.id)}
                      >
                        Delete
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      )}

      <hr />

      <h3>Invite user</h3>
      <DemoLock>
        <form onSubmit={(e) => { void handleSendInvite(e); }}>
          <label htmlFor="invite-email">Email</label>
          {' '}
          <input
            id="invite-email"
            type="email"
            value={inviteEmail}
            onChange={(e) => setInviteEmail(e.target.value)}
            required
          />
          {' '}
          <label htmlFor="invite-role">Role</label>
          {' '}
          <select
            id="invite-role"
            value={inviteRole}
            onChange={(e) => setInviteRole(e.target.value)}
          >
            <option value="GeneralUser">General user</option>
            <option value="Admin">Admin</option>
          </select>
          {' '}
          <button type="submit" disabled={inviteLoading}>
            {inviteLoading ? 'Sending...' : 'Send invite'}
          </button>
        </form>
      </DemoLock>

      {inviteError && <p role="alert">{inviteError}</p>}

      {inviteLink && (
        <p>
          Invite sent. If email delivery is not configured, share this link manually:{' '}
          <code>{inviteLink}</code>
        </p>
      )}

      {pendingInvitations.length > 0 && (
        <div>
          <h3>Pending invitations</h3>
          <table aria-label="Pending invitations">
            <thead>
              <tr>
                <th>Email</th>
                <th>Role</th>
                <th>Expires</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {pendingInvitations.map((i) => (
                <tr key={i.id}>
                  <td>{i.email}</td>
                  <td>{i.role === 'GeneralUser' ? 'General user' : i.role}</td>
                  <td>{new Date(i.expiresAt).toLocaleString()}</td>
                  <td>
                    <DemoLock>
                      <button type="button" onClick={() => { void handleRevokeInvitation(i.id); }}>
                        Revoke
                      </button>
                    </DemoLock>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
