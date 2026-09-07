import { describe, expect, it } from 'vitest'
import { render, screen } from '@testing-library/react'
import { QuickAddDialog, availableTreatmentsFor, sortTreatments } from './CardDialogs'

// The treatments reference table ships in the update package and is versioned
// independently, so nothing here may depend on a treatment this build knows about.
const REFERENCE = [
  { key: 'regular', displayName: 'Regular', sortOrder: 0 },
  { key: 'foil', displayName: 'Foil', sortOrder: 1 },
  { key: 'etched-foil', displayName: 'Etched Foil', sortOrder: 2 },
  { key: 'serialized', displayName: 'Serialized', sortOrder: 3 },
  { key: 'surge-foil', displayName: 'Surge Foil', sortOrder: 4 },
  { key: 'halo-foil', displayName: 'Halo Foil', sortOrder: 9 },
]

describe('sortTreatments', () => {
  it('orders by the sort_order the package ships, not by treatment name', () => {
    const shuffled = [...REFERENCE].reverse()
    expect(sortTreatments(shuffled).map(t => t.key)).toEqual(
      ['regular', 'foil', 'etched-foil', 'serialized', 'surge-foil', 'halo-foil'])
  })

  it('follows the package even when it demotes a familiar treatment', () => {
    // The old implementation forced 'regular' and 'foil' to the front by name. If the
    // Backend reorders the table, Product has to follow it.
    const reordered = [
      { key: 'regular', displayName: 'Regular', sortOrder: 50 },
      { key: 'halo-foil', displayName: 'Halo Foil', sortOrder: 1 },
    ]
    expect(sortTreatments(reordered).map(t => t.key)).toEqual(['halo-foil', 'regular'])
  })

  it('does not mutate the array it is given', () => {
    const input = [...REFERENCE].reverse()
    const snapshot = input.map(t => t.key)
    sortTreatments(input)
    expect(input.map(t => t.key)).toEqual(snapshot)
  })
})

describe('availableTreatmentsFor', () => {
  it('offers exactly the treatments a foil-only serialized card is printed in', () => {
    // Modelled on BRR "64z" (Adaptive Automaton): foil-only, so {Foil, Serialized}
    // and emphatically no Regular.
    const offered = availableTreatmentsFor(['foil', 'serialized'], REFERENCE)

    expect(offered.map(t => t.key)).toEqual(['foil', 'serialized'])
    expect(offered.map(t => t.key)).not.toContain('regular')
  })

  it('offers a premium treatment that only started carrying cards upstream', () => {
    const offered = availableTreatmentsFor(['surge-foil'], REFERENCE)
    expect(offered.map(t => t.displayName)).toEqual(['Surge Foil'])
  })

  it('falls back to the whole reference table when a card has no associations', () => {
    // An unknown card, or canonical data predating per-card treatments, must not be
    // left unpickable.
    expect(availableTreatmentsFor([], REFERENCE)).toHaveLength(REFERENCE.length)
    expect(availableTreatmentsFor(null, REFERENCE)).toHaveLength(REFERENCE.length)
    expect(availableTreatmentsFor(undefined, REFERENCE)).toHaveLength(REFERENCE.length)
  })

  it('ignores an association the reference table does not describe yet', () => {
    // Card data can arrive before the treatments table catches up. The treatments that
    // do resolve must still be offered rather than the picker collapsing.
    const offered = availableTreatmentsFor(['foil', 'not-in-the-table-yet'], REFERENCE)
    expect(offered.map(t => t.key)).toEqual(['foil'])
  })

  it('falls back rather than offering nothing when no association resolves', () => {
    const offered = availableTreatmentsFor(['entirely-unknown'], REFERENCE)
    expect(offered).toHaveLength(REFERENCE.length)
  })

  it('carries display names from the reference rather than deriving them from the key', () => {
    // A display name that is not the hyphenated key title-cased is the case that catches
    // any build that formats the key instead of reading the package.
    const renamed = [{ key: 'surge-foil', displayName: 'Surge (Textured)', sortOrder: 4 }]
    expect(availableTreatmentsFor(['surge-foil'], renamed)[0].displayName).toBe('Surge (Textured)')
  })
})

describe('QuickAddDialog', () => {
  const card = {
    identifier: 'brr064z',
    name: 'Adaptive Automaton',
    setCode: 'brr',
    currentMarketValue: 12.5,
    validTreatments: ['foil', 'serialized'],
  }

  it('defaults to a treatment the card is actually printed in', () => {
    render(<QuickAddDialog card={card as never} treatments={REFERENCE} onClose={() => {}} onAdded={() => {}} />)

    // Foil sorts first of the card's two, so it is the default - not Regular, which
    // this printing does not have.
    expect(screen.getByText('Foil')).toBeInTheDocument()
    expect(screen.queryByText('Regular')).not.toBeInTheDocument()
  })

  it('renders a card whose treatment this build has never seen', () => {
    const unknown = { ...card, validTreatments: ['rainbow-foil'] }
    const table = [...REFERENCE, { key: 'rainbow-foil', displayName: 'Rainbow Foil', sortOrder: 12 }]

    render(<QuickAddDialog card={unknown as never} treatments={table} onClose={() => {}} onAdded={() => {}} />)

    expect(screen.getByText('Rainbow Foil')).toBeInTheDocument()
  })
})
