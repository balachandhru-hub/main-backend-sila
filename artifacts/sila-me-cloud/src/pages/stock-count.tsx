import { useState, type FormEvent } from 'react';
import { Link, useParams } from 'wouter';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { customFetch, getGetAccessContextQueryKey, useGetAccessContext } from '@workspace/api-client-react';
import { SilaPageHeader, StatusBadge } from '@/components/sila-ui';

const request = { credentials: 'include' as const };
async function api<T>(url: string, init?: RequestInit): Promise<T> {
  return customFetch<T>(url, { ...request, ...init, responseType: 'json' });
}
function useOrg() {
  const context = useGetAccessContext({ request, query: { queryKey: getGetAccessContextQueryKey(), retry: false } });
  const organizations = context.data?.organizations;
  const organizationId = organizations?.find((item) => item.code.toUpperCase() === 'FIVE')?.id
    ?? organizations?.find((item) => item.name === 'Five Hotels and Resorts')?.id
    ?? organizations?.[0]?.id;
  return organizationId;
}
function money(value?: number | null, currency?: string | null) {
  if (value == null) return '—';
  return `${value.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })} ${currency ?? ''}`.trim();
}
async function download(url: string, name: string) {
  const response = await fetch(url, { credentials: 'include' });
  if (!response.ok) throw new Error(await response.text());
  const blob = await response.blob();
  const href = URL.createObjectURL(blob);
  const anchor = document.createElement('a');
  anchor.href = href;
  anchor.download = name;
  anchor.click();
  URL.revokeObjectURL(href);
}

type LocationRow = { id: string; locationName: string; locationType: string; propertyLocationId?: string | null; propertyName?: string | null };
type CountRow = { id: string; countNumber: string; countType: string; property?: string | null; location: string; businessDate: string; materials: number; counted: number; matched: number; shortage: number; surplus: number; shortageValue?: number | null; currency?: string | null; status: string; createdBy?: string | null };
type Line = { id: string; materialCode: string; description: string; systemQty?: number | null; systemUom: string; physicalQty?: number | null; varianceQty?: number | null; varianceValue?: number | null; unitCost?: number | null; currency?: string | null; countedBy?: string | null; status: string; enquiryId?: string | null; enquiryStatus?: string | null; manager?: string | null; sapStatus: string; sapMaterialDocument?: string | null; sapError?: string | null; bookVisible: boolean };
type Detail = { id: string; countNumber: string; countType: string; property?: string | null; location: string; businessDate: string; blindCount: boolean; status: string; notes?: string | null; materials: number; counted: number; remaining: number; matched: number; shortage: number; surplus: number; shortageValue?: number | null; currency?: string | null; lines: Line[] };
type Shortage = { locations: number; materials: number; shortageQuantity?: number | null; quantityUom?: string | null; currency?: string | null; totalValue?: number | null; awaitingValue?: number | null; justifiedValue?: number | null; unresolvedValue?: number | null; approvedValue?: number | null; postedValue?: number | null; failedValue?: number | null; byLocation: Array<{ property?: string | null; location?: string | null; value?: number | null; currency?: string | null }>; byReason: Array<{ category: string; count: number; value?: number | null; percent?: number | null }>; lines: Array<Record<string, string | number | null>> };
type Ledger = { id: string; transactionId: string; transactionType: string; direction: string; material?: string | null; location?: string | null; quantity: number; uom: string; referenceType?: string | null; businessDate: string };

