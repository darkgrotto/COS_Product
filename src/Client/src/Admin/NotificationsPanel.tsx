import { useEffect, useState } from 'react';
import { updatesApi, AdminNotification } from '../api/updates';

export function NotificationsPanel() {
  const [notifications, setNotifications] = useState<AdminNotification[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [markingAllRead, setMarkingAllRead] = useState(false);

  const load = () =>
    updatesApi.getNotifications()
      .then((data) => { setNotifications(data); setLoading(false); })
      .catch(() => { setError('Failed to load notifications'); setLoading(false); });

  useEffect(() => { load(); }, []);

  const markRead = async (id: number) => {
    try {
      await updatesApi.markNotificationRead(id);
      setNotifications((prev) => prev.filter((n) => n.id !== id));
    } catch {
      // non-critical
    }
  };

  const markAllRead = async () => {
    setMarkingAllRead(true);
    try {
      await updatesApi.markAllNotificationsRead();
      setNotifications([]);
    } catch {
      // non-critical
    } finally {
      setMarkingAllRead(false);
    }
  };

  if (loading) return <p>Loading...</p>;
  if (error) return <div role="alert">{error}</div>;

  return (
    <div>
      <h3>Notifications</h3>
      {notifications.length === 0 ? (
        <p>No unread notifications.</p>
      ) : (
        <>
          <button
            type="button"
            onClick={markAllRead}
            disabled={markingAllRead}
          >
            {markingAllRead ? 'Clearing...' : 'Mark all read'}
          </button>
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
        </>
      )}
    </div>
  );
}
