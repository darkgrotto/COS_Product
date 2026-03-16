import { useEffect, useState } from 'react';

interface UserSummary {
  id: string;
  username: string;
  role: string;
  status: string;
  isBuiltinAdmin: boolean;
}

interface ExportFile {
  id: string;
  username: string;
  removedAt: string;
  fileSizeBytes: number;
  createdAt: string;
}

export function UserManagement() {
  const [users, setUsers] = useState<UserSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [exportFiles, setExportFiles] = useState<ExportFile[]>([]);
  const [exportLoading, setExportLoading] = useState(false);
  const [selectedUserId, setSelectedUserId] = useState<string | null>(null);

  useEffect(() => {
    fetch('/api/users', { credentials: 'include' })
      .then((r) => r.json() as Promise<UserSummary[]>)
      .then((data) => { setUsers(data); setLoading(false); })
      .catch(() => { setError('Failed to load users'); setLoading(false); });
  }, []);

  const loadExports = async (userId: string) => {
    setExportLoading(true);
    setSelectedUserId(userId);
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

  const handleRemove = async (userId: string) => {
    if (!window.confirm('Remove this user? Their data will be exported before deletion.')) return;
    try {
      await fetch(`/api/users/${userId}`, { method: 'DELETE', credentials: 'include' });
      setUsers((prev) => prev.filter((u) => u.id !== userId));
    } catch {
      setError('Failed to remove user');
    }
  };

  const handleDisable = async (userId: string) => {
    try {
      await fetch(`/api/users/${userId}/disable`, { method: 'POST', credentials: 'include' });
      setUsers((prev) => prev.map((u) => u.id === userId ? { ...u, status: 'Disabled' } : u));
    } catch {
      setError('Failed to disable user');
    }
  };

  const handleEnable = async (userId: string) => {
    try {
      await fetch(`/api/users/${userId}/enable`, { method: 'POST', credentials: 'include' });
      setUsers((prev) => prev.map((u) => u.id === userId ? { ...u, status: 'Active' } : u));
    } catch {
      setError('Failed to enable user');
    }
  };

  const handleDeleteExport = async (userId: string, exportId: string) => {
    try {
      await fetch(`/api/users/${userId}/exports/${exportId}`, { method: 'DELETE', credentials: 'include' });
      setExportFiles((prev) => prev.filter((e) => e.id !== exportId));
    } catch {
      setError('Failed to delete export file');
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
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          {users.map((u) => (
            <tr key={u.id}>
              <td>{u.username}</td>
              <td>{u.role}</td>
              <td>{u.status}</td>
              <td>
                {!u.isBuiltinAdmin && (
                  <>
                    {u.status === 'Active' ? (
                      <button type="button" onClick={() => handleDisable(u.id)}>
                        Disable
                      </button>
                    ) : u.status === 'Disabled' ? (
                      <button type="button" onClick={() => handleEnable(u.id)}>
                        Enable
                      </button>
                    ) : null}
                    {u.status !== 'Removed' && (
                      <button type="button" onClick={() => handleRemove(u.id)}>
                        Remove
                      </button>
                    )}
                    {u.status === 'Removed' && (
                      <button type="button" onClick={() => loadExports(u.id)}>
                        View exports
                      </button>
                    )}
                  </>
                )}
              </td>
            </tr>
          ))}
        </tbody>
      </table>

      {selectedUserId && (
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
                      <button
                        type="button"
                        onClick={() => handleDeleteExport(selectedUserId, f.id)}
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
    </div>
  );
}