export function StockCountPage() {
  const organizationId = useOrg();
  const queryClient = useQueryClient();
  const [open, setOpen] = useState(false);
  const [form, setForm] = useState({ countType: 'MONTHLY', businessDate: '', propertyId: '', inventoryLocationId: '', blindCount: true, notes: '' });
  const list = useQuery({ queryKey: ['stock-counts', organizationId], enabled: Boolean(organizationId), queryFn: () => api<CountRow[]>(`/api/v1/inventory/stock-counts?organizationId=${organizationId}`) });
  const locations = useQuery({ queryKey: ['stock-count-locations', organizationId], enabled: open && Boolean(organizationId), queryFn: () => api<LocationRow[]>(`/api/v1/inventory/locations?organizationId=${organizationId}&active=true`) });
  const create = useMutation({
    mutationFn: () => api(`/api/v1/inventory/stock-counts?organizationId=${organizationId}`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ countType: form.countType, businessDate: form.businessDate, inventoryLocationId: form.inventoryLocationId, blindCount: form.blindCount, notes: form.notes }) }),
    onSuccess: () => { setOpen(false); void queryClient.invalidateQueries({ queryKey: ['stock-counts'] }); },
  });
  const properties = (locations.data ?? []).filter((item) => item.locationType === 'PROPERTY');
  const outlets = (locations.data ?? []).filter((item) => !form.propertyId || item.propertyLocationId === form.propertyId || item.id === form.propertyId);
  return (
    <>
      <SilaPageHeader eyebrow="Inventory" title="Stock Count" description="Monthly, periodic, surprise, and ad hoc counts use the materials already assigned to the location." actions={<button type="button" className="sila-button sila-button--primary" onClick={() => setOpen(true)}>New stock count</button>} />
      {open && (
        <form className="sila-card" style={{ marginBottom: 16, display: 'grid', gap: 12 }} onSubmit={(event: FormEvent) => { event.preventDefault(); create.mutate(); }}>
          <label>Count type<select className="sila-select" value={form.countType} onChange={(event) => setForm({ ...form, countType: event.target.value })}>{['MONTHLY', 'PERIODIC', 'SURPRISE', 'ADHOC'].map((item) => <option key={item}>{item}</option>)}</select></label>
          <label>Business date<input className="sila-input" type="date" required value={form.businessDate} onChange={(event) => setForm({ ...form, businessDate: event.target.value })} /></label>
          <label>Property<select className="sila-select" value={form.propertyId} onChange={(event) => setForm({ ...form, propertyId: event.target.value, inventoryLocationId: '' })}><option value="">All properties</option>{properties.map((item) => <option key={item.id} value={item.id}>{item.locationName}</option>)}</select></label>
          <label>Inventory location<select className="sila-select" required value={form.inventoryLocationId} onChange={(event) => setForm({ ...form, inventoryLocationId: event.target.value })}><option value="">Select</option>{outlets.map((item) => <option key={item.id} value={item.id}>{item.locationName} ({item.locationType})</option>)}</select></label>
          <label>Blind count<select className="sila-select" value={form.blindCount ? 'YES' : 'NO'} onChange={(event) => setForm({ ...form, blindCount: event.target.value === 'YES' })}><option>YES</option><option>NO</option></select></label>
          <label>Notes<input className="sila-input" value={form.notes} onChange={(event) => setForm({ ...form, notes: event.target.value })} /></label>
          {create.isError && <p>{(create.error as Error).message}</p>}
          <button className="sila-button sila-button--primary" type="submit" disabled={create.isPending}>Create count</button>
        </form>
      )}
      <div className="sila-card sila-table-wrap">
        <table className="sila-table"><thead><tr><th>Count</th><th>Type</th><th>Property</th><th>Location</th><th>Date</th><th>Materials</th><th>Counted</th><th>Matched</th><th>Shortage</th><th>Surplus</th><th>Shortage value</th><th>Status</th><th>Created by</th></tr></thead>
          <tbody>{(list.data ?? []).map((item) => (
            <tr key={item.id}><td><Link href={`/inventory/count/${item.id}`}>{item.countNumber}</Link></td><td>{item.countType}</td><td>{item.property}</td><td>{item.location}</td><td>{item.businessDate.slice(0, 10)}</td><td>{item.materials}</td><td>{item.counted}</td><td>{item.matched}</td><td>{item.shortage}</td><td>{item.surplus}</td><td>{money(item.shortageValue, item.currency)}</td><td><StatusBadge value={item.status} /></td><td>{item.createdBy}</td></tr>
          ))}</tbody></table>
        {!list.data?.length && <p className="sila-muted-copy">No stock counts yet.</p>}
      </div>
    </>
  );
}

