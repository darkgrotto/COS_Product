import { useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { api } from '../api/client';

// Matches the shape CollectionController.MapEntry returns on the wire.
interface AdminCollectionEntry {
  id: string;
  userId: string;
  cardIdentifier: string;
  treatmentKey: string;
  quantity: number;
  condition: string;
  autographed: boolean;
  acquisitionDate: string;
  acquisitionPrice: number;
  notes: string | null;
}

export function UserCollectionView() {
  const { userId } = useParams<{ userId: string }>();
  const [entries, setEntries] = useState<AdminCollectionEntry[]>([]);
  const [username, setUsername] = useState<string>('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!userId) return;

    const fetchCollection = api.get<AdminCollectionEntry[]>(
      `/api/collection?userId=${encodeURIComponent(userId)}`,
    );
    const fetchUser = api.get<{ username: string }>(`/api/users/${userId}`);

    Promise.allSettled([fetchCollection, fetchUser]).then(([collectionResult, userResult]) => {
      if (collectionResult.status === 'fulfilled') {
        setEntries(collectionResult.value);
      } else {
        setError('Failed to load collection');
      }
      if (userResult.status === 'fulfilled') {
        setUsername(userResult.value.username);
      }
      setLoading(false);
    });
  }, [userId]);

  if (loading) return <p>Loading...</p>;
  if (error) return <div role="alert">{error}</div>;

  return (
    <div>
      <p>
        <Link to="/admin/users">Back to user management</Link>
      </p>
      <h2>
        Collection{username ? ` - ${username}` : ''}{' '}
        <span>(read-only)</span>
      </h2>
      {entries.length === 0 ? (
        <p>No collection entries.</p>
      ) : (
        <table aria-label="User collection">
          <thead>
            <tr>
              <th>Card</th>
              <th>Treatment</th>
              <th>Qty</th>
              <th>Condition</th>
              <th>Autographed</th>
              <th>Acquired</th>
              <th>Acquisition price</th>
              <th>Notes</th>
            </tr>
          </thead>
          <tbody>
            {entries.map((e) => (
              <tr key={e.id}>
                <td>{e.cardIdentifier}</td>
                <td>{e.treatmentKey}</td>
                <td>{e.quantity}</td>
                <td>{e.condition}</td>
                <td>{e.autographed ? 'Yes' : ''}</td>
                <td>{e.acquisitionDate}</td>
                <td>
                  {e.acquisitionPrice != null
                    ? `$${e.acquisitionPrice.toFixed(2)}`
                    : '-'}
                </td>
                <td>{e.notes ?? ''}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}
