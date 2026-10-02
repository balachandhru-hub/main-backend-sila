import { useMemo, useState } from 'react';
import { Plus, RefreshCw, Search } from 'lucide-react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { customFetch, useGetAccessContext, getGetAccessContextQueryKey } from '@workspace/api-client-react';
import { SilaPageHeader, SilaDataTable, StatusBadge, QueryError } from '@/components/sila-ui';
import { MasterDataExcelPanel } from '@/components/master-data-excel-panel';

type Supplier = {
  id: string; supplierCode: string; name: string; legalName?: string | null; taxNumber?: string | null;
  searchName?: string | null; businessPartnerId?: string | null; trn?: string | null;
  email?: string | null; phone?: string | null; entityCode: string; country?: string | null;
  city?: string | null; postalCode?: string | null; street?: string | null; currency?: string | null;
  isBlocked: boolean; isDeleted: boolean; isActive: boolean; status: string; aliases: string[];
  updatedAt: string;
};

const request = { credentials: 'include' as const };
async function api<T>(url: string, init?: RequestInit): Promise<T> {
  return customFetch<T>(url, { ...request, ...init, responseType: 'json' });
}

export default function SuppliersPage() {
  const context = useGetAccessContext({ request, query: { queryKey: getGetAccessContextQueryKey(), retry: false } });
  const organizationId = context.data?.organizations[0]?.id;
  const client = useQueryClient();
  const [search, setSearch] = useState('');
  const [entityCode, setEntityCode] = useState('');
  const [status, setStatus] = useState('');
  const [showAdd, setShowAdd] = useState(false);
  const [form, setForm] = useState({ supplierCode: '', name: '', businessPartnerId: '', legalName: '', taxNumber: '', trn: '', city: '', postalCode: '', street: '', currency: '', entityCode: 'DEFAULT', aliases: '' });
  const query = useQuery({
    queryKey: ['supplier-master', organizationId, entityCode, search, status],
    queryFn: () => api<Supplier[]>(`/api/v1/master-data/suppliers?organizationId=${organizationId}&entityCode=${encodeURIComponent(entityCode)}&query=${encodeURIComponent(search)}&status=${encodeURIComponent(status)}`),
    enabled: Boolean(organizationId),
    retry: false,
  });
  const save = useMutation({
    mutationFn: () => api<Supplier>(`/api/v1/master-data/suppliers?organizationId=${organizationId}`, {
      method: 'PUT', headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ ...form, aliases: form.aliases.split(',').map(item => item.trim()).filter(Boolean), status: 'ACTIVE', isBlocked: false, isDeleted: false }),
    }),
    onSuccess: () => { setShowAdd(false); setForm({ supplierCode: '', name: '', businessPartnerId: '', legalName: '', taxNumber: '', trn: '', city: '', postalCode: '', street: '', currency: '', entityCode: 'DEFAULT', aliases: '' }); client.invalidateQueries({ queryKey: ['supplier-master', organizationId] }); },
  });
  const rows = useMemo(() => query.data ?? [], [query.data]);
  return <>
    <SilaPageHeader eyebrow="Master data" title="Supplier master" description="Authoritative supplier identity used by invoice review, PO selection, and ERP receiving." actions={<button className="sila-button sila-button--primary" type="button" onClick={() => setShowAdd(value => !value)}><Plus size={14} /> Add supplier</button>} />
    {showAdd && <section className="sila-card" style={{ padding: 20, marginBottom: 18 }}><div className="sila-form-grid">
      <label className="sila-form-field"><span>Supplier code</span><input className="sila-input" value={form.supplierCode} onChange={event => setForm({ ...form, supplierCode: event.target.value })} /></label>
      <label className="sila-form-field"><span>Name</span><input className="sila-input" value={form.name} onChange={event => setForm({ ...form, name: event.target.value })} /></label>
       <label className="sila-form-field"><span>Business partner ID</span><input className="sila-input" value={form.businessPartnerId} onChange={event => setForm({ ...form, businessPartnerId: event.target.value })} /></label>
       <label className="sila-form-field"><span>Legal name</span><input className="sila-input" value={form.legalName} onChange={event => setForm({ ...form, legalName: event.target.value })} /></label>
      <label className="sila-form-field"><span>TRN</span><input className="sila-input" value={form.taxNumber} onChange={event => setForm({ ...form, taxNumber: event.target.value })} /></label>
       <label className="sila-form-field"><span>Separate TRN</span><input className="sila-input" value={form.trn} onChange={event => setForm({ ...form, trn: event.target.value })} /></label>
      <label className="sila-form-field"><span>Entity</span><input className="sila-input" value={form.entityCode} onChange={event => setForm({ ...form, entityCode: event.target.value })} /></label>
       <label className="sila-form-field"><span>City</span><input className="sila-input" value={form.city} onChange={event => setForm({ ...form, city: event.target.value })} /></label>
       <label className="sila-form-field"><span>Postal code</span><input className="sila-input" value={form.postalCode} onChange={event => setForm({ ...form, postalCode: event.target.value })} /></label>
       <label className="sila-form-field"><span>Street</span><input className="sila-input" value={form.street} onChange={event => setForm({ ...form, street: event.target.value })} /></label>
       <label className="sila-form-field"><span>Currency</span><input className="sila-input" value={form.currency} onChange={event => setForm({ ...form, currency: event.target.value })} /></label>
      <label className="sila-form-field sila-form-field--wide"><span>Aliases (comma separated)</span><input className="sila-input" value={form.aliases} onChange={event => setForm({ ...form, aliases: event.target.value })} /></label>
    </div><div className="sila-integration-actions"><button className="sila-button sila-button--primary" type="button" disabled={save.isPending || !form.supplierCode || !form.name} onClick={() => save.mutate()}>{save.isPending ? 'Saving…' : 'Save supplier'}</button></div></section>}
    <MasterDataExcelPanel organizationId={organizationId} entityCode={entityCode} kind="SUPPLIERS" onImported={() => query.refetch()} />
    <div className="sila-filter-row"><input className="sila-input" value={search} onChange={event => setSearch(event.target.value)} placeholder="Search code, name, TRN, or alias" aria-label="Search suppliers" /><input className="sila-input" value={entityCode} onChange={event => setEntityCode(event.target.value)} placeholder="Entity code" aria-label="Filter suppliers by entity" /><select className="sila-select" value={status} onChange={event => setStatus(event.target.value)} aria-label="Filter suppliers by status"><option value="">All statuses</option><option value="ACTIVE">Active</option><option value="INACTIVE">Inactive</option><option value="BLOCKED">Blocked</option></select><button className="sila-button" type="button" onClick={() => query.refetch()}><Search size={14} /> Search</button><button className="sila-button" type="button" onClick={() => query.refetch()}><RefreshCw size={14} /> Refresh</button></div>
    {query.isError ? <QueryError onRetry={() => query.refetch()} /> : <SilaDataTable loading={query.isPending} empty={!query.isPending && rows.length === 0} emptyTitle="No suppliers found" emptyDescription="Import or add an authoritative supplier before matching invoices."><table className="sila-table"><thead><tr><th>Supplier</th><th>Entity</th><th>TRN</th><th>Location</th><th>Aliases</th><th>Status</th><th>Updated</th></tr></thead><tbody>{rows.map(row => <tr key={row.id}><td><strong>{row.supplierCode}</strong><span className="sila-table__secondary">{row.name}</span></td><td>{row.entityCode}</td><td>{row.trn ?? row.taxNumber ?? '—'}</td><td>{[row.city, row.country].filter(Boolean).join(', ') || '—'}</td><td>{row.aliases.length ? row.aliases.join(', ') : '—'}</td><td><StatusBadge value={row.isBlocked || row.isDeleted || !row.isActive ? 'BLOCKED' : row.status} /></td><td>{new Date(row.updatedAt).toLocaleDateString()}</td></tr>)}</tbody></table></SilaDataTable>}
  </>;
}