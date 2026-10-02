import { useEffect, useMemo, useRef, useState, type FormEvent, type ReactNode } from 'react';
import { Link, useLocation, useParams } from 'wouter';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Download, UploadCloud } from 'lucide-react';
import { customFetch, getGetAccessContextQueryKey, useGetAccessContext } from '@workspace/api-client-react';
import { QueryError, SilaPageHeader, StatusBadge } from '@/components/sila-ui';
import { formatCompactMoney } from '@/lib/formatters';

const request = { credentials: 'include' as const };
async function api<T>(url: string, init?: RequestInit): Promise<T> {
  return customFetch<T>(url, { ...request, ...init, responseType: 'json' });
}
function orgId(organizations: Array<{ id: string; code: string; name: string }> | undefined) {
  return organizations?.find((item) => item.code.toUpperCase() === 'FIVE')?.id
    ?? organizations?.find((item) => item.name === 'Five Hotels and Resorts')?.id
    ?? organizations?.[0]?.id;
}
function useOrg() {
  const context = useGetAccessContext({ request, query: { queryKey: getGetAccessContextQueryKey(), retry: false } });
  return { organizationId: orgId(context.data?.organizations), pending: context.isPending };
}
function qs(organizationId: string, extra: Record<string, string | undefined | null>) {
  const params = new URLSearchParams({ organizationId });
  for (const [key, value] of Object.entries(extra)) if (value) params.set(key, value);
  return params.toString();
}
function Field({ label, children, full }: { label: string; children: ReactNode; full?: boolean }) {
  return <label className={`sila-form-field${full ? ' sila-form-field--full' : ''}`}><span>{label}</span>{children}</label>;
}

type LocationRow = {
  id: string; locationCode: string; locationName: string; locationType: string; parentLocationId?: string | null;
  parentLocationName?: string | null; parentLocationCode?: string | null; propertyLocationId?: string | null; propertyCode?: string | null; propertyName?: string | null;
  inventoryEnabled: boolean; salesEnabled: boolean; consumptionEnabled: boolean; transferEnabled: boolean;
  companyCode?: string | null; generalLedgerNumber?: string | null; costCenter?: string | null;
  profitCenter?: string | null; managerGroup?: string | null; status: string; description?: string | null; currency?: string | null;
};
type LocationOptions = {
  locationTypes: Array<{ code: string; name: string }>;
  parents: LocationRow[];
  properties: Array<{ code: string; name: string }>;
  companyCodes: Array<{ code: string; name: string }>;
  managerGroups: Array<{ code: string; name: string }>;
};
type LocationUser = { userId: string; displayName: string; email: string; inventoryLocationId: string; isDefault: boolean };
type InventoryUser = { id: string; displayName: string; email: string };
const emptyLocationForm = {
  locationCode: '', locationName: '', locationType: 'STORE', parentLocationCode: '', propertyCode: '', description: '',
  inventoryEnabled: true, salesEnabled: false, consumptionEnabled: true, transferEnabled: true,
  companyCode: '', generalLedgerNumber: '', costCenter: '', profitCenter: '', currency: '', managerGroup: '', active: true,
};
type AlertRow = { id: string; kind: string; severity: string; status: string; title?: string | null; message?: string | null; locationName?: string | null; recommendedAction?: string | null; createdAt: string };
type MaterialHit = { id: string; materialCode: string; description: string; baseUom: string; unitCost?: number | null; currency?: string | null; priceStatus?: string | null; materialGroup?: string | null; totalAvailable: number };
type Availability = { inventoryLocationId: string; locationName: string; locationType: string; propertyCode?: string | null; onHandQty: number; reservedQty: number; availableQty: number; inTransitQty: number; transferableQty: number; transferableBasis: string; stockStatus: string; transferEnabled: boolean; stockingStatus?: string; stockingType?: string | null };
type LiveDetail = { material: MaterialHit; availability: Availability[]; requiredQty: number; localAvailable: number; shortage: number; recommendation: string; recommendationReason: string; nextActions: string[]; inventoryType?: string; inventoryItem?: boolean; batchManaged?: boolean; expiryManaged?: boolean; serialManaged?: boolean; localStockingStatus?: string; notStockedAtLocation?: boolean };
type MaterialLocationRow = { id: string; materialId: string; materialCode: string; description: string; inventoryLocationId: string; locationCode: string; locationName: string; stockingStatus: string; stockingType: string; minimumStock?: number | null; maximumStock?: number | null; reorderPoint?: number | null; safetyStock?: number | null; parLevel?: number | null; active: boolean };
type ItoRow = { id: string; itoNumber: string; mode: string; fromLocation: string; toLocation: string; transferRelationship: string; requestedAt: string; requiredBy?: string | null; totalValue: number; currency?: string | null; status: string; requestedByName: string };
type ItoDetail = {
  id: string; itoNumber: string; status: string; mode: string; transferRelationship: string; alreadyCollected: boolean;
  fromInventoryLocationId: string; fromLocation: string; fromType: string; toInventoryLocationId: string; toLocation: string; toType: string;
  reason?: string | null; requiredBy?: string | null; totalValue: number; currency?: string | null;
  lines: Array<{ id: string; materialCode: string; description: string; requestedQty: number; approvedQty: number; dispatchedQty: number; receivedQty: number; varianceQty: number; uom: string; unitCost?: number | null; transferValue: number; sourceAvailable?: number | null; sourceAfter?: number | null }>;
  approvals: Array<{ id: string; side: string; status: string; availableQty?: number | null; requestedQty?: number | null; approvedQty?: number | null; stockAfter?: number | null; comment?: string | null }>;
  events: Array<{ id: string; action: string; comment?: string | null; createdAt: string }>;
  allowedActions: string[];
};

type Dashboard = {
  kpis: Array<{ key: string; label: string; value?: number | null; display?: string | null; note?: string | null; configured: boolean; href?: string | null }>;
  alerts: AlertRow[];
  locations: LocationRow[];
  health: { healthy: number; lowStock: number; outOfStock: number; excessStock: number; nearExpiryNote: string; nearExpiryConfigured: boolean };
  actions: Array<{
    severity: string; kindLabel: string; title: string; locationName?: string | null; materialCode?: string | null; materialName?: string | null;
    impact?: string | null; recommendation?: string | null; primaryAction: string; primaryHref?: string | null; alertId?: string | null;
    materialId?: string | null; locationId?: string | null; transferId?: string | null; availableQty?: number | null; parLevel?: number | null;
    sourceAvailable?: number | null; sourceLocationName?: string | null; uom?: string | null;
  }>;
  replenishment: Array<{
    materialId: string; destinationLocationId: string; destination: string; materialCode: string; materialName: string; uom: string;
    availableQty: number; reorderPoint?: number | null; parLevel?: number | null; recommendedQty: number; sourceLocationId?: string | null;
    sourceName?: string | null; sourceAvailable?: number | null; status: string; action: string;
  }>;
  movement: { buckets: Array<{ key: string; label: string; movements: number; value?: number | null; note?: string | null; configured: boolean }>; currency?: string | null };
  transfers: { stages: Array<{ key: string; label: string; count: number; href: string }> };
  locationValues: Array<{ id: string; locationCode: string; locationName: string; locationType: string; value: number; sharePercent: number; aggregated: boolean }>;
  topConsumption: Array<{ materialId: string; materialCode: string; description: string; quantity: number; uom: string; value?: number | null; locationName?: string | null }>;
  currency?: string | null;
};

