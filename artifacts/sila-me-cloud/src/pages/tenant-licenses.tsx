import { useEffect, useState } from 'react';

type LicenseRow = {
  licenseType: string;
  productCode?: string;
  licensedQuantity: number;
  usedQuantity: number;
  status: string;
};

type Entitlements = {
  productEntitlements?: string[] | null;
  moduleEntitlements?: string[] | null;
};

export default function TenantLicensesPage() {
  const [licenses, setLicenses] = useState<LicenseRow[]>([]);
  const [entitlements, setEntitlements] = useState<Entitlements>({});
  const [error, setError] = useState('');

  useEffect(() => {
    document.title = 'Licenses · SILA ME';
    void Promise.all([
      fetch('/api/v1/licenses', { credentials: 'include' }).then((response) => response.json()),
      fetch('/api/me', { credentials: 'include' }).then((response) => response.json()),
    ]).then(([rows, me]) => {
      setLicenses(Array.isArray(rows) ? rows as LicenseRow[] : []);
      setEntitlements(me as Entitlements);
    }).catch((caught: Error) => setError(caught.message));
  }, []);

  return (
    <section>
      <h1 style={{ color: 'var(--sila-ink)', fontSize: 24 }}>Licenses and entitlements</h1>
      <p style={{ color: 'var(--sila-muted)' }}>Purchased quantities are managed by SILA Platform. Customer administrators can view usage only.</p>
      {error ? <p style={{ color: 'var(--sila-red)' }}>{error}</p> : null}
      <div style={{ display: 'grid', gap: 12, marginTop: 16 }}>
        {licenses.map((license) => (
          <article key={license.licenseType} className="sila-card" style={{ border: '1px solid var(--sila-line)', borderRadius: 10, padding: 16, background: '#fff' }}>
            <strong style={{ color: 'var(--sila-ink)' }}>{license.licenseType.replace('_', ' ')}</strong>
            <p style={{ margin: '8px 0 0', color: 'var(--sila-muted)' }}>{license.usedQuantity} / {license.licensedQuantity} · {license.status}</p>
          </article>
        ))}
      </div>
      <p style={{ marginTop: 20, color: 'var(--sila-muted)' }}>Products: {(entitlements.productEntitlements ?? []).join(', ') || '—'}</p>
      <p style={{ color: 'var(--sila-muted)' }}>Modules: {(entitlements.moduleEntitlements ?? []).join(', ') || '—'}</p>
    </section>
  );
}
