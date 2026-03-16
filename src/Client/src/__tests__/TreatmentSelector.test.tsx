import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { TreatmentSelector } from '../components/TreatmentSelector';
import * as treatmentsApiModule from '../api/treatments';
import { Treatment } from '../types/treatments';

const mockTreatments: Treatment[] = [
  { key: 'regular', displayName: 'Regular', sortOrder: 1 },
  { key: 'foil', displayName: 'Foil', sortOrder: 2 },
  { key: 'surge-foil', displayName: 'Surge Foil', sortOrder: 3 },
];

describe('TreatmentSelector', () => {
  beforeEach(() => {
    vi.spyOn(treatmentsApiModule.treatmentsApi, 'getAll').mockResolvedValue(mockTreatments);
  });

  it('fetches treatments from the API and renders them as options', async () => {
    render(<TreatmentSelector value="" onChange={() => {}} />);

    await waitFor(() => {
      expect(screen.getByRole('combobox', { name: /treatment/i })).toBeInTheDocument();
    });

    expect(screen.getByRole('option', { name: 'Regular' })).toBeInTheDocument();
    expect(screen.getByRole('option', { name: 'Foil' })).toBeInTheDocument();
    expect(screen.getByRole('option', { name: 'Surge Foil' })).toBeInTheDocument();
  });

  it('does not hardcode any treatment values', async () => {
    render(<TreatmentSelector value="" onChange={() => {}} />);

    await waitFor(() => {
      expect(screen.getByRole('option', { name: 'Regular' })).toBeInTheDocument();
    });

    const options = screen.getAllByRole('option');
    const optionValues = options.map((o) => (o as HTMLOptionElement).value).filter((v) => v !== '');
    expect(optionValues).toEqual(mockTreatments.map((t) => t.key));
  });

  it('calls onChange with the selected treatment key', async () => {
    const user = userEvent.setup();
    const handleChange = vi.fn();
    render(<TreatmentSelector value="" onChange={handleChange} />);

    await waitFor(() => {
      expect(screen.getByRole('option', { name: 'Foil' })).toBeInTheDocument();
    });

    await user.selectOptions(screen.getByRole('combobox', { name: /treatment/i }), 'foil');
    expect(handleChange).toHaveBeenCalledWith('foil');
  });

  it('shows loading state while fetching', () => {
    vi.spyOn(treatmentsApiModule.treatmentsApi, 'getAll').mockReturnValue(new Promise(() => {}));
    render(<TreatmentSelector value="" onChange={() => {}} />);
    expect(screen.getByRole('combobox')).toBeDisabled();
  });

  it('shows error state when fetch fails', async () => {
    vi.spyOn(treatmentsApiModule.treatmentsApi, 'getAll').mockRejectedValue(new Error('network error'));
    render(<TreatmentSelector value="" onChange={() => {}} />);

    await waitFor(() => {
      expect(screen.getByRole('alert')).toBeInTheDocument();
    });
  });

  it('calls the API exactly once on mount', async () => {
    const spy = vi.spyOn(treatmentsApiModule.treatmentsApi, 'getAll').mockResolvedValue(mockTreatments);
    render(<TreatmentSelector value="" onChange={() => {}} />);

    await waitFor(() => {
      expect(screen.getByRole('option', { name: 'Regular' })).toBeInTheDocument();
    });

    expect(spy).toHaveBeenCalledTimes(1);
  });
});
