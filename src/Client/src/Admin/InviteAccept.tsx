import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';

interface InvitationInfo {
  email: string;
  role: string;
  expiresAt: string;
}

export function InviteAccept() {
  const { token } = useParams<{ token: string }>();
  const navigate = useNavigate();

  const [invitation, setInvitation] = useState<InvitationInfo | null>(null);
  const [validating, setValidating] = useState(true);
  const [invalidReason, setInvalidReason] = useState<string | null>(null);

  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);

  useEffect(() => {
    if (!token) {
      setInvalidReason('No invitation token provided.');
      setValidating(false);
      return;
    }
    fetch(`/api/invitations/${token}/validate`)
      .then(async (r) => {
        if (!r.ok) {
          const body = await r.json().catch(() => ({})) as { error?: string };
          setInvalidReason(body.error ?? 'This invitation is invalid or has expired.');
        } else {
          const data = await r.json() as InvitationInfo;
          setInvitation(data);
        }
      })
      .catch(() => setInvalidReason('Failed to validate invitation.'))
      .finally(() => setValidating(false));
  }, [token]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitError(null);
    setSubmitting(true);
    try {
      const r = await fetch(`/api/invitations/${token}/accept`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ username: username.trim(), password }),
      });
      if (!r.ok) {
        const body = await r.json().catch(() => ({})) as { error?: string };
        setSubmitError(body.error ?? `Failed (${r.status})`);
        return;
      }
      navigate('/');
    } catch {
      setSubmitError('Request failed. Please try again.');
    } finally {
      setSubmitting(false);
    }
  };

  if (validating) return <p>Validating invitation...</p>;

  if (invalidReason) {
    return (
      <div>
        <h2>Invalid invitation</h2>
        <p role="alert">{invalidReason}</p>
      </div>
    );
  }

  return (
    <div>
      <h2>Create your account</h2>
      {invitation && (
        <p>
          You have been invited to join as a{' '}
          {invitation.role === 'GeneralUser' ? 'general user' : invitation.role.toLowerCase()}.
        </p>
      )}
      <form onSubmit={(e) => { void handleSubmit(e); }}>
        <div>
          <label htmlFor="accept-username">Username</label>
          {' '}
          <input
            id="accept-username"
            type="text"
            value={username}
            onChange={(e) => setUsername(e.target.value)}
            required
            autoComplete="username"
          />
        </div>
        <div>
          <label htmlFor="accept-password">Password</label>
          {' '}
          <input
            id="accept-password"
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
            minLength={15}
            autoComplete="new-password"
          />
          <small> Minimum 15 characters.</small>
        </div>
        {submitError && <p role="alert">{submitError}</p>}
        <button type="submit" disabled={submitting}>
          {submitting ? 'Creating account...' : 'Create account'}
        </button>
      </form>
    </div>
  );
}