export function InventoryDashboardPage() {
  const { organizationId } = useOrg();
  const [, navigate] = useLocation();
  const [filters, setFilters] = useState({ businessDate: '', propertyId: '', locationType: '', locationId: '', materialGroup: '' });
  const [more, setMore] = useState(false);
  const [liveQuery, setLiveQuery] = useState('');
  const [selected, setSelected] = useState<Record<string, boolean>>({});
  const query = useQuery({
    queryKey: ['inventory-dashboard', organizationId, filters],
    queryFn: () => api<Dashboard>(`/api/v1/inventory/dashboard?${qs(organizationId!, filters)}`),
    enabled: Boolean(organizationId), retry: false,
  });
  const client = useQueryClient();
  const act = useMutation({
    mutationFn: ({ id, action }: { id: string; action: string }) => api(`/api/v1/inventory/alerts/${id}/actions?organizationId=${organizationId}`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ action }) }),
    onSuccess: () => client.invalidateQueries({ queryKey: ['inventory-dashboard'] }),
  });
  const createIto = useMutation({
    mutationFn: async () => {
      const rows = (query.data?.replenishment ?? []).filter((item) => selected[`${item.destinationLocationId}:${item.materialId}`] && item.action === 'CREATE_TRANSFER' && item.sourceLocationId);
      const groups = new Map<string, typeof rows>();
      for (const row of rows) {
        const key = `${row.sourceLocationId}|${row.destinationLocationId}`;
        groups.set(key, [...(groups.get(key) ?? []), row]);
      }
      for (const [key, lines] of groups) {
        const [fromInventoryLocationId, toInventoryLocationId] = key.split('|');
        await api(`/api/v1/inventory/transfers?organizationId=${organizationId}`, {
          method: 'POST', headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            mode: 'STANDARD', fromInventoryLocationId, toInventoryLocationId, reason: 'Replenishment', alreadyCollected: false,
            lines: lines.map((item) => ({ materialId: item.materialId, quantity: item.recommendedQty, uom: item.uom })),
          }),
        });
      }
    },
    onSuccess: () => { setSelected({}); client.invalidateQueries({ queryKey: ['inventory-dashboard'] }); navigate('/inventory/transfers'); },
  });
  const createPr = useMutation({
    mutationFn: (item: Dashboard['replenishment'][number]) => api(`/api/v1/inventory/purchase-requests?organizationId=${organizationId}`, {
      method: 'POST', headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ materialId: item.materialId, inventoryLocationId: item.destinationLocationId, quantity: item.recommendedQty, uom: item.uom, reason: 'Replenishment — internal stock unavailable' }),
    }),
  });
  const properties = (query.data?.locations ?? []).filter((item) => item.locationType === 'PROPERTY');
  const childLocations = (query.data?.locations ?? []).filter((item) => !filters.propertyId || item.id === filters.propertyId || item.propertyLocationId === filters.propertyId);
  const health = query.data?.health;
  const healthTotal = (health?.healthy ?? 0) + (health?.lowStock ?? 0) + (health?.outOfStock ?? 0) + (health?.excessStock ?? 0);
  const width = (n: number) => `${healthTotal ? (n / healthTotal) * 100 : 0}%`;
  const selectedCount = Object.values(selected).filter(Boolean).length;
  return (
    <>
      <SilaPageHeader eyebrow="Inventory" title="Inventory Control Center" description="Real-time stock visibility, replenishment, transfers and inventory exceptions."
        actions={<>
          <input className="sila-input sila-header-search" placeholder="Search material ID or description…" value={liveQuery} onChange={(event) => setLiveQuery(event.target.value)}
            onKeyDown={(event) => { if (event.key === 'Enter' && liveQuery.trim().length >= 2) navigate(`/inventory/live?q=${encodeURIComponent(liveQuery.trim())}`); }} />
          <Link href={liveQuery.trim().length >= 2 ? `/inventory/live?q=${encodeURIComponent(liveQuery.trim())}` : '/inventory/live'} className="sila-button sila-button--primary">Live Inventory</Link>
          <Link href="/inventory/transfers/new" className="sila-button">+ Internal Transfer</Link>
        </>} />
      <div className="sila-filter-bar">
        <label>Business Date<input className="sila-input" type="date" value={filters.businessDate} onChange={(event) => setFilters((current) => ({ ...current, businessDate: event.target.value }))} /></label>
        <label>Property<select className="sila-select" value={filters.propertyId} onChange={(event) => setFilters((current) => ({ ...current, propertyId: event.target.value, locationId: '' }))}><option value="">All</option>{properties.map((item) => <option key={item.id} value={item.id}>{item.locationName}</option>)}</select></label>
        <label>Location<select className="sila-select" value={filters.locationId} onChange={(event) => setFilters((current) => ({ ...current, locationId: event.target.value }))}><option value="">All</option>{childLocations.map((item) => <option key={item.id} value={item.id}>{item.locationName}</option>)}</select></label>
        <label>Material Group<input className="sila-input" value={filters.materialGroup} onChange={(event) => setFilters((current) => ({ ...current, materialGroup: event.target.value }))} /></label>
        <button type="button" className="sila-button" onClick={() => setMore((value) => !value)}>{more ? 'Fewer filters' : 'More filters'}</button>
        {more && <label>Location Type<select className="sila-select" value={filters.locationType} onChange={(event) => setFilters((current) => ({ ...current, locationType: event.target.value }))}><option value="">All</option>{['PROPERTY', 'VENUE', 'STORE', 'OUTLET'].map((item) => <option key={item} value={item}>{item.charAt(0) + item.slice(1).toLowerCase()}</option>)}</select></label>}
      </div>
      {query.isError ? <QueryError onRetry={() => query.refetch()} /> : (
        <>
          <div className="sila-control-kpi">
            {(query.data?.kpis ?? []).map((kpi) => {
              const body = <article className="sila-card sila-kpi"><span className="sila-kpi__label">{kpi.label}</span><strong className="sila-kpi__value">{query.isPending ? '…' : kpi.configured ? (kpi.display ?? kpi.value ?? 0) : 'Not configured'}</strong><span className="sila-kpi__note">{kpi.note}</span></article>;
              return kpi.href?.startsWith('#') ? <a key={kpi.key} href={kpi.href}>{body}</a> : kpi.href ? <Link key={kpi.key} href={kpi.href}>{body}</Link> : <div key={kpi.key}>{body}</div>;
            })}
          </div>
          <div className="sila-control-split">
            <section className="sila-card sila-recipe-panel">
              <div className="sila-section-head"><h3>Stock Health</h3><Link href="/inventory/live" className="sila-button sila-button--quiet">View stock exceptions</Link></div>
              <div className="sila-health-meter" aria-hidden>
                <span style={{ width: width(health?.healthy ?? 0), background: 'var(--sila-green)' }} />
                <span style={{ width: width(health?.lowStock ?? 0), background: 'var(--sila-amber)' }} />
                <span style={{ width: width(health?.outOfStock ?? 0), background: 'var(--sila-red)' }} />
                <span style={{ width: width(health?.excessStock ?? 0), background: 'var(--sila-blue)' }} />
              </div>
              <ul className="sila-health-list">
                <li><span>Healthy</span><strong>{health?.healthy ?? 0}</strong></li>
                <li><span>Low Stock</span><strong>{health?.lowStock ?? 0}</strong></li>
                <li><span>Out of Stock</span><strong>{health?.outOfStock ?? 0}</strong></li>
                <li><span>Excess Stock</span><strong>{health?.excessStock ?? 0}</strong></li>
                <li><span>Near Expiry</span><strong>{health?.nearExpiryConfigured ? 0 : (health?.nearExpiryNote ?? 'Expiry tracking is not configured.')}</strong></li>
              </ul>
            </section>
            <section className="sila-card sila-recipe-panel" id="action-center">
              <div className="sila-section-head"><h3>Action Center</h3></div>
              {(query.data?.actions ?? []).length === 0 ? <p className="sila-muted-copy" style={{ margin: 0 }}>No inventory exceptions need action for this context.</p> : query.data?.actions.map((item) => (
                <article key={`${item.title}${item.materialId}${item.transferId}`} className="sila-action-item">
                  <p className="sila-action-item__meta">{item.severity} · {item.kindLabel}</p>
                  <h4>{item.title}</h4>
                  <p>{item.impact}{item.parLevel != null ? ` · Par ${item.parLevel} ${item.uom ?? ''}` : ''}{item.sourceLocationName ? ` · ${item.sourceLocationName} ${item.sourceAvailable ?? 0} available` : ''}</p>
                  <p>{item.recommendation}</p>
                  <div className="sila-integration-actions" style={{ borderTop: 'none', paddingTop: 0 }}>
                    {item.primaryAction === 'CREATE_TRANSFER' ? <Link className="sila-button sila-button--primary" href="/inventory/transfers/new">Create Transfer</Link> : null}
                    {item.primaryAction === 'INVESTIGATE' && item.primaryHref ? <Link className="sila-button sila-button--primary" href={item.primaryHref}>Investigate</Link> : null}
                    {item.primaryAction === 'CREATE_PR' ? <Link className="sila-button sila-button--primary" href="#replenishment">Create PR</Link> : null}
                    {item.materialId ? <Link className="sila-button" href={`/inventory/live?q=${encodeURIComponent(item.materialCode ?? '')}`}>View Stock</Link> : null}
                    {item.alertId ? <button type="button" className="sila-button sila-button--quiet" onClick={() => act.mutate({ id: item.alertId!, action: 'ACKNOWLEDGE' })}>Acknowledge</button> : null}
                  </div>
                </article>
              ))}
            </section>
          </div>
          <section className="sila-card sila-recipe-panel" id="replenishment" style={{ marginBottom: 16 }}>
            <div className="sila-section-head">
              <h3>Replenishment Required</h3>
              <button type="button" className="sila-button sila-button--primary" disabled={!selectedCount || createIto.isPending} onClick={() => createIto.mutate()}>{createIto.isPending ? 'Creating…' : 'Create Replenishment ITO'}</button>
            </div>
            {(query.data?.replenishment ?? []).length === 0 ? <p className="sila-muted-copy" style={{ margin: 0 }}>No locations currently require replenishment.</p> : (
              <>
                <div className="sila-recipe-grid-wrap sila-replenish-table">
                  <table className="sila-table"><thead><tr><th></th><th>Destination</th><th>Material</th><th>Available</th><th>Par</th><th>Need</th><th>Source</th><th>Source Available</th><th>Status</th><th></th></tr></thead>
                    <tbody>{query.data?.replenishment.map((row) => {
                      const key = `${row.destinationLocationId}:${row.materialId}`;
                      return (
                        <tr key={key}>
                          <td><input type="checkbox" checked={Boolean(selected[key])} disabled={row.action !== 'CREATE_TRANSFER'} onChange={(event) => setSelected((current) => ({ ...current, [key]: event.target.checked }))} /></td>
                          <td>{row.destination}</td><td>{row.materialName}<span className="sila-table__secondary">{row.materialCode}</span></td>
                          <td>{row.availableQty} {row.uom}</td><td>{row.parLevel ?? 'Not configured'}</td><td>{row.recommendedQty} {row.uom}</td>
                          <td>{row.sourceName ?? '—'}</td><td>{row.sourceAvailable == null ? 'Not available' : `${row.sourceAvailable} ${row.uom}`}</td>
                          <td>{row.status}</td>
                          <td>{row.action === 'CREATE_PR' ? <button type="button" className="sila-button" onClick={() => createPr.mutate(row)}>Create PR</button> : <Link className="sila-button" href="/inventory/transfers/new">Create Transfer</Link>}</td>
                        </tr>
                      );
                    })}</tbody></table>
                </div>
                <div className="sila-replenish-cards">{query.data?.replenishment.map((row) => (
                  <article key={`${row.destinationLocationId}:${row.materialId}`} className="sila-card" style={{ padding: 14 }}>
                    <strong>{row.destination}</strong>
                    <p className="sila-muted-copy" style={{ margin: '6px 0' }}>{row.materialName} · Available {row.availableQty} {row.uom} · Need {row.recommendedQty} {row.uom}</p>
                    <p className="sila-muted-copy" style={{ margin: '0 0 8px' }}>{row.sourceName ?? 'Internal stock unavailable'}</p>
                    {row.action === 'CREATE_PR' ? <button type="button" className="sila-button" onClick={() => createPr.mutate(row)}>Create PR</button> : <Link className="sila-button sila-button--primary" href="/inventory/transfers/new">Create Transfer</Link>}
                  </article>
                ))}</div>
              </>
            )}
          </section>
          <div className="sila-control-split">
            <section className="sila-card sila-recipe-panel">
              <div className="sila-section-head"><h3>Inventory Movement Today</h3></div>
              <ul className="sila-health-list">
                {(query.data?.movement.buckets ?? []).map((bucket) => (
                  <li key={bucket.key}><span>{bucket.label}</span><strong>{bucket.movements === 0 ? (bucket.note ?? 'No activity') : bucket.value != null ? formatCompactMoney(bucket.value, query.data?.movement.currency ?? query.data?.currency ?? 'AED') : `${bucket.movements} movements`}</strong></li>
                ))}
              </ul>
            </section>
            <section className="sila-card sila-recipe-panel">
              <div className="sila-section-head"><h3>Transfer Center</h3><Link href="/inventory/transfers" className="sila-button sila-button--quiet">Open Transfer Center</Link></div>
              {(query.data?.transfers.stages.every((item) => item.count === 0)) ? <p className="sila-muted-copy" style={{ margin: 0 }}>No transfers currently in transit.</p> : (
                <ul className="sila-health-list">{query.data?.transfers.stages.map((stage) => (
                  <li key={stage.key}><Link href={stage.href}>{stage.label}</Link><strong>{stage.count}</strong></li>
                ))}</ul>
              )}
            </section>
          </div>
          <div className="sila-control-split">
            <section className="sila-card sila-recipe-panel">
              <div className="sila-section-head"><h3>Inventory by Location</h3><Link href="/inventory/locations" className="sila-button sila-button--quiet">View location inventory</Link></div>
              {(query.data?.locationValues ?? []).length === 0 ? <p className="sila-muted-copy" style={{ margin: 0 }}>No inventory value for the selected context.</p> : (
                <ul className="sila-health-list">{query.data?.locationValues.map((item) => (
                  <li key={item.id}><span>{item.locationName}{item.aggregated ? ' (aggregated)' : ''}</span><strong>{formatCompactMoney(item.value, query.data?.currency ?? 'AED')} · {item.sharePercent}%</strong></li>
                ))}</ul>
              )}
            </section>
            <section className="sila-card sila-recipe-panel">
              <div className="sila-section-head"><h3>Top Consumption</h3></div>
              {(query.data?.topConsumption ?? []).length === 0 ? <p className="sila-muted-copy" style={{ margin: 0 }}>No consumption transactions for the selected period.</p> : (
                <ul className="sila-health-list">{query.data?.topConsumption.map((item) => (
                  <li key={item.materialId}><span><Link href={`/inventory/live?q=${encodeURIComponent(item.materialCode)}`}>{item.description}</Link><span className="sila-table__secondary"> {item.quantity} {item.uom}{item.locationName ? ` · ${item.locationName}` : ''}</span></span><strong>{item.value == null ? `${item.quantity} ${item.uom}` : formatCompactMoney(item.value, query.data?.currency ?? 'AED')}</strong></li>
                ))}</ul>
              )}
            </section>
          </div>
        </>
      )}
    </>
  );
}

