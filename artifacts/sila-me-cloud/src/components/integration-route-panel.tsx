import { useEffect, useMemo, useState } from 'react';
import { Plus, RefreshCw } from 'lucide-react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { customFetch } from '@workspace/api-client-react';
import { StatusBadge } from '@/components/sila-ui';
import { API_TYPES, SYSTEM_LABELS } from '@/lib/integration-designer';

const request = { credentials: 'include' as const };

type CompanyCode = { companyCode: string; companyName: string; status: string; isDeleted: boolean };
type RouteMatch = { id: string; name: string; processType: string; systemKind: string; status: string };
type RouteRow = {
  id: string;
  processType: string;
  systemKind: string;
  apiIntegrationConfigurationId: string;
  configurationName: string;
  appliesToAllCompanyCodes: boolean;
  companyCodes: string[];
  isActive: boolean;
};

async function api<T>(url: string, init?: RequestInit): Promise<T> {
  return customFetch<T>(url, { ...request, ...init, responseType: 'json' });
}

function errorText(error: unknown, fallback = 'Save failed.') {
  if (error && typeof error === 'object' && 'data' in error) {
    const data = (error as { data?: { code?: string; message?: string } | null }).data;
    return [data?.code, data?.message].filter(Boolean).join(' — ') || (error instanceof Error ? error.message : fallback);
  }
  return error instanceof Error ? error.message : fallback;
}

const empty = () => ({
  processType: 'POST_GRN',
  systemKind: 'SAP_ARIBA',
  apiIntegrationConfigurationId: '',
  companyCodes: [] as string[],
  appliesToAll: false,
  isActive: true,
});