export function StockCountDetailPage() {
  const { id } = useParams<{ id: string }>();
  const organizationId = useOrg();
  const queryClient = useQueryClient();
  const [lineQuery, setLineQuery] = useState('');
  const detail = useQuery({ queryKey: ['stock-count', id, organizationId], enabled: Boolean(id && organizationId), queryFn: () => api<Detail>(`/api/v1/inventory/stock-counts/${id}?organizationId=${organizationId}`) });
  const review = useMutation({
    mutationFn: (body: { lineId: string; action: string }) => api(`/api/v1/inventory/stock-counts/lines/${body.lineId}/review?organizationId=${organizationId}`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ action: body.action, comment: body.action }) }),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: ['stock-count', id] }),
  });
  const item = detail.data;
  if (!item) return <SilaPageHeader eyebrow="Inventory" title="Stock count" />;
  return (
    <>
      <SilaPageHeader eyebrow="Inventory / Stock Count" title={item.countNumber} description={`${item.property ?? ''} · ${item.location} · ${item.businessDate.slice(0, 10)} · ${item.countType} · Blind ${item.blindCount ? 'Yes' : 'No'}`} actions={<StatusBadge value={item.status} />} />
      <div className="sila-card" style={{ display: 'flex', gap: 16, marginBottom: 16, flexWrap: 'wrap' }}>
        <span>Materials {item.materials}</span><span>Counted {item.counted}</span><span>Remaining {item.remaining}</span><span>Matched {item.matched}</span><span>Shortage {item.shortage}</span><span>Surplus {item.surplus}</span><span>Value {money(item.shortageValue, item.currency)}</span>
      </div>
      <input className="sila-input" placeholder="Filter material" value={lineQuery} onChange={(event) => setLineQuery(event.target.value)} style={{ marginBottom: 12 }} />
      <div className="sila-card sila-table-wrap"><table className="sila-table"><thead><tr><th>Material</th><th>Description</th><th>System</th><th>Physical</th><th>Variance</th><th>UOM</th><th>Value</th><th>Counted by</th><th>Status</th><th>Enquiry</th><th>SAP</th><th></th></tr></thead>
        <tbody>{item.lines.filter((line) => !lineQuery || `${line.materialCode} ${line.description}`.toLowerCase().includes(lineQuery.toLowerCase())).slice(0, 80).map((line) => (
          <tr key={line.id}>
            <td>{line.materialCode}</td><td>{line.description}</td><td>{line.bookVisible ? line.systemQty ?? '—' : 'Hidden'}</td><td>{line.physicalQty ?? '—'}</td><td>{line.bookVisible ? line.varianceQty ?? '—' : 'Hidden'}</td><td>{line.systemUom}</td><td>{line.bookVisible ? money(line.varianceValue, line.currency) : '—'}</td><td>{line.countedBy}</td><td><StatusBadge value={line.status} /></td><td>{line.manager ?? line.enquiryStatus ?? '—'}</td><td>{line.sapStatus} {line.sapMaterialDocument} {line.sapError}</td>
            <td>{line.status !== 'NOT_COUNTED' && line.status !== 'MATCHED' && line.status !== 'POSTED' && (
              <span style={{ display: 'flex', gap: 4, flexWrap: 'wrap' }}>
                <button type="button" className="sila-button" onClick={() => review.mutate({ lineId: line.id, action: 'ACCEPT' })}>Approve</button>
                <button type="button" className="sila-button" onClick={() => review.mutate({ lineId: line.id, action: 'MORE_INFORMATION' })}>More info</button>
                <button type="button" className="sila-button" onClick={() => review.mutate({ lineId: line.id, action: 'RECOUNT' })}>Recount</button>
                <button type="button" className="sila-button" onClick={() => review.mutate({ lineId: line.id, action: 'REJECT' })}>Reject</button>
              </span>
            )}</td>
          </tr>
        ))}</tbody></table></div>
      {review.isError && <p>{(review.error as { message?: string }).message}</p>}
    </>
  );
}

