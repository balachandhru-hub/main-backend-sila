import { Link } from 'wouter';
import { UserPlus, ShieldCheck, KeyRound, Cloud, Smartphone, HardDrive, AlertTriangle } from 'lucide-react';
import { useEffect, useMemo, useState, type FormEvent } from 'react';
import { customFetch, getGetAccessContextQueryKey, getGetAccessRolesQueryKey, getGetAccessUsersQueryKey, useGetAccessContext, useGetAccessRoles, useGetAccessUsers } from '@workspace/api-client-react';
import { SilaPageHeader, SilaDataTable, StatusBadge, QueryError } from '@/components/sila-ui';
import { useAccessContext } from '@/components/sila-layout';

type AppKind = 'CLOUD' | 'MOBILE';
type PermissionView = { id: string; key: string; name: string; module?: string; riskLevel?: string; applicationScope?: string };
type RoleView = { id: string; key: string; name: string; description?: string | null; applicationScope?: string; permissions: PermissionView[] };
type StorageConnectionView = {
  id: string; provider: string; name: string; connectionStatus: string;
  siteIdentifier?: string | null; driveIdentifier?: string | null; folderIdentifier?: string | null;
  displayUrl?: string | null; displayName?: string | null; validatedAt?: string | null;
};
type FormState = {
  firstName: string; lastName: string; email: string; mobileNumber: string; applications: AppKind[];
  organizationId: string; unitIds: string[]; roleIds: string[]; passwordMode: 'generated' | 'manual'; password: string;
  storageProvider: 'NONE' | 'MICROSOFT' | 'GOOGLE' | 'OTHER'; storageDestinationUrl: string;
  storageConnectionId: string; storageSiteIdentifier: string; storageDriveIdentifier: string; storageFolderIdentifier: string;
  storageFolderPath: string;
};

const request = { credentials: 'include' as const };
const emptyForm: FormState = {
  firstName: '', lastName: '', email: '', mobileNumber: '', applications: ['CLOUD'],
  organizationId: '', unitIds: [], roleIds: [], passwordMode: 'generated', password: '',
  storageProvider: 'NONE', storageDestinationUrl: '', storageConnectionId: '', storageSiteIdentifier: '',
  storageDriveIdentifier: '', storageFolderIdentifier: '', storageFolderPath: '',
};

function appScope(role: RoleView) {
  return role.applicationScope ?? 'BOTH';
}

function canRunIn(role: RoleView, applications: AppKind[]) {
  return appScope(role) === 'BOTH' || applications.includes(appScope(role) as AppKind);
}

function riskIsHigh(risk?: string) {
  return risk === 'HIGH' || risk === 'CRITICAL';
}

function apiErrorMessage(reason: unknown, fallback: string) {
  if (reason && typeof reason === 'object' && 'data' in reason) {
    const data = (reason as { data?: unknown }).data;
    if (data && typeof data === 'object' && 'message' in data && typeof data.message === 'string') {
      return data.message;
    }
  }
  return reason instanceof Error ? reason.message : fallback;
}