export function IntegrationRoutePanel({ organizationId }: { organizationId: string }) {
  const queryClient = useQueryClient();
  const [editing, setEditing] = useState<string | 'new' | null>(null);
  const [form, setForm] = useState(empty());
  const [notice, setNotice] = useState<{ tone: 'good' | 'bad'; text: string }>();
  const [pendingDelete, setPendingDelete] = useState<RouteRow | null>(null);

  const routes = useQuery({
    queryKey: ['integration-routes', organizationId],
    queryFn: () => api<RouteRow[]>(`/api/v1/integrations/routes?organizationId=${organizationId}`),
  });
  const companies = useQuery({
    queryKey: ['company-codes', organizationId],
    queryFn: () => api<CompanyCode[]>(`/api/v1/master-data/company-codes?organizationId=${organizationId}&status=ACTIVE`),
  });
  const matches = useQuery({
    queryKey: ['integration-route-configs', organizationId, form.processType, form.systemKind],
    queryFn: () => api<RouteMatch[]>(`/api/v1/integrations/routes/configurations?organizationId=${organizationId}&processType=${form.processType}&systemKind=${form.systemKind}`),
    enabled: Boolean(editing),
  });

  useEffect(() => {
    if (!matches.data?.some(item => item.id === form.apiIntegrationConfigurationId)) {
      setForm(current => ({ ...current, apiIntegrationConfigurationId: matches.data?.[0]?.id ?? '' }));
    }
  }, [matches.data, form.apiIntegrationConfigurationId]);

  const save = useMutation({
    mutationFn: () => api<RouteRow>(
      editing && editing !== 'new'
        ? `/api/v1/integrations/routes/${editing}?organizationId=${organizationId}`
        : `/api/v1/integrations/routes?organizationId=${organizationId}`,
      {
        method: editing && editing !== 'new' ? 'PUT' : 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          processType: form.processType,
          systemKind: form.systemKind,
          apiIntegrationConfigurationId: form.apiIntegrationConfigurationId,
          companyCodes: form.appliesToAll ? [] : form.companyCodes,
          appliesToAllCompanyCodes: form.appliesToAll,
          isActive: form.isActive,
        }),
      },
    ),
    onSuccess: () => {
      setEditing(null);
      setNotice({ tone: 'good', text: 'Integration route saved.' });
      queryClient.invalidateQueries({ queryKey: ['integration-routes', organizationId] });
    },
    onError: error => setNotice({ tone: 'bad', text: errorText(error) }),
  });

  const remove = useMutation({
    mutationFn: (id: string) => api<{ deleted: boolean }>(`/api/v1/integrations/routes/${id}?organizationId=${organizationId}`, { method: 'DELETE' }),
    onSuccess: (_result, id) => {
      setPendingDelete(null);
      if (editing === id) setEditing(null);
      setNotice({ tone: 'good', text: 'Integration route deleted.' });
      queryClient.setQueryData<RouteRow[]>(['integration-routes', organizationId], current => (current ?? []).filter(item => item.id !== id));
      queryClient.invalidateQueries({ queryKey: ['integration-routes', organizationId] });
    },
    onError: error => {
      setPendingDelete(null);
      const data = error && typeof error === 'object' && 'data' in error
        ? (error as { data?: { message?: string } | null }).data
        : null;
      setNotice({ tone: 'bad', text: typeof data?.message === 'string' && data.message ? data.message : errorText(error, 'The integration route could not be deleted.') });
    },
  });

  const companyOptions = useMemo(() => (companies.data ?? []).filter(item => !item.isDeleted), [companies.data]);

  function startCreate() {
    setForm(empty());
    setEditing('new');
    setNotice(undefined);
  }
  function startEdit(row: RouteRow) {
    setForm({
      processType: row.processType,
      systemKind: row.systemKind,
      apiIntegrationConfigurationId: row.apiIntegrationConfigurationId,
      companyCodes: row.appliesToAllCompanyCodes ? [] : row.companyCodes,
      appliesToAll: row.appliesToAllCompanyCodes,
      isActive: row.isActive,
    });
    setEditing(row.id);
    setNotice(undefined);
  }
  function toggleCode(code: string) {
    setForm(current => {
      if (current.appliesToAll) return current;
      const next = current.companyCodes.includes(code)
        ? current.companyCodes.filter(item => item !== code)
        : [...current.companyCodes, code];
      return { ...current, companyCodes: next };
    });
  }

  return (
    <section className="sila-card" style={{ padding: 18 }}>
      <div className="sila-data-actions" style={{ marginBottom: 14 }}>
        <button className="sila-button sila-button--primary" type="button" onClick={startCreate}><Plus size={14} /> Add route</button>
        <button className="sila-button" type="button" onClick={() => routes.refetch()}><RefreshCw size={14} /> Refresh</button>
      </div>
      {notice && <p className={`sila-muted-copy sila-integration-notice sila-integration-notice--${notice.tone}`} role="status">{notice.text}</p>}
      {editing && (
        <form className="sila-integration-form" style={{ marginBottom: 18 }} onSubmit={event => { event.preventDefault(); save.mutate(); }}>
          <div className="sila-form-grid">
            <label className="sila-form-field"><span>API type *</span>
              <select className="sila-select" value={form.processType} onChange={event => setForm(current => ({ ...current, processType: event.target.value, apiIntegrationConfigurationId: '' }))}>
                {API_TYPES.map(value => <option key={value} value={value}>{value}</option>)}
              </select>
            </label>
            <label className="sila-form-field"><span>System *</span>
              <select className="sila-select" value={form.systemKind} onChange={event => setForm(current => ({ ...current, systemKind: event.target.value, apiIntegrationConfigurationId: '' }))}>
                {Object.entries(SYSTEM_LABELS).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
              </select>
            </label>
            <label className="sila-form-field"><span>API configuration *</span>
              <select className="sila-select" value={form.apiIntegrationConfigurationId} onChange={event => setForm(current => ({ ...current, apiIntegrationConfigurationId: event.target.value }))}>
                <option value="">Select a matching configuration</option>
                {(matches.data ?? []).map(item => <option key={item.id} value={item.id}>{item.name}</option>)}
              </select>
            </label>
            <label className="sila-form-field"><span>Status</span>
              <select className="sila-select" value={form.isActive ? 'ACTIVE' : 'INACTIVE'} onChange={event => setForm(current => ({ ...current, isActive: event.target.value === 'ACTIVE' }))}>
                <option value="ACTIVE">ACTIVE</option>
                <option value="INACTIVE">INACTIVE</option>
              </select>
            </label>
          </div>
          <div className="sila-form-field" style={{ marginTop: 12 }}>
            <span>Company code *</span>
            <label className="sila-checkbox" style={{ display: 'flex', marginTop: 8 }}>
              <input type="checkbox" checked={form.appliesToAll} onChange={event => setForm(current => ({ ...current, appliesToAll: event.target.checked, companyCodes: event.target.checked ? [] : current.companyCodes }))} />
              ALL
            </label>
            <div style={{ display: 'flex', flexWrap: 'wrap', gap: 10, marginTop: 8 }}>
              {companyOptions.map(item => (
                <label className="sila-checkbox" key={item.companyCode}>
                  <input type="checkbox" disabled={form.appliesToAll} checked={!form.appliesToAll && form.companyCodes.includes(item.companyCode)} onChange={() => toggleCode(item.companyCode)} />
                  {item.companyCode}
                </label>
              ))}
            </div>
            {companyOptions.length === 0 && <p className="sila-muted-copy">No company codes in master data yet.</p>}
          </div>
          <div className="sila-integration-actions" style={{ marginTop: 14 }}>
            <button className="sila-button" type="button" onClick={() => setEditing(null)}>Cancel</button>
            <button className="sila-button sila-button--primary" type="submit" disabled={save.isPending || !form.apiIntegrationConfigurationId || (!form.appliesToAll && form.companyCodes.length === 0)}>{save.isPending ? 'Saving…' : 'Save route'}</button>
          </div>
        </form>
      )}
      <div className="sila-table-wrap">
        <table className="sila-table">
          <thead><tr><th>API Type</th><th>Company Code</th><th>System</th><th>API Configuration</th><th>Status</th><th>Actions</th></tr></thead>
          <tbody>
            {(routes.data ?? []).length === 0 ? <tr><td colSpan={6} className="sila-muted-copy">No integration routes yet.</td></tr> : (routes.data ?? []).map(row => (
              <tr key={row.id}>
                <td>{row.processType}</td>
                <td>{row.appliesToAllCompanyCodes ? 'ALL' : row.companyCodes.join(', ')}</td>
                <td>{SYSTEM_LABELS[row.systemKind] ?? row.systemKind}</td>
                <td>{row.configurationName}</td>
                <td><StatusBadge value={row.isActive ? 'ACTIVE' : 'INACTIVE'} /></td>
                <td>
                  <div className="sila-integration-list__actions">
                    <button className="sila-button sila-button--quiet" type="button" onClick={() => startEdit(row)}>Edit</button>
                    <button className="sila-button sila-button--quiet" type="button" onClick={() => setPendingDelete(row)}>Delete</button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      {pendingDelete ? (
        <div style={{ position: 'fixed', inset: 0, background: 'rgba(38,58,77,.35)', display: 'grid', placeItems: 'center', zIndex: 50 }}>
          <div style={{ background: '#fff', border: '1px solid #c5d8e8', padding: 24, width: 'min(480px, 94vw)' }}>
            <h2 style={{ marginTop: 0 }}>Delete this integration route?</h2>
            <p>This removes only the routing rule and its company code relationships. API configurations, masters, and transaction data are not deleted.</p>
            <p><strong>API Type:</strong> {pendingDelete.processType}</p>
            <p><strong>Company Code:</strong> {pendingDelete.appliesToAllCompanyCodes ? 'ALL' : pendingDelete.companyCodes.join(', ')}</p>
            <p><strong>System:</strong> {SYSTEM_LABELS[pendingDelete.systemKind] ?? pendingDelete.systemKind}</p>
            <p><strong>API Configuration:</strong> {pendingDelete.configurationName}</p>
            <div style={{ display: 'flex', gap: 8, justifyContent: 'flex-end' }}>
              <button type="button" className="sila-button sila-button--quiet" onClick={() => setPendingDelete(null)}>Cancel</button>
              <button type="button" className="sila-button" disabled={remove.isPending} onClick={() => remove.mutate(pendingDelete.id)}>{remove.isPending ? 'Deleting…' : 'Delete Route'}</button>
            </div>
          </div>
        </div>
      ) : null}
    </section>
  );
}
