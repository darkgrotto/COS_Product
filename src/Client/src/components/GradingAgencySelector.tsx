import { useEffect, useState } from 'react';
import { GradingAgency } from '../types/gradingAgency';
import { gradingAgenciesApi } from '../api/gradingAgencies';

interface Props {
  value: string;
  onChange: (value: string) => void;
  id?: string;
  required?: boolean;
  disabled?: boolean;
}

export function GradingAgencySelector({ value, onChange, id, required, disabled }: Props) {
  const [agencies, setAgencies] = useState<GradingAgency[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    gradingAgenciesApi.getAll()
      .then((data) => {
        if (!cancelled) {
          setAgencies(data.filter((a) => a.active));
          setLoading(false);
        }
      })
      .catch(() => {
        if (!cancelled) {
          setError('Failed to load grading agencies');
          setLoading(false);
        }
      });
    return () => { cancelled = true; };
  }, []);

  if (loading) {
    return <select disabled aria-label="Grading agency"><option>Loading...</option></select>;
  }

  if (error) {
    return <span role="alert">{error}</span>;
  }

  return (
    <select
      id={id}
      value={value}
      onChange={(e) => onChange(e.target.value)}
      required={required}
      disabled={disabled}
      aria-label="Grading agency"
    >
      <option value="">Select grading agency</option>
      {agencies.map((a) => (
        <option key={a.code} value={a.code}>
          {a.code} - {a.fullName}
        </option>
      ))}
    </select>
  );
}
