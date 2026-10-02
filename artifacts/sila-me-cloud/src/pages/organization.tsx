import { Building2, Plus, Upload } from 'lucide-react';
import { useMemo, useRef, useState } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import {
  getGetAccessContextQueryKey,
  useCreateOrganization,
  useCreateOrganizationUnit,
  useDeleteOrganizationLogo,
  useUploadOrganizationLogo,
} from '@workspace/api-client-react';
import { SilaPageHeader, SilaDataTable } from '@/components/sila-ui';
import { useAccessContext } from '@/components/sila-layout';
import { silaLogoUrl, useCustomerLogoUrl } from '@/components/branding';
import { isSuperAdmin } from '@/lib/branding';

const request = { credentials: 'include' as const };

export default function OrganizationPage() {
  const { accessContext } = useAccessContext();
  const queryClient = useQueryClient();
  const createOrganization = useCreateOrganization({ request });
  const createUnit = useCreateOrganizationUnit({ request });
  const uploadLogo = useUploadOrganizationLogo({ request });
  const deleteLogo = useDeleteOrganizationLogo({ request });
  const fileRef = useRef<HTMLInputElement>(null);
  const [orgName, setOrgName] = useState('');
  const [orgCode, setOrgCode] = useState('');
  const [unitName, setUnitName] = useState('');
  const [unitCode, setUnitCode] = useState('');
  const [message, setMessage] = useState('');
  const [selectedOrgId, setSelectedOrgId] = useState('');
  const refresh = () => queryClient.invalidateQueries({ queryKey: getGetAccessContextQueryKey() });
  const canManageLogo = isSuperAdmin(accessContext?.roles);
  const organizations = accessContext?.organizations ?? [];
  const selectedOrganization = useMemo(
    () => organizations.find((item) => item.id === selectedOrgId) ?? organizations[0] ?? null,
    [organizations, selectedOrgId],
  );
  const previewLogo = useCustomerLogoUrl(selectedOrganization?.id, selectedOrganization?.logoUrl);

  const submitOrg = (event: React.FormEvent) => {
    event.preventDefault();
    createOrganization.mutate(
      { data: { name: orgName, code: orgCode, kind: 'CUSTOMER' } },
      {
        onSuccess: () => {
          setMessage('Organization created.');
          setOrgName('');
          setOrgCode('');
          refresh();
        },
        onError: () => setMessage('Organization could not be created.'),
      },
    );
  };

  const submitUnit = (event: React.FormEvent) => {
    event.preventDefault();
    const organization = accessContext?.organizations[0];
    if (!organization) return;
    createUnit.mutate(
      { organizationId: organization.id, data: { name: unitName, code: unitCode, kind: 'STORE' } },
      {
        onSuccess: () => {
          setMessage('Operating unit created.');
          setUnitName('');
          setUnitCode('');
          refresh();
        },
        onError: () => setMessage('Operating unit could not be created.'),
      },
    );
  };

  const onLogoSelected = (file?: File | null) => {
    if (!selectedOrganization || !file) return;
    uploadLogo.mutate(
      { organizationId: selectedOrganization.id, data: { file } },
      {
        onSuccess: () => {
          setMessage('Customer logo updated. It will appear across Cloud and Mobile.');
          refresh();
        },
        onError: () => setMessage('Customer logo could not be uploaded.'),
      },
    );
  };

  return (
    <>
      <SilaPageHeader
        eyebrow="Administration"
        title="Organization structure"
        description="Keep hospitality groups, properties, outlets, kitchens, and stores aligned to the access model."
        actions={<span className="sila-status sila-status--blue" data-testid="status-organization-scope"><Building2 size={12} /> Access context</span>}
      />
      {message ? <p style={{ margin: '-12px 0 18px', color: 'var(--sila-blue)', fontSize: 12 }} role="status" data-testid="status-organization-action">{message}</p> : null}

      <div className="sila-kpi-grid">
        <article className="sila-card sila-kpi"><span className="sila-kpi__label">Organizations</span><strong className="sila-kpi__value" data-testid="metric-organizations">{accessContext?.organizations.length ?? '—'}</strong></article>
        <article className="sila-card sila-kpi"><span className="sila-kpi__label">Operating units</span><strong className="sila-kpi__value" data-testid="metric-operating-units">{accessContext?.units.length ?? '—'}</strong></article>
        <article className="sila-card sila-kpi"><span className="sila-kpi__label">Resolved roles</span><strong className="sila-kpi__value" data-testid="metric-resolved-roles">{accessContext?.roles.length ?? '—'}</strong></article>
        <article className="sila-card sila-kpi"><span className="sila-kpi__label">Permissions</span><strong className="sila-kpi__value" data-testid="metric-permissions">{accessContext?.permissions.length ?? '—'}</strong></article>
      </div>

      {canManageLogo ? (
        <section className="sila-card sila-detail-section" style={{ marginBottom: 18 }} data-testid="customer-logo-panel">
          <h2 className="sila-detail-section__title">
            <span style={{ color: 'var(--sila-ink)', fontWeight: 700 }}>Customer logo</span>
            <span>SUPER_ADMIN</span>
          </h2>
          <div className="sila-branding-panel">
            <div className="sila-form-field">
              <label htmlFor="logo-organization">Organization</label>
              <select
                id="logo-organization"
                className="sila-select"
                value={selectedOrganization?.id ?? ''}
                onChange={(event) => setSelectedOrgId(event.target.value)}
                data-testid="select-logo-organization"
              >
                {organizations.map((organization) => (
                  <option key={organization.id} value={organization.id}>{organization.name}</option>
                ))}
              </select>
            </div>
            <div className="sila-branding-preview">
              <img
                src={previewLogo ?? silaLogoUrl}
                alt={previewLogo ? 'Customer logo preview' : 'SILA fallback logo'}
                data-testid="img-customer-logo-preview"
              />
            </div>
            <p style={{ margin: 0, color: 'var(--sila-muted)', fontSize: 12, lineHeight: 1.5 }}>
              This logo appears in Cloud and Mobile headers. SILA stays at the bottom of the apps. If no customer logo is set, SILA is shown instead.
            </p>
            <div className="sila-form-actions">
              <input
                ref={fileRef}
                type="file"
                accept="image/png,image/jpeg,image/webp,image/svg+xml"
                hidden
                onChange={(event) => onLogoSelected(event.target.files?.[0])}
                data-testid="input-customer-logo-file"
              />
              <button
                className="sila-button sila-button--primary"
                type="button"
                disabled={!selectedOrganization || uploadLogo.isPending}
                onClick={() => fileRef.current?.click()}
                data-testid="button-upload-customer-logo"
              >
                <Upload size={14} /> {uploadLogo.isPending ? 'Uploading…' : 'Upload customer logo'}
              </button>
              <button
                className="sila-button"
                type="button"
                disabled={!selectedOrganization?.logoUrl || deleteLogo.isPending}
                onClick={() => {
                  if (!selectedOrganization) return;
                  deleteLogo.mutate(
                    { organizationId: selectedOrganization.id },
                    {
                      onSuccess: () => {
                        setMessage('Customer logo removed. SILA will appear until a new logo is uploaded.');
                        refresh();
                      },
                      onError: () => setMessage('Customer logo could not be removed.'),
                    },
                  );
                }}
                data-testid="button-remove-customer-logo"
              >
                Remove logo
              </button>
            </div>
          </div>
        </section>
      ) : null}

      <div className="sila-detail-grid">
        <section className="sila-card sila-detail-section">
          <h2 className="sila-detail-section__title"><span style={{ color: 'var(--sila-ink)', fontWeight: 700 }}>Organizations</span><span>{accessContext?.organizations.length ?? 0} records</span></h2>
          <SilaDataTable empty={!accessContext?.organizations.length} emptyTitle="No organizations in scope" emptyDescription="Create an organization to establish the first Cloud operating context.">
            <table className="sila-table">
              <thead><tr><th>Name</th><th>Code</th><th>Kind</th><th>Logo</th><th>Status</th></tr></thead>
              <tbody>
                {(accessContext?.organizations ?? []).map((organization) => (
                  <tr key={organization.id}>
                    <td><strong>{organization.name}</strong></td>
                    <td>{organization.code}</td>
                    <td>{organization.kind}</td>
                    <td>{organization.logoUrl ? 'Custom' : 'SILA fallback'}</td>
                    <td>{organization.status}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </SilaDataTable>
        </section>
        <section className="sila-card sila-detail-section">
          <h2 className="sila-detail-section__title"><span style={{ color: 'var(--sila-ink)', fontWeight: 700 }}>Operating units</span><span>{accessContext?.units.length ?? 0} records</span></h2>
          <SilaDataTable empty={!accessContext?.units.length} emptyTitle="No operating units in scope" emptyDescription="Add a property, outlet, kitchen, or store to start routing operations.">
            <table className="sila-table">
              <thead><tr><th>Name</th><th>Code</th><th>Kind</th><th>Status</th></tr></thead>
              <tbody>
                {(accessContext?.units ?? []).map((unit) => (
                  <tr key={unit.id}>
                    <td><strong>{unit.name}</strong></td>
                    <td>{unit.code}</td>
                    <td>{unit.kind}</td>
                    <td>{unit.status}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </SilaDataTable>
        </section>
      </div>

      <section className="sila-card sila-detail-section" style={{ marginTop: 18 }}>
        <h2 className="sila-detail-section__title"><span style={{ color: 'var(--sila-ink)', fontWeight: 700 }}>Add to structure</span></h2>
        <div className="sila-form-grid">
          <form onSubmit={submitOrg} className="sila-form-grid" style={{ gridColumn: '1 / -1' }}>
            <div className="sila-form-field"><label htmlFor="organization-name">Organization name</label><input id="organization-name" value={orgName} onChange={(event) => setOrgName(event.target.value)} required data-testid="input-organization-name" /></div>
            <div className="sila-form-field"><label htmlFor="organization-code">Organization code</label><input id="organization-code" value={orgCode} onChange={(event) => setOrgCode(event.target.value)} required data-testid="input-organization-code" /></div>
            <div className="sila-form-actions"><button className="sila-button sila-button--primary" type="submit" disabled={createOrganization.isPending} data-testid="button-create-organization"><Plus size={14} /> Create organization</button></div>
          </form>
          <form onSubmit={submitUnit} className="sila-form-grid" style={{ gridColumn: '1 / -1', borderTop: '1px solid var(--sila-line)', paddingTop: 18 }}>
            <div className="sila-form-field"><label htmlFor="unit-name">Operating unit name</label><input id="unit-name" value={unitName} onChange={(event) => setUnitName(event.target.value)} required data-testid="input-unit-name" /></div>
            <div className="sila-form-field"><label htmlFor="unit-code">Operating unit code</label><input id="unit-code" value={unitCode} onChange={(event) => setUnitCode(event.target.value)} required data-testid="input-unit-code" /></div>
            <div className="sila-form-actions"><button className="sila-button sila-button--primary" type="submit" disabled={createUnit.isPending || !(accessContext?.organizations.length)} data-testid="button-create-operating-unit"><Plus size={14} /> Create operating unit</button></div>
          </form>
        </div>
      </section>
    </>
  );
}
