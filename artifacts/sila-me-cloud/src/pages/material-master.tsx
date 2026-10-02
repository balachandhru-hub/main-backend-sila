import { useRef, useState } from 'react';
import { Download, Plus, Search, Trash2, UploadCloud } from 'lucide-react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { customFetch, getGetAccessContextQueryKey, useGetAccessContext } from '@workspace/api-client-react';
import { QueryError, SilaDataTable, SilaPageHeader, StatusBadge } from '@/components/sila-ui';
import { MaterialPricePanel, money, priceStatusLabel, unitPriceDisplay, type MaterialPriceRow } from '@/components/material-price-panel';

const request = { credentials: 'include' as const };
async function api<T>(url: string, init?: RequestInit): Promise<T> {
  return customFetch<T>(url, { ...request, ...init, responseType: 'json' });
}
function orgId(organizations: Array<{ id: string; code: string; name: string }> | undefined) {
  return organizations?.find((item) => item.code.toUpperCase() === 'FIVE')?.id
    ?? organizations?.find((item) => item.name === 'Five Hotels and Resorts')?.id
    ?? organizations?.[0]?.id;
}

const empty = {
  materialCode: '', name: '', description: '', materialGroup: '', materialType: '', category: '', baseUom: '',
  alternateUom: '', convFactor: '', convUnit: '', convValue: '', companyCode: '', valuationArea: '', valuationClass: '', priceControl: '', standardPrice: '',
  movingAveragePrice: '', unitCost: '', currency: '',
  inventoryItem: 'false', inventoryType: 'NON_STOCK', batchManaged: 'false', expiryManaged: 'false', shelfLifeDays: '', serialManaged: 'false',
};

function stringify(row: MaterialPriceRow): Record<string, string> {
  return {
    id: row.id,
    materialCode: row.materialCode ?? '',
    name: row.name || row.description || '',
    description: row.description || row.name || '',
    materialGroup: row.materialGroup ?? '',
    materialType: row.materialType ?? '',
    category: row.category ?? '',
    baseUom: row.baseUom ?? '',
    alternateUom: row.alternateUom ?? '',
    convFactor: row.convFactor == null ? '' : String(row.convFactor),
    convUnit: row.convUnit ?? '',
    convValue: row.convValue == null ? '' : String(row.convValue),
    companyCode: row.companyCode ?? '',
    valuationArea: row.valuationArea ?? '',
    valuationClass: row.valuationClass ?? '',
    priceControl: row.priceControl ?? '',
    standardPrice: row.standardPrice == null ? '' : String(row.standardPrice),
    movingAveragePrice: row.movingAveragePrice == null ? '' : String(row.movingAveragePrice),
    unitCost: row.unitCost == null ? '' : String(row.unitCost),
    currency: row.currency ?? '',
    inventoryItem: row.inventoryItem ? 'true' : 'false',
    inventoryType: row.inventoryType ?? 'NON_STOCK',
    batchManaged: row.batchManaged ? 'true' : 'false',
    expiryManaged: row.expiryManaged ? 'true' : 'false',
    shelfLifeDays: row.shelfLifeDays == null ? '' : String(row.shelfLifeDays),
    serialManaged: row.serialManaged ? 'true' : 'false',
  };
}
function mutationError(error: unknown) {
  return error instanceof Error ? error.message : 'The material request failed.';
}

