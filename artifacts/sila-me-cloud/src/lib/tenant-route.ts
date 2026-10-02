const RESERVED = new Set([
  'api', 'platform', 'organizations', 'login', 'dashboard', 'admin', 'approvals',
  'analytics', 'documents', 'inventory', 'receiving', 'purchasing', 'procurement',
  'master-data', 'menu-engineering', 'assets', 'src', 'platform-launch',
]);

export function normalizeRouteSlug(value: string): string {
  return value.replace(/^\/+|\/+$/g, '').toLowerCase();
}

export function canonicalizeRouteSlug(value: string): string {
  const normalized = normalizeRouteSlug(value);
  return normalized === 'five-test' ? 'five' : normalized;
}

function usableSlug(value: string | undefined): string | null {
  if (!value || RESERVED.has(value.toLowerCase())) return null;
  if (!/^[a-z0-9][a-z0-9-]{0,62}$/i.test(value)) return null;
  return canonicalizeRouteSlug(value);
}

export function getViteBaseRouteSlug(): string | null {
  const raw = import.meta.env?.BASE_URL;
  if (typeof raw !== 'string') return null;
  return usableSlug(raw.split('/').filter(Boolean)[0]);
}

export function getCustomerRouteSlug(pathname = window.location.pathname): string | null {
  const first = pathname.split('/').filter(Boolean)[0];
  return usableSlug(first);
}

export function resolveCustomerRouteSlug(pathname?: string): string | null {
  return getCustomerRouteSlug(pathname ?? window.location.pathname) ?? getViteBaseRouteSlug();
}

export function customerBasePath(slug = resolveCustomerRouteSlug()): string {
  return slug ? `/${slug}` : '';
}

export function withCustomerBase(path: string, slug = resolveCustomerRouteSlug()): string {
  const suffix = path.startsWith('/') ? path : `/${path}`;
  return `${customerBasePath(slug)}${suffix}`;
}

function mergeFetchHeaders(input: RequestInfo | URL, init?: RequestInit): Headers {
  const headers = new Headers();
  if (typeof Request !== 'undefined' && input instanceof Request) {
    input.headers.forEach((value, key) => headers.set(key, value));
  }
  if (init?.headers) {
    new Headers(init.headers).forEach((value, key) => headers.set(key, value));
  }
  return headers;
}

export function installTenantRouteFetch(): void {
  const original = window.fetch.bind(window);
  window.fetch = (input: RequestInfo | URL, init?: RequestInit) => {
    const slug = resolveCustomerRouteSlug();
    if (!slug) return original(input, init);
    const headers = mergeFetchHeaders(input, init);
    if (!headers.has('X-Sila-Route-Slug')) {
      headers.set('X-Sila-Route-Slug', canonicalizeRouteSlug(slug));
    }
    if (typeof Request !== 'undefined' && input instanceof Request) {
      return original(new Request(input, { ...init, headers }));
    }
    return original(input, { ...init, headers });
  };
}
