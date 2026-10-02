import { FormEvent, useEffect, useMemo, useState } from 'react';
import { Link, useLocation } from 'wouter';
import { PlatformShell } from '@/components/platform-shell';
import { platformFetch, type PlatformTenantSummary } from '@/lib/platform-api';

const emptyForm = {
  organizationName: 'Five Hotels and Resorts',
  tenantId: 'five',
  address: 'Dubai, Palm, JVC',
  country: 'AE',
  taxRegistrationNumber: '',
  licenseCount: 10,
  totalUsers: 100,
  primaryContactName: '',
  primaryContactEmail: '',
  primaryContactPhone: '',
  productionUrl: 'http://localhost:5173/five',
  testUrl: 'http://localhost:5173/five',
  adminEmail: 'five-admin@silame.local',
  adminPassword: 'FiveAdmin123!',
};

export default function OrganizationsPage() {
  const [, setLocation] = useLocation();
  const [query, setQuery] = useState('');
  const [tenants, setTenants] = useState<PlatformTenantSummary[]>([]);
  const [error, setError] = useState('');
  const [pending, setPending] = useState(true);
  const [showAdd, setShowAdd] = useState(false);
  const [form, setForm] = useState(emptyForm);
  const [saving, setSaving] = useState(false);
  const [confirm, setConfirm] = useState<{ id: string; name: string; action: 'SUSPEND' | 'DELETE' } | null>(null);

  const load = async (term = query) => {
    setPending(true);
    setError('');
    try {
      const rows = await platformFetch<PlatformTenantSummary[]>(`/api/platform/organizations${term.trim() ? `?query=${encodeURIComponent(term.trim())}` : ''}`);
      setTenants(rows);
    } catch (caught) {
      if (caught && typeof caught === 'object' && 'status' in caught && (caught as { status: number }).status === 401) {
        setLocation('/platform/login');
        return;
      }
      setError(caught instanceof Error ? caught.message : 'Unable to load organizations.');
    } finally {
      setPending(false);
    }
  };

  useEffect(() => {
    document.title = 'Organizations · SILA Cloud';
    void load('');
  }, []);

  const launch = async (tenantId: string, environmentId?: string | null) => {
    if (!environmentId) return;
    const result = await platformFetch<{ redirectUrl: string }>(`/api/platform/tenants/${tenantId}/environments/${environmentId}/launch`, { method: 'POST' });
    window.location.assign(result.redirectUrl);
  };

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    setSaving(true);
    setError('');
    try {
      await platformFetch('/api/platform/organizations', {
        method: 'POST',
        body: JSON.stringify({
          organizationName: form.organizationName,
          tenantId: form.tenantId,
          address: form.address,
          country: form.country,
          taxRegistrationNumber: form.taxRegistrationNumber,
          licenseCount: Number(form.licenseCount),
          totalUsers: Number(form.totalUsers),
          primaryContactName: form.primaryContactName,
          primaryContactEmail: form.primaryContactEmail,
          primaryContactPhone: form.primaryContactPhone,
          productionUrl: form.productionUrl,
          testUrl: form.testUrl,
          adminEmail: form.adminEmail,
          adminDisplayName: form.organizationName,
          adminPassword: form.adminPassword,
        }),
      });
      setShowAdd(false);
      await load('');
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : 'Unable to save organization.');
    } finally {
      setSaving(false);
    }
  };

  const runAction = async () => {
    if (!confirm) return;
    const path = confirm.action === 'SUSPEND'
      ? `/api/platform/tenants/${confirm.id}/suspend`
      : `/api/platform/tenants/${confirm.id}/archive`;
    await platformFetch(path, { method: 'POST' });
    setConfirm(null);
    await load(query);
  };

  const filtered = useMemo(() => tenants, [tenants]);

  return (
    <PlatformShell
      title="Organizations"
      action={<button className="sila-button" type="button" onClick={() => setShowAdd(true)}>+ ADD ORGANIZATION</button>}
    >
      <form onSubmit={(event) => { event.preventDefault(); void load(query); }} style={{ marginBottom: 20 }}>
        <input
          value={query}
          onChange={(event) => setQuery(event.target.value)}
          placeholder="Search organization or Tenant ID..."
          style={{ width: '100%', minHeight: 44, border: '1px solid #c5d8e8', borderRadius: 6, padding: '0 12px', background: '#fff' }}
        />
      </form>
      {error ? <p style={{ color: 'var(--sila-red)' }}>{error}</p> : null}
      {pending ? <p>Loading organizations…</p> : (
        <div style={{ border: '1px solid #c5d8e8', background: '#fff' }}>
          <div style={{ display: 'grid', gridTemplateColumns: '1.4fr .8fr .7fr .5fr .5fr 1.6fr .9fr', gap: 8, padding: '10px 12px', borderBottom: '1px solid #c5d8e8', color: 'var(--sila-muted)', fontSize: 11, fontWeight: 700, letterSpacing: '.04em', textTransform: 'uppercase' }}>
            <span>Organization</span><span>Tenant ID</span><span>Status</span><span>Users</span><span>Licenses</span><span>URLs</span><span>Actions</span>
          </div>
          {filtered.map((tenant) => (
            <article key={tenant.id} style={{ display: 'grid', gridTemplateColumns: '1.4fr .8fr .7fr .5fr .5fr 1.6fr .9fr', gap: 8, padding: '14px 12px', borderBottom: '1px solid #e4eef5', alignItems: 'start' }}>
              <div>
                <Link href={`/organizations/${tenant.id}`} style={{ color: 'var(--sila-ink)', fontWeight: 700 }}>{tenant.organizationName || tenant.customerName}</Link>
                <div style={{ color: 'var(--sila-muted)', fontSize: 12, marginTop: 4 }}>{tenant.address || '—'}</div>
              </div>
              <div>{tenant.tenantCode}</div>
              <div style={{ fontWeight: 700 }}>{tenant.status}</div>
              <div>{tenant.totalUsers ?? '—'}</div>
              <div>{tenant.licenseCount ?? '—'}</div>
              <div style={{ fontSize: 12 }}>
                <div>TEST<br /><span style={{ color: 'var(--sila-muted)' }}>{tenant.testUrl || '—'}</span><br />
                  <button className="sila-button sila-button--quiet" type="button" disabled={!tenant.testEnvironmentId} onClick={() => void launch(tenant.id, tenant.testEnvironmentId)}>OPEN TEST</button>
                </div>
                <div style={{ marginTop: 8 }}>PRODUCTION<br /><span style={{ color: 'var(--sila-muted)' }}>{tenant.productionUrl || '—'}</span><br />
                  <button className="sila-button sila-button--quiet" type="button" disabled={!tenant.productionEnvironmentId} onClick={() => void launch(tenant.id, tenant.productionEnvironmentId)}>OPEN PRODUCTION</button>
                </div>
              </div>
              <div style={{ display: 'grid', gap: 6 }}>
                <Link href={`/organizations/${tenant.id}`}>EDIT</Link>
                {tenant.status === 'SUSPENDED' ? (
                  <button type="button" className="sila-button sila-button--quiet" onClick={() => void platformFetch(`/api/platform/tenants/${tenant.id}/reactivate`, { method: 'POST' }).then(() => load(query))}>REACTIVATE</button>
                ) : (
                  <button type="button" className="sila-button sila-button--quiet" onClick={() => setConfirm({ id: tenant.id, name: tenant.organizationName || tenant.customerName, action: 'SUSPEND' })}>SUSPEND</button>
                )}
                <button type="button" className="sila-button sila-button--quiet" onClick={() => setConfirm({ id: tenant.id, name: tenant.organizationName || tenant.customerName, action: 'DELETE' })}>DELETE</button>
              </div>
            </article>
          ))}
        </div>
      )}

      {showAdd ? (
        <div style={{ position: 'fixed', inset: 0, background: 'rgba(38,58,77,.35)', display: 'grid', placeItems: 'center', zIndex: 50 }}>
          <form onSubmit={(event) => void submit(event)} style={{ background: '#fff', border: '1px solid #c5d8e8', padding: 24, width: 'min(640px, 96vw)', maxHeight: '90vh', overflow: 'auto' }}>
            <h2 style={{ marginTop: 0 }}>Add organization</h2>
            {([
              ['organizationName', 'Organization Name *'],
              ['tenantId', 'Tenant ID *'],
              ['address', 'Address *'],
              ['country', 'Country'],
              ['taxRegistrationNumber', 'Tax Registration Number'],
              ['primaryContactName', 'Primary Contact Name'],
              ['primaryContactEmail', 'Primary Contact Email'],
              ['primaryContactPhone', 'Primary Contact Phone'],
              ['productionUrl', 'Production URL *'],
              ['testUrl', 'Test URL *'],
              ['adminEmail', 'Customer admin email'],
              ['adminPassword', 'Customer admin password'],
            ] as Array<[keyof typeof form, string]>).map(([key, label]) => (
              <label key={key} style={{ display: 'grid', gap: 4, marginBottom: 10, fontSize: 13 }}>
                {label}
                <input value={String(form[key])} onChange={(event) => setForm((current) => ({ ...current, [key]: event.target.value }))} style={{ minHeight: 40, border: '1px solid #c5d8e8', padding: '0 10px' }} />
              </label>
            ))}
            <label style={{ display: 'grid', gap: 4, marginBottom: 10 }}>Number of Licenses *
              <input type="number" value={form.licenseCount} onChange={(event) => setForm((current) => ({ ...current, licenseCount: Number(event.target.value) }))} style={{ minHeight: 40, border: '1px solid #c5d8e8', padding: '0 10px' }} />
            </label>
            <label style={{ display: 'grid', gap: 4, marginBottom: 16 }}>Total Users *
              <input type="number" value={form.totalUsers} onChange={(event) => setForm((current) => ({ ...current, totalUsers: Number(event.target.value) }))} style={{ minHeight: 40, border: '1px solid #c5d8e8', padding: '0 10px' }} />
            </label>
            <div style={{ display: 'flex', gap: 8, justifyContent: 'flex-end' }}>
              <button type="button" className="sila-button sila-button--quiet" onClick={() => setShowAdd(false)}>CANCEL</button>
              <button type="submit" className="sila-button" disabled={saving}>{saving ? 'Saving…' : 'SAVE'}</button>
            </div>
          </form>
        </div>
      ) : null}

      {confirm ? (
        <div style={{ position: 'fixed', inset: 0, background: 'rgba(38,58,77,.35)', display: 'grid', placeItems: 'center', zIndex: 50 }}>
          <div style={{ background: '#fff', border: '1px solid #c5d8e8', padding: 24, width: 'min(480px, 94vw)' }}>
            <h2 style={{ marginTop: 0 }}>{confirm.action === 'SUSPEND' ? `Suspend ${confirm.name}?` : `Delete ${confirm.name}?`}</h2>
            <p>{confirm.action === 'SUSPEND'
              ? 'Users will not be able to access this organization\'s customer applications while the organization is suspended.'
              : 'This archives the organization in SILA Cloud. Customer operational databases are not destroyed.'}</p>
            <div style={{ display: 'flex', gap: 8, justifyContent: 'flex-end' }}>
              <button type="button" className="sila-button sila-button--quiet" onClick={() => setConfirm(null)}>CANCEL</button>
              <button type="button" className="sila-button" onClick={() => void runAction()}>{confirm.action === 'SUSPEND' ? 'SUSPEND' : 'DELETE'}</button>
            </div>
          </div>
        </div>
      ) : null}
    </PlatformShell>
  );
}
