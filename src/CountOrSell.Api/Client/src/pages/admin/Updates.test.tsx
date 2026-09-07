import { describe, expect, it, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { UpdatesPage } from './Updates'

// The page reads status on mount and every action posts, so fetch is stubbed per-URL.
function stubFetch() {
  const calls: { url: string; method: string; body?: string }[] = []
  const fn = vi.fn(async (url: string | URL | Request, init?: RequestInit) => {
    const u = String(url)
    calls.push({ url: u, method: init?.method ?? 'GET', body: init?.body as string | undefined })
    if (u.includes('/api/updates/status')) {
      return new Response(JSON.stringify({
        currentContentVersion: '1.5.1', componentVersions: null,
        pendingSchemaUpdate: null, applicationUpdatePending: false,
      }), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
    if (u.includes('/api/updates/notifications')) {
      return new Response('[]', { status: 200, headers: { 'Content-Type': 'application/json' } })
    }
    return new Response(JSON.stringify({ message: 'ok', packagesAvailable: true }),
      { status: 200, headers: { 'Content-Type': 'application/json' } })
  })
  vi.stubGlobal('fetch', fn)
  return calls
}

describe('UpdatesPage actions', () => {
  let calls: { url: string; method: string; body?: string }[]

  beforeEach(() => { calls = stubFetch() })
  afterEach(() => { vi.unstubAllGlobals(); vi.restoreAllMocks() })

  it('offers all four update actions', async () => {
    // A button that renders but overflows a non-wrapping row looks identical to a missing
    // one, which is how Force Redownload came to be reported as absent.
    render(<UpdatesPage />)

    await waitFor(() => expect(screen.getByRole('button', { name: /Refresh Status/i })).toBeInTheDocument())
    expect(screen.getByRole('button', { name: /Update Metadata Only/i })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /Update Metadata & Images/i })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /Force Redownload/i })).toBeInTheDocument()
  })

  it('refreshing status downloads nothing', async () => {
    // The whole point of this button: re-read state without applying content. The previous
    // refresh-looking control applied a package, so this must stay a read.
    render(<UpdatesPage />)
    await waitFor(() => screen.getByRole('button', { name: /Refresh Status/i }))
    calls.length = 0

    await userEvent.click(screen.getByRole('button', { name: /Refresh Status/i }))

    await waitFor(() => expect(calls.length).toBeGreaterThan(0))
    expect(calls.every(c => c.method === 'GET')).toBe(true)
    expect(calls.some(c => c.url.includes('/check') || c.url.includes('redownload'))).toBe(false)
  })

  it('metadata-only asks for metadata and not images', async () => {
    // A full package lists over 100,000 images fetched one request each; the quick path
    // exists precisely to skip them.
    render(<UpdatesPage />)
    await waitFor(() => screen.getByRole('button', { name: /Update Metadata Only/i }))
    calls.length = 0

    await userEvent.click(screen.getByRole('button', { name: /Update Metadata Only/i }))

    await waitFor(() => expect(calls.some(c => c.method === 'POST')).toBe(true))
    const post = calls.find(c => c.method === 'POST')!
    expect(post.url).toContain('/api/updates/redownload-targeted')
    expect(JSON.parse(post.body!)).toMatchObject({ contentType: 'metadata', scope: 'all' })
  })

  it('the full update applies metadata and images together', async () => {
    render(<UpdatesPage />)
    await waitFor(() => screen.getByRole('button', { name: /Update Metadata & Images/i }))
    calls.length = 0

    await userEvent.click(screen.getByRole('button', { name: /Update Metadata & Images/i }))

    await waitFor(() => expect(calls.some(c => c.method === 'POST')).toBe(true))
    expect(calls.find(c => c.method === 'POST')!.url).toContain('/api/updates/check')
  })
})
