import { useEffect, useMemo, useState } from 'react';
import { Link, useLocation } from 'wouter';
import { platformFetch, type PlatformTenantSummary } from '@/lib/platform-api';

export default function PlatformCustomers() {
  const [, setLocation] = useLocation();
  const [query, setQuery] = useState('');
  const [tenants, setTenants] = useState<PlatformTenantSummary[]>([]);
  const [error, setError] = useState('');
  const [pending, setPending] = useState(true);
  const [creating, setCreating] = useState(false);
  const [createError, setCreateError] = useState('');
  const [form, setForm] = useState({
    tenantCode: '',
    customerName: '',
    legalName: '',
    countryCode: 'AE',
    taxRegistrationNumber: '',
    primaryContactName: '',
    primaryContactEmail: '',
    primaryContactPhone: '',
    cloudUsers: 25,
    mobileUsers: 100,
    products: 'SILA_ME',
    testHostname: '',
    productionHostname: '',
    adminEmail: '',
    adminDisplayName: '',
    adminPassword: '',
  });

  const load = async (term = query) => {
    setPending(true);
    setError('');
    try {
      const rows = await platformFetch<PlatformTenantSummary[]>(`/api/platform/tenants${term.trim() ? `?query=${encodeURIComponent(term.trim())}` : ''}`);
      setTenants(rows);
    } catch (caught) {
      if (caught && typeof caught === 'object' && 'status' in caught && (caught as { status: number }).status === 401) {
        setLocation('/platform/login');
        return;
      }
      setError(caught instanceof Error ? caught.message : 'Unable to load customers.');
    } finally {
      setPending(false);
    }
  };

  useEffect(() => {
    document.title = 'Customers · SILA Platform';
    void load('');
  }, []);

  const filtered = useMemo(() => tenants, [tenants]);

  const launch = async (tenantId: string, environmentId?: string | null) => {
    if (!environmentId) return;
    const result = await platformFetch<{ redirectUrl: string }>(`/api/platform/tenants/${tenantId}/environments/${environmentId}/launch`, { method: 'POST' });
    window.location.assign(result.redirectUrl);
  };

  return (
    <div className="sila-app" style={{ minHeight: '100vh', background: '#fff' }}>
      <header className="sila-header" style={{ borderBottom: '1px solid var(--sila-line)' }}>
        <div className="sila-header__left">
          <strong style={{ color: 'var(--sila-ink)', fontSize: 18 }}>SILA Platform</strong>
          <nav style={{ display: 'flex', gap: 16, marginLeft: 24 }}>
            <Link href="/platform/customers">Customers</Link>
            <Link href="/platform/users">Users</Link>
            <Link href="/platform/audit">Audit</Link>
          </nav>
        </div>
      </header>
      <main style={{ padding: 32, maxWidth: 1200, margin: '0 auto' }}>
        <h1 style={{ color: 'var(--sila-ink)', fontSize: 28, marginBottom: 8 }}>Customers</h1>
        <p style={{ color: 'var(--sila-muted)', marginBottom: 20 }}>Search customer, organization, tenant ID…</p>
        <form onSubmit={(event) => { event.preventDefault(); void load(query); }} style={{ display: 'flex', gap: 12, marginBottom: 24 }}>
          <input className="sila-input" value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Search customer, tenant ID..." style={{ flex: 1, minHeight: 44, border: '1px solid var(--sila-line)', borderRadius: 8, padding: '0 12px' }} />
          <button className="sila-button" type="submit">Search</button>
        </form>
        <details style={{ marginBottom: 24, border: '1px solid var(--sila-line)', borderRadius: 10, padding: 16, background: '#fff' }}>
          <summary style={{ cursor: 'pointer', color: 'var(--sila-ink)', fontWeight: 700 }}>NEW CUSTOMER</summary>
          <form
            onSubmit={(event) => {
              event.preventDefault();
              setCreateError('');
              setCreating(true);
              void platformFetch('/api/platform/tenants', {
                method: 'POST',
                body: JSON.stringify({
                  tenantCode: form.tenantCode,
                  customerName: form.customerName,
                  legalName: form.legalName,
                  countryCode: form.countryCode,
                  taxRegistrationNumber: form.taxRegistrationNumber,
                  primaryContactName: form.primaryContactName,
                  primaryContactEmail: form.primaryContactEmail,
                  primaryContactPhone: form.primaryContactPhone,
                  cloudUsers: Number(form.cloudUsers),
                  mobileUsers: Number(form.mobileUsers),
                  products: form.products.split(',').map((item) => item.trim()).filter(Boolean),
                  modules: [],
                  createTest: Boolean(form.testHostname.trim()),
                  createProduction: Boolean(form.productionHostname.trim()),
                  testHostname: form.testHostname.trim() || null,
                  productionHostname: form.productionHostname.trim() || null,
                  adminEmail: form.adminEmail,
                  adminDisplayName: form.adminDisplayName,
                  adminPassword: form.adminPassword,
                  adminApplications: ['CLOUD'],
                }),
              }).then(() => load('')).catch((caught: Error) => setCreateError(caught.message)).finally(() => setCreating(false));
            }}
            style={{ display: 'grid', gap: 10, marginTop: 16, maxWidth: 640 }}
          >
            {createError ? <p style={{ color: 'var(--sila-red)' }}>{createError}</p> : null}
            {([
              ['tenantCode', 'Tenant code'],
              ['customerName', 'Customer name'],
              ['legalName', 'Legal name'],
              ['countryCode', 'Country'],
              ['taxRegistrationNumber', 'Tax registration number'],
              ['primaryContactName', 'Primary contact'],
              ['primaryContactEmail', 'Primary email'],
              ['primaryContactPhone', 'Phone'],
              ['testHostname', 'TEST hostname'],
              ['productionHostname', 'PRODUCTION hostname'],
              ['adminDisplayName', 'Initial customer admin name'],
              ['adminEmail', 'Initial customer admin email'],
              ['adminPassword', 'Initial customer admin password'],
              ['products', 'Products (comma-separated)'],
            ] as Array<[keyof typeof form, string]>).map(([key, label]) => (
              <label key={key} style={{ display: 'grid', gap: 4, color: 'var(--sila-ink)' }}>
                {label}
                <input className="sila-input" value={String(form[key])} onChange={(event) => setForm((current) => ({ ...current, [key]: event.target.value }))} style={{ minHeight: 40, border: '1px solid var(--sila-line)', borderRadius: 8, padding: '0 12px' }} />
              </label>
            ))}
            <label style={{ color: 'var(--sila-ink)' }}>Cloud users
              <input type="number" value={form.cloudUsers} onChange={(event) => setForm((current) => ({ ...current, cloudUsers: Number(event.target.value) }))} style={{ minHeight: 40, border: '1px solid var(--sila-line)', borderRadius: 8, padding: '0 12px', width: '100%' }} />
            </label>
            <label style={{ color: 'var(--sila-ink)' }}>Mobile users
              <input type="number" value={form.mobileUsers} onChange={(event) => setForm((current) => ({ ...current, mobileUsers: Number(event.target.value) }))} style={{ minHeight: 40, border: '1px solid var(--sila-line)', borderRadius: 8, padding: '0 12px', width: '100%' }} />
            </label>
            <button className="sila-button" type="submit" disabled={creating}>{creating ? 'Provisioning…' : 'Review & provision'}</button>
          </form>
        </details>
        {error ? <p style={{ color: 'var(--sila-red)' }}>{error}</p> : null}
        {pending ? <p>Loading customers…</p> : (
          <div style={{ display: 'grid', gap: 12 }}>
            {filtered.map((tenant) => (
              <article key={tenant.id} className="sila-card" style={{ border: '1px solid var(--sila-line)', borderRadius: 10, padding: 16, background: '#fff' }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', gap: 16, flexWrap: 'wrap' }}>
                  <div>
                    <h2 style={{ margin: 0, color: 'var(--sila-ink)' }}>{tenant.customerName}</h2>
                    <p style={{ margin: '6px 0', color: 'var(--sila-muted)' }}>{tenant.tenantCode} · {tenant.countryCode || '—'} · {tenant.status}</p>
                    <p style={{ margin: 0, color: 'var(--sila-muted)' }}>TEST {tenant.testUrl || '—'} · PRODUCTION {tenant.productionUrl || '—'}</p>
                    <p style={{ margin: '6px 0 0', color: 'var(--sila-muted)' }}>{(tenant.products ?? []).join(', ') || 'No products'}</p>
                  </div>
                  <div style={{ display: 'flex', gap: 8, alignItems: 'flex-start' }}>
                    <button className="sila-button" type="button" disabled={!tenant.testEnvironmentId} onClick={() => void launch(tenant.id, tenant.testEnvironmentId)}>OPEN TEST</button>
                    <button className="sila-button" type="button" disabled={!tenant.productionEnvironmentId} onClick={() => void launch(tenant.id, tenant.productionEnvironmentId)}>OPEN PRODUCTION</button>
                    <Link href={`/platform/customers/${tenant.id}`}>MANAGE</Link>
                  </div>
                </div>
              </article>
            ))}
          </div>
        )}
      </main>
    </div>
  );
}
