import { useEffect, useState } from 'react';
import { Link, useRoute } from 'wouter';
import { platformFetch } from '@/lib/platform-api';

export default function PlatformCustomerDetail() {
  const [, params] = useRoute('/platform/customers/:tenantId');
  const [tenant, setTenant] = useState<Record<string, unknown> | null>(null);
  const [error, setError] = useState('');

  useEffect(() => {
    if (!params?.tenantId) return;
    void platformFetch<Record<string, unknown>>(`/api/platform/tenants/${params.tenantId}`)
      .then(setTenant)
      .catch((caught: Error) => setError(caught.message));
  }, [params?.tenantId]);

  return (
    <div className="sila-app" style={{ minHeight: '100vh', background: '#fff', padding: 32 }}>
      <Link href="/platform/customers">← Customers</Link>
      {error ? <p style={{ color: 'var(--sila-red)' }}>{error}</p> : null}
      {tenant ? (
        <section style={{ marginTop: 16 }}>
          <h1 style={{ color: 'var(--sila-ink)' }}>{String(tenant.customerName)}</h1>
          <p style={{ color: 'var(--sila-muted)' }}>{String(tenant.tenantCode)} · {String(tenant.status)} · {String(tenant.countryCode ?? '—')}</p>
          <nav style={{ display: 'flex', gap: 12, margin: '16px 0' }}>
            {['Overview', 'Environments', 'Licenses', 'Products', 'Support access', 'Audit'].map((tab) => (
              <span key={tab} style={{ color: 'var(--sila-ink)', borderBottom: '2px solid var(--sila-line)', paddingBottom: 4 }}>{tab}</span>
            ))}
          </nav>
          <pre style={{ background: '#f8fafc', border: '1px solid var(--sila-line)', padding: 16, overflow: 'auto' }}>{JSON.stringify(tenant, null, 2)}</pre>
        </section>
      ) : null}
    </div>
  );
}
