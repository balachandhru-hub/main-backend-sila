export async function platformFetch<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(path, {
    credentials: 'include',
    headers: { 'Content-Type': 'application/json', ...(init?.headers ?? {}) },
    ...init,
  });
  if (!response.ok) {
    let code = 'PLATFORM_ACCESS_DENIED';
    let message = 'The platform request failed.';
    try {
      const body = await response.json() as { code?: string; message?: string };
      code = body.code ?? code;
      message = body.message ?? message;
    } catch {
      /* keep defaults */
    }
    throw Object.assign(new Error(message), { code, status: response.status });
  }
  if (response.status === 204) return undefined as T;
  return await response.json() as T;
}

export type PlatformTenantSummary = {
  id: string;
  tenantCode: string;
  customerName: string;
  organizationName?: string;
  address?: string | null;
  countryCode?: string | null;
  status: string;
  licenseCount?: number;
  totalUsers?: number;
  products: string[];
  testUrl?: string | null;
  productionUrl?: string | null;
  testEnvironmentId?: string | null;
  productionEnvironmentId?: string | null;
  licenses: Array<{ licenseType: string; licensedQuantity: number; status: string }>;
};