export function InventoryLocationsPage() {
  const { organizationId } = useOrg();
  const client = useQueryClient();
  const fileRef = useRef<HTMLInputElement>(null);
  const pendingFile = useRef<File | null>(null);
  const [queryText, setQueryText] = useState('');
  const [locationType, setLocationType] = useState('');
  const [propertyId, setPropertyId] = useState('');
  const [active, setActive] = useState('');
  const [view, setView] = useState<'TABLE' | 'TREE'>('TABLE');
  const [selectedId, setSelectedId] = useState('');
  const [assignUserId, setAssignUserId] = useState('');
  const [assignMaterialId, setAssignMaterialId] = useState('');
  const locMaterials = useQuery({
    queryKey: ['location-materials', organizationId, selectedId],
    queryFn: () => api<MaterialLocationRow[]>(`/api/v1/inventory/locations/${selectedId}/materials?organizationId=${organizationId}`),
    enabled: Boolean(organizationId && selectedId), retry: false,
  });
  const addMaterial = useMutation({
    mutationFn: () => api(`/api/v1/inventory/locations/${selectedId}/materials?organizationId=${organizationId}`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ materialId: assignMaterialId, inventoryLocationId: selectedId, stockingType: 'REGULAR', stockingStatus: 'ACTIVE', active: true }) }),
    onSuccess: () => { setAssignMaterialId(''); client.invalidateQueries({ queryKey: ['location-materials'] }); },
  });
  const [form, setForm] = useState(emptyLocationForm);
  const [preview, setPreview] = useState<{ fileName: string; totalRows: number; new: number; changed: number; unchanged: number; invalid: number; errors: Array<{ row: number; code: string; message: string }> } | null>(null);
  const list = useQuery({
    queryKey: ['inventory-locations', organizationId, queryText, locationType, propertyId, active],
    queryFn: () => api<LocationRow[]>(`/api/v1/inventory/locations?${qs(organizationId!, { query: queryText, locationType, propertyLocationId: propertyId, active: active === '' ? undefined : active === 'active' ? 'true' : 'false' })}`),
    enabled: Boolean(organizationId), retry: false,
  });
  const options = useQuery({
    queryKey: ['inventory-location-options', organizationId],
    queryFn: () => api<LocationOptions>(`/api/v1/inventory/locations/options?organizationId=${organizationId}`),
    enabled: Boolean(organizationId), retry: false,
  });
  const users = useQuery({
    queryKey: ['inventory-location-users', organizationId, selectedId],
    queryFn: () => api<LocationUser[]>(`/api/v1/inventory/locations/${selectedId}/users?organizationId=${organizationId}`),
    enabled: Boolean(organizationId && selectedId), retry: false,
  });
  const directory = useQuery({
    queryKey: ['inventory-assignable-users', organizationId],
    queryFn: () => api<InventoryUser[]>(`/api/v1/inventory/locations/assignable-users?organizationId=${organizationId}`),
    enabled: Boolean(organizationId), retry: false,
  });
  const save = useMutation({
    mutationFn: () => {
      const payload = form.locationType === 'PROPERTY'
        ? { ...form, parentLocationCode: '', propertyCode: '' }
        : { ...form, propertyCode: form.parentLocationCode };
      return api(`/api/v1/inventory/locations?organizationId=${organizationId}`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(payload) });
    },
    onSuccess: () => { client.invalidateQueries({ queryKey: ['inventory-locations'] }); client.invalidateQueries({ queryKey: ['inventory-location-options'] }); },
  });
  const toggle = useMutation({
    mutationFn: ({ id, next }: { id: string; next: boolean }) => api(`/api/v1/inventory/locations/${id}/activate?organizationId=${organizationId}&active=${next}`, { method: 'POST' }),
    onSuccess: () => client.invalidateQueries({ queryKey: ['inventory-locations'] }),
  });
  const assign = useMutation({
    mutationFn: (body: { userId: string; isDefault: boolean; remove?: boolean }) => api(`/api/v1/inventory/locations/${selectedId}/users?organizationId=${organizationId}`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) }),
    onSuccess: () => client.invalidateQueries({ queryKey: ['inventory-location-users'] }),
  });
  const previewFile = useMutation({
    mutationFn: async (file: File) => {
      const body = new FormData(); body.append('file', file);
      return api<NonNullable<typeof preview>>(`/api/v1/inventory/locations/import/preview?organizationId=${organizationId}`, { method: 'POST', body });
    },
    onSuccess: setPreview,
  });
  const commitFile = useMutation({
    mutationFn: async (file: File) => {
      const body = new FormData(); body.append('file', file);
      return api(`/api/v1/inventory/locations/import?organizationId=${organizationId}`, { method: 'POST', body });
    },
    onSuccess: () => { setPreview(null); list.refetch(); },
  });
  const properties = (list.data ?? []).filter((item) => item.locationType === 'PROPERTY');
  const tree = useMemo(() => {
    const rows = list.data ?? [];
    const byParent = new Map<string | null, LocationRow[]>();
    for (const row of rows) {
      const key = row.parentLocationId ?? null;
      byParent.set(key, [...(byParent.get(key) ?? []), row]);
    }
    const walk = (parentId: string | null, depth: number): Array<LocationRow & { depth: number }> =>
      (byParent.get(parentId) ?? []).flatMap((item) => [{ ...item, depth }, ...walk(item.id, depth + 1)]);
    return walk(null, 0);
  }, [list.data]);
  const edit = (item: LocationRow) => {
    setSelectedId(item.id);
    setForm({
      locationCode: item.locationCode, locationName: item.locationName, locationType: item.locationType,
      parentLocationCode: item.locationType === 'PROPERTY' ? '' : (item.parentLocationCode ?? item.propertyCode ?? ''),
      propertyCode: item.locationType === 'PROPERTY' ? '' : (item.parentLocationCode ?? item.propertyCode ?? ''),
      description: item.description ?? '',
      inventoryEnabled: item.inventoryEnabled, salesEnabled: item.salesEnabled, consumptionEnabled: item.consumptionEnabled, transferEnabled: item.transferEnabled,
      companyCode: item.companyCode ?? '', generalLedgerNumber: item.generalLedgerNumber ?? '',
      costCenter: item.costCenter ?? '', profitCenter: item.profitCenter ?? '', currency: item.currency ?? '', managerGroup: item.managerGroup ?? '',
      active: item.status === 'ACTIVE',
    });
  };
  const codes = (rows?: Array<{ code: string; name: string }>) => rows ?? [];
  return (
    <>
      <SilaPageHeader eyebrow="Administration" title="Location Master" description="Property hangs under Company Code. Store, Outlet, and Venue hang under a Property. No separate property assignment on Property."
        actions={<>
          <a className="sila-button" href={`/api/v1/inventory/locations/template?organizationId=${organizationId}`}><Download size={14} /> Download Template</a>
          <a className="sila-button" href={`/api/v1/inventory/locations/export?organizationId=${organizationId}`}><Download size={14} /> Download</a>
          <button type="button" className="sila-button sila-button--primary" onClick={() => fileRef.current?.click()}><UploadCloud size={14} /> Upload</button>
        </>} />
      <input ref={fileRef} type="file" accept=".xlsx" hidden onChange={(event) => { const file = event.target.files?.[0]; if (file) { pendingFile.current = file; previewFile.mutate(file); } event.target.value = ''; }} />
      {preview && (
        <section className="sila-card sila-recipe-panel" style={{ marginBottom: 18 }}>
          <h3 className="sila-recipe-panel__title">Excel preview · {preview.fileName}</h3>
          <p className="sila-muted-copy" style={{ margin: '0 0 12px' }}>New {preview.new} · Changed {preview.changed} · Unchanged {preview.unchanged} · Invalid {preview.invalid}</p>
          {preview.errors.slice(0, 8).map((error) => <p key={`${error.row}${error.code}`} className="sila-muted-copy" style={{ margin: '0 0 6px' }}>{error.row}: {error.message}</p>)}
          <div className="sila-integration-actions">
            <button type="button" className="sila-button" onClick={() => setPreview(null)}>Cancel</button>
            <button type="button" className="sila-button sila-button--primary" onClick={() => pendingFile.current && commitFile.mutate(pendingFile.current)}>Commit upload</button>
          </div>
        </section>
      )}
      <div className="sila-filter-panel">
        <label>Search<input className="sila-input" value={queryText} onChange={(event) => setQueryText(event.target.value)} /></label>
        <label>Type<select className="sila-select" value={locationType} onChange={(event) => setLocationType(event.target.value)}><option value="">All types</option>{['PROPERTY', 'VENUE', 'STORE', 'OUTLET'].map((item) => <option key={item}>{item}</option>)}</select></label>
        <label>Property<select className="sila-select" value={propertyId} onChange={(event) => setPropertyId(event.target.value)}><option value="">All properties</option>{properties.map((item) => <option key={item.id} value={item.id}>{item.locationName}</option>)}</select></label>
        <label>Status<select className="sila-select" value={active} onChange={(event) => setActive(event.target.value)}><option value="">All statuses</option><option value="active">Active</option><option value="inactive">Inactive</option></select></label>
        <label>View<select className="sila-select" value={view} onChange={(event) => setView(event.target.value as 'TABLE' | 'TREE')}><option value="TABLE">Table</option><option value="TREE">Tree</option></select></label>
      </div>
      <div className="sila-inventory-editor">
        <div className="sila-inventory-stack">
          <form className="sila-card sila-recipe-panel" onSubmit={(event: FormEvent) => { event.preventDefault(); save.mutate(); }}>
            <h3 className="sila-recipe-panel__title">{selectedId ? 'Edit location' : 'New location'}</h3>
            <div className="sila-form-grid">
              <Field label="Location code"><input className="sila-input" required value={form.locationCode} onChange={(event) => setForm({ ...form, locationCode: event.target.value })} /></Field>
              <Field label="Location name"><input className="sila-input" required value={form.locationName} onChange={(event) => setForm({ ...form, locationName: event.target.value })} /></Field>
              <Field label="Type"><select className="sila-select" value={form.locationType} onChange={(event) => {
                const next = event.target.value;
                setForm({ ...form, locationType: next, parentLocationCode: next === 'PROPERTY' ? '' : form.parentLocationCode, propertyCode: '' });
              }}>{(options.data?.locationTypes ?? [{ code: 'STORE', name: 'Store' }]).map((item) => <option key={item.code} value={item.code}>{item.name}</option>)}</select></Field>
              {form.locationType === 'PROPERTY' ? (
                <Field label="Parent (company code)"><select className="sila-select" required value={form.companyCode} onChange={(event) => setForm({ ...form, companyCode: event.target.value })}><option value="">Select company code</option>{codes(options.data?.companyCodes).map((item) => <option key={item.code} value={item.code}>{item.code} — {item.name}</option>)}</select></Field>
              ) : (
                <Field label={form.locationType === 'VENUE' ? 'Parent (property)' : 'Parent (property or venue)'}><select className="sila-select" required value={form.parentLocationCode} onChange={(event) => {
                  const parent = (options.data?.parents ?? []).find((item) => item.locationCode === event.target.value);
                  setForm({ ...form, parentLocationCode: event.target.value, propertyCode: event.target.value, companyCode: parent?.companyCode || form.companyCode });
                }}><option value="">Select parent</option>{(options.data?.parents ?? []).filter((item) => form.locationType === 'VENUE' ? item.locationType === 'PROPERTY' : item.locationType === 'PROPERTY' || item.locationType === 'VENUE').map((item) => <option key={item.id} value={item.locationCode}>{item.locationCode} — {item.locationName}</option>)}</select></Field>
              )}
              <Field label="Description"><input className="sila-input" value={form.description} onChange={(event) => setForm({ ...form, description: event.target.value })} /></Field>
              {form.locationType !== 'PROPERTY' && (
                <Field label="Company code"><select className="sila-select" value={form.companyCode} onChange={(event) => setForm({ ...form, companyCode: event.target.value })}><option value="">Inherited from property</option>{codes(options.data?.companyCodes).map((item) => <option key={item.code} value={item.code}>{item.code} — {item.name}</option>)}</select></Field>
              )}
              <Field label="General ledger number"><input className="sila-input" value={form.generalLedgerNumber} onChange={(event) => setForm({ ...form, generalLedgerNumber: event.target.value })} /></Field>
              <Field label="Cost center"><input className="sila-input" value={form.costCenter} onChange={(event) => setForm({ ...form, costCenter: event.target.value })} /></Field>
              <Field label="Profit center"><input className="sila-input" value={form.profitCenter} onChange={(event) => setForm({ ...form, profitCenter: event.target.value })} /></Field>
              <Field label="Currency"><input className="sila-input" value={form.currency} onChange={(event) => setForm({ ...form, currency: event.target.value })} /></Field>
              <Field label="Manager group" full><select className="sila-select" value={form.managerGroup} onChange={(event) => setForm({ ...form, managerGroup: event.target.value })}><option value="">None</option>{codes(options.data?.managerGroups).map((item) => <option key={item.code} value={item.code}>{item.code} — {item.name}</option>)}</select></Field>
            </div>
            <h3 className="sila-recipe-panel__title" style={{ marginTop: 16 }}>Flags</h3>
            <div className="sila-inventory-flags">
              {([['inventoryEnabled', 'Inventory enabled'], ['salesEnabled', 'Sales enabled'], ['consumptionEnabled', 'Consumption enabled'], ['transferEnabled', 'Transfer enabled'], ['active', 'Active']] as const).map(([key, label]) => (
                <label key={key}><input type="checkbox" checked={form[key]} onChange={(event) => setForm({ ...form, [key]: event.target.checked })} /> {label}</label>
              ))}
            </div>
            <div className="sila-integration-actions">
              <button type="button" className="sila-button" onClick={() => { setSelectedId(''); setForm(emptyLocationForm); }}>New</button>
              <button type="submit" className="sila-button sila-button--primary">Save location</button>
            </div>
          </form>
          {selectedId && (
            <section className="sila-card sila-recipe-panel">
              <h3 className="sila-recipe-panel__title">User assignment</h3>
              <p className="sila-muted-copy" style={{ margin: '0 0 12px' }}>Default assignment is My Location on Mobile. Operators without an assignment see an empty location, not the first property.</p>
              <div className="sila-filter-row">
                <select className="sila-select" value={assignUserId} onChange={(event) => setAssignUserId(event.target.value)}><option value="">Select user</option>{(directory.data ?? []).map((item) => <option key={item.id} value={item.id}>{item.displayName} ({item.email})</option>)}</select>
                <button type="button" className="sila-button sila-button--primary" disabled={!assignUserId} onClick={() => assign.mutate({ userId: assignUserId, isDefault: true })}>Assign as default</button>
                <button type="button" className="sila-button" disabled={!assignUserId} onClick={() => assign.mutate({ userId: assignUserId, isDefault: false })}>Assign</button>
              </div>
              <div className="sila-recipe-grid-wrap">
                <table className="sila-table"><thead><tr><th>User</th><th>Email</th><th>Default</th><th></th></tr></thead>
                  <tbody>{(users.data ?? []).map((item) => (
                    <tr key={item.userId}><td>{item.displayName}</td><td>{item.email}</td><td>{item.isDefault ? 'Yes' : 'No'}</td>
                      <td><button type="button" className="sila-button" onClick={() => assign.mutate({ userId: item.userId, isDefault: false, remove: true })}>Remove</button></td></tr>
                  ))}</tbody></table>
              </div>
            </section>
          )}
          {selectedId && (
            <section className="sila-card sila-recipe-panel">
              <h3 className="sila-recipe-panel__title">Materials at this location</h3>
              <p className="sila-muted-copy" style={{ margin: '0 0 12px' }}>Assignment authorizes stocking. It does not create quantity.</p>
              <div className="sila-filter-row">
                <input className="sila-input" value={assignMaterialId} onChange={(event) => setAssignMaterialId(event.target.value)} placeholder="Material GUID from Material Master" />
                <button type="button" className="sila-button sila-button--primary" disabled={!assignMaterialId} onClick={() => addMaterial.mutate()}>Add material</button>
              </div>
              <div className="sila-recipe-grid-wrap">
                <table className="sila-table"><thead><tr><th>Material</th><th>Name</th><th>Type</th><th>Min</th><th>Max</th><th>Reorder</th><th>Safety</th><th>Par</th><th>Active</th></tr></thead>
                  <tbody>{(locMaterials.data ?? []).map((item) => (
                    <tr key={item.id}><td>{item.materialCode}</td><td>{item.description}</td><td>{item.stockingType}</td><td>{item.minimumStock ?? '—'}</td><td>{item.maximumStock ?? '—'}</td><td>{item.reorderPoint ?? '—'}</td><td>{item.safetyStock ?? '—'}</td><td>{item.parLevel ?? '—'}</td><td>{item.active ? 'Yes' : 'No'}</td></tr>
                  ))}</tbody></table>
              </div>
            </section>
          )}
        </div>
        <section className="sila-card sila-recipe-panel">
          <h3 className="sila-recipe-panel__title">Locations</h3>
          {list.isPending ? <div className="sila-loading-row" /> : !list.data?.length ? <p className="sila-muted-copy" style={{ margin: 0 }}>Create a property first, then add venues, stores, and outlets.</p> : (
            <div className="sila-recipe-grid-wrap">
              <table className="sila-table"><thead><tr>
                <th>Location Code</th><th>Location Name</th><th>Location Type</th><th>Parent Location</th><th>Property</th>
                <th>Inventory</th><th>Sales</th><th>Consumption</th><th>Transfer</th>
                <th>Company Code</th><th>General Ledger Number</th><th>Cost Center</th><th>Profit Center</th><th>Currency</th><th>Manager Group</th><th>Active</th><th></th>
              </tr></thead>
                <tbody>{(view === 'TREE' ? tree : (list.data ?? [])).map((item) => (
                  <tr key={item.id} onClick={() => edit(item)} style={{ cursor: 'pointer', background: selectedId === item.id ? 'var(--sila-surface-muted, #f4f1ea)' : undefined }}>
                    <td style={{ paddingLeft: view === 'TREE' ? 12 + ('depth' in item ? (item as LocationRow & { depth: number }).depth * 16 : 0) : undefined }}>{item.locationCode}</td>
                    <td>{item.locationName}</td><td>{item.locationType}</td><td>{item.parentLocationName}</td><td>{item.propertyName}</td>
                    <td>{item.inventoryEnabled ? 'Yes' : 'No'}</td><td>{item.salesEnabled ? 'Yes' : 'No'}</td><td>{item.consumptionEnabled ? 'Yes' : 'No'}</td><td>{item.transferEnabled ? 'Yes' : 'No'}</td>
                    <td>{item.companyCode}</td><td>{item.generalLedgerNumber}</td><td>{item.costCenter}</td><td>{item.profitCenter}</td><td>{item.currency}</td><td>{item.managerGroup}</td>
                    <td><StatusBadge value={item.status} /></td>
                    <td><button type="button" className="sila-button" onClick={(event) => { event.stopPropagation(); toggle.mutate({ id: item.id, next: item.status !== 'ACTIVE' }); }}>{item.status === 'ACTIVE' ? 'Deactivate' : 'Activate'}</button></td>
                  </tr>
                ))}</tbody></table>
            </div>
          )}
        </section>
      </div>
    </>
  );
}