export function RecipeMaterialsPage({ embedded = false }: { embedded?: boolean }) {
  const context = useGetAccessContext({ request, query: { queryKey: getGetAccessContextQueryKey(), retry: false } });
  const organizationId = orgId(context.data?.organizations);
  const client = useQueryClient();
  const [search, setSearch] = useState('');
  const [status, setStatus] = useState('');
  const [priceStatus, setPriceStatus] = useState('');
  const [editing, setEditing] = useState<Record<string, string> | null>(null);
  const [viewing, setViewing] = useState<MaterialPriceRow | null>(null);
  const [priceMaterialId, setPriceMaterialId] = useState<string | null>(null);
  const [creating, setCreating] = useState(false);
  const [erpOpen, setErpOpen] = useState(false);
  const [companyCode, setCompanyCode] = useState('');
  const [preview, setPreview] = useState<{ fileName: string; totalRows: number; validRows: number; invalidRows: number; newRows: number; changedRows: number; unchangedRows: number } | null>(null);
  const fileRef = useRef<HTMLInputElement>(null);
  const pendingFile = useRef<File | null>(null);
  const query = useQuery({
    queryKey: ['recipe-materials', organizationId, search, status, priceStatus],
    queryFn: () => api<MaterialPriceRow[]>(`/api/v1/recipe-management/materials?organizationId=${organizationId}&query=${encodeURIComponent(search)}&status=${encodeURIComponent(status)}&priceStatus=${encodeURIComponent(priceStatus)}`),
    enabled: Boolean(organizationId), retry: false,
  });
  const companies = useQuery({
    queryKey: ['company-codes', organizationId],
    queryFn: () => api<Array<{ companyCode: string; companyName: string }>>(`/api/v1/master-data/company-codes?organizationId=${organizationId}`),
    enabled: Boolean(organizationId && erpOpen), retry: false,
  });
  const route = useQuery({
    queryKey: ['material-erp-route', organizationId, companyCode],
    queryFn: () => api<{ systemKind: string; configurationName: string; lastSyncAt?: string; lastStatus?: string }>(`/api/v1/recipe-management/materials/erp-route?organizationId=${organizationId}&companyCode=${encodeURIComponent(companyCode)}`),
    enabled: Boolean(organizationId && companyCode), retry: false,
  });
  const approvals = useQuery({
    queryKey: ['material-approvals', organizationId],
    queryFn: () => api<Array<{ id: string; materialCode: string; event: string; source: string; level: number; status: string }>>(`/api/v1/recipe-management/materials/approvals?organizationId=${organizationId}`),
    enabled: Boolean(organizationId), retry: false,
  });
  function payload(row: Record<string, string>) {
    return {
      materialCode: row.materialCode, name: row.name || row.description, description: row.description || row.name, materialGroup: row.materialGroup, materialType: row.materialType,
      category: row.category, baseUom: row.baseUom, alternateUom: row.alternateUom,
      convFactor: row.convUnit || row.convValue ? Number(row.convFactor || '1') : null, convUnit: row.convUnit || null, convValue: row.convValue ? Number(row.convValue) : null,
      companyCode: row.companyCode, valuationArea: row.valuationArea,
      valuationClass: row.valuationClass, priceControl: row.priceControl, standardPrice: row.standardPrice ? Number(row.standardPrice) : null,
      movingAveragePrice: row.movingAveragePrice ? Number(row.movingAveragePrice) : null, unitCost: row.unitCost ? Number(row.unitCost) : null, currency: row.currency,
      inventoryItem: row.inventoryItem === 'true', inventoryType: row.inventoryType || 'NON_STOCK',
      batchManaged: row.batchManaged === 'true', expiryManaged: row.expiryManaged === 'true',
      shelfLifeDays: row.expiryManaged === 'true' && row.shelfLifeDays ? Number(row.shelfLifeDays) : null,
      serialManaged: row.serialManaged === 'true',
    };
  }
  const save = useMutation({
    mutationFn: () => {
      const row = editing ?? empty;
      return api(creating ? `/api/v1/recipe-management/materials?organizationId=${organizationId}` : `/api/v1/recipe-management/materials/${row.id}?organizationId=${organizationId}`, {
        method: creating ? 'POST' : 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(payload(row)),
      });
    },
    onSuccess: () => { setEditing(null); setCreating(false); client.invalidateQueries({ queryKey: ['recipe-materials', organizationId] }); client.invalidateQueries({ queryKey: ['material-approvals', organizationId] }); },
  });
  const remove = useMutation({
    mutationFn: (id: string) => api(`/api/v1/recipe-management/materials/${id}?organizationId=${organizationId}`, { method: 'DELETE' }),
    onSuccess: () => client.invalidateQueries({ queryKey: ['recipe-materials', organizationId] }),
  });
  const previewFile = useMutation({
    mutationFn: async (file: File) => {
      const body = new FormData();
      body.append('file', file);
      return api<NonNullable<typeof preview>>(`/api/v1/recipe-management/materials/import/preview?organizationId=${organizationId}`, { method: 'POST', body });
    },
    onSuccess: setPreview,
  });
  const commitFile = useMutation({
    mutationFn: async (file: File) => {
      const body = new FormData();
      body.append('file', file);
      return api(`/api/v1/recipe-management/materials/import?organizationId=${organizationId}`, { method: 'POST', body });
    },
    onSuccess: () => { setPreview(null); query.refetch(); client.invalidateQueries({ queryKey: ['material-approvals', organizationId] }); },
  });
  const pull = useMutation({
    mutationFn: () => api<{ recordsRead: number; newCount: number; changedCount: number; unchangedCount: number; failedCount: number; failures: string[] }>(`/api/v1/recipe-management/materials/erp-pull?organizationId=${organizationId}`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ companyCode }) }),
    onSuccess: () => { query.refetch(); route.refetch(); client.invalidateQueries({ queryKey: ['material-approvals', organizationId] }); },
  });
  const decide = useMutation({
    mutationFn: ({ id, verb }: { id: string; verb: 'approve' | 'reject' }) => api(`/api/v1/recipe-management/materials/approvals/${id}/${verb}?organizationId=${organizationId}`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ comment: verb }) }),
    onSuccess: () => { client.invalidateQueries({ queryKey: ['material-approvals', organizationId] }); query.refetch(); },
  });
  const rows = query.data ?? [];
  const fields: Array<[string, string]> = [
    ['materialCode', 'Material ID'], ['name', 'Material name'], ['description', 'Description'], ['materialType', 'Material type'],
    ['materialGroup', 'Material group'], ['category', 'Category'], ['baseUom', 'Base UOM'], ['alternateUom', 'Alternate UOM'],
    ['convFactor', 'ConvFactor'], ['convUnit', 'ConvUnit'], ['convValue', 'ConvValue'],
    ['companyCode', 'Company code'], ['valuationArea', 'Valuation area'], ['valuationClass', 'Valuation class'],
    ['priceControl', 'Price control'], ['standardPrice', 'Standard price'], ['movingAveragePrice', 'Moving average price'],
    ['unitCost', 'Unit cost'], ['currency', 'Currency'],
  ];
  return (
    <>
      {!embedded && (
        <SilaPageHeader eyebrow="Recipe Management" title="Material Master" description="Applicable Unit Price comes from Material valuation. Update Unit Price submits a CHANGE for approval and does not overwrite the approved price."
          actions={<><button className="sila-button sila-button--primary" type="button" onClick={() => { setCreating(true); setEditing({ ...empty }); }}><Plus size={14} /> Create Material</button>
            <button className="sila-button" type="button" onClick={() => setErpOpen(true)}>Pull from ERP</button>
            <button className="sila-button" type="button" onClick={() => fileRef.current?.click()}><UploadCloud size={14} /> Upload Excel</button>
            <a className="sila-button" href={`/api/v1/recipe-management/materials/export?organizationId=${organizationId}`}><Download size={14} /> Download</a>
            <a className="sila-button" href={`/api/v1/recipe-management/materials/template?organizationId=${organizationId}`}><Download size={14} /> Download Template</a></>} />
      )}
      {embedded && (
        <div className="sila-integration-actions" style={{ marginBottom: 14 }}>
          <button className="sila-button sila-button--primary" type="button" onClick={() => { setCreating(true); setEditing({ ...empty }); }}><Plus size={14} /> Create Material</button>
          <button className="sila-button" type="button" onClick={() => setErpOpen(true)}>Pull from ERP</button>
          <button className="sila-button" type="button" onClick={() => fileRef.current?.click()}><UploadCloud size={14} /> Upload Excel</button>
        </div>
      )}
      <input ref={fileRef} type="file" accept=".xlsx" hidden onChange={(event) => { const file = event.target.files?.[0]; if (file) { pendingFile.current = file; previewFile.mutate(file); } event.target.value = ''; }} />
      <div className="sila-filter-row">
        <input className="sila-input" value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Search materials" />
        <select className="sila-select" value={status} onChange={(event) => setStatus(event.target.value)}><option value="">All active status</option><option value="ACTIVE">Active</option><option value="INACTIVE">Inactive</option></select>
        <select className="sila-select" value={priceStatus} onChange={(event) => setPriceStatus(event.target.value)} data-testid="filter-price-status">
          <option value="">All price status</option>
          <option value="PRICE_APPROVED">Approved</option>
          <option value="PRICE_MISSING">Price Missing</option>
          <option value="PRICE_PENDING_APPROVAL">Pending Approval</option>
          <option value="PRICE_REJECTED">Rejected</option>
          <option value="PRICE_INVALID">Invalid</option>
          <option value="HAS_PRICE">Has Price</option>
          <option value="MISSING_PRICE">Missing Price</option>
        </select>
        <button className="sila-button" type="button" onClick={() => query.refetch()}><Search size={14} /> Search</button>
      </div>
      {query.isError ? <QueryError onRetry={() => query.refetch()} /> : (
        <SilaDataTable loading={query.isPending} empty={!query.isPending && rows.length === 0} emptyTitle="No materials" emptyDescription="Create, pull from ERP, or upload the template.">
          <table className="sila-table" data-testid="material-master-grid"><thead><tr>
            <th>Material ID</th><th>Description</th><th>Material Group</th><th>Base UOM</th><th>ConvFactor</th><th>ConvUnit</th><th>ConvValue</th><th>Unit Price</th><th>Price UOM</th><th>Currency</th><th>Price Status</th><th>Source</th><th>Last Updated</th><th className="sila-table__actions">Actions</th>
          </tr></thead>
            <tbody>{rows.map((row) => {
              const statusCode = row.priceStatus || (row.hasValidUnitPrice ? 'PRICE_APPROVED' : 'PRICE_MISSING');
              const approved = row.approvedUnitPrice ?? (row.hasValidUnitPrice ? row.unitCost ?? null : null);
              return (
              <tr key={row.id} onClick={() => setViewing(row)} style={{ cursor: 'pointer' }}><td>{row.materialCode}</td><td>{row.name}</td><td>{row.materialGroup}</td><td>{row.baseUom}</td>
                <td>{row.convFactor ?? ''}</td><td>{row.convUnit ?? ''}</td><td>{row.convValue ?? ''}</td>
                <td>{unitPriceDisplay(statusCode, approved, row.proposedUnitPrice ?? null, row.currency || 'AED', row.priceUom || row.baseUom)}</td>
                <td>{row.priceUom || row.baseUom}</td><td>{row.currency}</td>
                <td><StatusBadge value={row.priceStatusLabel || priceStatusLabel(statusCode)} /></td>
                <td>{row.source}</td><td>{row.updatedAt ? String(row.updatedAt).slice(0, 10) : ''}</td>
                <td className="sila-table__actions" onClick={(event) => event.stopPropagation()}>
                  <button className="sila-button sila-button--primary" type="button" data-testid={`button-update-unit-price-${row.materialCode}`} disabled={Boolean(row.hasPendingPriceChange)} onClick={() => setPriceMaterialId(row.id)}>Update Unit Price</button>
                  <button className="sila-button" type="button" onClick={() => setViewing(row)}>View</button>
                  <button className="sila-button" type="button" onClick={() => { setCreating(false); setEditing({ ...empty, ...stringify(row), name: row.name }); }}>Edit</button>
                  <button className="sila-button" type="button" onClick={() => remove.mutate(row.id)}><Trash2 size={14} /> Deactivate</button>
                </td></tr>
              );
            })}</tbody>
          </table>
        </SilaDataTable>
      )}
      {viewing && (
        <section className="sila-card" style={{ padding: 20, marginBottom: 18 }} data-testid="material-pricing-section">
          <h3>{viewing.materialCode} · {viewing.name}</h3>
          <h4>Pricing &amp; Valuation</h4>
          <p>Current approved price: {viewing.hasValidUnitPrice && viewing.unitCost != null ? `${money(viewing.unitCost, viewing.currency || 'AED')} / ${viewing.priceUom || viewing.baseUom}` : 'NONE'}</p>
          <p>Currency: {viewing.currency || 'AED'}</p>
          <p>Price UOM: {viewing.priceUom || viewing.baseUom}</p>
          <p>Conversion: {viewing.conversionText || (viewing.convUnit && viewing.convValue != null ? `${viewing.convFactor ?? 1} ${viewing.baseUom} = ${viewing.convValue} ${viewing.convUnit}` : '—')}</p>
          <p>ConvFactor: {viewing.convFactor ?? '—'}</p>
          <p>ConvUnit: {viewing.convUnit || '—'}</p>
          <p>ConvValue: {viewing.convValue ?? '—'}</p>
          <p>Valuation area: {viewing.valuationArea || '—'}</p>
          <p>Plant: {viewing.plant || viewing.valuationArea || '—'}</p>
          <p>Effective from: {viewing.priceEffectiveFrom ? viewing.priceEffectiveFrom.slice(0, 10) : '—'}</p>
          <p>Price status: {viewing.priceStatusLabel || priceStatusLabel(viewing.priceStatus)}</p>
          {viewing.hasPendingPriceChange && (
            <div>
              <h4>Pending price change</h4>
              <p>Current: {viewing.hasValidUnitPrice && viewing.unitCost != null ? money(viewing.unitCost, viewing.currency || 'AED') : 'NONE'}</p>
              <p>Proposed: {viewing.proposedUnitPrice == null ? '—' : `${money(viewing.proposedUnitPrice, viewing.currency || 'AED')} / ${viewing.priceUom || viewing.baseUom}`}</p>
              <p>Status: Pending Approval</p>
            </div>
          )}
          <div className="sila-integration-actions" style={{ marginTop: 14 }}>
            <button className="sila-button" type="button" onClick={() => setViewing(null)}>Close</button>
            <button className="sila-button sila-button--primary" type="button" data-testid="button-update-unit-price" disabled={Boolean(viewing.hasPendingPriceChange)} onClick={() => setPriceMaterialId(viewing.id)}>Update Unit Price</button>
          </div>
        </section>
      )}
      {erpOpen && (
        <section className="sila-card" style={{ padding: 20, marginBottom: 18 }}>
          <h3>Pull from ERP</h3>
          <div className="sila-form-grid">
            <label className="sila-form-field"><span>Company Code *</span>
              <select className="sila-select" value={companyCode} onChange={(event) => setCompanyCode(event.target.value)}>
                <option value="">Select</option>
                {companies.data?.map((item) => <option key={item.companyCode} value={item.companyCode}>{item.companyCode} {item.companyName}</option>)}
              </select>
            </label>
            <label className="sila-form-field"><span>ERP / System</span><input className="sila-input" readOnly value={route.data?.systemKind ?? ''} /></label>
            <label className="sila-form-field"><span>API configuration</span><input className="sila-input" readOnly value={route.data?.configurationName ?? ''} /></label>
            <label className="sila-form-field"><span>Last sync</span><input className="sila-input" readOnly value={route.data?.lastSyncAt ?? ''} /></label>
            <label className="sila-form-field"><span>Sync status</span><input className="sila-input" readOnly value={route.data?.lastStatus ?? ''} /></label>
          </div>
          {route.isError && <p>No GET_MATERIAL integration route for this Company Code. Configure it under Administration → Workflows & configuration → Integration Routing.</p>}
          {pull.data && <p>Read {pull.data.recordsRead} · New {pull.data.newCount} · Changed {pull.data.changedCount} · Unchanged {pull.data.unchangedCount} · Failed {pull.data.failedCount}</p>}
          {pull.data?.failures?.map((item) => <p key={item}>{item}</p>)}
          {pull.isError && <p>ERP pull failed. Check the GET_MATERIAL route and S/4 API Product service.</p>}
          <div className="sila-integration-actions" style={{ marginTop: 14 }}>
            <button className="sila-button" type="button" onClick={() => setErpOpen(false)}>Close</button>
            <button className="sila-button sila-button--primary" type="button" disabled={!companyCode || pull.isPending} onClick={() => pull.mutate()}>{pull.isPending ? 'Pulling…' : 'Pull / Sync Materials'}</button>
          </div>
        </section>
      )}
      {preview && (
        <section className="sila-card" style={{ padding: 20, marginBottom: 18 }}>
          <h3>Excel preview · {preview.fileName}</h3>
          <p>{preview.totalRows} rows · {preview.validRows} valid · {preview.newRows} new · {preview.changedRows} changed · {preview.unchangedRows} unchanged · {preview.invalidRows} failed</p>
          {commitFile.isError && <p>{mutationError(commitFile.error)}</p>}
          <div className="sila-integration-actions" style={{ marginTop: 14 }}>
            <button className="sila-button" type="button" onClick={() => setPreview(null)}>Cancel</button>
            <button className="sila-button sila-button--primary" type="button" disabled={!pendingFile.current} onClick={() => pendingFile.current && commitFile.mutate(pendingFile.current)}>Import</button>
          </div>
        </section>
      )}
      {previewFile.isError && <p>{mutationError(previewFile.error)}</p>}
      {editing && (
        <section className="sila-card" style={{ padding: 20, marginBottom: 18 }}>
          <h3>{creating ? 'Create material' : 'Material'}</h3>
          <div className="sila-form-grid">
            {fields.map(([key, label]) => (
              <label className="sila-form-field" key={key}><span>{label}</span><input className="sila-input" value={editing[key] ?? ''} onChange={(event) => setEditing({ ...editing, [key]: event.target.value })} /></label>
            ))}
          </div>
          <h3 style={{ marginTop: 18 }}>Inventory Control</h3>
          <div className="sila-form-grid">
            <label className="sila-form-field"><span>Inventory Item</span>
              <select className="sila-select" value={editing.inventoryItem} onChange={(event) => setEditing({ ...editing, inventoryItem: event.target.value, inventoryType: event.target.value === 'true' ? (editing.inventoryType === 'SERVICE' ? 'STOCK' : editing.inventoryType) : editing.inventoryType })}>
                <option value="true">Yes</option><option value="false">No</option>
              </select>
            </label>
            <label className="sila-form-field"><span>Inventory Type</span>
              <select className="sila-select" value={editing.inventoryType} onChange={(event) => setEditing({ ...editing, inventoryType: event.target.value, inventoryItem: event.target.value === 'STOCK' ? 'true' : editing.inventoryItem })}>
                <option value="STOCK">STOCK</option><option value="NON_STOCK">NON_STOCK</option><option value="SERVICE">SERVICE</option>
              </select>
            </label>
            <label className="sila-form-field"><span>Batch Managed</span>
              <select className="sila-select" value={editing.batchManaged} onChange={(event) => setEditing({ ...editing, batchManaged: event.target.value })}>
                <option value="true">Yes</option><option value="false">No</option>
              </select>
            </label>
            <label className="sila-form-field"><span>Expiry Managed</span>
              <select className="sila-select" value={editing.expiryManaged} onChange={(event) => setEditing({ ...editing, expiryManaged: event.target.value, shelfLifeDays: event.target.value === 'true' ? editing.shelfLifeDays : '' })}>
                <option value="true">Yes</option><option value="false">No</option>
              </select>
            </label>
            {editing.expiryManaged === 'true' && (
              <label className="sila-form-field"><span>Shelf Life Days</span>
                <input className="sila-input" value={editing.shelfLifeDays} onChange={(event) => setEditing({ ...editing, shelfLifeDays: event.target.value })} />
              </label>
            )}
            <label className="sila-form-field"><span>Serial Managed</span>
              <select className="sila-select" value={editing.serialManaged} onChange={(event) => setEditing({ ...editing, serialManaged: event.target.value })}>
                <option value="true">Yes</option><option value="false">No</option>
              </select>
            </label>
          </div>
          {!creating && editing.id && organizationId && <AssignLocations organizationId={organizationId} materialId={editing.id} />}
          {save.isError && <p>{mutationError(save.error)}</p>}
          <div className="sila-integration-actions" style={{ marginTop: 14 }}>
            <button className="sila-button" type="button" onClick={() => { setEditing(null); setCreating(false); }}>Cancel</button>
            <button className="sila-button sila-button--primary" type="button" onClick={() => save.mutate()}>{creating ? 'Create and submit' : 'Submit change'}</button>
          </div>
        </section>
      )}
      {(approvals.data ?? []).some((item) => item.status === 'PENDING') && (
        <section className="sila-card" style={{ padding: 20, marginBottom: 18 }}>
          <h3>Material approvals</h3>
          <table className="sila-table"><thead><tr><th>Material</th><th>Event</th><th>Source</th><th>Level</th><th>Status</th><th></th></tr></thead>
            <tbody>{approvals.data?.filter((item) => item.status === 'PENDING').map((item) => (
              <tr key={item.id}><td>{item.materialCode}</td><td>{item.event}</td><td>{item.source}</td><td>{item.level}</td><td><StatusBadge value={item.status} /></td>
                <td><button className="sila-button sila-button--primary" type="button" onClick={() => decide.mutate({ id: item.id, verb: 'approve' })}>Approve</button> <button className="sila-button" type="button" onClick={() => decide.mutate({ id: item.id, verb: 'reject' })}>Reject</button></td></tr>
            ))}</tbody>
          </table>
        </section>
      )}
      {priceMaterialId && organizationId && (
        <MaterialPricePanel organizationId={organizationId} materialId={priceMaterialId}
          onClose={() => setPriceMaterialId(null)}
          onSubmitted={(row) => {
            setPriceMaterialId(null);
            setViewing(row);
            query.refetch();
            client.invalidateQueries({ queryKey: ['material-approvals', organizationId] });
          }} />
      )}
    </>
  );
}

