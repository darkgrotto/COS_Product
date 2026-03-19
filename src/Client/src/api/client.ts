async function request<T>(
  url: string,
  options?: RequestInit,
): Promise<T> {
  const response = await fetch(url, {
    credentials: 'include',
    headers: {
      'Content-Type': 'application/json',
      ...options?.headers,
    },
    ...options,
  });

  if (!response.ok) {
    const text = await response.text().catch(() => '');
    throw new ApiError(response.status, text);
  }

  if (response.status === 204) {
    return undefined as unknown as T;
  }

  return response.json() as Promise<T>;
}

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    public readonly body: string,
  ) {
    super(`HTTP ${status}: ${body}`);
  }
}

async function requestText(url: string): Promise<string> {
  const response = await fetch(url, { credentials: 'include' });
  if (!response.ok) {
    const text = await response.text().catch(() => '');
    throw new ApiError(response.status, text);
  }
  return response.text();
}

export const api = {
  get: <T>(url: string) => request<T>(url),
  getText: (url: string) => requestText(url),
  post: <T>(url: string, body?: unknown) =>
    request<T>(url, { method: 'POST', body: body !== undefined ? JSON.stringify(body) : undefined }),
  put: <T>(url: string, body?: unknown) =>
    request<T>(url, { method: 'PUT', body: body !== undefined ? JSON.stringify(body) : undefined }),
  patch: <T>(url: string, body?: unknown) =>
    request<T>(url, { method: 'PATCH', body: body !== undefined ? JSON.stringify(body) : undefined }),
  delete: <T = void>(url: string, body?: unknown) =>
    request<T>(url, { method: 'DELETE', body: body !== undefined ? JSON.stringify(body) : undefined }),
};
