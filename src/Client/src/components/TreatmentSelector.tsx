import { useEffect, useState } from 'react';
import { Treatment } from '../types/treatments';
import { treatmentsApi } from '../api/treatments';

interface Props {
  value: string;
  onChange: (value: string) => void;
  id?: string;
  required?: boolean;
  disabled?: boolean;
}

export function TreatmentSelector({ value, onChange, id, required, disabled }: Props) {
  const [treatments, setTreatments] = useState<Treatment[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    treatmentsApi.getAll()
      .then((data) => {
        if (!cancelled) {
          setTreatments(data);
          setLoading(false);
        }
      })
      .catch(() => {
        if (!cancelled) {
          setError('Failed to load treatments');
          setLoading(false);
        }
      });
    return () => { cancelled = true; };
  }, []);

  if (loading) {
    return <select disabled aria-label="Treatment"><option>Loading...</option></select>;
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
      aria-label="Treatment"
    >
      <option value="">Select treatment</option>
      {treatments.map((t) => (
        <option key={t.key} value={t.key}>
          {t.displayName}
        </option>
      ))}
    </select>
  );
}