export function LiveInventoryPage() {
  const { organizationId } = useOrg();
  const [text, setText] = useState(() => typeof window === 'undefined' ? '' : new URLSearchParams(window.location.search).get('q') ?? '');
  const [debounced, setDebounced] = useState('');
  const [requiredQty, setRequiredQty] = useState('1');
  const [debouncedQty, setDebouncedQty] = useState('1');
  const [transferLocationId, setTransferLocationId] = useState('');
  const [selectedId, setSelectedId] = useState('');
  const [sourceLocationId, setSourceLocationId] = useState('');
  const [resultsOpen, setResultsOpen] = useState(false);
  const [transferMessage, setTransferMessage] = useState('');
  useEffect(() => { const handle = setTimeout(() => setDebounced(text.trim()), 300); return () => clearTimeout(handle); }, [text]);
  useEffect(() => { const handle = setTimeout(() => setDebouncedQty(requiredQty.trim() || '0'), 400); return () => clearTimeout(handle); }, [requiredQty]);
  const stableQuery = { retry: false as const, refetchOnWindowFocus: false, staleTime: 30_000 };
  const locations = useQuery({
    queryKey: ['inventory-locations', organizationId],
    queryFn: () => api<LocationRow[]>(`/api/v1/inventory/locations?organizationId=${organizationId}`),
    enabled: Boolean(organizationId), ...stableQuery,
  });
  const mine = useQuery({
    queryKey: ['inventory-my-location', organizationId],
    queryFn: async () => {
      try { return await api<LocationRow>(`/api/v1/inventory/my-location?organizationId=${organizationId}`); }
      catch { return null; }
    },
    enabled: Boolean(organizationId), ...stableQuery,
  });
  useEffect(() => {
    const id = mine.data?.id;
    if (!id) return;
    setTransferLocationId((current) => current || id);
  }, [mine.data?.id]);
  const search = useQuery({
    queryKey: ['live-search', organizationId, debounced],
    queryFn: () => api<{ items: MaterialHit[]; nextCursor?: string }>(`/api/v1/inventory/live/search?${qs(organizationId!, { query: debounced, pageSize: '20' })}`),
    enabled: Boolean(organizationId && debounced.length >= 2), ...stableQuery,
  });
  const detail = useQuery({
    queryKey: ['live-detail', organizationId, selectedId, transferLocationId, debouncedQty],
    queryFn: () => api<LiveDetail>(`/api/v1/inventory/live/${selectedId}?${qs(organizationId!, { requiredQty: debouncedQty, currentLocationId: transferLocationId })}`),
    enabled: Boolean(organizationId && selectedId),
    placeholderData: (previous) => previous,
    ...stableQuery,
  });
  const createIto = useMutation({
    mutationFn: (body: object) => api<ItoDetail>(`/api/v1/inventory/transfers?organizationId=${organizationId}`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) }),
    onSuccess: (item) => setTransferMessage(`Opened ${item.itoNumber}. Approve, then dispatch and receive to move stock.`),
  });
  const createPr = useMutation({
    mutationFn: () => api(`/api/v1/inventory/purchase-requests?organizationId=${organizationId}`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ materialId: selectedId, inventoryLocationId: transferLocationId || null, quantity: Number(requiredQty), reason: 'Live Inventory shortage' }) }),
  });
  const opening = useMutation({
    mutationFn: () => api(`/api/v1/inventory/opening-stock?organizationId=${organizationId}`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ materialId: selectedId, inventoryLocationId: transferLocationId, quantity: Number(requiredQty) || 1 }) }),
    onSuccess: () => { void detail.refetch(); },
  });
  const assignHere = useMutation({
    mutationFn: () => api(`/api/v1/inventory/materials/${selectedId}/locations?organizationId=${organizationId}`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ materialId: selectedId, inventoryLocationId: transferLocationId, stockingType: 'REGULAR', stockingStatus: 'ACTIVE', active: true }) }),
  });
  const count = useMutation({
    mutationFn: () => api(`/api/v1/inventory/physical-inventory-requests?organizationId=${organizationId}`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ inventoryLocationId: transferLocationId, reason: 'Live Inventory', priority: 'NORMAL', surpriseCount: false }) }),
  });
  const live = detail.data && detail.data.material.id === selectedId ? detail.data : undefined;
  const localRow = live?.availability.find((item) => item.inventoryLocationId === transferLocationId);
  const required = Number(requiredQty) || 0;
  const localAvailable = localRow?.availableQty ?? live?.localAvailable ?? 0;
  const shortage = Math.max(0, required - localAvailable);
  const raiseFrom = (sourceId: string, extras: Record<string, unknown> = {}) => {
    if (!transferLocationId) { setTransferMessage('Select a Transfer Location first.'); return; }
    if (sourceId === transferLocationId) { setTransferMessage('Pick a different availability location as the source.'); return; }
    const source = live?.availability.find((item) => item.inventoryLocationId === sourceId);
    if (!source?.transferEnabled) { setTransferMessage('Transfers are disabled for that location.'); return; }
    const qty = Math.max(1, Math.min(required || shortage || 1, source.transferableQty || required || 1));
    createIto.mutate({ mode: extras.mode ?? 'STANDARD', fromInventoryLocationId: sourceId, toInventoryLocationId: transferLocationId, reason: extras.reason ?? 'Live Inventory transfer', alreadyCollected: false, ...extras, lines: [{ materialId: selectedId, quantity: extras.quantity ?? qty }] });
  };
  const pickSource = (row: Availability) => {
    setSourceLocationId(row.inventoryLocationId);
    if (!row.transferEnabled || row.transferableQty <= 0) {
      setTransferMessage(`${row.locationName} has no transferable stock.`);
      return;
    }
    raiseFrom(row.inventoryLocationId, { reason: `Transfer from ${row.locationName}` });
  };
  return (
    <>
      <SilaPageHeader eyebrow="Inventory" title="Live Inventory" description="Search the Material Master on the server. Quantities come from the inventory ledger. Recommendations never auto-create transfers or PRs." />
      <div className="sila-inventory-editor">
        <div className="sila-inventory-stack">
          <section className="sila-card sila-recipe-panel">
            <h3 className="sila-recipe-panel__title">Material search</h3>
            <div className="sila-recipe-search">
              <div className="sila-recipe-search__input">
                <input className="sila-input" value={text} onChange={(event) => { setText(event.target.value); setResultsOpen(true); }} placeholder="Material ID, name, barcode, External ID" />
              </div>
              {resultsOpen && search.data?.items.length ? (
                <div className="sila-card sila-recipe-results">
                  {search.data.items.map((item) => (
                    <button type="button" key={item.id} className="sila-recipe-result" onClick={() => { setSelectedId(item.id); setSourceLocationId(''); setResultsOpen(false); setTransferMessage(''); }}>
                      <span><strong>{item.materialCode}</strong><span>{item.description}</span></span>
                      <em>{item.totalAvailable}</em>
                    </button>
                  ))}
                </div>
              ) : null}
            </div>
            <p className="sila-muted-copy" style={{ margin: '8px 0 0' }}>Type at least two characters. Exact Material ID matches first.</p>
          </section>
          {selectedId && live && (
            <section className="sila-card sila-recipe-panel">
              <h3 className="sila-recipe-panel__title">Material</h3>
              <div className="sila-form-grid">
                <Field label="Material ID"><input className="sila-input" readOnly value={live.material.materialCode} /></Field>
                <Field label="Description"><input className="sila-input" readOnly value={live.material.description} /></Field>
                <Field label="Base UOM"><input className="sila-input" readOnly value={live.material.baseUom} /></Field>
                <Field label="Inventory type"><input className="sila-input" readOnly value={live.inventoryType ?? '—'} /></Field>
                <Field label="Stocking here"><input className="sila-input" readOnly value={live.notStockedAtLocation ? 'NOT STOCKED AT THIS LOCATION' : (live.localStockingStatus ?? '—')} /></Field>
                <Field label="Batch / expiry / serial"><input className="sila-input" readOnly value={`${live.batchManaged ? 'Batch' : '—'} / ${live.expiryManaged ? 'Expiry' : '—'} / ${live.serialManaged ? 'Serial' : '—'}`} /></Field>
                <Field label="Transfer location"><select className="sila-select" value={transferLocationId} onChange={(event) => setTransferLocationId(event.target.value)}><option value="">Unspecified</option>{(locations.data ?? []).map((item) => <option key={item.id} value={item.id}>{item.locationName}</option>)}</select></Field>
                <Field label="Required quantity"><input className="sila-input" type="number" min="0" value={requiredQty} onChange={(event) => setRequiredQty(event.target.value)} /></Field>
                <Field label="Available locally"><input className="sila-input" readOnly value={String(localAvailable)} /></Field>
                <Field label="Shortage"><input className="sila-input" readOnly value={String(shortage)} /></Field>
              </div>
              <h3 className="sila-recipe-panel__title" style={{ marginTop: 16 }}>Recommendation</h3>
              <p className="sila-muted-copy" style={{ margin: 0 }}><strong>{live.recommendation.replaceAll('_', ' ')}</strong> · {live.recommendationReason}</p>
              <div className="sila-integration-actions">
                {live.nextActions.includes('REQUEST_TRANSFER') && <button type="button" className="sila-button sila-button--primary" onClick={() => {
                  const source = live.availability.find((item) => item.inventoryLocationId !== transferLocationId && item.transferableQty > 0);
                  if (source) raiseFrom(source.inventoryLocationId, { reason: 'Live Inventory recommendation' });
                }}>Request Transfer</button>}
                {live.nextActions.includes('QUICK_TRANSFER') && <button type="button" className="sila-button" onClick={() => {
                  const source = live.availability.find((item) => item.inventoryLocationId !== transferLocationId && item.transferableQty >= 1);
                  if (source) raiseFrom(source.inventoryLocationId, { mode: 'QUICK', reason: 'Guest Service', quantity: 1 });
                }}>Quick Transfer</button>}
                {live.nextActions.includes('REDISTRIBUTE_STOCK') && <button type="button" className="sila-button" onClick={() => {
                  const source = live.availability.find((item) => item.inventoryLocationId !== transferLocationId && item.transferableQty > 0);
                  if (source) raiseFrom(source.inventoryLocationId, { reason: 'Redistribute stock' });
                }}>Redistribute Stock</button>}
                {live.nextActions.includes('ADD_TO_LOCATION') && <button type="button" className="sila-button sila-button--primary" onClick={async () => {
                  const source = live.availability.find((item) => item.inventoryLocationId !== transferLocationId && item.transferableQty > 0);
                  if (!source || !transferLocationId) return;
                  await assignHere.mutateAsync();
                  raiseFrom(source.inventoryLocationId, { reason: 'Add to location', addToLocation: true });
                }}>Add to Location & Request Transfer</button>}
                {live.nextActions.includes('ONE_TIME_TRANSFER') && <button type="button" className="sila-button" onClick={() => {
                  const source = live.availability.find((item) => item.inventoryLocationId !== transferLocationId && item.transferableQty > 0);
                  if (source) raiseFrom(source.inventoryLocationId, { mode: 'QUICK', reason: 'One-time transfer', oneTimeTransfer: true });
                }}>One-Time Transfer</button>}
                {live.nextActions.includes('REQUEST_ITEM') && <button type="button" className="sila-button" onClick={() => {
                  const source = live.availability.find((item) => item.inventoryLocationId !== transferLocationId && item.transferableQty > 0);
                  if (source) raiseFrom(source.inventoryLocationId, { reason: 'Request item' });
                }}>Request Item</button>}
                {live.nextActions.includes('CREATE_PR') && <button type="button" className="sila-button" onClick={() => createPr.mutate()}>Create Purchase Request</button>}
                {transferLocationId && <button type="button" className="sila-button" onClick={() => opening.mutate()}>Post Opening Stock</button>}
                {live.nextActions.includes('REQUEST_PHYSICAL_INVENTORY') && <button type="button" className="sila-button" disabled={!transferLocationId} onClick={() => count.mutate()}>Request Physical Inventory</button>}
                <Link href="/inventory/transfers" className="sila-button">View Incoming Transfer</Link>
                <Link href="/inventory/transactions" className="sila-button">View Transactions</Link>
              </div>
              {(transferMessage || createIto.data) && (
                <p className="sila-muted-copy" style={{ margin: '12px 0 0' }} role="status">
                  {createIto.data ? <Link href={`/inventory/transfers/${createIto.data.id}`}>Opened {createIto.data.itoNumber}</Link> : transferMessage}
                  {transferMessage && createIto.data ? ` — ${transferMessage}` : null}
                </p>
              )}
            </section>
          )}
        </div>
        <section className="sila-card sila-recipe-panel">
          <h3 className="sila-recipe-panel__title">Availability by location</h3>
          {live ? (
            <>
              <p className="sila-muted-copy" style={{ margin: '0 0 10px' }}>Click a location with transferable stock to raise a transfer order into the Transfer Location.</p>
              <div className="sila-recipe-grid-wrap">
                <table className="sila-table"><thead><tr><th>Location</th><th>Type</th><th>Property</th><th>On Hand</th><th>Reserved</th><th>Available</th><th>In Transit</th><th>Transferable Qty</th><th>Stocking</th><th>Stock Status</th></tr></thead>
                  <tbody>{live.availability.map((row) => (
                    <tr
                      key={row.inventoryLocationId}
                      className={sourceLocationId === row.inventoryLocationId ? 'sila-row--active' : undefined}
                      style={{ cursor: row.transferableQty > 0 && row.transferEnabled ? 'pointer' : 'default' }}
                      onClick={() => pickSource(row)}
                    >
                      <td>{row.locationName}</td><td>{row.locationType}</td><td>{row.propertyCode}</td><td>{row.onHandQty}</td><td>{row.reservedQty}</td><td>{row.availableQty}</td><td>{row.inTransitQty}</td><td>{row.transferableQty}</td><td>{row.stockingStatus ?? '—'}</td><td>{row.stockStatus}</td>
                    </tr>
                  ))}</tbody></table>
              </div>
              <p className="sila-muted-copy" style={{ margin: '12px 0 0' }}>{live.availability[0]?.transferableBasis}</p>
            </>
          ) : <p className="sila-muted-copy" style={{ margin: 0 }}>{selectedId && detail.isFetching ? 'Loading availability…' : 'Select a material to see location availability.'}</p>}
        </section>
      </div>
    </>
  );
}