export default function UsersPage() {
  const { accessContext } = useAccessContext();
  const users = useGetAccessUsers({ request, query: { queryKey: getGetAccessUsersQueryKey(), retry: false } });
  const rolesQuery = useGetAccessRoles({ request, query: { queryKey: getGetAccessRolesQueryKey(), retry: false } });
  const contextQuery = useGetAccessContext({ request, query: { queryKey: getGetAccessContextQueryKey(), retry: false } });
  const [open, setOpen] = useState(false);
  const [form, setForm] = useState<FormState>(emptyForm);
  const [granted, setGranted] = useState<string[]>([]);
  const [denied, setDenied] = useState<string[]>([]);
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');
  const [temporaryPassword, setTemporaryPassword] = useState('');
  const [microsoftStatus, setMicrosoftStatus] = useState('AUTHENTICATION_REQUIRED');
  const [microsoftBusy, setMicrosoftBusy] = useState(false);
  const [microsoftConnections, setMicrosoftConnections] = useState<StorageConnectionView[]>([]);
  const roles = (rolesQuery.data ?? []) as unknown as RoleView[];
  const organizations = accessContext?.organizations ?? contextQuery.data?.organizations ?? [];
  const units = accessContext?.units ?? contextQuery.data?.units ?? [];

  useEffect(() => {
    if (!form.organizationId && organizations[0]) setForm((current) => ({ ...current, organizationId: organizations[0].id }));
  }, [form.organizationId, organizations]);

  useEffect(() => {
    if (!form.organizationId) return;
    customFetch<StorageConnectionView[]>(`/api/v1/access/storage-connections?organizationId=${encodeURIComponent(form.organizationId)}`, { credentials: 'include', responseType: 'json' })
      .then((connections) => setMicrosoftConnections(connections.filter((item) =>
        item.provider === 'MICROSOFT' && item.connectionStatus === 'CONNECTED')))
      .catch((reason) => {
        setMicrosoftConnections([]);
        setError(apiErrorMessage(reason, 'Storage connections could not be loaded.'));
      });
  }, [form.organizationId]);

  useEffect(() => {
    const params = new URLSearchParams(window.location.search);
    const draftId = params.get('draft');
    if (!draftId || params.get('microsoft') !== 'connected') return;
    setOpen(true);
    setMicrosoftBusy(true);
    customFetch<{
      draft?: {
        form?: Partial<FormState>;
        granted?: string[];
        denied?: string[];
      };
      connectionId?: string;
      connectionStatus?: string;
    }>(`/api/v1/integrations/microsoft/drafts/${draftId}`, { credentials: 'include', responseType: 'json' })
      .then((body) => {
        const saved = body?.draft ?? {};
        if (saved.form) setForm((current) => ({ ...current, ...saved.form, password: '' }));
        if (Array.isArray(saved.granted)) setGranted(saved.granted);
        if (Array.isArray(saved.denied)) setDenied(saved.denied);
        const connectionId = body?.connectionId;
        if (connectionId) setForm((current) => ({ ...current, storageConnectionId: connectionId }));
        setMicrosoftStatus(body.connectionStatus ?? 'AUTHENTICATED');
        setMessage('Microsoft authorization completed. Select the SharePoint destination and run the read/write test.');
        window.history.replaceState({}, '', '/admin/users');
      })
      .catch((reason) => setError(apiErrorMessage(reason, 'The Microsoft provisioning draft could not be restored.')))
      .finally(() => setMicrosoftBusy(false));
  }, []);

  const availableRoles = useMemo(
    () => roles.filter((role) => canRunIn(role, form.applications)),
    [roles, form.applications],
  );
  const selectedPermissions = useMemo(() => {
    const permissions = new Map<string, PermissionView>();
    roles.filter((role) => form.roleIds.includes(role.id)).forEach((role) => role.permissions.forEach((permission) => permissions.set(permission.id, permission)));
    return [...permissions.values()].sort((a, b) => `${a.module}:${a.name}`.localeCompare(`${b.module}:${b.name}`));
  }, [roles, form.roleIds]);
  const highRisk = form.storageProvider !== 'NONE' ||
    selectedPermissions.some((permission) => riskIsHigh(permission.riskLevel) && (granted.includes(permission.id) || denied.includes(permission.id))) ||
    roles.some((role) => form.roleIds.includes(role.id) && ['SUPER_ADMIN', 'PLATFORM_ADMIN', 'CUSTOMER_ADMIN'].includes(role.key));

  const updateForm = <K extends keyof FormState>(key: K, value: FormState[K]) => setForm((current) => ({ ...current, [key]: value }));
  const toggleApplication = (application: AppKind) => {
    const applications = form.applications.includes(application)
      ? form.applications.filter((item) => item !== application)
      : [...form.applications, application];
    if (applications.length) {
      updateForm('applications', applications);
      setForm((current) => ({ ...current, applications, roleIds: current.roleIds.filter((id) => {
        const role = roles.find((candidate) => candidate.id === id);
        return role ? canRunIn(role, applications) : false;
      }) }));
    }
  };
  const toggleUnit = (unitId: string) => updateForm('unitIds', form.unitIds.includes(unitId) ? form.unitIds.filter((id) => id !== unitId) : [...form.unitIds, unitId]);
  const toggleRole = (roleId: string) => updateForm('roleIds', form.roleIds.includes(roleId) ? form.roleIds.filter((id) => id !== roleId) : [...form.roleIds, roleId]);
  const setDecision = (permissionId: string, decision: 'DEFAULT' | 'GRANT' | 'DENY') => {
    setGranted((current) => decision === 'GRANT' ? [...new Set([...current.filter((id) => id !== permissionId), permissionId])] : current.filter((id) => id !== permissionId));
    setDenied((current) => decision === 'DENY' ? [...new Set([...current.filter((id) => id !== permissionId), permissionId])] : current.filter((id) => id !== permissionId));
  };

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    setError('');
    setMessage('');
    if (!form.applications.length || !form.organizationId || !form.firstName.trim() || !form.lastName.trim()) {
      setError('Complete the name, organization, and at least one application.');
      return;
    }
    if (form.passwordMode === 'manual' && form.password.length < 8) {
      setError('Manual temporary passwords must be at least 8 characters.');
      return;
    }
    if (form.storageProvider === 'MICROSOFT' &&
      (!form.storageConnectionId || microsoftStatus !== 'CONNECTED')) {
      setError('Connect Microsoft and validate SharePoint read/write access before creating this user.');
      return;
    }
    if (highRisk && !window.confirm('This change includes high-risk access. Confirm that the scope and authorization package are correct.')) return;
    try {
      const body = await customFetch<{ temporaryPassword?: string; message?: string; errors?: unknown } | null>('/api/v1/access/users', {
        method: 'POST',
        credentials: 'include',
        headers: { 'content-type': 'application/json' },
        responseType: 'json',
        body: JSON.stringify({
          displayName: `${form.firstName.trim()} ${form.lastName.trim()}`,
          firstName: form.firstName.trim(),
          lastName: form.lastName.trim(),
          email: form.email.trim(),
          mobileNumber: form.mobileNumber.trim() || null,
          password: form.passwordMode === 'manual' ? form.password : null,
          generateTemporaryPassword: form.passwordMode === 'generated',
          applications: form.applications,
          organizationId: form.organizationId,
          organizationUnitIds: form.unitIds,
          roleIds: form.roleIds,
          grantedAuthorizationIds: granted,
          deniedAuthorizationIds: denied,
          storageProvider: form.storageProvider,
          storageConnectionId: form.storageConnectionId || null,
          storageSiteIdentifier: form.storageSiteIdentifier || null,
          storageDriveIdentifier: form.storageDriveIdentifier || null,
          storageFolderIdentifier: form.storageFolderIdentifier || null,
          storageFolderPath: form.storageFolderPath || null,
          storageDestinationUrl: form.storageDestinationUrl.trim() || null,
          externalStorageTransferEnabled: form.storageProvider !== 'NONE',
        }),
      });
      setTemporaryPassword(body?.temporaryPassword ?? '');
    } catch (reason) {
      setError(apiErrorMessage(reason, 'User could not be created.'));
      return;
    }
    setMessage('User created. Share the temporary password through a secure channel; it must be changed on first sign-in.');
    setForm(emptyForm);
    setGranted([]);
    setDenied([]);
    setMicrosoftStatus('AUTHENTICATION_REQUIRED');
    setOpen(false);
    users.refetch();
  };

  const connectMicrosoft = async () => {
    if (!form.organizationId) {
      setError('Select an organization before connecting Microsoft.');
      return;
    }
    setError('');
    setMicrosoftBusy(true);
    try {
      const body = await customFetch<{ authorizationUrl?: string }>('/api/v1/integrations/microsoft/connect', {
        method: 'POST',
        credentials: 'include',
        headers: { 'content-type': 'application/json' },
        responseType: 'json',
        body: JSON.stringify({
          organizationId: form.organizationId,
          returnUrl: '/admin/users',
          draft: { form: { ...form, password: '' }, granted, denied },
        }),
      });
      if (!body.authorizationUrl) {
        setError('Microsoft integration did not return an authorization URL.');
        return;
      }
      window.location.assign(body.authorizationUrl);
    } catch (reason) {
      setError(apiErrorMessage(reason, 'Microsoft integration is not configured on the backend.'));
    } finally {
      setMicrosoftBusy(false);
    }
  };

  const validateMicrosoft = async () => {
    if (!form.storageConnectionId || !form.storageDestinationUrl.trim()) {
      setError('Enter a SharePoint site URL before running validation.');
      return;
    }
    setError('');
    setMicrosoftBusy(true);
    try {
      const body = await customFetch<{ siteId?: string; driveId?: string; folderId?: string }>(
        `/api/v1/integrations/microsoft/connections/${form.storageConnectionId}/validate`,
        {
          method: 'POST',
          credentials: 'include',
          headers: { 'content-type': 'application/json' },
          responseType: 'json',
          body: JSON.stringify({
            siteUrl: form.storageDestinationUrl.trim(),
            driveId: form.storageDriveIdentifier || null,
            folderId: form.storageFolderIdentifier || null,
            folderPath: form.storageFolderPath || null,
          }),
        },
      );
      setForm((current) => ({
        ...current,
        storageSiteIdentifier: body.siteId ?? current.storageSiteIdentifier,
        storageDriveIdentifier: body.driveId ?? current.storageDriveIdentifier,
        storageFolderIdentifier: body.folderId ?? current.storageFolderIdentifier,
      }));
      setMicrosoftStatus('CONNECTED');
      setMessage('Microsoft SharePoint read/write access validated. The user can now be created.');
    } catch (reason) {
      setMicrosoftStatus('VALIDATION_FAILED');
      setError(apiErrorMessage(reason, 'Microsoft could not validate the SharePoint destination.'));
    } finally {
      setMicrosoftBusy(false);
    }
  };

  const chooseMicrosoftConnection = (connectionId: string) => {
    const connection = microsoftConnections.find((item) => item.id === connectionId);
    if (!connection) {
      updateForm('storageConnectionId', '');
      setMicrosoftStatus('AUTHENTICATION_REQUIRED');
      return;
    }
    setForm((current) => ({
      ...current,
      storageConnectionId: connection.id,
      storageSiteIdentifier: connection.siteIdentifier ?? '',
      storageDriveIdentifier: connection.driveIdentifier ?? '',
      storageFolderIdentifier: connection.folderIdentifier ?? '',
      storageDestinationUrl: connection.displayUrl ?? current.storageDestinationUrl,
    }));
    setMicrosoftStatus(connection.connectionStatus);
    setMessage('Using the validated organization SharePoint destination.');
  };

  return (
    <>
      <SilaPageHeader eyebrow="Administration" title="Users" description="Manage people, application access, role packages, operating scope, and document storage destinations." actions={<button className="sila-button sila-button--primary" type="button" onClick={() => { setOpen((value) => !value); setMessage(''); setError(''); }} data-testid="button-toggle-create-user"><UserPlus size={14} /> Add user</button>} />
      {message && <div className="sila-card sila-action-banner" role="status" data-testid="status-user-action"><ShieldCheck size={16} /><span>{message}{temporaryPassword && <><br /><strong>Temporary password: {temporaryPassword}</strong></>}</span></div>}
      {error && <div className="sila-card sila-action-banner sila-action-banner--error" role="alert"><AlertTriangle size={16} /><span>{error}</span></div>}
      {open && <form className="sila-user-provisioning sila-card" onSubmit={submit} data-testid="form-create-user">
        <div className="sila-provisioning__heading"><div><p className="sila-page-header__eyebrow">Secure provisioning</p><h2>Add user</h2><p>Access is evaluated by the backend from the selected applications, roles, scope, and explicit overrides.</p></div><span className="sila-status sila-status--blue"><KeyRound size={12} /> Must change password</span></div>
        <section className="sila-provisioning__section"><h3>1. Personal information</h3><div className="sila-form-grid"><div className="sila-form-field"><label htmlFor="new-user-first-name">First name</label><input id="new-user-first-name" value={form.firstName} onChange={(event) => updateForm('firstName', event.target.value)} required data-testid="input-new-user-first-name" /></div><div className="sila-form-field"><label htmlFor="new-user-last-name">Last name</label><input id="new-user-last-name" value={form.lastName} onChange={(event) => updateForm('lastName', event.target.value)} required data-testid="input-new-user-last-name" /></div><div className="sila-form-field"><label htmlFor="new-user-email">Email</label><input id="new-user-email" type="email" value={form.email} onChange={(event) => updateForm('email', event.target.value)} required data-testid="input-new-user-email" /></div><div className="sila-form-field"><label htmlFor="new-user-mobile">Mobile number</label><input id="new-user-mobile" value={form.mobileNumber} onChange={(event) => updateForm('mobileNumber', event.target.value)} placeholder="+971 ..." data-testid="input-new-user-mobile" /></div><div className="sila-form-field sila-form-field--full"><label htmlFor="new-user-name">Display name</label><input id="new-user-name" value={`${form.firstName} ${form.lastName}`.trim()} readOnly data-testid="input-new-user-name" /></div></div></section>
        <section className="sila-provisioning__section"><h3>2. Application access</h3><div className="sila-choice-grid"><label className="sila-choice"><input type="checkbox" checked={form.applications.includes('CLOUD')} onChange={() => toggleApplication('CLOUD')} /><Cloud size={15} /><span><strong>Cloud</strong><small>Administration and browser workspace</small></span></label><label className="sila-choice"><input type="checkbox" checked={form.applications.includes('MOBILE')} onChange={() => toggleApplication('MOBILE')} /><Smartphone size={15} /><span><strong>Mobile</strong><small>Receiving and field operations</small></span></label></div></section>
        <section className="sila-provisioning__section"><h3>3. Organization and operating scope</h3><div className="sila-form-grid"><div className="sila-form-field"><label htmlFor="new-user-organization">Organization</label><select id="new-user-organization" value={form.organizationId} onChange={(event) => updateForm('organizationId', event.target.value)} required data-testid="select-new-user-organization">{organizations.map((organization) => <option key={organization.id} value={organization.id}>{organization.name}</option>)}</select></div><div className="sila-form-field sila-form-field--full"><label>Properties / operating units</label><div className="sila-checkbox-list">{units.filter((unit) => unit.organizationId === form.organizationId).map((unit) => <label key={unit.id}><input type="checkbox" checked={form.unitIds.includes(unit.id)} onChange={() => toggleUnit(unit.id)} /> {unit.name} <small>{unit.kind}</small></label>)}{!units.length && <span className="sila-muted-copy">No operating units are available in this organization.</span>}</div></div></div></section>
        <section className="sila-provisioning__section"><h3>4. Roles and effective authorization preview</h3><div className="sila-role-grid">{availableRoles.map((role) => <label className={`sila-role-card ${form.roleIds.includes(role.id) ? 'sila-role-card--selected' : ''}`} key={role.id}><input type="checkbox" checked={form.roleIds.includes(role.id)} onChange={() => toggleRole(role.id)} /><span><strong>{role.name}</strong><small>{appScope(role)} · {role.permissions.length} default authorizations</small></span></label>)}{!availableRoles.length && <span className="sila-muted-copy">No role is available for the selected applications.</span>}</div>{selectedPermissions.length > 0 && <div className="sila-preview-table"><div className="sila-preview-table__heading"><strong>Effective authorization preview</strong><span>Role grants are defaults. Explicit DENY wins over every role grant.</span></div>{selectedPermissions.map((permission) => <div className="sila-preview-row" key={permission.id}><div><strong>{permission.name}</strong><small>{permission.module} · {permission.riskLevel ?? 'LOW'}</small></div><select value={denied.includes(permission.id) ? 'DENY' : granted.includes(permission.id) ? 'GRANT' : 'DEFAULT'} onChange={(event) => setDecision(permission.id, event.target.value as 'DEFAULT' | 'GRANT' | 'DENY')}><option value="DEFAULT">Role default</option><option value="GRANT">Explicit grant</option><option value="DENY">Explicit deny</option></select></div>)}</div>}</section>
        <section className="sila-provisioning__section"><h3>5. Temporary password</h3><div className="sila-choice-grid"><label className="sila-choice"><input type="radio" checked={form.passwordMode === 'generated'} onChange={() => updateForm('passwordMode', 'generated')} /><KeyRound size={15} /><span><strong>Generate securely</strong><small>Shown once after creation</small></span></label><label className="sila-choice"><input type="radio" checked={form.passwordMode === 'manual'} onChange={() => updateForm('passwordMode', 'manual')} /><span><strong>Enter manually</strong><small>Must be changed at first sign-in</small></span></label></div>{form.passwordMode === 'manual' && <div className="sila-form-field" style={{ marginTop: 14 }}><label htmlFor="new-user-password">Temporary password</label><input id="new-user-password" type="password" minLength={8} value={form.password} onChange={(event) => updateForm('password', event.target.value)} required data-testid="input-new-user-password" /></div>}</section>
         <section className="sila-provisioning__section"><h3>6. Document storage destination</h3><div className="sila-form-grid"><div className="sila-form-field"><label htmlFor="new-user-storage">Provider</label><select id="new-user-storage" value={form.storageProvider} onChange={(event) => { const provider = event.target.value as FormState['storageProvider']; updateForm('storageProvider', provider); if (provider !== 'MICROSOFT') setMicrosoftStatus('AUTHENTICATION_REQUIRED'); }}><option value="NONE">None — retain in SILA ME</option><option value="MICROSOFT">Microsoft SharePoint / OneDrive</option><option value="GOOGLE">Google Drive</option><option value="OTHER">Other provider</option></select></div>{form.storageProvider === 'MICROSOFT' && microsoftConnections.length > 0 && <div className="sila-form-field"><label htmlFor="new-user-storage-connection">Validated organization destination</label><select id="new-user-storage-connection" value={form.storageConnectionId} onChange={(event) => chooseMicrosoftConnection(event.target.value)}><option value="">Choose a validated destination</option>{microsoftConnections.map((connection) => <option key={connection.id} value={connection.id}>{connection.name}{connection.displayName ? ` — ${connection.displayName}` : ''}</option>)}</select></div>}{form.storageProvider !== 'NONE' && <div className="sila-form-field"><label htmlFor="new-user-storage-url">SharePoint site URL</label><input id="new-user-storage-url" value={form.storageDestinationUrl} onChange={(event) => updateForm('storageDestinationUrl', event.target.value)} placeholder="https://company.sharepoint.com/sites/..." /></div>}</div>{form.storageProvider === 'MICROSOFT' && <div className="sila-storage-connection"><p className="sila-pending-note"><HardDrive size={14} /> Microsoft status: <strong>{microsoftStatus}</strong>. A typed URL never counts as connected.</p><div className="sila-form-grid"><div className="sila-form-field"><label htmlFor="new-user-storage-drive">Library ID (optional)</label><input id="new-user-storage-drive" value={form.storageDriveIdentifier} onChange={(event) => updateForm('storageDriveIdentifier', event.target.value)} placeholder="Default: Documents" /></div><div className="sila-form-field"><label htmlFor="new-user-storage-folder">Folder path (optional)</label><input id="new-user-storage-folder" value={form.storageFolderPath} onChange={(event) => updateForm('storageFolderPath', event.target.value)} placeholder="Invoices/2026" /></div></div><div className="sila-provisioning__actions sila-provisioning__actions--inline"><button className="sila-button" type="button" onClick={connectMicrosoft} disabled={microsoftBusy || !form.organizationId}>Connect Microsoft</button><button className="sila-button" type="button" onClick={validateMicrosoft} disabled={microsoftBusy || !form.storageConnectionId}>{microsoftBusy ? 'Testing…' : 'Test read/write access'}</button></div></div>}</section>
        <div className="sila-provisioning__actions"><span className="sila-muted-copy">{highRisk ? 'High-risk confirmation will be requested before saving.' : 'Review scope before saving.'}</span><button className="sila-button" type="button" onClick={() => setOpen(false)} data-testid="button-cancel-create-user">Cancel</button><button className="sila-button sila-button--primary" type="submit" disabled={users.isFetching || !organizations.length} data-testid="button-submit-create-user">Create user</button></div>
      </form>}
      {users.isError ? <QueryError onRetry={() => users.refetch()} /> : <SilaDataTable loading={users.isPending} empty={!users.isPending && !(users.data?.length)} emptyTitle="No accessible users" emptyDescription="Users with Cloud access will appear here."><table className="sila-table"><thead><tr><th>User</th><th>Applications</th><th>Roles</th><th>Scope</th><th>Status</th></tr></thead><tbody>{(users.data ?? []).map((user) => <tr key={user.id}><td><Link className="sila-table__primary" href={`/admin/users/${user.id}`}><strong>{user.displayName}</strong></Link><span className="sila-table__secondary">{user.email}</span></td><td>{user.applications.join(' · ')}</td><td>{user.roles.map((role) => role.name).join(', ') || '—'}</td><td>{user.units.map((item) => item.name).join(', ') || user.organizations.map((item) => item.name).join(', ') || '—'}</td><td><StatusBadge value={user.status} /></td></tr>)}</tbody></table></SilaDataTable>}
    </>
  );
}