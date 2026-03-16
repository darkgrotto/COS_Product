import { useState, FormEvent } from 'react';
import { SealedInventoryRequest } from '../api/sealedInventory';
import { ConditionSelector } from '../components/ConditionSelector';
import { CardCondition } from '../types/filters';

interface Props {
  onSubmit: (request: SealedInventoryRequest) => Promise<void>;
  onCancel?: () => void;
  submitting?: boolean;
}

export function SealedInventoryForm({ onSubmit, onCancel, submitting }: Props) {
  const today = new Date().toISOString().split('T')[0];
  const [productIdentifier, setProductIdentifier] = useState('');
  const [quantity, setQuantity] = useState('1');
  const [condition, setCondition] = useState<CardCondition | ''>('');
  const [acquisitionDate, setAcquisitionDate] = useState(today);
  const [acquisitionPrice, setAcquisitionPrice] = useState('');
  const [notes, setNotes] = useState('');
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setError(null);

    if (!productIdentifier.trim()) { setError('Product identifier is required'); return; }
    if (!condition) { setError('Condition is required'); return; }
    if (!acquisitionDate) { setError('Acquisition date is required'); return; }
    if (!acquisitionPrice) { setError('Acquisition price is required'); return; }

    await onSubmit({
      productIdentifier: productIdentifier.trim(),
      quantity: parseInt(quantity, 10),
      condition: condition as CardCondition,
      acquisitionDate,
      acquisitionPrice: parseFloat(acquisitionPrice),
      notes: notes.trim() || undefined,
    });
  };

  return (
    <form onSubmit={handleSubmit} aria-label="Sealed inventory form" noValidate>
      {error && <div role="alert">{error}</div>}

      <label htmlFor="sealed-product-id">Product identifier</label>
      <input
        id="sealed-product-id"
        type="text"
        value={productIdentifier}
        onChange={(e) => setProductIdentifier(e.target.value)}
        required
        disabled={submitting}
      />

      <label htmlFor="sealed-qty">Quantity</label>
      <input
        id="sealed-qty"
        type="number"
        value={quantity}
        onChange={(e) => setQuantity(e.target.value)}
        min={1}
        required
        disabled={submitting}
      />

      <label htmlFor="sealed-condition">Condition</label>
      <ConditionSelector
        id="sealed-condition"
        value={condition}
        onChange={setCondition}
        required
        disabled={submitting}
      />

      <label htmlFor="sealed-acq-date">Acquisition date</label>
      <input
        id="sealed-acq-date"
        type="date"
        value={acquisitionDate}
        onChange={(e) => setAcquisitionDate(e.target.value)}
        required
        disabled={submitting}
      />

      <label htmlFor="sealed-acq-price">Acquisition price</label>
      <input
        id="sealed-acq-price"
        type="number"
        value={acquisitionPrice}
        onChange={(e) => setAcquisitionPrice(e.target.value)}
        min={0}
        step="0.01"
        required
        disabled={submitting}
      />

      <label htmlFor="sealed-notes">Notes (optional)</label>
      <textarea
        id="sealed-notes"
        value={notes}
        onChange={(e) => setNotes(e.target.value)}
        disabled={submitting}
      />

      <button type="submit" disabled={submitting}>
        {submitting ? 'Saving...' : 'Save sealed product'}
      </button>
      {onCancel && (
        <button type="button" onClick={onCancel} disabled={submitting}>
          Cancel
        </button>
      )}
    </form>
  );
}
