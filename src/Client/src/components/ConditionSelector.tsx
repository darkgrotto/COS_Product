import { CardCondition, CARD_CONDITIONS, CARD_CONDITION_LABELS } from '../types/filters';

interface Props {
  value: CardCondition | '';
  onChange: (value: CardCondition) => void;
  id?: string;
  required?: boolean;
  disabled?: boolean;
}

export function ConditionSelector({ value, onChange, id, required, disabled }: Props) {
  return (
    <select
      id={id}
      value={value}
      onChange={(e) => onChange(e.target.value as CardCondition)}
      required={required}
      disabled={disabled}
      aria-label="Condition"
    >
      <option value="">Select condition</option>
      {CARD_CONDITIONS.map((c) => (
        <option key={c} value={c}>
          {c} - {CARD_CONDITION_LABELS[c]}
        </option>
      ))}
    </select>
  );
}