function AssignLocations({ organizationId, materialId }: { organizationId: string; materialId: string }) {
  const client = useQueryClient();
  const locations = useQuery({
    queryKey: ['inventory-locations', organizationId],
    queryFn: () => api<Array<{ id: string; locationCode: string; locationName: string }>>(`/api/v1/inventory/locations?organizationId=${organizationId}`),
    retry: false,
  });
  const assigned = useQuery({
    queryKey: ['material-locations', organizationId, materialId],
    queryFn: () => api<Array<{ inventoryLocationId: string; active: boolean; stockingType: string }>>(`/api/v1/inventory/materials/${materialId}/locations?organizationId=${organizationId}`),
    retry: false,
  });
  const save = useMutation({
    mutationFn: (locationId: string) => api(`/api/v1/inventory/materials/${materialId}/locations?organizationId=${organizationId}`, {
      method: 'PUT', headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ materialId, inventoryLocationId: locationId, stockingType: 'REGULAR', stockingStatus: 'ACTIVE', active: true }),
    }),
    onSuccess: () => client.invalidateQueries({ queryKey: ['material-locations', organizationId, materialId] }),
  });
  const current = new Set((assigned.data ?? []).filter((item) => item.active).map((item) => item.inventoryLocationId));
  return (
    <div style={{ marginTop: 18 }}>
      <h3>Assign to locations</h3>
      <p className="sila-muted-copy">Selecting a location authorizes stocking. Quantity stays 0 until an inventory transaction.</p>
      {(locations.data ?? []).map((item) => (
        <label key={item.id} style={{ display: 'block', marginBottom: 6 }}>
          <input type="checkbox" checked={current.has(item.id)} onChange={() => { if (!current.has(item.id)) save.mutate(item.id); }} /> {item.locationCode} — {item.locationName}
        </label>
      ))}
    </div>
  );
}
