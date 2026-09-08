import { describe, expect, it, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { DataManagementPage } from './DataManagement'

const SUMMARY = {
  metadata: { setCount: 3, cardCount: 300, sealedProductCount: 2, treatmentCount: 15 },
  images: { totalCount: 150, sealedCount: 1, bySet: [] },
  sets: [
    // metadata and images both present
    { setCode: 'eoe', name: 'Edge of Eternities', cardCount: 200, imageCount: 150 },
    // metadata but no images
    { setCode: 'fdn', name: 'Foundations', cardCount: 100, imageCount: 0 },
    // images orphaned by a metadata purge - invisible in a per-content-type list
    { setCode: 'zzz', name: null, cardCount: 0, imageCount: 42 },
  ],
  sealedProducts: [
    { identifier: 'eoe-bundle', name: 'EOE Bundle', setCode: 'eoe', imageCount: 1 },
    { identifier: 'fdn-box', name: 'FDN Play Booster Box', setCode: 'fdn', imageCount: 0 },
  ],
}

function stubFetch() {
  vi.stubGlobal('fetch', vi.fn(async () =>
    new Response(JSON.stringify(SUMMARY), { status: 200, headers: { 'Content-Type': 'application/json' } })))
}

describe('DataManagementPage', () => {
  beforeEach(stubFetch)
  afterEach(() => { vi.unstubAllGlobals(); vi.restoreAllMocks() })

  it('shows metadata and image state on one row per set', async () => {
    render(<DataManagementPage />)
    const row = await waitFor(() => screen.getByText('Edge of Eternities').closest('tr')!)

    // Both content types are readable together rather than across two lists.
    expect(within(row).getByText('200')).toBeInTheDocument()
    expect(within(row).getByText('150')).toBeInTheDocument()
  })

  it('surfaces a set whose metadata was purged but whose images remain', async () => {
    // The orphan case: it has no set row to appear under, so a metadata-driven list
    // would omit it entirely and leave the images unreachable.
    render(<DataManagementPage />)
    await waitFor(() => expect(screen.getByText('ZZZ')).toBeInTheDocument())
    expect(screen.getByText('no metadata')).toBeInTheDocument()
  })

  it('filters to sets missing images', async () => {
    render(<DataManagementPage />)
    await waitFor(() => screen.getByText('Foundations'))

    // Both filter rows offer 'Missing images'; index 0 is the sets row.
    await userEvent.click(screen.getAllByRole('button', { name: 'Missing images' })[0])

    expect(screen.getByText('Foundations')).toBeInTheDocument()
    expect(screen.queryByText('Edge of Eternities')).not.toBeInTheDocument()
  })

  it('filters to sets missing metadata', async () => {
    render(<DataManagementPage />)
    await waitFor(() => screen.getByText('Foundations'))

    await userEvent.click(screen.getByRole('button', { name: 'Missing metadata' }))

    expect(screen.getByText('ZZZ')).toBeInTheDocument()
    expect(screen.queryByText('Foundations')).not.toBeInTheDocument()
  })

  it('searches by set code and name', async () => {
    render(<DataManagementPage />)
    await waitFor(() => screen.getByText('Foundations'))

    await userEvent.type(screen.getByPlaceholderText(/Search set code or name/i), 'found')

    expect(screen.getByText('Foundations')).toBeInTheDocument()
    expect(screen.queryByText('Edge of Eternities')).not.toBeInTheDocument()
  })

  it('sorts by a column and reverses on a second click', async () => {
    render(<DataManagementPage />)
    await waitFor(() => screen.getByText('Foundations'))

    const codes = () => screen.getAllByRole('row').slice(1)
      .map(r => r.querySelector('td')?.textContent).filter(Boolean)

    // Default is set code ascending.
    expect(codes()[0]).toBe('EOE')

    await userEvent.click(screen.getByRole('button', { name: /^Cards/ }))
    expect(codes()[0]).toBe('ZZZ')      // 0 cards first

    await userEvent.click(screen.getByRole('button', { name: /^Cards/ }))
    expect(codes()[0]).toBe('EOE')      // 200 cards first
  })

  it('lists sealed products in their own table', async () => {
    // They previously had no listing at all - only an aggregate count.
    render(<DataManagementPage />)
    await waitFor(() => expect(screen.getByText('EOE Bundle')).toBeInTheDocument())
    expect(screen.getByText('FDN Play Booster Box')).toBeInTheDocument()
  })

  it('filters sealed products missing images', async () => {
    render(<DataManagementPage />)
    await waitFor(() => screen.getByText('EOE Bundle'))

    await userEvent.click(screen.getAllByRole('button', { name: 'Missing images' })[1])

    expect(screen.getByText('FDN Play Booster Box')).toBeInTheDocument()
    expect(screen.queryByText('EOE Bundle')).not.toBeInTheDocument()
  })

  it('does not show a standing purge warning', async () => {
    // The warning belongs in the confirmation, where it is read at the moment it matters,
    // rather than permanently occupying the page.
    render(<DataManagementPage />)
    await waitFor(() => screen.getByText('Edge of Eternities'))

    expect(screen.queryByText(/permanently/i)).not.toBeInTheDocument()
  })

  it('puts the purge warning in the confirmation dialog', async () => {
    render(<DataManagementPage />)
    await waitFor(() => screen.getByText('Edge of Eternities'))

    await userEvent.click(screen.getByRole('button', { name: /Purge All Metadata/i }))

    await waitFor(() => expect(screen.getByText(/permanently delete/i)).toBeInTheDocument())
    expect(screen.getByText(/collection entries and wishlist entries/i)).toBeInTheDocument()
  })
})
