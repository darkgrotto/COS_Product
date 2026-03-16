import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { SlabEntryForm } from '../Slabs/SlabEntryForm';
import * as treatmentsApiModule from '../api/treatments';
import * as gradingAgenciesApiModule from '../api/gradingAgencies';
import { Treatment } from '../types/treatments';
import { GradingAgency } from '../types/gradingAgency';

// Stub CardLookup to render a plain input so tests can set cardIdentifier directly
vi.mock('../components/CardLookup', () => ({
  CardLookup: ({ value, onChange, id, required }: {
    value: string;
    onChange: (v: string) => void;
    id?: string;
    required?: boolean;
  }) => (
    <input
      id={id}
      aria-label="Card identifier"
      value={value}
      onChange={(e) => onChange(e.target.value)}
      required={required}
      data-testid="card-lookup-input"
    />
  ),
}));

const mockTreatments: Treatment[] = [
  { key: 'regular', displayName: 'Regular', sortOrder: 1 },
  { key: 'foil', displayName: 'Foil', sortOrder: 2 },
];

const mockAgencies: GradingAgency[] = [
  {
    code: 'PSA',
    fullName: 'Professional Sports Authenticator',
    validationUrlTemplate: 'https://psa.example.com/{cert}',
    supportsDirectLookup: true,
    source: 'Canonical',
    active: true,
  },
  {
    code: 'BGS',
    fullName: 'Beckett Grading Services',
    validationUrlTemplate: 'https://bgs.example.com/{cert}',
    supportsDirectLookup: true,
    source: 'Canonical',
    active: true,
  },
];

describe('SlabEntryForm', () => {
  beforeEach(() => {
    vi.spyOn(treatmentsApiModule.treatmentsApi, 'getAll').mockResolvedValue(mockTreatments);
    vi.spyOn(gradingAgenciesApiModule.gradingAgenciesApi, 'getAll').mockResolvedValue(mockAgencies);
  });

  it('renders the form with required fields', async () => {
    render(<SlabEntryForm onSubmit={async () => {}} />);

    await waitFor(() => {
      expect(screen.getByLabelText(/treatment/i)).toBeInTheDocument();
    });

    expect(screen.getByLabelText(/grading agency/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/^grade$/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/certificate number/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/condition/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/acquisition date/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/acquisition price/i)).toBeInTheDocument();
  });

  it('print run total is not required when serial number is empty', async () => {
    render(<SlabEntryForm onSubmit={async () => {}} />);

    await waitFor(() => {
      expect(screen.getByLabelText(/serial number/i)).toBeInTheDocument();
    });

    const printRunField = screen.getByLabelText(/print run total/i);
    expect(printRunField).not.toBeRequired();
  });

  it('print run total becomes required when serial number is entered', async () => {
    const user = userEvent.setup();
    render(<SlabEntryForm onSubmit={async () => {}} />);

    await waitFor(() => {
      expect(screen.getByLabelText(/serial number/i)).toBeInTheDocument();
    });

    await user.type(screen.getByLabelText(/serial number/i), '42');

    await waitFor(() => {
      const printRunField = screen.getByLabelText(/print run total/i);
      expect(printRunField).toBeRequired();
    });
  });

  it('shows validation error when serial number present but print run total empty', async () => {
    const user = userEvent.setup();
    const onSubmit = vi.fn();
    render(<SlabEntryForm onSubmit={onSubmit} />);

    await waitFor(() => {
      expect(screen.getByLabelText(/treatment/i)).toBeInTheDocument();
    });

    // Fill in all required fields except print run total
    await user.type(screen.getByLabelText(/card identifier/i), 'eoe019');
    await user.selectOptions(screen.getByLabelText(/treatment/i), 'regular');
    await user.selectOptions(screen.getByLabelText(/grading agency/i), 'PSA');
    await user.type(screen.getByLabelText(/^grade$/i), '9');
    await user.type(screen.getByLabelText(/certificate number/i), 'CERT123');
    await user.type(screen.getByLabelText(/serial number/i), '42');
    // Intentionally skip print run total
    await user.selectOptions(screen.getByLabelText(/condition/i), 'NM');
    await user.type(screen.getByLabelText(/acquisition date/i), '2026-03-15');
    await user.type(screen.getByLabelText(/acquisition price/i), '100');
    await user.click(screen.getByRole('button', { name: /save slab/i }));

    await waitFor(() => {
      expect(screen.getByRole('alert')).toBeInTheDocument();
    });

    expect(screen.getByRole('alert')).toHaveTextContent(/print run total/i);
    expect(onSubmit).not.toHaveBeenCalled();
  });

  it('submits successfully with serial number and print run total both provided', async () => {
    const user = userEvent.setup();
    const onSubmit = vi.fn().mockResolvedValue(undefined);
    render(<SlabEntryForm onSubmit={onSubmit} />);

    await waitFor(() => {
      expect(screen.getByLabelText(/treatment/i)).toBeInTheDocument();
    });

    await user.type(screen.getByLabelText(/card identifier/i), 'eoe019');
    await user.selectOptions(screen.getByLabelText(/treatment/i), 'regular');
    await user.selectOptions(screen.getByLabelText(/grading agency/i), 'PSA');
    await user.type(screen.getByLabelText(/^grade$/i), '9.5');
    await user.type(screen.getByLabelText(/certificate number/i), 'CERT123');
    await user.type(screen.getByLabelText(/serial number/i), '42');
    await user.type(screen.getByLabelText(/print run total/i), '100');
    await user.selectOptions(screen.getByLabelText(/condition/i), 'NM');
    await user.type(screen.getByLabelText(/acquisition date/i), '2026-03-15');
    await user.type(screen.getByLabelText(/acquisition price/i), '150.00');
    await user.click(screen.getByRole('button', { name: /save slab/i }));

    await waitFor(() => {
      expect(onSubmit).toHaveBeenCalledOnce();
    });

    const callArg = onSubmit.mock.calls[0][0];
    expect(callArg.serialNumber).toBe(42);
    expect(callArg.printRunTotal).toBe(100);
  });

  it('submits successfully without serial number', async () => {
    const user = userEvent.setup();
    const onSubmit = vi.fn().mockResolvedValue(undefined);
    render(<SlabEntryForm onSubmit={onSubmit} />);

    await waitFor(() => {
      expect(screen.getByLabelText(/treatment/i)).toBeInTheDocument();
    });

    await user.type(screen.getByLabelText(/card identifier/i), 'eoe019');
    await user.selectOptions(screen.getByLabelText(/treatment/i), 'regular');
    await user.selectOptions(screen.getByLabelText(/grading agency/i), 'PSA');
    await user.type(screen.getByLabelText(/^grade$/i), '9');
    await user.type(screen.getByLabelText(/certificate number/i), 'CERT456');
    await user.selectOptions(screen.getByLabelText(/condition/i), 'NM');
    await user.type(screen.getByLabelText(/acquisition date/i), '2026-03-15');
    await user.type(screen.getByLabelText(/acquisition price/i), '75.00');
    await user.click(screen.getByRole('button', { name: /save slab/i }));

    await waitFor(() => {
      expect(onSubmit).toHaveBeenCalledOnce();
    });

    const callArg = onSubmit.mock.calls[0][0];
    expect(callArg.serialNumber).toBeUndefined();
    expect(callArg.printRunTotal).toBeUndefined();
  });
});
