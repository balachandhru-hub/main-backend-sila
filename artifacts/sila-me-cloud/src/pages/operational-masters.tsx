import { useMemo, useState } from 'react';
import { Pencil, Plus, RefreshCw, Search, Trash2 } from 'lucide-react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { customFetch, useGetAccessContext, getGetAccessContextQueryKey } from '@workspace/api-client-react';
import { SilaPageHeader, SilaDataTable, StatusBadge, QueryError } from '@/components/sila-ui';
import { MasterDataExcelPanel, type OperationalMasterKind } from '@/components/master-data-excel-panel';

const request = { credentials: 'include' as const };
async function api<T>(url: string, init?: RequestInit): Promise<T> {
  return customFetch<T>(url, { ...request, ...init, responseType: 'json' });
}

type Field = { key: string; label: string; required?: boolean };

type MasterRow = {
  id: string;
  status: string;
  isDeleted: boolean;
  updatedAt: string;
  [key: string]: string | boolean;
};

export function OperationalMasterPage({
  title,
  description,
  kind,
  path,
  fields,
}: {
  title: string;
  description: string;
  kind: OperationalMasterKind;
  path: string;
  fields: Field[];
}) {
  const context = useGetAccessContext({ request, query: { queryKey: getGetAccessContextQueryKey(), retry: false } });
  const organizations = context.data?.organizations ?? [];
  const organizationId = organizations.find((item) => item.code.toUpperCase() === 'FIVE')?.id
    ?? organizations.find((item) => item.name === 'Five Hotels and Resorts')?.id
    ?? organizations[0]?.id;
  const client = useQueryClient();
  const [search, setSearch] = useState('');
  const [status, setStatus] = useState('');
  const [editing, setEditing] = useState<MasterRow | null | 'new'>(null);
  const empty = Object.fromEntries(fields.map((field) => [field.key, ''])) as Record<string, string>;
  const [form, setForm] = useState<Record<string, string>>(empty);
  const queryKey = ['operational-master', path, organizationId, search, status];
  const query = useQuery({
    queryKey,
    queryFn: () => api<MasterRow[]>(`/api/v1/master-data/${path}?organizationId=${organizationId}&query=${encodeURIComponent(search)}&status=${encodeURIComponent(status)}`),
    enabled: Boolean(organizationId),
    retry: false,
  });
  const save = useMutation({
    mutationFn: () => {
      const id = editing && editing !== 'new' ? `/${editing.id}` : '';
      return api(`/api/v1/master-data/${path}${id}?organizationId=${organizationId}`, {
        method: 'PUT', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ ...form, status: form.status || 'ACTIVE' }),
      });
    },
    onSuccess: () => { setEditing(null); setForm(empty); client.invalidateQueries({ queryKey: ['operational-master', path, organizationId] }); },
  });
  const action = useMutation({
    mutationFn: ({ id, verb }: { id: string; verb: 'suspend' | 'activate' | 'delete' }) =>
      api(`/api/v1/master-data/${path}/${id}${verb === 'delete' ? '' : `/${verb}`}?organizationId=${organizationId}`, { method: verb === 'delete' ? 'DELETE' : 'POST' }),
    onSuccess: () => client.invalidateQueries({ queryKey: ['operational-master', path, organizationId] }),
  });
  const rows = useMemo(() => query.data ?? [], [query.data]);
  function openEdit(row?: MasterRow) {
    if (!row) { setEditing('new'); setForm(empty); return; }
    setEditing(row);
    setForm(Object.fromEntries(fields.map((field) => [field.key, String(row[field.key] ?? '')])));
  }
  return (
    <>
      <SilaPageHeader eyebrow="Administration" title={title} description={description} actions={<button className="sila-button sila-button--primary" type="button" onClick={() => openEdit()}><Plus size={14} /> Add</button>} />
      {editing && (
        <section className="sila-card" style={{ padding: 20, marginBottom: 18 }}>
          <div className="sila-form-grid">
            {fields.map((field) => (
              <label className="sila-form-field" key={field.key}>
                <span>{field.label}</span>
                {field.key === 'status' ? (
                  <select className="sila-select" value={form.status || 'ACTIVE'} onChange={(event) => setForm({ ...form, status: event.target.value })}>
                    <option value="ACTIVE">Active</option>
                    <option value="INACTIVE">Suspended</option>
                  </select>
                ) : (
                  <input className="sila-input" value={form[field.key] ?? ''} onChange={(event) => setForm({ ...form, [field.key]: event.target.value })} />
                )}
              </label>
            ))}
          </div>
          <div className="sila-integration-actions" style={{ marginTop: 14 }}>
            <button className="sila-button" type="button" onClick={() => setEditing(null)}>Cancel</button>
            <button className="sila-button sila-button--primary" type="button" disabled={save.isPending || fields.some((field) => field.required && !form[field.key])} onClick={() => save.mutate()}>{save.isPending ? 'Saving…' : 'Save'}</button>
          </div>
        </section>
      )}
      <MasterDataExcelPanel organizationId={organizationId} recordKind={kind} onImported={() => query.refetch()} />
      <div className="sila-filter-row">
        <input className="sila-input" value={search} onChange={(event) => setSearch(event.target.value)} placeholder={`Search ${title.toLowerCase()}`} aria-label={`Search ${title}`} />
        <select className="sila-select" value={status} onChange={(event) => setStatus(event.target.value)} aria-label="Filter by status">
          <option value="">All statuses</option>
          <option value="ACTIVE">Active</option>
          <option value="INACTIVE">Suspended</option>
        </select>
        <button className="sila-button" type="button" onClick={() => query.refetch()}><Search size={14} /> Search</button>
        <button className="sila-button" type="button" onClick={() => query.refetch()}><RefreshCw size={14} /> Refresh</button>
      </div>
      {query.isError ? <QueryError onRetry={() => query.refetch()} /> : (
        <SilaDataTable loading={query.isPending} empty={!query.isPending && rows.length === 0} emptyTitle={`No ${title.toLowerCase()} yet`} emptyDescription="Add a row or upload the Excel template.">
          <table className="sila-table">
            <thead>
              <tr>
                {fields.filter((field) => field.key !== 'status').map((field) => <th key={field.key}>{field.label}</th>)}
                <th>Status</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {rows.map((row) => (
                <tr key={row.id}>
                  {fields.filter((field) => field.key !== 'status').map((field) => <td key={field.key}>{String(row[field.key] ?? '—')}</td>)}
                  <td><StatusBadge value={row.status === 'INACTIVE' ? 'SUSPENDED' : row.status} /></td>
                  <td>
                    <div className="sila-integration-actions">
                      <button className="sila-button" type="button" onClick={() => openEdit(row)}><Pencil size={13} /> Edit</button>
                      {row.status === 'ACTIVE'
                        ? <button className="sila-button" type="button" onClick={() => action.mutate({ id: row.id, verb: 'suspend' })}>Suspend</button>
                        : <button className="sila-button" type="button" onClick={() => action.mutate({ id: row.id, verb: 'activate' })}>Activate</button>}
                      <button className="sila-button sila-button--danger" type="button" onClick={() => action.mutate({ id: row.id, verb: 'delete' })}><Trash2 size={13} /> Delete</button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </SilaDataTable>
      )}
    </>
  );
}

export function CompanyCodesPage() {
  return <OperationalMasterPage title="Company code master" description="Company codes that can be assigned to suppliers and other operational masters." kind="COMPANY_CODES" path="company-codes" fields={[
    { key: 'companyCode', label: 'Company code', required: true },
    { key: 'companyName', label: 'Company name', required: true },
    { key: 'country', label: 'Country' },
    { key: 'status', label: 'Status' },
  ]} />;
}

export function PropertyMasterPage() {
  return <OperationalMasterPage title="Property master" description="Properties used by receiving, inventory, and supplier assignment." kind="PROPERTIES" path="properties" fields={[
    { key: 'propertyCode', label: 'Property code', required: true },
    { key: 'propertyName', label: 'Property name', required: true },
    { key: 'country', label: 'Country' },
    { key: 'companyCode', label: 'Company code' },
    { key: 'status', label: 'Status' },
  ]} />;
}
