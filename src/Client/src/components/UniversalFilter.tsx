import { useEffect, useState } from 'react';
import { CollectionFilter, CardCondition, CARD_CONDITIONS, CARD_COLORS, CARD_TYPES } from '../types/filters';
import { Treatment } from '../types/treatments';
import { treatmentsApi } from '../api/treatments';

interface Props {
  filter: CollectionFilter;
  onChange: (filter: CollectionFilter) => void;
  hideFields?: Array<keyof CollectionFilter>;
}

export function UniversalFilter({ filter, onChange, hideFields = [] }: Props) {
  const [treatments, setTreatments] = useState<Treatment[]>([]);

  useEffect(() => {
    treatmentsApi.getAll().then(setTreatments).catch(() => {});
  }, []);

  const show = (field: keyof CollectionFilter) => !hideFields.includes(field);

  const update = (patch: Partial<CollectionFilter>) => onChange({ ...filter, ...patch });

  return (
    <div aria-label="Collection filters">
      {show('setCode') && (
        <label>
          Set
          <input
            type="text"
            value={filter.setCode ?? ''}
            onChange={(e) => update({ setCode: e.target.value || undefined })}
            placeholder="e.g. EOE"
            aria-label="Filter by set"
          />
        </label>
      )}
      {show('color') && (
        <label>
          Color
          <select
            value={filter.color ?? ''}
            onChange={(e) => update({ color: e.target.value || undefined })}
            aria-label="Filter by color"
          >
            <option value="">All colors</option>
            {CARD_COLORS.map((c) => (
              <option key={c} value={c}>{c}</option>
            ))}
          </select>
        </label>
      )}
      {show('condition') && (
        <label>
          Condition
          <select
            value={filter.condition ?? ''}
            onChange={(e) => update({ condition: (e.target.value as CardCondition) || undefined })}
            aria-label="Filter by condition"
          >
            <option value="">All conditions</option>
            {CARD_CONDITIONS.map((c) => (
              <option key={c} value={c}>{c}</option>
            ))}
          </select>
        </label>
      )}
      {show('cardType') && (
        <label>
          Card type
          <select
            value={filter.cardType ?? ''}
            onChange={(e) => update({ cardType: e.target.value || undefined })}
            aria-label="Filter by card type"
          >
            <option value="">All types</option>
            {CARD_TYPES.map((t) => (
              <option key={t} value={t}>{t}</option>
            ))}
          </select>
        </label>
      )}
      {show('treatment') && (
        <label>
          Treatment
          <select
            value={filter.treatment ?? ''}
            onChange={(e) => update({ treatment: e.target.value || undefined })}
            aria-label="Filter by treatment"
          >
            <option value="">All treatments</option>
            {treatments.map((t) => (
              <option key={t.key} value={t.key}>{t.displayName}</option>
            ))}
          </select>
        </label>
      )}
      {show('autographed') && (
        <label>
          <input
            type="checkbox"
            checked={filter.autographed ?? false}
            onChange={(e) => update({ autographed: e.target.checked || undefined })}
          />
          Autographed only
        </label>
      )}
    </div>
  );
}