export function InventoryTransfersPage() {
  const { organizationId } = useOrg();
  const [filters, setFilters] = useState(() => {
    const status = typeof window === 'undefined' ? '' : new URLSearchParams(window.location.search).get('status') ?? '';
    const mapped = status === 'AWAITING_APPROVAL' ? 'PENDING_APPROVAL' : status === 'AWAITING_RECEIPT' ? 'DISPATCHED' : status === 'DISCREPANCIES' ? 'DISCREPANCY' : status;
    return { mode: '', status: mapped, fromLocationId: '', toLocationId: '', propertyId: '', fromDate: '', toDate: '' };
  });
  const locations = useQuery({ queryKey: ['inventory-locations', organizationId], queryFn: () => api<LocationRow[]>(`/api/v1/inventory/locations?organizationId=${organizationId}`), enabled: Boolean(organizationId), retry: false });
  const list = useQuery({
    queryKey: ['inventory-transfers', organizationId, filters],
    queryFn: () => api<ItoRow[]>(`/api/v1/inventory/transfers?${qs(organizationId!, filters)}`),
    enabled: Boolean(organizationId), retry: false,
  });
  return (
    <>
      <SilaPageHeader eyebrow="Inventory" title="Internal Transfers" description="One Internal Transfer Order for every location relationship. STANDARD and QUICK share the same document and ledger."
        actions={<Link href="/inventory/transfers/new" className="sila-button sila-button--primary">New ITO</Link>} />
      <div className="sila-filter-panel">
        <label>Mode<select className="sila-select" value={filters.mode} onChange={(event) => setFilters({ ...filters, mode: event.target.value })}><option value="">All</option><option>STANDARD</option><option>QUICK</option></select></label>
        <label>Status<input className="sila-input" value={filters.status} onChange={(event) => setFilters({ ...filters, status: event.target.value })} /></label>
        <label>From<select className="sila-select" value={filters.fromLocationId} onChange={(event) => setFilters({ ...filters, fromLocationId: event.target.value })}><option value="">All</option>{(locations.data ?? []).map((item) => <option key={item.id} value={item.id}>{item.locationName}</option>)}</select></label>
        <label>To<select className="sila-select" value={filters.toLocationId} onChange={(event) => setFilters({ ...filters, toLocationId: event.target.value })}><option value="">All</option>{(locations.data ?? []).map((item) => <option key={item.id} value={item.id}>{item.locationName}</option>)}</select></label>
        <label>Property<select className="sila-select" value={filters.propertyId} onChange={(event) => setFilters({ ...filters, propertyId: event.target.value })}><option value="">All</option>{(locations.data ?? []).filter((item) => item.locationType === 'PROPERTY').map((item) => <option key={item.id} value={item.id}>{item.locationName}</option>)}</select></label>
        <label>From date<input className="sila-input" type="date" value={filters.fromDate} onChange={(event) => setFilters({ ...filters, fromDate: event.target.value })} /></label>
        <label>To date<input className="sila-input" type="date" value={filters.toDate} onChange={(event) => setFilters({ ...filters, toDate: event.target.value })} /></label>
      </div>
      <section className="sila-card sila-recipe-panel">
        <h3 className="sila-recipe-panel__title">Transfer orders</h3>
        <div className="sila-recipe-grid-wrap">
          {list.isPending ? <div className="sila-loading-row" /> : !list.data?.length ? <p className="sila-muted-copy" style={{ margin: 0 }}>No internal transfers.</p> : (
            <table className="sila-table"><thead><tr><th>ITO Number</th><th>Mode</th><th>From</th><th>To</th><th>Transfer Relationship</th><th>Requested Date</th><th>Required By</th><th>Total Value</th><th>Status</th><th>Requested By</th></tr></thead>
              <tbody>{list.data?.map((item) => (
                <tr key={item.id}><td><Link href={`/inventory/transfers/${item.id}`}>{item.itoNumber}</Link></td><td>{item.mode}</td><td>{item.fromLocation}</td><td>{item.toLocation}</td><td>{item.transferRelationship}</td><td>{item.requestedAt.slice(0, 10)}</td><td>{item.requiredBy?.slice(0, 10) ?? ''}</td><td>{item.totalValue} {item.currency ?? ''}</td><td><StatusBadge value={item.status} /></td><td>{item.requestedByName}</td></tr>
              ))}</tbody></table>
          )}
        </div>
      </section>
    </>
  );
}

