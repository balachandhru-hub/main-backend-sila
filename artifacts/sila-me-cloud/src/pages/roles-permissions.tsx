import { ShieldCheck } from 'lucide-react';
import { useMemo } from 'react';
import { getGetAccessPermissionsQueryKey, getGetAccessRolesQueryKey, useGetAccessPermissions, useGetAccessRoles } from '@workspace/api-client-react';
import { SilaDataTable, SilaPageHeader, StatusBadge, QueryError } from '@/components/sila-ui';

type PermissionView = { id: string; key: string; name: string; description?: string; module?: string; applicationScope?: string; riskLevel?: string; isSystemAuthorization?: boolean };
type RoleView = { id: string; key: string; name: string; description?: string; isSystem: boolean; applicationScope?: string; permissions: PermissionView[] };

export default function RolesPermissionsPage() {
  const request = { credentials: 'include' as const };
  const roles = useGetAccessRoles({ request, query: { queryKey: getGetAccessRolesQueryKey(), retry: false } });
  const permissions = useGetAccessPermissions({ request, query: { queryKey: getGetAccessPermissionsQueryKey(), retry: false } });
  const grouped = useMemo(() => {
    const map = new Map<string, PermissionView[]>();
    (permissions.data as unknown as PermissionView[] | undefined ?? []).forEach((permission) => {
      const key = permission.module ?? 'Other';
      map.set(key, [...(map.get(key) ?? []), permission]);
    });
    return [...map.entries()].sort(([left], [right]) => left.localeCompare(right));
  }, [permissions.data]);
  if (roles.isError || permissions.isError) return <><SilaPageHeader eyebrow="Administration" title="Roles & Permissions" /><QueryError onRetry={() => { roles.refetch(); permissions.refetch(); }} /></>;
  return (
    <>
      <SilaPageHeader eyebrow="Administration" title="Roles & Permissions" description="System roles provide default authorization packages. User-level DENY overrides remain authoritative across multiple role grants." />
      <div className="sila-kpi-grid"><div className="sila-card sila-kpi"><span className="sila-kpi__label">System roles</span><span className="sila-kpi__value">{roles.data?.filter((role) => role.isSystem).length ?? 0}</span><span className="sila-kpi__note">Seeded idempotently</span></div><div className="sila-card sila-kpi"><span className="sila-kpi__label">Authorizations</span><span className="sila-kpi__value">{permissions.data?.length ?? 0}</span><span className="sila-kpi__note">Metadata-driven</span></div><div className="sila-card sila-kpi"><span className="sila-kpi__label">High risk</span><span className="sila-kpi__value">{permissions.data?.filter((permission) => ['HIGH', 'CRITICAL'].includes((permission as unknown as PermissionView).riskLevel ?? '')).length ?? 0}</span><span className="sila-kpi__note">Review before assignment</span></div></div>
      <div className="sila-detail-grid"><SilaDataTable loading={roles.isPending} empty={!roles.isPending && !(roles.data?.length)} emptyTitle="No roles configured"><table className="sila-table"><thead><tr><th>Role</th><th>Application</th><th>Authorizations</th><th>Type</th></tr></thead><tbody>{(roles.data as unknown as RoleView[] ?? []).map((role) => <tr key={role.id}><td><strong>{role.name}</strong><span className="sila-table__secondary">{role.key}</span></td><td>{role.applicationScope ?? 'BOTH'}</td><td>{role.permissions.length}</td><td><StatusBadge value={role.isSystem ? 'SYSTEM' : 'CUSTOM'} /></td></tr>)}</tbody></table></SilaDataTable><section className="sila-card sila-detail-section"><h2 className="sila-detail-section__title"><span><ShieldCheck size={15} /> Authorization catalog</span><span>{grouped.length} modules</span></h2><div className="sila-permission-catalog">{grouped.map(([module, items]) => <div key={module}><h3>{module}</h3>{items.slice(0, 12).map((permission) => <div className="sila-permission-row" key={permission.id}><span><strong>{permission.name}</strong><small>{permission.key} · {permission.applicationScope ?? 'BOTH'}</small></span><StatusBadge value={permission.riskLevel ?? 'LOW'} /></div>)}</div>)}</div></section></div>
    </>
  );
}