import { FormEvent, useEffect, useState } from 'react';
import { Link, useRoute } from 'wouter';
import { PlatformShell } from '@/components/platform-shell';
import { platformFetch } from '@/lib/platform-api';

type Detail = {
  id: string;
  tenantCode: string;
  organizationName?: string;
  customerName: string;
  address?: string;
  countryCode?: string;
  taxRegistrationNumber?: string;
  primaryContactName?: string;
  primaryContactEmail?: string;
  primaryContactPhone?: string;
  licenseCount?: number;
  totalUsers?: number;
  status: string;
  environments?: Array<{ id: string; environmentType: string; baseUrl: string; routeSlug?: string }>;
};

export default function OrganizationDetailPage() {
  const [, orgParams] = useRoute('/organizations/:tenantId');
  const [, legacyParams] = useRoute('/platform/customers/:tenantId');
  const params = orgParams ?? legacyParams;
  const [tenant, setTenant] = useState<Detail | null>(null);
  const [tab, setTab] = useState('OVERVIEW');
  const [error, setError] = useState('');
  const [form, setForm] = useState({ organizationName: '', address: '', country: '', taxRegistrationNumber: '', licenseCount: 0, totalUsers: 0, primaryContactName: '', primaryContactEmail: '', primaryContactPhone: '', productionUrl: '', testUrl: '' });

  const load = async () => {
    if (!params?.tenantId) return;
    const row = await platformFetch<Detail>(`/api/platform/tenants/${params.tenantId}`);
    setTenant(row);
    const prod = row.environments?.find((item) => item.environmentType === 'PRODUCTION');
    const test = row.environments?.find((item) => item.environmentType === 'TEST' || item.environmentType === 'DEVELOPMENT');
    setForm({
      organizationName: row.organizationName || row.customerName,
      address: row.address || '',
      country: row.countryCode || '',
      taxRegistrationNumber: row.taxRegistrationNumber || '',
      licenseCount: row.licenseCount || 0,
      totalUsers: row.totalUsers || 0,
      primaryContactName: row.primaryContactName || '',
      primaryContactEmail: row.primaryContactEmail || '',
      primaryContactPhone: row.primaryContactPhone || '',
      productionUrl: prod?.baseUrl || '',
      testUrl: test?.baseUrl || '',
    });
  };

  useEffect(() => { void load().catch((caught: Error) => setError(caught.message)); }, [params?.tenantId]);

  const save = async (event: FormEvent) => {
    event.preventDefault();
    if (!params?.tenantId) return;
    await platformFetch(`/api/platform/organizations/${params.tenantId}`, { method: 'PUT', body: JSON.stringify(form) });
    await load();
  };

  return (
    <PlatformShell title={tenant?.organizationName || tenant?.customerName || 'Organization'}>
      <Link href="/organizations">← Organizations</Link>
      {error ? <p style={{ color: 'var(--sila-red)' }}>{error}</p> : null}
      <p style={{ color: 'var(--sila-muted)' }}>Tenant ID {tenant?.tenantCode} · {tenant?.status}</p>
      {tenant ? (
        <div style={{ display: 'flex', gap: 8, margin: '12px 0 8px' }}>
          <button className="sila-button" type="button" disabled={!tenant.environments?.some((item) => item.environmentType === 'TEST' || item.environmentType === 'DEVELOPMENT')} onClick={() => {
            const env = tenant.environments?.find((item) => item.environmentType === 'TEST' || item.environmentType === 'DEVELOPMENT');
            if (!env) return;
            void platformFetch<{ redirectUrl: string }>(`/api/platform/tenants/${tenant.id}/environments/${env.id}/launch`, { method: 'POST' }).then((result) => window.location.assign(result.redirectUrl));
          }}>OPEN TEST</button>
          <button className="sila-button" type="button" disabled={!tenant.environments?.some((item) => item.environmentType === 'PRODUCTION')} onClick={() => {
            const env = tenant.environments?.find((item) => item.environmentType === 'PRODUCTION');
            if (!env) return;
            void platformFetch<{ redirectUrl: string }>(`/api/platform/tenants/${tenant.id}/environments/${env.id}/launch`, { method: 'POST' }).then((result) => window.location.assign(result.redirectUrl));
          }}>OPEN PRODUCTION</button>
        </div>
      ) : null}
      <nav style={{ display: 'flex', gap: 12, margin: '16px 0 24px' }}>
        {['OVERVIEW', 'USERS & ACCESS', 'LICENSES', 'ENVIRONMENTS / URLS', 'AUDIT'].map((item) => (
          <button key={item} type="button" onClick={() => setTab(item)} style={{ border: 0, background: 'none', borderBottom: tab === item ? '2px solid var(--sila-blue)' : '2px solid transparent', color: 'var(--sila-ink)', paddingBottom: 6, fontWeight: 700 }}>{item}</button>
        ))}
      </nav>
      {tab === 'OVERVIEW' && tenant ? (
        <form onSubmit={(event) => void save(event)} style={{ display: 'grid', gap: 10, maxWidth: 640 }}>
          <p style={{ color: 'var(--sila-muted)', fontSize: 13 }}>Tenant ID is read-only after creation.</p>
          {([
            ['organizationName', 'Organization Name'],
            ['address', 'Address'],
            ['country', 'Country'],
            ['taxRegistrationNumber', 'Tax Registration Number'],
            ['primaryContactName', 'Primary Contact Name'],
            ['primaryContactEmail', 'Primary Contact Email'],
            ['primaryContactPhone', 'Primary Contact Phone'],
            ['productionUrl', 'Production URL'],
            ['testUrl', 'Test URL'],
          ] as Array<[keyof typeof form, string]>).map(([key, label]) => (
            <label key={key} style={{ display: 'grid', gap: 4 }}>{label}
              <input value={String(form[key])} onChange={(event) => setForm((current) => ({ ...current, [key]: event.target.value }))} style={{ minHeight: 40, border: '1px solid #c5d8e8', padding: '0 10px' }} />
            </label>
          ))}
          <label>Licenses<input type="number" value={form.licenseCount} onChange={(event) => setForm((current) => ({ ...current, licenseCount: Number(event.target.value) }))} style={{ minHeight: 40, border: '1px solid #c5d8e8', padding: '0 10px', width: '100%' }} /></label>
          <label>Total Users<input type="number" value={form.totalUsers} onChange={(event) => setForm((current) => ({ ...current, totalUsers: Number(event.target.value) }))} style={{ minHeight: 40, border: '1px solid #c5d8e8', padding: '0 10px', width: '100%' }} /></label>
          <button className="sila-button" type="submit">Save</button>
        </form>
      ) : null}
      {tab !== 'OVERVIEW' ? (
        <p style={{ color: 'var(--sila-muted)' }}>Platform-level {tab.toLowerCase()} only. Customer operational configuration stays in the customer application.</p>
      ) : null}
    </PlatformShell>
  );
}
