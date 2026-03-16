import { useState, FormEvent } from 'react';
import { CollectionEntryRequest } from '../api/collection';
import { TreatmentSelector } from '../components/TreatmentSelector';
import { ConditionSelector } from '../components/ConditionSelector';
import { CardLookup } from '../components/CardLookup';
import { CardCondition } from '../types/filters';

interface Props {
  onSubmit: (request: CollectionEntryRequest) => Promise<void>;
  onCancel?: () => void;
  submitting?: boolean;
  initialValues?: Partial<CollectionEntryRequest>;
}

export function CollectionEntryForm({ onSubmit, onCancel, submitting, initialValues }: Props) {
  const today = new Date().toISOString().split('T')[0];
  const [cardIdentifier, setCardIdentifier] = useState(initialValues?.cardIdentifier ?? '');
  const [treatment, setTreatment] = useState(initialValues?.treatment ?? '');
  const [quantity, setQuantity] = useState(String(initialValues?.quantity ?? 1));
  const [condition, setCondition] = useState<CardCondition | ''>(initialValues?.condition ?? '');
  const [autographed, setAutographed] = useState(initialValues?.autographed ?? false);
  const [acquisitionDate, setAcquisitionDate] = useState(initialValues?.acquisitionDate ?? today);
  const [acquisitionPrice, setAcquisitionPrice] = useState(
    initialValues?.acquisitionPrice !== undefined ? String(initialValues.acquisitionPrice) : '',
  );
  const [notes, setNotes] = useState(initialValues?.notes ?? '');
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setError(null);

    if (!cardIdentifier) { setError('Card is required'); return; }
    if (!treatment) { setError('Treatment is required'); return; }
    if (!condition) { setError('Condition is required'); return; }
    if (!acquisitionDate) { setError('Acquisition date is required'); return; }
    if (!acquisitionPrice) { setError('Acquisition price is required'); return; }

    await onSubmit({
      cardIdentifier,
      treatment,
      quantity: parseInt(quantity, 10),
      condition: condition as CardCondition,
      autographed,
      acquisitionDate,
      acquisitionPrice: parseFloat(acquisitionPrice),
      notes: notes.trim() || undefined,
    });
  };

  return (
    <form onSubmit={handleSubmit} aria-label="Collection entry form" noValidate>
      {error && <div role="alert">{error}</div>}

      <label htmlFor="col-card">Card</label>
      <CardLookup
        id="col-card"
        value={cardIdentifier}
        onChange={setCardIdentifier}
        required
        disabled={submitting}
      />

      <label htmlFor="col-treatment">Treatment</label>
      <TreatmentSelector
        id="col-treatment"
        value={treatment}
        onChange={setTreatment}
        required
        disabled={submitting}
      />

      <label htmlFor="col-quantity">Quantity</label>
      <input
        id="col-quantity"
        type="number"
        value={quantity}
        onChange={(e) => setQuantity(e.target.value)}
        min={1}
        required
        disabled={submitting}
      />

      <label htmlFor="col-condition">Condition</label>
      <ConditionSelector
        id="col-condition"
        value={condition}
        onChange={setCondition}
        required
        disabled={submitting}
      />

      <label>
        <input
          type="checkbox"
          checked={autographed}
          onChange={(e) => setAutographed(e.target.checked)}
          disabled={submitting}
        />
        Autographed
      </label>

      <label htmlFor="col-acq-date">Acquisition date</label>
      <input
        id="col-acq-date"
        type="date"
        value={acquisitionDate}
        onChange={(e) => setAcquisitionDate(e.target.value)}
        required
        disabled={submitting}
      />

      <label htmlFor="col-acq-price">Acquisition price</label>
      <input
        id="col-acq-price"
        type="number"
        value={acquisitionPrice}
        onChange={(e) => setAcquisitionPrice(e.target.value)}
        min={0}
        step="0.01"
        required
        disabled={submitting}
      />

      <label htmlFor="col-notes">Notes (optional)</label>
      <textarea
        id="col-notes"
        value={notes}
        onChange={(e) => setNotes(e.target.value)}
        disabled={submitting}
      />

      <button type="submit" disabled={submitting}>
        {submitting ? 'Saving...' : 'Save'}
      </button>
      {onCancel && (
        <button type="button" onClick={onCancel} disabled={submitting}>
          Cancel
        </button>
      )}
    </form>
  );
}
