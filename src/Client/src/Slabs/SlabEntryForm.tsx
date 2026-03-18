import { useState, FormEvent } from 'react';
import { SlabEntryRequest } from '../api/slabs';
import { TreatmentSelector } from '../components/TreatmentSelector';
import { ConditionSelector } from '../components/ConditionSelector';
import { GradingAgencySelector } from '../components/GradingAgencySelector';
import { CardLookup } from '../components/CardLookup';
import { CardCondition } from '../types/filters';

interface Props {
  onSubmit: (request: SlabEntryRequest) => Promise<void>;
  onCancel?: () => void;
  submitting?: boolean;
}

export function SlabEntryForm({ onSubmit, onCancel, submitting }: Props) {
  const [cardIdentifier, setCardIdentifier] = useState('');
  const [treatment, setTreatment] = useState('');
  const [gradingAgencyCode, setGradingAgencyCode] = useState('');
  const [grade, setGrade] = useState('');
  const [certificateNumber, setCertificateNumber] = useState('');
  const [serialNumber, setSerialNumber] = useState('');
  const [printRunTotal, setPrintRunTotal] = useState('');
  const [condition, setCondition] = useState<CardCondition | ''>('');
  const [acquisitionDate, setAcquisitionDate] = useState('');
  const [acquisitionPrice, setAcquisitionPrice] = useState('');
  const [notes, setNotes] = useState('');
  const [error, setError] = useState<string | null>(null);

  const serialNumberValue = serialNumber ? parseInt(serialNumber, 10) : undefined;
  const printRunTotalValue = printRunTotal ? parseInt(printRunTotal, 10) : undefined;

  const serialNumberPresent = serialNumber.trim() !== '';
  const printRunTotalRequired = serialNumberPresent;

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setError(null);

    if (!cardIdentifier) { setError('Card identifier is required'); return; }
    if (!treatment) { setError('Treatment is required'); return; }
    if (!gradingAgencyCode) { setError('Grading agency is required'); return; }
    if (!grade.trim()) { setError('Grade is required'); return; }
    if (!certificateNumber.trim()) { setError('Certificate number is required'); return; }
    if (!condition) { setError('Condition is required'); return; }
    if (!acquisitionDate) { setError('Acquisition date is required'); return; }
    if (!acquisitionPrice) { setError('Acquisition price is required'); return; }
    if (serialNumberPresent && !printRunTotal.trim()) {
      setError('Print run total is required when serial number is provided');
      return;
    }

    await onSubmit({
      cardIdentifier,
      treatment,
      gradingAgencyCode,
      grade: grade.trim(),
      certificateNumber: certificateNumber.trim(),
      serialNumber: serialNumberValue,
      printRunTotal: printRunTotalRequired ? printRunTotalValue : undefined,
      condition: condition as CardCondition,
      autographed: false,
      acquisitionDate,
      acquisitionPrice: parseFloat(acquisitionPrice),
      notes: notes.trim() || undefined,
    });
  };

  return (
    <form onSubmit={handleSubmit} aria-label="Slab entry form" noValidate>
      {error && <div role="alert">{error}</div>}

      <label htmlFor="slab-card">Card</label>
      <CardLookup
        id="slab-card"
        value={cardIdentifier}
        onChange={setCardIdentifier}
        required
        disabled={submitting}
      />

      <label htmlFor="slab-treatment">Treatment</label>
      <TreatmentSelector
        id="slab-treatment"
        value={treatment}
        onChange={setTreatment}
        required
        disabled={submitting}
      />

      <label htmlFor="slab-agency">Grading agency</label>
      <GradingAgencySelector
        id="slab-agency"
        value={gradingAgencyCode}
        onChange={setGradingAgencyCode}
        required
        disabled={submitting}
      />

      <label htmlFor="slab-grade">Grade</label>
      <input
        id="slab-grade"
        type="text"
        value={grade}
        onChange={(e) => setGrade(e.target.value)}
        required
        disabled={submitting}
      />

      <label htmlFor="slab-cert">Certificate number</label>
      <input
        id="slab-cert"
        type="text"
        value={certificateNumber}
        onChange={(e) => setCertificateNumber(e.target.value)}
        required
        disabled={submitting}
      />

      <label htmlFor="slab-serial">Serial number (optional)</label>
      <input
        id="slab-serial"
        type="number"
        value={serialNumber}
        onChange={(e) => setSerialNumber(e.target.value)}
        min={1}
        disabled={submitting}
        aria-label="Serial number"
      />

      <label htmlFor="slab-printrun">
        Print run total{printRunTotalRequired ? ' (required)' : ' (optional)'}
      </label>
      <input
        id="slab-printrun"
        type="number"
        value={printRunTotal}
        onChange={(e) => setPrintRunTotal(e.target.value)}
        min={1}
        required={printRunTotalRequired}
        disabled={submitting}
        aria-label="Print run total"
        aria-required={printRunTotalRequired}
      />

      <label htmlFor="slab-condition">Condition</label>
      <ConditionSelector
        id="slab-condition"
        value={condition}
        onChange={setCondition}
        required
        disabled={submitting}
      />

      <label htmlFor="slab-acq-date">Acquisition date</label>
      <input
        id="slab-acq-date"
        type="date"
        value={acquisitionDate}
        onChange={(e) => setAcquisitionDate(e.target.value)}
        required
        disabled={submitting}
      />

      <label htmlFor="slab-acq-price">Acquisition price</label>
      <input
        id="slab-acq-price"
        type="number"
        value={acquisitionPrice}
        onChange={(e) => setAcquisitionPrice(e.target.value)}
        min={0}
        step="0.01"
        required
        disabled={submitting}
      />

      <label htmlFor="slab-notes">Notes (optional)</label>
      <textarea
        id="slab-notes"
        value={notes}
        onChange={(e) => setNotes(e.target.value)}
        disabled={submitting}
      />

      <button type="submit" disabled={submitting}>
        {submitting ? 'Saving...' : 'Save slab'}
      </button>
      {onCancel && (
        <button type="button" onClick={onCancel} disabled={submitting}>
          Cancel
        </button>
      )}
    </form>
  );
}