export function InventoryTransferNewPage() {
  const { organizationId } = useOrg();
  const [, setLocation] = useLocation();
  const locations = useQuery({ queryKey: ['inventory-locations', organizationId], queryFn: () => api<LocationRow[]>(`/api/v1/inventory/locations?organizationId=${organizationId}`), enabled: Boolean(organizationId), retry: false });
  const [form, setForm] = useState({ mode: 'STANDARD', fromInventoryLocationId: '', toInventoryLocationId: '', reason: '', requiredBy: '', materialQuery: '', materialId: '', quantity: '1', alreadyCollected: false });
  const [hits, setHits] = useState<MaterialHit[]>([]);
  useEffect(() => {
    const handle = setTimeout(() => {
      if (!organizationId || form.materialQuery.trim().length < 2) { setHits([]); return; }
      void api<{ items: MaterialHit[] }>(`/api/v1/inventory/live/search?${qs(organizationId, { query: form.materialQuery, pageSize: '12' })}`).then((result) => setHits(result.items));
    }, 300);
    return () => clearTimeout(handle);
  }, [form.materialQuery, organizationId]);
  const save = useMutation({
    mutationFn: () => api<ItoDetail>(`/api/v1/inventory/transfers?organizationId=${organizationId}`, {
      method: 'POST', headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ mode: form.mode, fromInventoryLocationId: form.fromInventoryLocationId, toInventoryLocationId: form.toInventoryLocationId, reason: form.reason, requiredBy: form.requiredBy || null, alreadyCollected: form.alreadyCollected, lines: [{ materialId: form.materialId, quantity: Number(form.quantity) }] }),
    }),
    onSuccess: (item) => setLocation(`/inventory/transfers/${item.id}`),
  });
  return (
    <>
      <SilaPageHeader eyebrow="Inventory" title="New Internal Transfer" description="From and To are inventory locations. Relationship is derived from Location Type." />
      <form onSubmit={(event) => { event.preventDefault(); save.mutate(); }}>
        <div className="sila-inventory-editor">
          <section className="sila-card sila-recipe-panel">
            <h3 className="sila-recipe-panel__title">Transfer header</h3>
            <div className="sila-form-grid">
              <Field label="Mode"><select className="sila-select" value={form.mode} onChange={(event) => setForm({ ...form, mode: event.target.value })}><option>STANDARD</option><option>QUICK</option></select></Field>
              <Field label="Required by"><input className="sila-input" type="date" value={form.requiredBy} onChange={(event) => setForm({ ...form, requiredBy: event.target.value })} /></Field>
              <Field label="From"><select className="sila-select" required value={form.fromInventoryLocationId} onChange={(event) => setForm({ ...form, fromInventoryLocationId: event.target.value })}><option value="">Select</option>{(locations.data ?? []).filter((item) => item.transferEnabled).map((item) => <option key={item.id} value={item.id}>{item.locationName} ({item.locationType})</option>)}</select></Field>
              <Field label="To"><select className="sila-select" required value={form.toInventoryLocationId} onChange={(event) => setForm({ ...form, toInventoryLocationId: event.target.value })}><option value="">Select</option>{(locations.data ?? []).filter((item) => item.transferEnabled).map((item) => <option key={item.id} value={item.id}>{item.locationName} ({item.locationType})</option>)}</select></Field>
              <Field label="Reason" full><input className="sila-input" value={form.reason} onChange={(event) => setForm({ ...form, reason: event.target.value })} /></Field>
            </div>
            <div className="sila-inventory-flags" style={{ marginTop: 12 }}>
              <label><input type="checkbox" checked={form.alreadyCollected} onChange={(event) => setForm({ ...form, alreadyCollected: event.target.checked, mode: event.target.checked ? 'QUICK' : form.mode })} /> Record already collected</label>
            </div>
          </section>
          <section className="sila-card sila-recipe-panel">
            <h3 className="sila-recipe-panel__title">Material line</h3>
            <div className="sila-recipe-search">
              <div className="sila-recipe-search__input">
                <input className="sila-input" value={form.materialQuery} onChange={(event) => setForm({ ...form, materialQuery: event.target.value })} placeholder="Search material ID or name" />
                <input className="sila-input" type="number" min="0.0001" value={form.quantity} onChange={(event) => setForm({ ...form, quantity: event.target.value })} aria-label="Quantity" />
              </div>
              {hits.length > 0 && (
                <div className="sila-card sila-recipe-results">
                  {hits.map((item) => (
                    <button type="button" key={item.id} className="sila-recipe-result" onClick={() => setForm({ ...form, materialId: item.id, materialQuery: `${item.materialCode} ${item.description}` })}>
                      <span><strong>{item.materialCode}</strong><span>{item.description}</span></span>
                    </button>
                  ))}
                </div>
              )}
            </div>
            <div className="sila-integration-actions">
              <button type="submit" className="sila-button sila-button--primary" disabled={!form.materialId}>Submit ITO</button>
            </div>
          </section>
        </div>
      </form>
    </>
  );
}

