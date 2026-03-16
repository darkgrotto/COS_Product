import { useState, FormEvent } from 'react';
import { SerializedEntryRequest } from '../api/serialized';
import { TreatmentSelector } from '../components/TreatmentSelector';
import { ConditionSelector } from '../components/ConditionSelector';
import { CardLookup } from '../components/CardLookup';
import { CardCondition } from '../types/filters';

interface Props {
  onSubmit: (request: SerializedEntryRequest) => Promise<void>;
  onCancel?: () => void;
  submitting?: boolean;
}

export function SerializedEntryForm({ onSubmit, onCancel, submitting }: Props) {
  const [cardIdentifier, setCardIdentifier] = useState('');
  const [treatment, setTreatment] = useState('');
  const [serialNumber, setSerialNumber] = useState('');
  const [printRunTotal, setPrintRunTotal] = useState('');
  const [condition, setCondition] = useState<CardCondition | ''>('');
  const [autographed, setAutographed] = useState(false);
  const [acquisitionDate, setAcquisitionDate] = useState('');
  const [acquisitionPrice, setAcquisitionPrice] = useState('');
  const [notes, setNotes] = useState('');
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setError(null);

    if (!cardIdentifier) { setError('Card is required'); return; }
    if (!treatment) { setError('Treatment is required'); return; }
    if (!serialNumber.trim()) { setError('Serial number is required'); return; }
    if (!printRunTotal.trim()) { setError('Print run total is required'); return; }
    if (!condition) { setError('Condition is required'); return; }
    if (!acquisitionDate) { setError('Acquisition date is required'); return; }
    if (!acquisitionPrice) { setError('Acquisition price is required'); return; }

    await onSubmit({
      cardIdentifier,
      treatment,
      serialNumber: parseInt(serialNumber, 10),
      printRunTotal: parseInt(printRunTotal, 10),
      condition: condition as CardCondition,
      autographed,
      acquisitionDate,
      acquisitionPrice: parseFloat(acquisitionPrice),
      notes: notes.trim() || undefined,
    });
  };

  return (
    <form onSubmit={handleSubmit} aria-label="Serialized entry form" noValidate>
      {error && <div role="alert">{error}</div>}

      <label htmlFor="ser-card">Card</label>
      <CardLookup
        id="ser-card"
        value={cardIdentifier}
        onChange={setCardIdentifier}
        required
        disabled={submitting}
      />

      <label htmlFor="ser-treatment">Treatment</label>
      <TreatmentSelector
        id="ser-treatment"
        value={treatment}
        onChange={setTreatment}
        required
        disabled={submitting}
      />

      <label htmlFor="ser-serial">Serial number</label>
      <input
        id="ser-serial"
        type="number"
        value={serialNumber}
        onChange={(e) => setSerialNumber(e.target.value)}
        min={1}
        required
        disabled={submitting}
      />

      <label htmlFor="ser-printrun">Print run total</label>
      <input
        id="ser-printrun"
        type="number"
        value={printRunTotal}
        onChange={(e) => setPrintRunTotal(e.target.value)}
        min={1}
        required
        disabled={submitting}
      />

      <label htmlFor="ser-condition">Condition</label>
      <ConditionSelector
        id="ser-condition"
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

      <label htmlFor="ser-acq-date">Acquisition date</label>
      <input
        id="ser-acq-date"
        type="date"
        value={acquisitionDate}
        onChange={(e) => setAcquisitionDate(e.target.value)}
        required
        disabled={submitting}
      />

      <label htmlFor="ser-acq-price">Acquisition price</label>
      <input
        id="ser-acq-price"
        type="number"
        value={acquisitionPrice}
        onChange={(e) => setAcquisitionPrice(e.target.value)}
        min={0}
        step="0.01"
        required
        disabled={submitting}
      />

      <label htmlFor="ser-notes">Notes (optional)</label>
      <textarea
        id="ser-notes"
        value={notes}
        onChange={(e) => setNotes(e.target.value)}
        disabled={submitting}
      />

      <button type="submit" disabled={submitting}>
        {submitting ? 'Saving...' : 'Save serialized card'}
      </button>
      {onCancel && (
        <button type="button" onClick={onCancel} disabled={submitting}>
          Cancel
        </button>
      )}
    </form>
  );
}
