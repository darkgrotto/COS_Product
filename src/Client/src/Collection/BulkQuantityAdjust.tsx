import { useState, FormEvent } from 'react';
import { collectionApi } from '../api/collection';

interface Props {
  entryId: string;
  currentQuantity: number;
  onAdjusted: (newQuantity: number) => void;
  onCancel?: () => void;
}

export function BulkQuantityAdjust({ entryId, currentQuantity, onAdjusted, onCancel }: Props) {
  const [quantity, setQuantity] = useState(String(currentQuantity));
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setError(null);
    const qty = parseInt(quantity, 10);
    if (isNaN(qty) || qty < 1) { setError('Quantity must be at least 1'); return; }
    setSubmitting(true);
    try {
      const updated = await collectionApi.adjustQuantity(entryId, qty);
      onAdjusted(updated.quantity);
    } catch {
      setError('Failed to update quantity');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} aria-label="Adjust quantity">
      {error && <div role="alert">{error}</div>}
      <label htmlFor="adj-qty">New quantity</label>
      <input
        id="adj-qty"
        type="number"
        value={quantity}
        onChange={(e) => setQuantity(e.target.value)}
        min={1}
        required
        disabled={submitting}
      />
      <button type="submit" disabled={submitting}>
        {submitting ? 'Saving...' : 'Update quantity'}
      </button>
      {onCancel && (
        <button type="button" onClick={onCancel} disabled={submitting}>
          Cancel
        </button>
      )}
    </form>
  );
}
