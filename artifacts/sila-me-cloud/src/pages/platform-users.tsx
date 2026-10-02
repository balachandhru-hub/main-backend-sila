import { FormEvent, useEffect, useState } from 'react';
import { PlatformShell } from '@/components/platform-shell';
import { platformFetch, type PlatformTenantSummary } from '@/lib/platform-api';

type PlatformUser = { id: string; email: string; displayName: string; firstName?: string; lastName?: string; status: string; roles: string[] };

export default function PlatformUsersPage() {
  const [users, setUsers] = useState<PlatformUser[]>([]);
  const [orgs, setOrgs] = useState<PlatformTenantSummary[]>([]);
  const [error, setError] = useState('');
  const [form, setForm] = useState({ firstName: '', lastName: '', email: '', password: '', roleKey: 'CUSTOMER_SUPPORT_CONSULTANT', assignedOrganizationId: '', environmentAccess: 'BOTH' });

  const load = () => Promise.all([
    platformFetch<PlatformUser[]>('/api/platform/users').then(setUsers),
    platformFetch<PlatformTenantSummary[]>('/api/platform/organizations').then(setOrgs),
  ]).catch((caught: Error) => setError(caught.message));

  useEffect(() => { void load(); }, []);

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    await platformFetch('/api/platform/users', {
      method: 'POST',
      body: JSON.stringify({
        ...form,
        displayName: `${form.firstName} ${form.lastName}`.trim(),
        assignedOrganizationId: form.assignedOrganizationId || null,
      }),
    });
    setForm({ firstName: '', lastName: '', email: '', password: '', roleKey: 'CUSTOMER_SUPPORT_CONSULTANT', assignedOrganizationId: '', environmentAccess: 'BOTH' });
    await load();
  };

  return (
    <PlatformShell title="Platform users">
      {error ? <p style={{ color: 'var(--sila-red)' }}>{error}</p> : null}
      <form onSubmit={(event) => void submit(event)} style={{ display: 'grid', gap: 8, maxWidth: 520, marginBottom: 24 }}>
        <input placeholder="First Name" value={form.firstName} onChange={(event) => setForm((current) => ({ ...current, firstName: event.target.value }))} style={{ minHeight: 40, border: '1px solid #c5d8e8', padding: '0 10px' }} />
        <input placeholder="Last Name" value={form.lastName} onChange={(event) => setForm((current) => ({ ...current, lastName: event.target.value }))} style={{ minHeight: 40, border: '1px solid #c5d8e8', padding: '0 10px' }} />
        <input placeholder="Email" value={form.email} onChange={(event) => setForm((current) => ({ ...current, email: event.target.value }))} style={{ minHeight: 40, border: '1px solid #c5d8e8', padding: '0 10px' }} />
        <input placeholder="Password" type="password" value={form.password} onChange={(event) => setForm((current) => ({ ...current, password: event.target.value }))} style={{ minHeight: 40, border: '1px solid #c5d8e8', padding: '0 10px' }} />
        <select value={form.roleKey} onChange={(event) => setForm((current) => ({ ...current, roleKey: event.target.value }))} style={{ minHeight: 40, border: '1px solid #c5d8e8' }}>
          <option value="SILA_ADMIN">SILA ADMIN</option>
          <option value="CUSTOMER_SUPPORT_ADMIN">CUSTOMER SUPPORT ADMIN</option>
          <option value="CUSTOMER_SUPPORT_CONSULTANT">CUSTOMER SUPPORT CONSULTANT</option>
        </select>
        <select value={form.assignedOrganizationId} onChange={(event) => setForm((current) => ({ ...current, assignedOrganizationId: event.target.value }))} style={{ minHeight: 40, border: '1px solid #c5d8e8' }}>
          <option value="">Assigned organization (optional)</option>
          {orgs.map((org) => <option key={org.id} value={org.id}>{org.organizationName || org.customerName}</option>)}
        </select>
        <select value={form.environmentAccess} onChange={(event) => setForm((current) => ({ ...current, environmentAccess: event.target.value }))} style={{ minHeight: 40, border: '1px solid #c5d8e8' }}>
          <option value="BOTH">TEST + PRODUCTION</option>
          <option value="TEST">TEST</option>
          <option value="PRODUCTION">PRODUCTION</option>
        </select>
        <button className="sila-button" type="submit">Create platform user</button>
      </form>
      <div style={{ border: '1px solid #c5d8e8' }}>
        {users.map((user) => (
          <div key={user.id} style={{ padding: 12, borderBottom: '1px solid #e4eef5' }}>
            <strong>{user.displayName}</strong> · {user.email} · {(user.roles || []).join(', ')} · {user.status}
          </div>
        ))}
      </div>
    </PlatformShell>
  );
}
