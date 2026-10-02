import { ArrowLeft, HardDrive, ShieldCheck, UserRound } from 'lucide-react';
import { Link, useRoute } from 'wouter';
import { useEffect, useState } from 'react';
import { SilaPageHeader, StatusBadge, QueryError } from '@/components/sila-ui';

type UserDetail = {
  user: {
    id: string; displayName: string; email: string; status: string; applications: string[];
    organizations: Array<{ name: string }>; units: Array<{ name: string }>; roles: Array<{ key: string; name: string; applicationScope?: string }>;
    firstName?: string; lastName?: string; mobileNumber?: string; mustChangePassword?: boolean;
  };
  effectiveAuthorizations: Array<{ key: string; name: string; module: string; riskLevel: string; decision: string; source: string }>;
  overrides: Array<{ key: string; name: string; decision: string; source: string }>;
  storage: { provider: string; status: string; destinationUrl?: string; connectionMessage?: string; externalTransferEnabled: boolean };
};

export default function UserDetailPage() {
  const [, params] = useRoute('/admin/users/:userId');
  const [detail, setDetail] = useState<UserDetail | null>(null);
  const [error, setError] = useState('');
  useEffect(() => {
    if (!params?.userId) return;
    fetch(`/api/v1/access/users/${params.userId}`, { credentials: 'include' })
      .then(async (response) => {
        if (!response.ok) throw new Error((await response.json().catch(() => ({})))?.message ?? 'User could not be loaded.');
        return response.json();
      })
      .then(setDetail)
      .catch((reason: Error) => setError(reason.message));
  }, [params?.userId]);

  if (error) return <><SilaPageHeader eyebrow="Administration" title="User access" /><QueryError onRetry={() => window.location.reload()} /></>;
  if (!detail) return <div className="sila-card sila-loading-row" data-testid="user-detail-loading" />;
  const { user } = detail;
  return (
    <>
      <SilaPageHeader eyebrow="Administration / Users" title={user.displayName} description={user.email} actions={<Link href="/admin/users" className="sila-button"><ArrowLeft size={14} /> Back to users</Link>} />
      <div className="sila-detail-grid">
        <div>
          <section className="sila-card sila-detail-section"><h2 className="sila-detail-section__title"><span><UserRound size={15} /> Profile</span><StatusBadge value={user.status} /></h2><div className="sila-meta-grid"><div><span className="sila-meta-label">Name</span><span className="sila-meta-value">{user.firstName || user.displayName} {user.lastName || ''}</span></div><div><span className="sila-meta-label">Email</span><span className="sila-meta-value">{user.email}</span></div><div><span className="sila-meta-label">Mobile</span><span className="sila-meta-value">{user.mobileNumber || '—'}</span></div><div><span className="sila-meta-label">Applications</span><span className="sila-meta-value">{user.applications.join(' · ')}</span></div><div><span className="sila-meta-label">Password state</span><span className="sila-meta-value">{user.mustChangePassword ? 'Must change temporary password' : 'Set'}</span></div></div></section>
          <section className="sila-card sila-detail-section"><h2 className="sila-detail-section__title"><span><ShieldCheck size={15} /> Roles and authorization</span><span>{detail.effectiveAuthorizations.length} effective entries</span></h2><div className="sila-side-list">{user.roles.map((role) => <div className="sila-side-list__row" key={role.key}><span className="sila-side-list__label">{role.name}</span><span className="sila-side-list__value">{role.applicationScope ?? 'BOTH'}</span></div>)}</div><div className="sila-mini-table"><div className="sila-mini-table__head"><span>Authorization</span><span>Decision</span></div>{detail.effectiveAuthorizations.map((authorization) => <div className="sila-mini-table__row" key={`${authorization.key}-${authorization.source}`}><span><strong>{authorization.name}</strong><small>{authorization.module} · {authorization.source}</small></span><StatusBadge value={authorization.decision} /></div>)}</div></section>
        </div>
        <aside>
          <section className="sila-card sila-detail-section"><h2 className="sila-detail-section__title"><span>Scope</span></h2><div className="sila-side-list"><div className="sila-side-list__row"><span className="sila-side-list__label">Organizations</span><span className="sila-side-list__value">{user.organizations.map((item) => item.name).join(', ') || '—'}</span></div><div className="sila-side-list__row"><span className="sila-side-list__label">Operating units</span><span className="sila-side-list__value">{user.units.map((item) => item.name).join(', ') || 'All units'}</span></div></div></section>
          <section className="sila-card sila-detail-section"><h2 className="sila-detail-section__title"><span><HardDrive size={15} /> Document storage</span></h2><div className="sila-side-list"><div className="sila-side-list__row"><span className="sila-side-list__label">Provider</span><span className="sila-side-list__value">{userStorageName(detail.storage.provider)}</span></div><div className="sila-side-list__row"><span className="sila-side-list__label">Status</span><span className="sila-side-list__value">{detail.storage.status}</span></div><div className="sila-side-list__row"><span className="sila-side-list__label">Destination</span><span className="sila-side-list__value">{detail.storage.destinationUrl || 'SILA ME managed storage'}</span></div></div>{detail.storage.provider === 'MICROSOFT' && detail.storage.status !== 'VALIDATED' && <p className="sila-pending-note">Microsoft connection requires backend validation before transfer is enabled.</p>}</section>
        </aside>
      </div>
    </>
  );
}

function userStorageName(provider: string) {
  return provider === 'MICROSOFT' ? 'Microsoft' : provider === 'GOOGLE' ? 'Google' : provider === 'OTHER' ? 'Other' : 'None';
}