export function ShortagePage() {
  const organizationId = useOrg();
  const [filters, setFilters] = useState({ from: '', to: '', category: '', enquiryStatus: '', sapStatus: '' });
  const query = new URLSearchParams({ organizationId: organizationId ?? '' });
  for (const [key, value] of Object.entries(filters)) if (value) query.set(key, value);
  const report = useQuery({ queryKey: ['shortages', organizationId, filters], enabled: Boolean(organizationId), queryFn: () => api<Shortage>(`/api/v1/inventory/stock-counts/shortages?${query}`) });
  const data = report.data;
  const send = useMutation({ mutationFn: () => api(`/api/v1/inventory/stock-counts/report/send?organizationId=${organizationId}`, { method: 'POST' }) });
  return (
    <>
      <SilaPageHeader eyebrow="Inventory" title="Shortage & Enquiries" description="Shortages from physical counts. Values are summed only when the currency matches." actions={
        <span style={{ display: 'flex', gap: 8 }}>
          <button type="button" className="sila-button" onClick={() => void download(`/api/v1/inventory/stock-counts/report.xlsx?${query}`, 'sila-shortage-report.xls')}>Download Excel</button>
          <button type="button" className="sila-button" onClick={() => void download(`/api/v1/inventory/stock-counts/report.pdf?${query}`, 'sila-shortage-report.pdf')}>Download PDF</button>
        </span>
      } />
      <form className="sila-card" style={{ display: 'flex', gap: 8, flexWrap: 'wrap', marginBottom: 16 }} onSubmit={(event) => { event.preventDefault(); void report.refetch(); }}>
        <input className="sila-input" type="date" value={filters.from} onChange={(event) => setFilters({ ...filters, from: event.target.value })} />
        <input className="sila-input" type="date" value={filters.to} onChange={(event) => setFilters({ ...filters, to: event.target.value })} />
        <input className="sila-input" placeholder="Justification category" value={filters.category} onChange={(event) => setFilters({ ...filters, category: event.target.value })} />
        <input className="sila-input" placeholder="Enquiry status" value={filters.enquiryStatus} onChange={(event) => setFilters({ ...filters, enquiryStatus: event.target.value })} />
        <input className="sila-input" placeholder="SAP status" value={filters.sapStatus} onChange={(event) => setFilters({ ...filters, sapStatus: event.target.value })} />
        <button className="sila-button" type="submit">Generate report</button>
      </form>
      {data && (
        <>
          <div className="sila-card" style={{ display: 'flex', gap: 16, flexWrap: 'wrap', marginBottom: 16 }}>
            <span>Total {money(data.totalValue, data.currency)}</span>
            <span>Awaiting {money(data.awaitingValue, data.currency)}</span>
            <span>Justified {money(data.justifiedValue, data.currency)}</span>
            <span>Unresolved {money(data.unresolvedValue, data.currency)}</span>
            <span>Approved {money(data.approvedValue, data.currency)}</span>
            <span>SAP failed/pending {money(data.failedValue, data.currency)}</span>
          </div>
          <div className="sila-card" style={{ marginBottom: 16 }}>
            <h3>By location</h3>
            {(data.byLocation ?? []).map((item) => <p key={`${item.property}-${item.location}`}>{item.property} · {item.location} · {money(item.value, item.currency)}</p>)}
            <h3>By reason</h3>
            {(data.byReason ?? []).map((item) => <p key={item.category}>{item.category} · {item.count} · {money(item.value, data.currency)} · {item.percent ?? '—'}%</p>)}
          </div>
          <div className="sila-card sila-table-wrap"><table className="sila-table"><thead><tr><th>Property</th><th>Location</th><th>Manager</th><th>Count</th><th>Date</th><th>Material</th><th>System</th><th>Physical</th><th>Shortage</th><th>UOM</th><th>Value</th><th>Reason</th><th>Enquiry</th><th>SAP</th></tr></thead>
            <tbody>{(data.lines ?? []).map((line, index) => (
              <tr key={index}><td>{line.property}</td><td>{line.location}</td><td>{String(line.manager ?? line.managerGroup ?? '')}</td><td>{line.countNumber}</td><td>{String(line.countDate).slice(0, 10)}</td><td>{line.materialCode} {line.description}</td><td>{line.systemQty}</td><td>{line.physicalQty}</td><td>{line.shortageQty}</td><td>{line.uom}</td><td>{money(line.shortageValue as number, line.currency as string)}</td><td>{line.category}</td><td>{line.enquiryNumber} {line.enquiryStatus}</td><td>{line.sapStatus} {line.sapMaterialDocument}</td></tr>
            ))}</tbody></table></div>
        </>
      )}
      <form className="sila-card" style={{ marginTop: 16, display: 'grid', gap: 8 }} onSubmit={(event) => { event.preventDefault(); send.mutate(); }}>
        <h3>Send report</h3>
        <input className="sila-input" placeholder="To" required />
        <input className="sila-input" placeholder="CC" />
        <input className="sila-input" placeholder="Subject" defaultValue="SILA Inventory Shortage Report" />
        <textarea className="sila-input" placeholder="Message" />
        <button className="sila-button" type="submit">Send report</button>
        {send.isError && <p>EMAIL NOT CONFIGURED. Download the Excel or PDF report.</p>}
      </form>
    </>
  );
}

export function InventoryTransactionsPage() {
  const organizationId = useOrg();
  const rows = useQuery({ queryKey: ['inventory-tx', organizationId], enabled: Boolean(organizationId), queryFn: () => api<Ledger[]>(`/api/v1/inventory/transactions?organizationId=${organizationId}`) });
  return (
    <>
      <SilaPageHeader eyebrow="Inventory" title="Inventory Transactions" description="Ledger posted by internal transfers, consumption, and approved stock-count adjustments." />
      <div className="sila-card sila-table-wrap"><table className="sila-table"><thead><tr><th>Transaction</th><th>Type</th><th>Direction</th><th>Material</th><th>Location</th><th>Quantity</th><th>Reference</th><th>Date</th></tr></thead>
        <tbody>{(rows.data ?? []).map((item) => <tr key={item.id}><td>{item.transactionId}</td><td>{item.transactionType}</td><td>{item.direction}</td><td>{item.material}</td><td>{item.location}</td><td>{item.quantity} {item.uom}</td><td>{item.referenceType}</td><td>{item.businessDate.slice(0, 10)}</td></tr>)}</tbody></table></div>
    </>
  );
}