export function InventoryTransferDetailPage() {
  const { organizationId } = useOrg();
  const params = useParams<{ id: string }>();
  const client = useQueryClient();
  const [qty, setQty] = useState('');
  const [comment, setComment] = useState('');
  const detail = useQuery({
    queryKey: ['ito', organizationId, params.id],
    queryFn: () => api<ItoDetail>(`/api/v1/inventory/transfers/${params.id}?organizationId=${organizationId}`),
    enabled: Boolean(organizationId && params.id), retry: false,
  });
  const act = useMutation({
    mutationFn: (path: string) => api(`/api/v1/inventory/transfers/${params.id}/${path.replace('?confirm', '')}?organizationId=${organizationId}`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ quantity: Number(qty) || 0, approvedQty: Number(qty) || null, comment, confirmAlreadyCollected: path.includes('confirm') }) }),
    onSuccess: () => client.invalidateQueries({ queryKey: ['ito', organizationId, params.id] }),
  });
  const item = detail.data;
  if (!item) return <SilaPageHeader eyebrow="Inventory" title="Internal Transfer" />;
  return (
    <>
      <SilaPageHeader eyebrow="Inventory" title={item.itoNumber} description={`${item.mode} · ${item.transferRelationship}`} actions={<StatusBadge value={item.status} />} />
      <div className="sila-inventory-editor">
        <section className="sila-card sila-recipe-panel">
          <h3 className="sila-recipe-panel__title">Transfer header</h3>
          <div className="sila-form-grid">
            <Field label="From"><input className="sila-input" readOnly value={`${item.fromLocation} (${item.fromType})`} /></Field>
            <Field label="To"><input className="sila-input" readOnly value={`${item.toLocation} (${item.toType})`} /></Field>
            <Field label="Mode"><input className="sila-input" readOnly value={item.mode} /></Field>
            <Field label="Value"><input className="sila-input" readOnly value={`${item.totalValue} ${item.currency ?? ''}`.trim()} /></Field>
            <Field label="Reason" full><input className="sila-input" readOnly value={item.reason ?? ''} /></Field>
          </div>
          <h3 className="sila-recipe-panel__title" style={{ marginTop: 16 }}>Approvals</h3>
          {item.approvals.length === 0 ? <p className="sila-muted-copy" style={{ margin: 0 }}>No manager approval rows.</p> : item.approvals.map((row) => (
            <p key={row.id} className="sila-muted-copy" style={{ margin: '0 0 6px' }}>{row.side} · {row.status} · available {row.availableQty ?? '—'} · requested {row.requestedQty ?? '—'} · after {row.stockAfter ?? '—'}</p>
          ))}
          <h3 className="sila-recipe-panel__title" style={{ marginTop: 16 }}>History</h3>
          {item.events.map((row) => <p key={row.id} className="sila-muted-copy" style={{ margin: '0 0 6px' }}>{row.createdAt.slice(0, 19)} · {row.action} {row.comment ?? ''}</p>)}
        </section>
        <section className="sila-card sila-recipe-panel">
          <h3 className="sila-recipe-panel__title">Lines</h3>
          <div className="sila-recipe-grid-wrap">
            <table className="sila-table"><thead><tr><th>Material</th><th>Description</th><th>Requested</th><th>Approved</th><th>Dispatched</th><th>Received</th><th>UOM</th><th>Unit Cost</th><th>Value</th><th>Source available</th><th>After</th></tr></thead>
              <tbody>{item.lines.map((line) => <tr key={line.id}><td>{line.materialCode}</td><td>{line.description}</td><td>{line.requestedQty}</td><td>{line.approvedQty}</td><td>{line.dispatchedQty}</td><td>{line.receivedQty}</td><td>{line.uom}</td><td>{line.unitCost ?? '—'}</td><td>{line.transferValue}</td><td>{line.sourceAvailable ?? '—'}</td><td>{line.sourceAfter ?? '—'}</td></tr>)}</tbody></table>
          </div>
          <h3 className="sila-recipe-panel__title" style={{ marginTop: 16 }}>Actions</h3>
          <div className="sila-form-grid">
            <Field label="Qty / approved qty"><input className="sila-input" value={qty} onChange={(event) => setQty(event.target.value)} /></Field>
            <Field label="Comment"><input className="sila-input" value={comment} onChange={(event) => setComment(event.target.value)} /></Field>
          </div>
          <div className="sila-integration-actions">
            {item.allowedActions.includes('APPROVE') && <button type="button" className="sila-button sila-button--primary" onClick={() => act.mutate('approve')}>Approve</button>}
            {item.allowedActions.includes('APPROVE') && <button type="button" className="sila-button" onClick={() => act.mutate('approve')}>Change approved qty</button>}
            {item.allowedActions.includes('REJECT') && <button type="button" className="sila-button" onClick={() => act.mutate('reject')}>Reject</button>}
            {item.allowedActions.includes('CONFIRM_HANDOVER') && <button type="button" className="sila-button sila-button--primary" onClick={() => act.mutate('handover')}>Hand over</button>}
            {item.allowedActions.includes('DISPATCH') && !item.allowedActions.includes('CONFIRM_HANDOVER') && <button type="button" className="sila-button sila-button--primary" onClick={() => act.mutate('dispatch')}>Dispatch</button>}
            {item.allowedActions.includes('DISPATCH') && item.allowedActions.includes('CONFIRM_HANDOVER') && <button type="button" className="sila-button" onClick={() => act.mutate('dispatch')}>Dispatch</button>}
            {item.allowedActions.includes('CONFIRM_ALREADY_COLLECTED') && <button type="button" className="sila-button" onClick={() => act.mutate('dispatch?confirm')}>Confirm already collected</button>}
            {item.allowedActions.includes('DISPUTE_ALREADY_COLLECTED') && <button type="button" className="sila-button" onClick={() => act.mutate('dispute-already-collected')}>Dispute already collected</button>}
            {item.allowedActions.includes('RECEIVE') && <button type="button" className="sila-button sila-button--primary" onClick={() => act.mutate('receive')}>Confirm receipt</button>}
            {item.allowedActions.includes('REPORT_DISCREPANCY') && <button type="button" className="sila-button" onClick={() => act.mutate('discrepancy')}>Report discrepancy</button>}
          </div>
        </section>
      </div>
    </>
  );
}
