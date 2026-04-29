// Anti-forgery token plumbing.
//
// The server issues a request token via GET /api/auth/csrf and a paired cookie
// token (HttpOnly, SameSite=Strict). It validates that the X-CSRF-TOKEN header
// on every state-changing request matches the cookie. Defense-in-depth on top
// of the existing SameSite=Strict auth cookie.
//
// installCsrfFetch() patches window.fetch once at app startup so every existing
// fetch() callsite picks up the header automatically without code changes.

const SAFE_METHODS = new Set(['GET', 'HEAD', 'OPTIONS', 'TRACE'])

let cachedToken: string | null = null
let pendingFetch: Promise<string> | null = null
let originalFetch: typeof window.fetch | null = null

async function loadToken(): Promise<string> {
  const fetcher = originalFetch ?? window.fetch.bind(window)
  const res = await fetcher('/api/auth/csrf', { credentials: 'include' })
  if (!res.ok) {
    throw new Error(`CSRF token fetch failed with status ${res.status}`)
  }
  const data = (await res.json()) as { token: string }
  if (!data.token) {
    throw new Error('CSRF token response missing token field')
  }
  return data.token
}

async function getToken(): Promise<string> {
  if (cachedToken) return cachedToken
  if (!pendingFetch) {
    pendingFetch = loadToken()
      .then((t) => {
        cachedToken = t
        return t
      })
      .finally(() => {
        pendingFetch = null
      })
  }
  return pendingFetch
}

function methodOf(input: RequestInfo | URL, init?: RequestInit): string {
  if (init?.method) return init.method.toUpperCase()
  if (input instanceof Request) return input.method.toUpperCase()
  return 'GET'
}

// Forces the next state-changing request to fetch a fresh token. Useful after
// auth state changes (login / logout) when the cookie may have rotated.
export function invalidateCsrfToken(): void {
  cachedToken = null
}

export function installCsrfFetch(): void {
  if (originalFetch) return // already installed
  originalFetch = window.fetch.bind(window)

  window.fetch = async (input, init) => {
    const method = methodOf(input, init)
    if (SAFE_METHODS.has(method)) {
      return originalFetch!(input, init)
    }

    const token = await getToken()
    const headers = new Headers(
      init?.headers ?? (input instanceof Request ? input.headers : undefined),
    )
    if (!headers.has('X-CSRF-TOKEN')) {
      headers.set('X-CSRF-TOKEN', token)
    }

    // Cookie-based anti-forgery validation requires the auth cookie to ride along.
    const credentials =
      init?.credentials ?? (input instanceof Request ? input.credentials : 'include')

    return originalFetch!(input, { ...init, headers, credentials })
  }
}
