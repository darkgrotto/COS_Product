import { useEffect, useState } from 'react';
import { api } from '../api/client';

interface UserOption {
  id: string;
  username: string;
  displayName: string | null;
  role: string;
  state: string;
}

interface AdminCollectionEntry {
  id: string;
  cardIdentifier: string;
  cardName: string | null;
  setCode: string | null;
  treatmentKey: string;
  quantity: number;
  condition: string;
  autographed: boolean;
  acquisitionDate: string;
  acquisitionPrice: number;
  currentMarketValue: number | null;
  notes: string | null;
}

export function AdminContentUsers() {
  const [users, setUsers] = useState<UserOption[]>([]);
  const [usersLoading, setUsersLoading] = useState(true);
  const [selectedUserId, setSelectedUserId] = useState('');
  const [entries, setEntries] = useState<AdminCollectionEntry[]>([]);
  const [collectionLoading, setCollectionLoading] = useState(false);
  const [collectionError, setCollectionError] = useState<string | null>(null);

  useEffect(() => {
    api.get<UserOption[]>('/api/users')
      .then((data) => {
        // Only general users have collections
        setUsers(data.filter((u) => u.role === 'User' && u.state === 'Active'));
        setUsersLoading(false);
      })
      .catch(() => setUsersLoading(false));
  }, []);

  const handleUserChange = async (userId: string) => {
    setSelectedUserId(userId);
    setEntries([]);
    setCollectionError(null);
    if (!userId) return;

    setCollectionLoading(true);
    try {
      const data = await api.get<AdminCollectionEntry[]>(
        `/api/collection?userId=${encodeURIComponent(userId)}`,
      );
      setEntries(data);
    } catch {
      setCollectionError('Failed to load collection');
    } finally {
      setCollectionLoading(false);
    }
  };

  const selectedUser = users.find((u) => u.id === selectedUserId);

  return (
    <div>
      <div>
        <label htmlFor="user-select">User</label>
        {' '}
        <select
          id="user-select"
          value={selectedUserId}
          onChange={(e) => handleUserChange(e.target.value)}
          disabled={usersLoading}
          aria-label="Select user to view collection"
        >
          <option value="">
            {usersLoading ? 'Loading users...' : '-- Select a user --'}
          </option>
          {users.map((u) => (
            <option key={u.id} value={u.id}>
              {u.username}{u.displayName ? ` (${u.displayName})` : ''}
            </option>
          ))}
        </select>
      </div>

      {selectedUserId && (
        <div>
          <h4>
            {selectedUser?.username ?? selectedUserId} - Collection
            {' '}
            <small>(read-only)</small>
          </h4>
          {collectionLoading ? (
            <p>Loading...</p>
          ) : collectionError ? (
            <div role="alert">{collectionError}</div>
          ) : entries.length === 0 ? (
            <p>No collection entries.</p>
          ) : (
            <table aria-label={`Collection for ${selectedUser?.username ?? selectedUserId}`}>
              <thead>
                <tr>
                  <th>Card</th>
                  <th>Set</th>
                  <th>Treatment</th>
                  <th>Qty</th>
                  <th>Condition</th>
                  <th>Autographed</th>
                  <th>Acquired</th>
                  <th>Acquisition price</th>
                  <th>Market value</th>
                  <th>Notes</th>
                </tr>
              </thead>
              <tbody>
                {entries.map((e) => (
                  <tr key={e.id}>
                    <td>{e.cardName ?? e.cardIdentifier}</td>
                    <td>{e.setCode ?? '--'}</td>
                    <td>{e.treatmentKey}</td>
                    <td>{e.quantity}</td>
                    <td>{e.condition}</td>
                    <td>{e.autographed ? 'Yes' : ''}</td>
                    <td>{e.acquisitionDate}</td>
                    <td>${e.acquisitionPrice.toFixed(2)}</td>
                    <td>
                      {e.currentMarketValue !== null
                        ? `$${e.currentMarketValue.toFixed(2)}`
                        : '--'}
                    </td>
                    <td>{e.notes ?? ''}</td>
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
