import { useEffect, useRef, useState, type FormEvent } from 'react';
import { Link, useLocation, useParams } from 'wouter';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Download, Info, Plus, Search, Trash2, UploadCloud } from 'lucide-react';
import { customFetch, getGetAccessContextQueryKey, useGetAccessContext } from '@workspace/api-client-react';
import { QueryError, SilaDataTable, SilaPageHeader, StatusBadge } from '@/components/sila-ui';
import {
  MaterialPricePanel, canCorrectPrice, money, priceStatusLabel, unitPriceDisplay,
  type MaterialPriceRow,
} from '@/components/material-price-panel';
import { RecipeMaterialsPage } from '@/pages/material-master';
import { RecipeLocationsPage } from '@/pages/recipe-locations';
export { RecipeMaterialsPage, RecipeLocationsPage };

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

export function RecipeDashboardPage() {
  const { organizationId } = useOrg();
  const query = useQuery({
    queryKey: ['recipe-dashboard', organizationId],
    queryFn: () => api<{ recipeCount: number; activeVersionCount: number; pendingApprovalCount: number; materialCount: number; failedTransactionCount: number; postedConsumptionCount: number }>(`/api/v1/recipe-management/dashboard?organizationId=${organizationId}`),
    enabled: Boolean(organizationId), retry: false,
  });
  const data = query.data;
  const kpis = [
    { label: 'Active recipes', value: data?.activeVersionCount, note: `${data?.recipeCount ?? 0} recipes in master`, href: '/recipe-management/recipes' },
    { label: 'Pending approvals', value: data?.pendingApprovalCount, note: 'CREATE and CHANGE tasks', href: '/recipe-management/approvals' },
    { label: 'Failed POS sales', value: data?.failedTransactionCount, note: 'Need mapping or stock posting', href: '/recipe-management/transactions' },
    { label: 'Posted consumption', value: data?.postedConsumptionCount, note: 'Sales already deducted from stock', href: '/recipe-management/transactions' },
  ];
  return (
    <>
      <SilaPageHeader eyebrow="Recipe Management" title="Recipe Control Center" description="Approved recipes, POS sales posting, and consumption that feeds inventory. Menu Engineering is not built here."
        actions={<><Link href="/recipe-management/recipes/new" className="sila-button sila-button--primary">New recipe</Link><Link href="/recipe-management/pos" className="sila-button">POS sales upload</Link></>} />
      {query.isError ? <QueryError onRetry={() => query.refetch()} /> : (
        <>
          <div className="sila-control-kpi">
            {kpis.map((kpi) => (
              <Link key={kpi.label} href={kpi.href} className="sila-card sila-kpi">
                <span className="sila-kpi__label">{kpi.label}</span>
                <strong className="sila-kpi__value" data-testid={`metric-${kpi.label}`}>{query.isPending ? '…' : kpi.value ?? 0}</strong>
                <span className="sila-kpi__note">{kpi.note}</span>
              </Link>
            ))}
          </div>
          <div className="sila-control-split">
            <section className="sila-card sila-recipe-panel">
              <div className="sila-section-head"><h3>What to do next</h3></div>
              <ul className="sila-health-list">
                <li><span>Materials</span><strong><Link href="/recipe-management/master-data">{data?.materialCount ?? 0} active</Link></strong></li>
                <li><span>Approvals waiting</span><strong><Link href="/recipe-management/approvals">{data?.pendingApprovalCount ?? 0}</Link></strong></li>
                <li><span>POS posting failures</span><strong><Link href="/recipe-management/transactions">{data?.failedTransactionCount ?? 0}</Link></strong></li>
              </ul>
              {(data?.failedTransactionCount ?? 0) === 0 && (data?.pendingApprovalCount ?? 0) === 0 ? <p className="sila-muted-copy" style={{ margin: '12px 0 0' }}>No recipe or POS exceptions need action.</p> : null}
            </section>
            <section className="sila-card sila-recipe-panel">
              <div className="sila-section-head"><h3>Consumption</h3></div>
              <p className="sila-muted-copy" style={{ margin: 0 }}>
                {(data?.postedConsumptionCount ?? 0) === 0
                  ? 'No consumption transactions posted yet. Upload POS sales to deduct outlet stock.'
                  : `${data?.postedConsumptionCount} POS sales posted to inventory. Open Transaction Tracker for the audit trail.`}
              </p>
              <div className="sila-integration-actions" style={{ borderTop: 'none', paddingTop: 12 }}>
                <Link href="/recipe-management/pos" className="sila-button sila-button--primary">Upload POS sales</Link>
                <Link href="/inventory/dashboard" className="sila-button">Inventory Control Center</Link>
              </div>
            </section>
          </div>
        </>
      )}
    </>
  );
}

export function RecipeFamiliesPage() {
  return <RecipeCodeNameMaster kind="families" title="Family Master" description="Controlled family values for Recipe Create/Edit. Upload Code, Name, Description, Active." />;
}

export function RecipeCategoriesPage() {
  return <RecipeCodeNameMaster kind="categories" title="Category Master" description="Controlled category values for Recipe Create/Edit. Upload Code, Name, Description, Active." />;
}

function RecipeCodeNameMaster({ kind, title, description }: { kind: 'families' | 'categories'; title: string; description: string }) {
  const { organizationId } = useOrg();
  const client = useQueryClient();
  const fileRef = useRef<HTMLInputElement>(null);
  const pendingFile = useRef<File | null>(null);
  const [form, setForm] = useState({ code: '', name: '', description: '', status: 'ACTIVE' });
  const [preview, setPreview] = useState<{ fileName: string; totalRows: number; validRows: number; invalidRows: number } | null>(null);
  const query = useQuery({
    queryKey: ['recipe-master', kind, organizationId],
    queryFn: () => api<Array<{ id: string; code: string; name: string; description?: string; status: string }>>(`/api/v1/recipe-management/${kind}?organizationId=${organizationId}`),
    enabled: Boolean(organizationId), retry: false,
  });
  const save = useMutation({
    mutationFn: () => api(`/api/v1/recipe-management/${kind}?organizationId=${organizationId}`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(form) }),
    onSuccess: () => { setForm({ code: '', name: '', description: '', status: 'ACTIVE' }); client.invalidateQueries({ queryKey: ['recipe-master', kind, organizationId] }); },
  });
  const previewFile = useMutation({
    mutationFn: async (file: File) => {
      const body = new FormData();
      body.append('file', file);
      return api<NonNullable<typeof preview>>(`/api/v1/recipe-management/${kind}/import/preview?organizationId=${organizationId}`, { method: 'POST', body });
    },
    onSuccess: setPreview,
  });
  const commitFile = useMutation({
    mutationFn: async (file: File) => {
      const body = new FormData();
      body.append('file', file);
      return api(`/api/v1/recipe-management/${kind}/import?organizationId=${organizationId}`, { method: 'POST', body });
    },
    onSuccess: () => { setPreview(null); query.refetch(); },
  });
  return (
    <>
      <SilaPageHeader eyebrow="Recipe Management" title={title} description={description} actions={<>
        <button className="sila-button" type="button" onClick={() => fileRef.current?.click()}><UploadCloud size={14} /> Upload Excel</button>
        <a className="sila-button" href={`/api/v1/recipe-management/${kind}/export?organizationId=${organizationId}`}><Download size={14} /> Download</a>
        <a className="sila-button" href={`/api/v1/recipe-management/${kind}/template?organizationId=${organizationId}`}><Download size={14} /> Download Template</a>
      </>} />
      <input ref={fileRef} type="file" accept=".xlsx" hidden onChange={(event) => { const file = event.target.files?.[0]; if (file) { pendingFile.current = file; previewFile.mutate(file); } event.target.value = ''; }} />
      {preview && (
        <section className="sila-card" style={{ padding: 20, marginBottom: 18 }}>
          <h3>Excel preview · {preview.fileName}</h3>
          <p>{preview.totalRows} rows · {preview.validRows} valid · {preview.invalidRows} failed</p>
          <div className="sila-integration-actions" style={{ marginTop: 14 }}>
            <button className="sila-button" type="button" onClick={() => setPreview(null)}>Cancel</button>
            <button className="sila-button sila-button--primary" type="button" disabled={!pendingFile.current || commitFile.isPending} onClick={() => pendingFile.current && commitFile.mutate(pendingFile.current)}>{commitFile.isPending ? 'Importing…' : 'Import'}</button>
          </div>
        </section>
      )}
      <div className="sila-filter-row">
        <input className="sila-input" placeholder="Code" value={form.code} onChange={(event) => setForm({ ...form, code: event.target.value })} />
        <input className="sila-input" placeholder="Name" value={form.name} onChange={(event) => setForm({ ...form, name: event.target.value })} />
        <input className="sila-input" placeholder="Description" value={form.description} onChange={(event) => setForm({ ...form, description: event.target.value })} />
        <select className="sila-select" value={form.status} onChange={(event) => setForm({ ...form, status: event.target.value })}>
          <option value="ACTIVE">Active</option>
          <option value="INACTIVE">Inactive</option>
        </select>
        <button className="sila-button sila-button--primary" type="button" onClick={() => save.mutate()}>Save</button>
      </div>
      <SilaDataTable loading={query.isPending} empty={!query.isPending && (query.data?.length ?? 0) === 0} emptyTitle="No records">
        <table className="sila-table"><thead><tr><th>Code</th><th>Name</th><th>Description</th><th>Active</th></tr></thead>
          <tbody>{query.data?.map((row) => <tr key={row.id}><td>{row.code}</td><td>{row.name}</td><td>{row.description}</td><td><StatusBadge value={row.status} /></td></tr>)}</tbody></table>
      </SilaDataTable>
    </>
  );
}

export function RecipesPage() {
  const { organizationId } = useOrg();
  const fileRef = useRef<HTMLInputElement>(null);
  const pendingFile = useRef<File | null>(null);
  const [preview, setPreview] = useState<{ fileName: string; totalRows: number; validRows: number; invalidRows: number; recipeCount: number } | null>(null);
  const query = useQuery({
    queryKey: ['recipes', organizationId],
    queryFn: () => api<Array<{
      id: string; recipeCode: string; title: string; posCode?: string; posItem?: string; servingUom?: string; servingQty?: number;
      family?: string; category?: string; menuPrice?: number | null; recipeCost?: number | null; costPerServing?: number | null;
      costPercent?: number | null; marginAmount?: number | null; marginPercent?: number | null; status: string;
      currency?: string; currentVersionNumber: number; readiness: string; readinessIssueCount: number;
    }>>(`/api/v1/recipe-management/recipes?organizationId=${organizationId}`),
    enabled: Boolean(organizationId), retry: false,
  });
  const previewFile = useMutation({
    mutationFn: async (file: File) => {
      const body = new FormData();
      body.append('file', file);
      return api<NonNullable<typeof preview>>(`/api/v1/recipe-management/recipes/import/preview?organizationId=${organizationId}`, { method: 'POST', body });
    },
    onSuccess: setPreview,
  });
  const commitFile = useMutation({
    mutationFn: async (file: File) => {
      const body = new FormData();
      body.append('file', file);
      return api(`/api/v1/recipe-management/recipes/import?organizationId=${organizationId}`, { method: 'POST', body });
    },
    onSuccess: () => { setPreview(null); query.refetch(); },
  });
  return (
    <>
      <SilaPageHeader eyebrow="Recipe Management" title="Recipe Master" description="Menu items, serving, costing, and Material Master ingredients. POS Code / POS Item / menu price update from POS Excel, mapping, or sales."
        actions={<>
          <Link href="/recipe-management/recipes/new" className="sila-button sila-button--primary"><Plus size={14} /> New recipe</Link>
          <button className="sila-button" type="button" onClick={() => fileRef.current?.click()}><UploadCloud size={14} /> Upload Excel</button>
          <a className="sila-button" href={`/api/v1/recipe-management/recipes/export?organizationId=${organizationId}`}><Download size={14} /> Download</a>
          <a className="sila-button" href={`/api/v1/recipe-management/recipes/template?organizationId=${organizationId}`}><Download size={14} /> Download Template</a>
        </>} />
      <input ref={fileRef} type="file" accept=".xlsx" hidden onChange={(event) => { const file = event.target.files?.[0]; if (file) { pendingFile.current = file; previewFile.mutate(file); } event.target.value = ''; }} />
      {preview && (
        <section className="sila-card" style={{ padding: 20, marginBottom: 18 }}>
          <h3>Excel preview · {preview.fileName}</h3>
          <p>{preview.totalRows} rows · {preview.validRows} valid · {preview.invalidRows} failed · {preview.recipeCount} recipes</p>
          <div className="sila-integration-actions" style={{ marginTop: 14 }}>
            <button className="sila-button" type="button" onClick={() => setPreview(null)}>Cancel</button>
            <button className="sila-button sila-button--primary" type="button" disabled={!pendingFile.current || commitFile.isPending} onClick={() => pendingFile.current && commitFile.mutate(pendingFile.current)}>{commitFile.isPending ? 'Importing…' : 'Import'}</button>
          </div>
        </section>
      )}
      <SilaDataTable loading={query.isPending} empty={!query.isPending && (query.data?.length ?? 0) === 0} emptyTitle="No recipes">
        <div className="sila-table-wrap">
          <table className="sila-table"><thead><tr>
            <th>POS Code</th><th>Title</th><th>POS Item</th><th>Serving UOM</th><th>Serving Qty</th><th>RecipeID</th><th>Family</th><th>Category</th>
            <th>Menu Price</th><th>Recipe Cost</th><th>Cost / Serving</th><th>Cost %</th><th>Margin</th><th>Status</th><th>Readiness</th>
          </tr></thead>
            <tbody>{query.data?.map((row) => (
              <tr key={row.id}>
                <td>{row.posCode}</td>
                <td><Link href={`/recipe-management/recipes/${row.id}`}>{row.title}</Link></td>
                <td>{row.posItem}</td>
                <td>{row.servingUom}</td>
                <td>{row.servingQty}</td>
                <td><Link href={`/recipe-management/recipes/${row.id}`}>{row.recipeCode}</Link></td>
                <td>{row.family}</td>
                <td>{row.category}</td>
                <td>{row.menuPrice}</td>
                <td>{row.recipeCost == null ? 'INCOMPLETE' : row.recipeCost}</td>
                <td>{row.costPerServing == null ? 'INCOMPLETE' : row.costPerServing}</td>
                <td>{row.recipeCost == null ? 'INCOMPLETE' : row.menuPrice == null ? 'NO MENU PRICE' : row.costPercent}</td>
                <td>{row.recipeCost == null ? 'INCOMPLETE' : row.menuPrice == null ? 'NO MENU PRICE' : row.marginAmount}</td>
                <td><StatusBadge value={row.status} /></td>
                <td>{row.readiness === 'READY_FOR_APPROVAL' ? 'READY FOR APPROVAL' : `NOT READY${row.readinessIssueCount ? ` (${row.readinessIssueCount})` : ''}`}</td>
              </tr>
            ))}</tbody>
          </table>
        </div>
      </SilaDataTable>
    </>
  );
}

type MaterialOption = MaterialPriceRow;
type IngredientLine = {
  recipeIngredientId: string; materialId: string; materialCode: string; materialName: string;
  quantity: string; uom: string; baseUom: string; unitCost: number | null; currency: string;
  priceStatus: string; priceStatusLabel: string; proposedUnitPrice: number | null; canUpdatePrice: boolean;
  consumptionQuantity: number | null; conversionMissing: boolean; packSummary: string;
  conversions: Array<{ fromUom: string; toUom: string; numerator: number; denominator: number }>;
};

function normalizeUom(value: string) {
  const key = value.trim().toUpperCase();
  if (['KILO', 'KILOGRAM', 'KILOGRAMS'].includes(key)) return 'KG';
  if (['GRAM', 'GRAMS'].includes(key)) return 'G';
  if (['LITRE', 'LITER', 'LITRES', 'LITERS', 'LT'].includes(key)) return 'L';
  if (['MILLILITRE', 'MILLILITER', 'MLS'].includes(key)) return 'ML';
  if (['EACH', 'UNIT', 'UNITS'].includes(key)) return 'EA';
  return key;
}
function dimension(uom: string) {
  const key = normalizeUom(uom);
  if (key === 'KG' || key === 'G') return 'MASS';
  if (key === 'L' || key === 'ML') return 'VOLUME';
  if (key === 'EA' || key === 'EACH' || key === 'PC' || key === 'PCS') return 'COUNT';
  return 'UNKNOWN';
}
function convertQty(quantity: number, from: string, to: string, conversions: IngredientLine['conversions'] = []) {
  const a = normalizeUom(from);
  const b = normalizeUom(to);
  if (a === b) return quantity;
  if (dimension(a) !== 'UNKNOWN' && dimension(a) === dimension(b)) {
    const canonical = (qty: number, uom: string) => (uom === 'G' || uom === 'ML' ? qty / 1000 : qty);
    return canonical(quantity, a) / canonical(1, b);
  }
  const match = conversions.find((item) => normalizeUom(item.fromUom) === a && normalizeUom(item.toUom) === b && item.denominator);
  if (match) return quantity * (match.numerator / match.denominator);
  const reverse = conversions.find((item) => normalizeUom(item.fromUom) === b && normalizeUom(item.toUom) === a && item.numerator);
  if (reverse) return quantity * (reverse.denominator / reverse.numerator);
  return null;
}

function recipeQtyAndUom(material: MaterialOption) {
  const convUnit = (material.convUnit || '').trim();
  const convValue = material.convValue;
  if (convUnit && convValue != null && Number(convValue) > 0) {
    return { quantity: String(convValue), uom: convUnit };
  }
  return { quantity: '1', uom: material.baseUom || 'EA' };
}
function materialConversions(material: MaterialOption): IngredientLine['conversions'] {
  const rows = material.conversions ?? [];
  if (rows.length) return rows;
  const convUnit = (material.convUnit || '').trim();
  if (convUnit && material.convValue && material.baseUom) {
    return [{ fromUom: material.baseUom, toUom: convUnit, numerator: material.convValue, denominator: material.convFactor || 1 }];
  }
  return [];
}
function nextIngredientId(recipeCode: string, existing: IngredientLine[]) {
  if (!recipeCode) return '';
  const used = new Set(existing.map((item) => item.recipeIngredientId).filter(Boolean));
  let line = existing.length + 1;
  let id = `${recipeCode}I${line}`;
  while (used.has(id)) {
    line += 1;
    id = `${recipeCode}I${line}`;
  }
  return id;
}

function costOrIncomplete(complete: boolean, value: string | number | null | undefined) {
  if (!complete) return 'INCOMPLETE';
  return value == null || value === '' ? 'INCOMPLETE' : String(value);
}

function profitOrReason(complete: boolean, hasMenu: boolean, value: string | number | null | undefined) {
  if (!complete) return 'INCOMPLETE';
  if (!hasMenu) return 'NO MENU PRICE';
  return value == null || value === '' ? 'NO MENU PRICE' : String(value);
}

function ingredientFromMaterial(material: MaterialOption): IngredientLine {
  const status = material.priceStatus || (material.hasValidUnitPrice ? 'PRICE_APPROVED' : 'PRICE_MISSING');
  const approved = status === 'PRICE_APPROVED' ? (material.unitCost ?? null) : null;
  const { quantity, uom } = recipeQtyAndUom(material);
  const conversions = materialConversions(material);
  const qty = Number(quantity) || 0;
  const consumption = convertQty(qty, uom, material.baseUom || uom, conversions);
  return {
    recipeIngredientId: '', materialId: material.id, materialCode: material.materialCode,
    materialName: material.name || material.description || '', quantity, uom,
    baseUom: material.baseUom || 'EA', unitCost: approved, currency: material.currency || 'AED',
    priceStatus: status, priceStatusLabel: material.priceStatusLabel || priceStatusLabel(status),
    proposedUnitPrice: material.proposedUnitPrice ?? null, canUpdatePrice: canCorrectPrice(status),
    consumptionQuantity: consumption, conversionMissing: consumption == null,
    packSummary: material.packSummary || material.conversionText || `Base: ${material.baseUom}`,
    conversions,
  };
}

function IngredientSearchBox({ organizationId, selectedIds, onSelect }: {
  organizationId: string | undefined; selectedIds: string[]; onSelect: (material: MaterialOption) => void;
}) {
  const [term, setTerm] = useState('');
  const [debounced, setDebounced] = useState('');
  const [category, setCategory] = useState('');
  const [materialGroup, setMaterialGroup] = useState('');
  const [materialType, setMaterialType] = useState('');
  const [supplier, setSupplier] = useState('');
  const [open, setOpen] = useState(false);
  useEffect(() => {
    const handle = window.setTimeout(() => setDebounced(term.trim()), 220);
    return () => window.clearTimeout(handle);
  }, [term]);
  const facets = useQuery({
    queryKey: ['recipe-material-facets', organizationId],
    queryFn: () => api<{ categories: string[]; materialGroups: string[]; materialTypes: string[]; suppliers: Array<{ id: string; code: string; name: string }> }>(
      `/api/v1/recipe-management/materials/search-facets?organizationId=${organizationId}`),
    enabled: Boolean(organizationId), retry: false,
  });
  const filtered = Boolean(category || materialGroup || materialType || supplier);
  const results = useQuery({
    queryKey: ['recipe-material-search', organizationId, debounced, category, materialGroup, materialType, supplier],
    queryFn: () => {
      const params = new URLSearchParams({ organizationId: organizationId || '' });
      if (debounced) params.set('query', debounced);
      if (category) params.set('category', category);
      if (materialGroup) params.set('materialGroup', materialGroup);
      if (materialType) params.set('materialType', materialType);
      if (supplier) params.set('supplier', supplier);
      return api<MaterialOption[]>(`/api/v1/recipe-management/materials?${params.toString()}`);
    },
    enabled: Boolean(organizationId) && (debounced.length > 0 || filtered), retry: false,
  });
  return (
    <div className="sila-recipe-search">
      <div className="sila-recipe-search__input">
        <input className="sila-input" data-testid="input-ingredient-search" placeholder="Search Material ID, name, category, group, or supplier"
          value={term} onChange={(event) => { setTerm(event.target.value); setOpen(true); }} onFocus={() => setOpen(true)} />
        <button className="sila-button sila-button--primary" type="button" data-testid="button-add-ingredient" onClick={() => setOpen(true)}><Search size={14} /> Add Ingredient</button>
      </div>
      <div className="sila-recipe-search__filters">
        <select className="sila-select" data-testid="filter-material-category" value={category} onChange={(event) => { setCategory(event.target.value); setOpen(true); }}>
          <option value="">All categories</option>
          {facets.data?.categories.map((item) => <option key={item} value={item}>{item}</option>)}
        </select>
        <select className="sila-select" data-testid="filter-material-group" value={materialGroup} onChange={(event) => { setMaterialGroup(event.target.value); setOpen(true); }}>
          <option value="">All material groups</option>
          {facets.data?.materialGroups.map((item) => <option key={item} value={item}>{item}</option>)}
        </select>
        <select className="sila-select" data-testid="filter-material-type" value={materialType} onChange={(event) => { setMaterialType(event.target.value); setOpen(true); }}>
          <option value="">All material types</option>
          {facets.data?.materialTypes.map((item) => <option key={item} value={item}>{item}</option>)}
        </select>
        <select className="sila-select" data-testid="filter-material-supplier" value={supplier} onChange={(event) => { setSupplier(event.target.value); setOpen(true); }}>
          <option value="">All suppliers</option>
          {facets.data?.suppliers.map((item) => <option key={item.id} value={item.id}>{item.code} {item.name}</option>)}
        </select>
      </div>
      {open && (debounced || filtered) && (
        <div className="sila-card sila-recipe-results" data-testid="ingredient-search-results">
          {results.isPending && <p className="sila-muted-copy" style={{ margin: 8 }}>Searching Material Master…</p>}
          {results.isError && <p className="sila-muted-copy" style={{ margin: 8 }}>Material search failed.</p>}
          {!results.isPending && (results.data?.length ?? 0) === 0 && <p className="sila-muted-copy" style={{ margin: 8 }}>No materials matched.</p>}
          {results.data?.map((item) => {
            const status = item.priceStatus || (item.hasValidUnitPrice ? 'PRICE_APPROVED' : 'PRICE_MISSING');
            const already = selectedIds.includes(item.id);
            const meta = [item.category, item.materialGroup, item.materialType, item.supplierSummary].filter(Boolean).join(' · ');
            return (
              <button key={item.id} className="sila-recipe-result" type="button" disabled={already} data-testid={`material-result-${item.materialCode}`}
                onMouseDown={(event) => { event.preventDefault(); if (!already) { onSelect(item); setTerm(''); setOpen(false); } }}>
                <span>
                  <strong>{item.materialCode}</strong>
                  <span>{item.name || item.description}</span>
                  {meta && <span>{meta}</span>}
                  <span>Base UOM: {item.baseUom}{item.packSummary ? ` · ${item.packSummary}` : ''}</span>
                  <span>{unitPriceDisplay(status, item.hasValidUnitPrice ? (item.unitCost ?? null) : null, item.proposedUnitPrice ?? null, item.currency || 'AED', item.baseUom)} · {item.priceStatusLabel || priceStatusLabel(status)}</span>
                </span>
                {already ? <em>ADDED</em> : status === 'PRICE_MISSING' ? <em>PRICE MISSING · Add</em> : <span>Add</span>}
              </button>
            );
          })}
        </div>
      )}
    </div>
  );
}

type RecipeLocationOption = { id: string; kind: string; code: string; name: string; description?: string | null };

function LocationAssignmentSelect({
  options, selectedIds, onChange,
}: {
  options: RecipeLocationOption[];
  selectedIds: string[];
  onChange: (ids: string[]) => void;
}) {
  const [open, setOpen] = useState(false);
  const [term, setTerm] = useState('');
  const query = term.trim().toUpperCase();
  const selected = options.filter((item) => selectedIds.includes(item.id));
  const matches = options.filter((item) => {
    if (!query) return true;
    const haystack = `${item.code} ${item.name} ${item.description ?? ''} ${item.kind} ${item.id}`.toUpperCase();
    return haystack.includes(query);
  });
  function toggle(id: string) {
    onChange(selectedIds.includes(id) ? selectedIds.filter((item) => item !== id) : [...selectedIds, id]);
  }
  return (
    <div className="sila-recipe-search" data-testid="recipe-location-assignment">
      <div className="sila-multi-select">
        {selected.map((item) => (
          <button key={item.id} type="button" className="sila-chip" onClick={() => toggle(item.id)} title="Remove">
            {item.kind} · {item.code} {item.name}
            <span aria-hidden="true">×</span>
          </button>
        ))}
        <input
          className="sila-input sila-multi-select__search"
          data-testid="input-recipe-location-search"
          value={term}
          onChange={(event) => { setTerm(event.target.value); setOpen(true); }}
          onFocus={() => setOpen(true)}
          onBlur={() => window.setTimeout(() => setOpen(false), 180)}
          placeholder="Search outlet / venue ID or description"
        />
      </div>
      {options.length === 0 && <p className="sila-muted-copy">Add locations under Administration → Location Master.</p>}
      {open && options.length > 0 && (
        <div className="sila-card sila-recipe-results" data-testid="recipe-location-results">
          {matches.length === 0 && <p className="sila-muted-copy" style={{ margin: 8 }}>No outlets or venues matched.</p>}
          {matches.map((item) => {
            const chosen = selectedIds.includes(item.id);
            return (
              <button key={item.id} type="button" className="sila-recipe-result" data-testid={`recipe-location-${item.code}`}
                onMouseDown={(event) => { event.preventDefault(); toggle(item.id); }}>
                <span>
                  <strong>{item.code}</strong>
                  <span>{item.name}{item.description ? ` · ${item.description}` : ''}</span>
                </span>
                {chosen ? <em>SELECTED</em> : <span>{item.kind}</span>}
              </button>
            );
          })}
        </div>
      )}
    </div>
  );
}

export function RecipeEditorPage({ create = false }: { create?: boolean }) {
  const { organizationId } = useOrg();
  const params = useParams<{ id: string }>();
  const [, setLocation] = useLocation();
  const client = useQueryClient();
  const [form, setForm] = useState({
    recipeCode: '', title: '', familyId: '', categoryId: '', itemMode: 'RECIPE', servingUom: 'EA', servingUnit: '1',
    menuPrice: '', posCode: '', posItem: '', lastSaleDate: '',
  });
  const [locationIds, setLocationIds] = useState<string[]>([]);
  const [lines, setLines] = useState<IngredientLine[]>([]);
  const [loaded, setLoaded] = useState(false);
  const [showIssues, setShowIssues] = useState(false);
  const [priceMaterialId, setPriceMaterialId] = useState<string | null>(null);
  const families = useQuery({
    queryKey: ['recipe-families-active', organizationId],
    queryFn: () => api<Array<{ id: string; code: string; name: string }>>(`/api/v1/recipe-management/families?organizationId=${organizationId}&status=ACTIVE`),
    enabled: Boolean(organizationId), retry: false,
  });
  const categories = useQuery({
    queryKey: ['recipe-categories-active', organizationId],
    queryFn: () => api<Array<{ id: string; code: string; name: string }>>(`/api/v1/recipe-management/categories?organizationId=${organizationId}&status=ACTIVE`),
    enabled: Boolean(organizationId), retry: false,
  });
  const locations = useQuery({
    queryKey: ['recipe-locations-active', organizationId],
    queryFn: () => api<Array<{ id: string; kind: string; code: string; name: string; description?: string | null }>>(`/api/v1/recipe-management/locations?organizationId=${organizationId}&status=ACTIVE`),
    enabled: Boolean(organizationId), retry: false,
  });
  const uoms = useQuery({
    queryKey: ['recipe-uoms', organizationId],
    queryFn: () => api<Array<{ id: string; code: string; name: string }>>(`/api/v1/recipe-management/uoms?organizationId=${organizationId}&status=ACTIVE`),
    enabled: Boolean(organizationId), retry: false,
  });
  const existing = useQuery({
    queryKey: ['recipe', organizationId, params.id],
    queryFn: () => api<{
      recipeId: string; recipeCode: string; title: string; familyId?: string; categoryId?: string; itemMode?: string;
      servingUom: string; servingUnit: number; posItemMenuPrice?: number | null; posCode?: string | null; posItem?: string | null;
      lastSaleDate?: string | null; status: string; currency?: string;
      locations?: Array<{ locationId: string; kind: string; code: string; name: string }>;
      ingredients: Array<{
        recipeIngredientId?: string; materialId?: string; materialCode?: string; materialName?: string; quantity: number; uom: string;
        unitCost?: number | null; priceUnavailable?: boolean; ingredientCost?: number | null; percentageOfTotalCost?: number | null;
        baseUom?: string; packSummary?: string; consumptionQuantity?: number | null; consumptionUom?: string; conversionMissing?: boolean;
        priceStatus?: string; priceStatusLabel?: string; proposedUnitPrice?: number | null; canUpdatePrice?: boolean;
        conversions?: Array<{ fromUom: string; toUom: string; numerator: number; denominator: number }>;
      }>;
      costing: {
        recipeCost?: number | null; costPerServing?: number | null; costPercent?: number | null; marginAmount?: number | null;
        marginPercent?: number | null; costingComplete: boolean; missingPriceCount: number; pendingPriceCount: number; costingMessage?: string | null;
        servingQty?: number; servingUom?: string; currency?: string;
      };
      readiness: { status: string; issueCount: number; issues: Array<{ materialCode: string; description: string; code: string; message: string }>; readyForApproval: boolean };
    }>(`/api/v1/recipe-management/recipes/${params.id}?organizationId=${organizationId}`),
    enabled: Boolean(organizationId) && !create && Boolean(params.id), retry: false,
  });
  const versions = useQuery({
    queryKey: ['recipe-versions', organizationId, params.id],
    queryFn: () => api<Array<{ id: string; versionNumber: number; status: string }>>(`/api/v1/recipe-management/recipes/${params.id}/versions?organizationId=${organizationId}`),
    enabled: Boolean(organizationId) && !create && Boolean(params.id), retry: false,
  });
  useEffect(() => {
    if (create || !existing.data || loaded) return;
    const row = existing.data;
    setForm({
      recipeCode: row.recipeCode, title: row.title ?? '', familyId: row.familyId ?? '', categoryId: row.categoryId ?? '',
      itemMode: row.itemMode || 'RECIPE', servingUom: row.servingUom || 'EA', servingUnit: String(row.servingUnit ?? 1),
      menuPrice: row.posItemMenuPrice == null ? '' : String(row.posItemMenuPrice),
      posCode: row.posCode ?? '', posItem: row.posItem ?? '', lastSaleDate: row.lastSaleDate ?? '',
    });
    setLocationIds((row.locations ?? []).map((item) => item.locationId));
    setLines(row.ingredients.map((item) => {
      const status = item.priceStatus || (item.priceUnavailable ? 'PRICE_MISSING' : 'PRICE_APPROVED');
      return {
        recipeIngredientId: item.recipeIngredientId ?? '', materialId: item.materialId ?? '', materialCode: item.materialCode ?? '',
        materialName: item.materialName ?? '', quantity: String(item.quantity), uom: item.uom, baseUom: item.baseUom || item.uom,
        unitCost: item.priceUnavailable ? null : (item.unitCost ?? null), currency: row.currency || 'AED',
        priceStatus: status, priceStatusLabel: item.priceStatusLabel || priceStatusLabel(status),
        proposedUnitPrice: item.proposedUnitPrice ?? null, canUpdatePrice: item.canUpdatePrice ?? canCorrectPrice(status),
        consumptionQuantity: item.consumptionQuantity ?? convertQty(item.quantity, item.uom, item.baseUom || item.uom, item.conversions ?? []),
        conversionMissing: Boolean(item.conversionMissing), packSummary: item.packSummary ?? `Base: ${item.baseUom || item.uom}`,
        conversions: item.conversions ?? [],
      };
    }));
    setLoaded(true);
  }, [create, existing.data, loaded]);
  function payload() {
    const menuPrice = form.menuPrice.trim() === '' ? null : Number(form.menuPrice);
    return {
      title: form.title || null, familyId: form.familyId || null, categoryId: form.categoryId || null, itemMode: form.itemMode,
      servingUnit: Number(form.servingUnit) || 1, servingUom: form.servingUom || 'EA',
      posCode: form.posCode || null, posItem: form.posItem || null, lastSaleDate: form.lastSaleDate || null,
      posItemMenuPrice: menuPrice == null || Number.isNaN(menuPrice) ? null : menuPrice,
      locationIds,
      ingredients: lines.filter((line) => line.materialId).map((line, index) => ({
        materialId: line.materialId, quantity: Number(line.quantity) || 0, uom: line.uom, wastagePercent: 0, yieldPercent: 100, sequence: (index + 1) * 10,
      })),
    };
  }
  const save = useMutation({
    mutationFn: () => api<{ recipeId: string; recipeCode: string }>(create ? `/api/v1/recipe-management/recipes?organizationId=${organizationId}` : `/api/v1/recipe-management/recipes/${params.id}/versions?organizationId=${organizationId}`, {
      method: 'POST', headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload()),
    }),
    onSuccess: (detail) => {
      client.invalidateQueries({ queryKey: ['recipes', organizationId] });
      if (create) setLocation(`/recipe-management/recipes/${detail.recipeId}`);
      else { setLoaded(false); existing.refetch(); }
    },
  });
  const submit = useMutation({
    mutationFn: async () => {
      const detail = await api<{ recipeId: string }>(create ? `/api/v1/recipe-management/recipes?organizationId=${organizationId}` : `/api/v1/recipe-management/recipes/${params.id}/versions?organizationId=${organizationId}`, {
        method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(payload()),
      });
      await api(`/api/v1/recipe-management/recipes/${detail.recipeId}/submit?organizationId=${organizationId}`, { method: 'POST' });
      return detail;
    },
    onSuccess: (detail) => {
      client.invalidateQueries({ queryKey: ['recipes', organizationId] });
      if (create) setLocation(`/recipe-management/recipes/${detail.recipeId}`);
      else { setLoaded(false); existing.refetch(); versions.refetch(); }
    },
  });
  const currency = existing.data?.currency || lines.find((item) => item.currency)?.currency || 'AED';
  const computed = (() => {
    const filled = lines.map((line) => {
      const qty = Number(line.quantity) || 0;
      const consumption = convertQty(qty, line.uom, line.baseUom || line.uom, line.conversions);
      const approved = line.priceStatus === 'PRICE_APPROVED' ? line.unitCost : null;
      const cost = approved == null || consumption == null ? null : consumption * approved;
      return { ...line, qty, consumption, cost, conversionMissing: consumption == null };
    });
    const issues = filled.flatMap((item) => {
      const rows: string[] = [];
      if (item.priceStatus === 'PRICE_MISSING') rows.push(`${item.materialName} — Price Missing`);
      if (item.priceStatus === 'PRICE_PENDING_APPROVAL') rows.push(`${item.materialName} — Material price pending approval`);
      if (item.priceStatus === 'PRICE_REJECTED') rows.push(`${item.materialName} — Price Rejected`);
      if (item.priceStatus === 'PRICE_INVALID' || item.priceStatus === 'PRICE_EXPIRED') rows.push(`${item.materialName} — Price Invalid`);
      if (item.conversionMissing) rows.push(`${item.materialName} — UOM conversion missing`);
      return rows;
    });
    const complete = filled.length > 0 && issues.length === 0;
    const totalCost = complete ? filled.reduce((sum, item) => sum + (item.cost ?? 0), 0) : null;
    const percents = filled.map((item) => {
      if (!complete || item.cost == null || totalCost == null || !totalCost) return 'NOT CALCULATED';
      return `${((item.cost / totalCost) * 100).toFixed(2)}%`;
    });
    const missing = filled.filter((item) => item.priceStatus === 'PRICE_MISSING').length;
    const pending = filled.filter((item) => item.priceStatus === 'PRICE_PENDING_APPROVAL').length;
    const notes = [
      pending ? `${pending} MATERIAL PRICE${pending === 1 ? '' : 'S'} PENDING` : '',
      missing ? `${missing} MATERIAL PRICE${missing === 1 ? '' : 'S'} MISSING` : '',
      pending && !missing ? 'COST PENDING MATERIAL PRICE APPROVAL' : '',
    ].filter(Boolean);
    const serving = Number(form.servingUnit) || 1;
    const menu = form.menuPrice.trim() === '' ? null : Number(form.menuPrice);
    const hasMenu = menu != null && !Number.isNaN(menu) && menu !== 0;
    const costPerServing = totalCost == null ? null : totalCost / (serving || 1);
    const costPercent = costPerServing == null || menu == null || !menu ? null : (costPerServing / menu) * 100;
    const margin = costPerServing == null || menu == null ? null : menu - costPerServing;
    const marginPercent = margin == null || menu == null || !menu ? null : (margin / menu) * 100;
    return { filled, complete, totalCost, percents, issues, missing, pending, notes, costPerServing, costPercent, margin, marginPercent, hasMenu };
  })();
  const readinessStatus = computed.complete ? 'READY_FOR_APPROVAL' : 'NOT_READY';
  const issueCount = computed.issues.length;
  function addMaterial(material: MaterialOption) {
    const line = { ...ingredientFromMaterial(material), recipeIngredientId: nextIngredientId(form.recipeCode, lines) };
    if (form.itemMode === 'DIRECT' && lines.length > 0 && lines[0].materialId !== material.id) {
      setLines([line]);
      return;
    }
    if (lines.some((item) => item.materialId === material.id)) return;
    setLines([...lines, line]);
  }
  function incomplete(value: string | number | null | undefined) {
    return profitOrReason(computed.complete, computed.hasMenu, value);
  }
  function onSubmit(event: FormEvent) {
    event.preventDefault();
    save.mutate();
  }
  const submitBlocked = !computed.complete || save.isPending || submit.isPending;
  return (
    <>
      <SilaPageHeader
        eyebrow="Recipe Management"
        title={create ? 'New Recipe' : (existing.data?.title || existing.data?.recipeCode || 'Recipe')}
        description="Define the menu item on the left and build ingredients from Material Master on the right."
        actions={
          <div className="sila-recipe-headline-tags" data-testid="recipe-readiness">
            <span className="sila-recipe-tag">
              <span className="sila-recipe-tag__label">Status</span>
              <StatusBadge value={existing.data?.status ?? 'DRAFT'} />
            </span>
            <span className="sila-recipe-tag">
              <span className="sila-recipe-tag__label">Recipe Readiness</span>
              <strong
                className={`sila-status ${readinessStatus === 'READY_FOR_APPROVAL' ? 'sila-status--good' : 'sila-status--warn'}`}
                data-testid="recipe-readiness-status"
              >
                {readinessStatus === 'READY_FOR_APPROVAL' ? 'READY FOR APPROVAL' : 'NOT READY'}
              </strong>
            </span>
            <span className="sila-recipe-tag sila-recipe-tag--issues">
              <button
                className={`sila-status ${issueCount > 0 ? 'sila-status--warn' : 'sila-status--blue'}`}
                type="button"
                data-testid="button-recipe-issues"
                onClick={() => setShowIssues((open) => !open)}
              >
                Issues: {issueCount}
              </button>
              {showIssues && issueCount > 0 && (
                <ul className="sila-recipe-issues">{computed.issues.map((issue) => <li key={issue}>{issue}</li>)}</ul>
              )}
              {showIssues && issueCount === 0 && <p className="sila-muted-copy">No readiness issues.</p>}
            </span>
          </div>
        }
      />
      {versions.data && (
        <p style={{ marginBottom: 12 }}>Versions: {versions.data.map((item) => `V${item.versionNumber} (${item.status})`).join(' · ')}</p>
      )}
      <form onSubmit={onSubmit}>
        <div className="sila-recipe-editor">
          <section className="sila-card sila-recipe-panel" data-testid="recipe-menu-item-panel">
            <h3 className="sila-recipe-panel__title">Menu Item</h3>
            <div className="sila-form-grid">
              <label className="sila-form-field"><span>Item Code</span><input className="sila-input" value={form.recipeCode} readOnly placeholder="Assigned when draft is saved" /></label>
              <label className="sila-form-field"><span>Title / POS Item</span><input className="sila-input" value={form.title} onChange={(event) => setForm({ ...form, title: event.target.value })} placeholder="Optional until POS update" /></label>
              <label className="sila-form-field"><span>Family *</span>
                <select className="sila-select" required value={form.familyId} onChange={(event) => setForm({ ...form, familyId: event.target.value })}>
                  <option value="">Select</option>
                  {families.data?.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}
                </select>
              </label>
              <label className="sila-form-field"><span>Category *</span>
                <select className="sila-select" required value={form.categoryId} onChange={(event) => setForm({ ...form, categoryId: event.target.value })}>
                  <option value="">Select</option>
                  {categories.data?.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}
                </select>
              </label>
              <label className="sila-form-field"><span>Item Mode</span>
                <select className="sila-select" data-testid="select-item-mode" value={form.itemMode} onChange={(event) => {
                  const itemMode = event.target.value;
                  setForm({ ...form, itemMode });
                  if (itemMode === 'DIRECT' && lines.length > 1) setLines(lines.slice(0, 1));
                }}>
                  <option value="DIRECT">DIRECT</option>
                  <option value="RECIPE">RECIPE</option>
                  <option value="BATCH_RECIPE">BATCH_RECIPE</option>
                </select>
              </label>
              <label className="sila-form-field"><span>Serving Qty</span><input className="sila-input" data-testid="input-serving-qty" value={form.servingUnit} onChange={(event) => setForm({ ...form, servingUnit: event.target.value })} /></label>
              <label className="sila-form-field"><span>Serving UOM</span>
                <select className="sila-select" data-testid="select-serving-uom" value={form.servingUom} onChange={(event) => setForm({ ...form, servingUom: event.target.value })}>
                  {(uoms.data?.length ? uoms.data : [{ code: 'EA', name: 'Each' }]).map((item) => <option key={item.code} value={item.code}>{item.code}</option>)}
                </select>
              </label>
              <label className="sila-form-field"><span>Menu Price</span>
                <input className="sila-input" data-testid="input-menu-price" inputMode="decimal" value={form.menuPrice} onChange={(event) => setForm({ ...form, menuPrice: event.target.value })} placeholder={`${currency} 0.00`} />
              </label>
              <label className="sila-form-field"><span>Currency</span><input className="sila-input" readOnly value={currency} /></label>
            </div>
            <h3 className="sila-recipe-panel__title" style={{ marginTop: 16 }}>POS data</h3>
            <p className="sila-muted-copy">Manual POS Code / POS Item / menu price / last sale updates save as a CHANGE and must be submitted for approval again.</p>
            <div className="sila-form-grid">
              <label className="sila-form-field"><span>POS Code</span><input className="sila-input" data-testid="input-pos-code" value={form.posCode} onChange={(event) => setForm({ ...form, posCode: event.target.value })} /></label>
              <label className="sila-form-field"><span>POS Item</span><input className="sila-input" data-testid="input-pos-item" value={form.posItem} onChange={(event) => setForm({ ...form, posItem: event.target.value })} /></label>
              <label className="sila-form-field"><span>Last Sale Date</span><input className="sila-input" type="date" data-testid="input-last-sale-date" value={form.lastSaleDate} onChange={(event) => setForm({ ...form, lastSaleDate: event.target.value })} /></label>
            </div>
            <h3 className="sila-recipe-panel__title" style={{ marginTop: 16 }}>Outlet / Venue assignment</h3>
            <LocationAssignmentSelect
              options={(locations.data ?? []).filter((item) => item.kind === 'OUTLET' || item.kind === 'VENUE' || locationIds.includes(item.id))}
              selectedIds={locationIds}
              onChange={setLocationIds}
            />
            <h3 className="sila-recipe-panel__title" style={{ marginTop: 16 }}>Cost &amp; Profitability</h3>
            <div className="sila-form-grid">
              <label className="sila-form-field"><span>Recipe / Item Cost</span><input className="sila-input sila-recipe-incomplete" readOnly data-testid="input-recipe-cost" value={costOrIncomplete(computed.complete, computed.complete ? money(computed.totalCost, currency) : null)} /></label>
              <label className="sila-form-field"><span>Cost / Serving</span><input className="sila-input" readOnly value={costOrIncomplete(computed.complete, computed.complete ? money(computed.costPerServing, currency) : null)} /></label>
              <label className="sila-form-field"><span>Cost %</span><input className="sila-input" readOnly value={incomplete(computed.costPercent == null ? null : computed.costPercent.toFixed(2))} /></label>
              <label className="sila-form-field"><span>Margin Amount</span><input className="sila-input" readOnly value={profitOrReason(computed.complete, computed.hasMenu, computed.complete && computed.hasMenu ? money(computed.margin, currency) : null)} /></label>
              <label className="sila-form-field"><span>Margin %</span><input className="sila-input" readOnly value={incomplete(computed.marginPercent == null ? null : computed.marginPercent.toFixed(2))} /></label>
            </div>
            {computed.notes.length > 0 && <p className="sila-recipe-cost-note" data-testid="recipe-costing-message">{computed.notes.join(' · ')}</p>}
          </section>
          <section className="sila-card sila-recipe-panel" data-testid="recipe-ingredients-panel">
            <h3 className="sila-recipe-panel__title">{form.itemMode === 'DIRECT' ? 'Direct Consumption' : 'Ingredients / Consumption'}</h3>
            <IngredientSearchBox organizationId={organizationId} selectedIds={lines.map((item) => item.materialId)} onSelect={addMaterial} />
            <div className="sila-recipe-grid-wrap">
              <table className="sila-table">
                <thead><tr>
                  <th>Ingredient ID</th><th>Ingredient</th><th>Material ID</th>
                  <th>{form.itemMode === 'DIRECT' ? 'Consumption Qty' : 'Recipe Qty'}</th>
                  <th>{form.itemMode === 'DIRECT' ? 'Consumption UOM' : 'Recipe UOM'}</th>
                  <th>Base UOM</th><th>Pack / Conversion</th><th>Unit Price</th><th>Price UOM</th><th>Price Status</th>
                  <th>Inventory Consumption</th><th>{form.itemMode === 'DIRECT' ? 'Item Cost' : 'Ingredient Cost'}</th>
                  {form.itemMode !== 'DIRECT' && <th>Cost %</th>}
                  <th>Actions</th>
                </tr></thead>
                <tbody>
                  {lines.length === 0 && <tr><td colSpan={form.itemMode === 'DIRECT' ? 13 : 14} className="sila-muted-copy">Search Material Master and add {form.itemMode === 'DIRECT' ? 'the consumption material' : 'an ingredient'}. A missing price does not block a draft.</td></tr>}
                  {computed.filled.map((line, index) => (
                    <tr key={line.materialId || index}>
                      <td>{line.recipeIngredientId || 'Assigned on save'}</td>
                      <td>{line.materialName}</td>
                      <td>{line.materialCode}</td>
                      <td><input className="sila-input" value={line.quantity} onChange={(event) => setLines(lines.map((item, i) => i === index ? { ...item, quantity: event.target.value } : item))} /></td>
                      <td>
                        <select className="sila-select" value={line.uom} onChange={(event) => setLines(lines.map((item, i) => i === index ? { ...item, uom: event.target.value } : item))}>
                          {Array.from(new Map([...(uoms.data ?? []), { code: line.uom, name: line.uom }, { code: line.baseUom, name: line.baseUom }].filter((item) => item.code).map((item) => [item.code, item])).values()).map((item) => <option key={item.code} value={item.code}>{item.code}</option>)}
                        </select>
                      </td>
                      <td>{line.baseUom}</td>
                      <td>{line.packSummary}</td>
                      <td>{unitPriceDisplay(line.priceStatus, line.unitCost, line.proposedUnitPrice, line.currency || currency)}</td>
                      <td>{line.baseUom}</td>
                      <td>{line.priceStatusLabel}</td>
                      <td>{line.consumption == null ? 'NOT CALCULATED' : `${line.consumption.toFixed(3)} ${line.baseUom}`}</td>
                      <td>{line.cost == null ? 'NOT CALCULATED' : money(line.cost, line.currency || currency)}</td>
                      {form.itemMode !== 'DIRECT' && <td>{computed.percents[index]}</td>}
                      <td>
                        {line.canUpdatePrice && (
                          <button className="sila-button sila-button--primary" type="button" data-testid={`button-update-price-${line.materialCode}`}
                            onClick={() => setPriceMaterialId(line.materialId)}>Update Material Price</button>
                        )}
                        <button className="sila-button" type="button" onClick={() => setLines(lines.filter((_, i) => i !== index))}><Trash2 size={14} /> Remove</button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            {lines.length > 0 && <p style={{ marginTop: 12, fontWeight: 600 }}>Total cost: {computed.complete ? money(computed.totalCost, currency) : 'INCOMPLETE'}</p>}
          </section>
        </div>
        <div className="sila-integration-actions" style={{ marginTop: 16 }}>
          <button className="sila-button" type="submit" disabled={save.isPending || submit.isPending}>{save.isPending ? 'Saving…' : 'Save draft'}</button>
          <button className="sila-button sila-button--primary" type="button" data-testid="button-submit-recipe" disabled={submitBlocked} onClick={() => submit.mutate()}>{submit.isPending ? 'Submitting…' : existing.data?.status === 'ACTIVE' || existing.data?.status === 'APPROVED' ? 'Submit POS / recipe change' : 'Submit for approval'}</button>
          {submitBlocked && lines.length > 0 && <span>Recipe approval is blocked until every ingredient has an approved Material price.</span>}
          {(save.isError || submit.isError) && <span>Could not save recipe.</span>}
          {save.isSuccess && <span>Draft saved.</span>}
          {submit.isSuccess && <span>Submitted for approval.</span>}
        </div>
      </form>
      {priceMaterialId && organizationId && (
        <MaterialPricePanel organizationId={organizationId} materialId={priceMaterialId}
          onClose={() => setPriceMaterialId(null)}
          onSubmitted={(material) => {
            const status = material.priceStatus || 'PRICE_PENDING_APPROVAL';
            setLines((current) => current.map((line) => line.materialId !== material.id ? line : {
              ...line,
              priceStatus: status,
              priceStatusLabel: material.priceStatusLabel || priceStatusLabel(status),
              proposedUnitPrice: material.proposedUnitPrice ?? null,
              unitCost: material.hasValidUnitPrice ? (material.unitCost ?? null) : null,
              canUpdatePrice: canCorrectPrice(status),
              currency: material.currency || line.currency,
            }));
            setPriceMaterialId(null);
            setLoaded(false);
            existing.refetch();
            client.invalidateQueries({ queryKey: ['recipe-material-search'] });
          }} />
      )}
    </>
  );
}

export function RecipeApprovalsPage() {
  const { organizationId } = useOrg();
  const client = useQueryClient();
  const query = useQuery({
    queryKey: ['recipe-approvals', organizationId],
    queryFn: () => api<Array<{ id: string; recipeName: string; versionNumber: number; event: string; level: number; roleKey: string; status: string }>>(`/api/v1/recipe-management/approvals?organizationId=${organizationId}`),
    enabled: Boolean(organizationId), retry: false,
  });
  const workflows = useQuery({
    queryKey: ['recipe-workflows', organizationId],
    queryFn: () => api<Array<{ event: string; levelCount: number; levels: Array<{ level: number; roleKey: string; label: string }> }>>(`/api/v1/recipe-management/approvals/workflows?organizationId=${organizationId}`),
    enabled: Boolean(organizationId), retry: false,
  });
  const decide = useMutation({
    mutationFn: ({ id, verb }: { id: string; verb: 'approve' | 'reject' }) => api(`/api/v1/recipe-management/approvals/${id}/${verb}?organizationId=${organizationId}`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ comment: verb }) }),
    onSuccess: () => client.invalidateQueries({ queryKey: ['recipe-approvals', organizationId] }),
  });
  return (
    <>
      <SilaPageHeader eyebrow="Recipe Management" title="Recipe Approvals" description="CREATE and CHANGE tasks. Workflow levels are configured under Administration → Workflows & configuration." />
      <section className="sila-card" style={{ padding: 18, marginBottom: 18 }}>
        <h3>Workflow configuration</h3>
        <p className="sila-muted-copy">Approval type, property / company code / outlet / store, greater-than / less-than conditions, and multi-level approvers are maintained in Administration → Workflows & configuration.</p>
        <Link href="/admin/configuration" className="sila-button sila-button--primary">Open Configurations</Link>
        {workflows.data?.map((item) => <p key={item.event}>{item.event}: {item.levels.map((level) => `L${level.level} ${level.label}`).join(' → ')}</p>)}
      </section>
      <SilaDataTable loading={query.isPending} empty={!query.isPending && (query.data?.length ?? 0) === 0} emptyTitle="No approval tasks">
        <table className="sila-table"><thead><tr><th>Recipe</th><th>Version</th><th>Event</th><th>Level</th><th>Status</th><th></th></tr></thead>
          <tbody>{query.data?.map((row) => (
            <tr key={row.id}><td>{row.recipeName}</td><td>V{row.versionNumber}</td><td>{row.event}</td><td>{row.level} {row.roleKey}</td><td><StatusBadge value={row.status} /></td>
              <td>{row.status === 'PENDING' && <><button className="sila-button sila-button--primary" type="button" onClick={() => decide.mutate({ id: row.id, verb: 'approve' })}>Approve</button> <button className="sila-button" type="button" onClick={() => decide.mutate({ id: row.id, verb: 'reject' })}>Reject</button></>}</td></tr>
          ))}</tbody>
        </table>
      </SilaDataTable>
    </>
  );
}

type PosSalesPreview = {
  fileName: string; totalRows: number; validRows: number; invalidRows: number;
  batchId?: string; duplicateRows?: number; unmappedPosCodes?: number; invalidOutlets?: number; invalidUom?: number;
  recipeNotReady?: number; readyToProcess?: number;
  rows: Array<{ rowNumber: number; isValid: boolean; status?: string; transactionId?: string; lineId?: number; posCode?: string; qty?: number; uom?: string; outletId?: string; currency?: string; errors: string[]; message?: string }>;
};
type PosSalesImportResult = { processed: number; posted: number; failed: number; alreadyPosted: number; failures: string[]; batchId?: string; duplicates?: number; postingUnknown?: number };
type PosSalesUploadRow = { id: string; fileName: string; businessDateRange?: string; uploadedAt: string; rows: number; valid: number; processed: number; duplicates: number; failed: number; postingUnknown: number; status: string };

export function RecipePosIntegrationPage() {
  const { organizationId } = useOrg();
  const client = useQueryClient();
  const posFileRef = useRef<HTMLInputElement>(null);
  const salesFileRef = useRef<HTMLInputElement>(null);
  const pendingPosFile = useRef<File | null>(null);
  const pendingSalesFile = useRef<File | null>(null);
  const [form, setForm] = useState({ name: '', posSystem: '', integrationKind: 'API', host: '', databaseName: '' });
  const [posNotice, setPosNotice] = useState('');
  const [salesPreview, setSalesPreview] = useState<PosSalesPreview | null>(null);
  const [salesResult, setSalesResult] = useState<PosSalesImportResult | null>(null);
  const query = useQuery({
    queryKey: ['pos-sources', organizationId],
    queryFn: () => api<Array<Record<string, string>>>(`/api/v1/recipe-management/pos-sources?organizationId=${organizationId}`),
    enabled: Boolean(organizationId), retry: false,
  });
  const save = useMutation({
    mutationFn: () => api(`/api/v1/recipe-management/pos-sources?organizationId=${organizationId}`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(form) }),
    onSuccess: () => client.invalidateQueries({ queryKey: ['pos-sources', organizationId] }),
  });
  const selected = query.data?.[0];
  const outlets = useQuery({
    queryKey: ['pos-outlets', organizationId, selected?.id],
    queryFn: () => api<Array<Record<string, string>>>(`/api/v1/recipe-management/pos-sources/${selected?.id}/outlets?organizationId=${organizationId}`),
    enabled: Boolean(organizationId && selected?.id), retry: false,
  });
  const items = useQuery({
    queryKey: ['pos-items', organizationId, selected?.id],
    queryFn: () => api<Array<Record<string, string>>>(`/api/v1/recipe-management/pos-sources/${selected?.id}/items?organizationId=${organizationId}`),
    enabled: Boolean(organizationId && selected?.id), retry: false,
  });
  const [outlet, setOutlet] = useState({ posOutletCode: '', propertyCode: '', outletCode: '', plantCode: '', storageLocationCode: '' });
  const [openUploadId, setOpenUploadId] = useState('');
  const [showSalesErrors, setShowSalesErrors] = useState(false);
  const [item, setItem] = useState({ posItemCode: '', posItemDescription: '', recipeId: '' });
  const recipes = useQuery({
    queryKey: ['recipes', organizationId],
    queryFn: () => api<Array<{ id: string; recipeCode: string; title: string }>>(`/api/v1/recipe-management/recipes?organizationId=${organizationId}`),
    enabled: Boolean(organizationId), retry: false,
  });
  const posRecipes = useQuery({
    queryKey: ['recipes-pos-item', organizationId],
    queryFn: () => api<Array<{ id: string; recipeCode: string; title: string; posCode?: string; posItem?: string }>>(`/api/v1/recipe-management/recipes?organizationId=${organizationId}&withPosItem=true`),
    enabled: Boolean(organizationId), retry: false,
  });
  const [selectedOutletId, setSelectedOutletId] = useState('');
  const [menuRecipeId, setMenuRecipeId] = useState('');
  const menuItems = useQuery({
    queryKey: ['outlet-menu-items', organizationId, selected?.id, selectedOutletId],
    queryFn: () => api<Array<{ id: string; recipeCode: string; title: string; posCode?: string; posItem?: string; posItemMenuPrice?: number }>>(`/api/v1/recipe-management/pos-sources/${selected?.id}/outlets/${selectedOutletId}/menu-items?organizationId=${organizationId}`),
    enabled: Boolean(organizationId && selected?.id && selectedOutletId), retry: false,
  });
  const importPos = useMutation({
    mutationFn: async (file: File) => {
      const body = new FormData();
      body.append('file', file);
      return api(`/api/v1/recipe-management/recipes/import?organizationId=${organizationId}`, { method: 'POST', body });
    },
    onSuccess: () => { setPosNotice('POS item data applied to Recipe Master.'); client.invalidateQueries({ queryKey: ['recipes', organizationId] }); },
  });
  const history = useQuery({
    queryKey: ['pos-sales-uploads', organizationId],
    queryFn: () => api<PosSalesUploadRow[]>(`/api/v1/recipe-management/pos-sales/uploads?organizationId=${organizationId}`),
    enabled: Boolean(organizationId), retry: false,
  });
  const uploadDetail = useQuery({
    queryKey: ['pos-sales-upload', organizationId, openUploadId],
    queryFn: () => api<{ header: PosSalesUploadRow; lines: Array<{ id: string; transactionId?: string; lineId?: number; posCode?: string; menuItem?: string; qty?: number; uom?: string; outletId?: string; recipeVersion?: number; status: string; message?: string; consumptionTransactionId?: string }> }>(`/api/v1/recipe-management/pos-sales/uploads/${openUploadId}?organizationId=${organizationId}`),
    enabled: Boolean(organizationId && openUploadId), retry: false,
  });
  const previewSales = useMutation({
    mutationFn: async (file: File) => {
      const body = new FormData();
      body.append('file', file);
      const source = selected?.id ? `&posSourceId=${selected.id}` : '';
      return api<PosSalesPreview>(`/api/v1/recipe-management/pos-sales/import/preview?organizationId=${organizationId}${source}`, { method: 'POST', body });
    },
    onSuccess: (data) => { setSalesPreview(data); setSalesResult(null); setShowSalesErrors(false); },
  });
  const importSales = useMutation({
    mutationFn: async () => {
      if (salesPreview?.batchId) {
        return api<PosSalesImportResult>(`/api/v1/recipe-management/pos-sales/uploads/${salesPreview.batchId}/process?organizationId=${organizationId}`, { method: 'POST' });
      }
      const file = pendingSalesFile.current;
      if (!file) throw new Error('Choose a sales file first.');
      const body = new FormData();
      body.append('file', file);
      const source = selected?.id ? `&posSourceId=${selected.id}` : '';
      return api<PosSalesImportResult>(`/api/v1/recipe-management/pos-sales/import?organizationId=${organizationId}${source}`, { method: 'POST', body });
    },
    onSuccess: (data) => {
      setSalesResult(data);
      setSalesPreview(null);
      pendingSalesFile.current = null;
      client.invalidateQueries({ queryKey: ['recipe-transactions', organizationId] });
      client.invalidateQueries({ queryKey: ['pos-sales-uploads', organizationId] });
    },
  });
  const saveOutlet = useMutation({
    mutationFn: () => api(`/api/v1/recipe-management/pos-sources/${selected?.id}/outlets?organizationId=${organizationId}`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(outlet) }),
    onSuccess: () => outlets.refetch(),
  });
  const saveItem = useMutation({
    mutationFn: () => api(`/api/v1/recipe-management/pos-sources/${selected?.id}/items?organizationId=${organizationId}`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(item) }),
    onSuccess: () => items.refetch(),
  });
  const addMenuItem = useMutation({
    mutationFn: () => api(`/api/v1/recipe-management/pos-sources/${selected?.id}/outlets/${selectedOutletId}/menu-items?organizationId=${organizationId}`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ recipeId: menuRecipeId }) }),
    onSuccess: () => { setMenuRecipeId(''); menuItems.refetch(); },
  });
  const removeMenuItem = useMutation({
    mutationFn: (id: string) => api(`/api/v1/recipe-management/pos-sources/${selected?.id}/outlets/${selectedOutletId}/menu-items/${id}?organizationId=${organizationId}`, { method: 'DELETE' }),
    onSuccess: () => menuItems.refetch(),
  });
  return (
    <>
      <SilaPageHeader eyebrow="Recipe Management" title="POS Integration" description="Sales Data Upload validates POS lines, explodes the approved recipe for the business date, consumes outlet stock, then posts SAP UPDATE_STOCK through FIVE_POS_UPDATE." />
      <input ref={posFileRef} type="file" accept=".xlsx" hidden onChange={(event) => { const file = event.target.files?.[0]; if (file) { pendingPosFile.current = file; importPos.mutate(file); } event.target.value = ''; }} />
      <input ref={salesFileRef} type="file" accept=".xlsx" hidden data-testid="input-pos-sales-file" onChange={(event) => { const file = event.target.files?.[0]; if (file) { pendingSalesFile.current = file; previewSales.mutate(file); } event.target.value = ''; }} />
      <section className="sila-card" style={{ padding: 18, marginBottom: 18 }}>
        <h3 className="sila-recipe-panel__title">Sales data upload</h3>
        <p className="sila-muted-copy">Template sheet SalesData: BusinessDate · TransactionID · LineID · POSCode · Qty · UOM · OutletID · Currency. Preview first. Process Sales starts inventory and SAP posting.</p>
        <div className="sila-filter-row" style={{ marginTop: 12 }}>
          <a className="sila-button" href={`/api/v1/recipe-management/pos-sales/template?organizationId=${organizationId}`} data-testid="link-pos-sales-template"><Download size={14} /> Download template</a>
          <button className="sila-button sila-button--primary" type="button" data-testid="button-upload-pos-sales" onClick={() => salesFileRef.current?.click()}><UploadCloud size={14} /> Upload sales</button>
          <Link href="/recipe-management/transactions" className="sila-button">Open Transaction Tracker</Link>
        </div>
        {salesPreview && (
          <div style={{ marginTop: 16 }}>
            <p>{salesPreview.fileName}</p>
            <p className="sila-muted-copy">Rows {salesPreview.totalRows} · Valid {salesPreview.validRows} · Duplicates {salesPreview.duplicateRows ?? 0} · Invalid {salesPreview.invalidRows} · Unmapped POS codes {salesPreview.unmappedPosCodes ?? 0} · Invalid outlets {salesPreview.invalidOutlets ?? 0} · Invalid UOM {salesPreview.invalidUom ?? 0} · Recipe not ready {salesPreview.recipeNotReady ?? 0} · Ready to process {salesPreview.readyToProcess ?? salesPreview.validRows}</p>
            {showSalesErrors && (
              <div className="sila-table-wrap">
                <table className="sila-table"><thead><tr><th>Status</th><th>TransactionID</th><th>LineID</th><th>POSCode</th><th>OutletID</th><th>Message</th></tr></thead>
                  <tbody>{salesPreview.rows.filter((row) => row.status !== 'READY').slice(0, 40).map((row) => (
                    <tr key={row.rowNumber}>
                      <td>{row.status}</td><td>{row.transactionId}</td><td>{row.lineId}</td><td>{row.posCode}</td><td>{row.outletId}</td>
                      <td>{row.message || row.errors.join('; ')}</td>
                    </tr>
                  ))}</tbody>
                </table>
              </div>
            )}
            <div className="sila-integration-actions" style={{ marginTop: 14 }}>
              <button className="sila-button" type="button" onClick={() => { setSalesPreview(null); pendingSalesFile.current = null; }}>Cancel</button>
              <button className="sila-button" type="button" data-testid="button-view-sales-errors" onClick={() => setShowSalesErrors((value) => !value)}>View errors</button>
              <button className="sila-button sila-button--primary" type="button" data-testid="button-post-pos-sales" disabled={importSales.isPending || (salesPreview.readyToProcess ?? salesPreview.validRows) === 0} onClick={() => importSales.mutate()}>{importSales.isPending ? 'Processing…' : 'Process sales'}</button>
            </div>
          </div>
        )}
        {salesResult && (
          <p className="sila-muted-copy" style={{ marginTop: 12 }} role="status">
            Posted {salesResult.posted} · duplicates {salesResult.duplicates ?? salesResult.alreadyPosted} · failed {salesResult.failed} · posting unknown {salesResult.postingUnknown ?? 0}
            {salesResult.failures.length ? ` — ${salesResult.failures[0]}` : ''}
          </p>
        )}
        <h4 style={{ marginTop: 20 }}>Upload history</h4>
        <SilaDataTable loading={history.isPending} empty={!history.isPending && (history.data?.length ?? 0) === 0} emptyTitle="No sales uploads yet">
          <table className="sila-table"><thead><tr><th>Upload ID</th><th>File</th><th>Business date</th><th>Uploaded at</th><th>Rows</th><th>Valid</th><th>Processed</th><th>Duplicates</th><th>Failed</th><th>Unknown</th><th>Status</th></tr></thead>
            <tbody>{history.data?.map((row) => (
              <tr key={row.id}>
                <td><button className="sila-button" type="button" onClick={() => setOpenUploadId(row.id)}>{row.id.slice(0, 8)}</button></td>
                <td>{row.fileName}</td><td>{row.businessDateRange ?? '—'}</td><td>{row.uploadedAt}</td>
                <td>{row.rows}</td><td>{row.valid}</td><td>{row.processed}</td><td>{row.duplicates}</td><td>{row.failed}</td><td>{row.postingUnknown}</td>
                <td><StatusBadge value={row.status} /></td>
              </tr>
            ))}            </tbody>
          </table>
        </SilaDataTable>
        {openUploadId && uploadDetail.data && (
          <div style={{ marginTop: 16 }}>
            <h4>Upload {openUploadId.slice(0, 8)}</h4>
            <table className="sila-table"><thead><tr><th>Transaction ID</th><th>Line ID</th><th>POS Code</th><th>Menu item</th><th>Qty</th><th>UOM</th><th>Outlet</th><th>Status</th><th></th></tr></thead>
              <tbody>{uploadDetail.data.lines.map((line) => (
                <tr key={line.id}>
                  <td>{line.transactionId}</td><td>{line.lineId}</td><td>{line.posCode}</td><td>{line.menuItem ?? '—'}</td>
                  <td>{line.qty}</td><td>{line.uom}</td><td>{line.outletId}</td><td><StatusBadge value={line.status} /></td>
                  <td>{line.consumptionTransactionId ? <Link href={`/recipe-management/transactions/${line.consumptionTransactionId}`}>Detail</Link> : line.message}</td>
                </tr>
              ))}</tbody>
            </table>
          </div>
        )}
      </section>
      <section className="sila-card" style={{ padding: 18, marginBottom: 18 }}>
        <div className="sila-form-grid">
          <label className="sila-form-field"><span>Integration name</span><input className="sila-input" value={form.name} onChange={(event) => setForm({ ...form, name: event.target.value })} /></label>
          <label className="sila-form-field"><span>POS system</span><input className="sila-input" value={form.posSystem} onChange={(event) => setForm({ ...form, posSystem: event.target.value })} /></label>
          <label className="sila-form-field"><span>Type</span><select className="sila-select" value={form.integrationKind} onChange={(event) => setForm({ ...form, integrationKind: event.target.value })}><option>API</option><option>DATABASE</option></select></label>
        </div>
        <button className="sila-button sila-button--primary" type="button" style={{ marginTop: 12 }} onClick={() => save.mutate()}>Save POS source</button>
        <button className="sila-button" type="button" style={{ marginTop: 12, marginLeft: 8 }} onClick={() => posFileRef.current?.click()}><UploadCloud size={14} /> Upload POS item Excel</button>
        <a className="sila-button" style={{ marginTop: 12, marginLeft: 8 }} href={`/api/v1/recipe-management/recipes/template?organizationId=${organizationId}`}><Download size={14} /> POS / Recipe template</a>
        {posNotice && <p className="sila-muted-copy">{posNotice}</p>}
        <p style={{ marginTop: 10, color: 'var(--sila-muted)' }}>API sources can attach an existing Integration configuration ID. DATABASE passwords use the protected credential store and are never shown again.</p>
      </section>
      {selected && (
        <>
          <h3>Outlet / store / venue mapping</h3>
          <p className="sila-muted-copy">POS OutletID maps to Location Master. Stock is reduced at that outlet, store, or venue.</p>
          <div className="sila-filter-row">
            <input className="sila-input" placeholder="POS OutletID" value={outlet.posOutletCode} onChange={(event) => setOutlet({ ...outlet, posOutletCode: event.target.value })} />
            <input className="sila-input" placeholder="Property code" value={outlet.propertyCode} onChange={(event) => setOutlet({ ...outlet, propertyCode: event.target.value })} />
            <input className="sila-input" placeholder="Location code (outlet / store / venue)" value={outlet.outletCode} onChange={(event) => setOutlet({ ...outlet, outletCode: event.target.value })} />
            <input className="sila-input" placeholder="SAP plant" value={outlet.plantCode} onChange={(event) => setOutlet({ ...outlet, plantCode: event.target.value })} />
            <input className="sila-input" placeholder="SAP storage location" value={outlet.storageLocationCode} onChange={(event) => setOutlet({ ...outlet, storageLocationCode: event.target.value })} />
            <button className="sila-button" type="button" onClick={() => saveOutlet.mutate()}>Save outlet</button>
          </div>
          <SilaDataTable loading={outlets.isPending} empty={!outlets.isPending && (outlets.data?.length ?? 0) === 0} emptyTitle="No outlet mappings">
            <table className="sila-table"><thead><tr><th>POS outlet</th><th>Property</th><th>Location</th><th>Type</th><th>Plant</th><th>SLoc</th></tr></thead>
              <tbody>{outlets.data?.map((row) => (
                <tr key={row.id} onClick={() => setSelectedOutletId(row.id)} style={{ cursor: 'pointer', background: selectedOutletId === row.id ? 'var(--sila-surface-muted, #f4f1ea)' : undefined }}>
                  <td>{row.posOutletCode}</td><td>{row.propertyCode}</td><td>{row.locationName ? `${row.locationName} (${row.outletCode})` : row.outletCode}</td><td>{row.locationType ?? '—'}</td>
                  <td>{row.plantCode || '—'}</td><td>{row.storageLocationCode || '—'}</td>
                </tr>
              ))}</tbody></table>
          </SilaDataTable>
          {selectedOutletId && (
            <>
              <h3>Outlet menu items</h3>
              <p className="sila-muted-copy">Pull recipes that already have POS Item updated. An outlet can have multiple menu items.</p>
              <div className="sila-filter-row">
                <select className="sila-select" value={menuRecipeId} onChange={(event) => setMenuRecipeId(event.target.value)}>
                  <option value="">Recipe with POS Item</option>
                  {posRecipes.data?.map((recipe) => <option key={recipe.id} value={recipe.id}>{recipe.recipeCode} · {recipe.title} · {recipe.posItem}</option>)}
                </select>
                <button className="sila-button sila-button--primary" type="button" disabled={!menuRecipeId || addMenuItem.isPending} onClick={() => addMenuItem.mutate()}>Add menu item</button>
              </div>
              <SilaDataTable loading={menuItems.isPending} empty={!menuItems.isPending && (menuItems.data?.length ?? 0) === 0} emptyTitle="No menu items on this outlet">
                <table className="sila-table"><thead><tr><th>RecipeID</th><th>Title</th><th>POS Code</th><th>POS Item</th><th>Menu price</th><th></th></tr></thead>
                  <tbody>{menuItems.data?.map((row) => (
                    <tr key={row.id}>
                      <td>{row.recipeCode}</td><td>{row.title}</td><td>{row.posCode}</td><td>{row.posItem}</td><td>{row.posItemMenuPrice}</td>
                      <td><button className="sila-button" type="button" onClick={() => removeMenuItem.mutate(row.id)}><Trash2 size={14} /></button></td>
                    </tr>
                  ))}</tbody>
                </table>
              </SilaDataTable>
            </>
          )}
          <h3>POS item → recipe</h3>
          <div className="sila-filter-row">
            <input className="sila-input" placeholder="POS item code" value={item.posItemCode} onChange={(event) => setItem({ ...item, posItemCode: event.target.value })} />
            <input className="sila-input" placeholder="Description" value={item.posItemDescription} onChange={(event) => setItem({ ...item, posItemDescription: event.target.value })} />
            <select className="sila-select" value={item.recipeId} onChange={(event) => setItem({ ...item, recipeId: event.target.value })}>
              <option value="">Recipe</option>
              {recipes.data?.map((recipe) => <option key={recipe.id} value={recipe.id}>{recipe.recipeCode} · {recipe.title}</option>)}
            </select>
            <button className="sila-button" type="button" onClick={() => saveItem.mutate()}>Save mapping</button>
          </div>
          <SilaDataTable loading={items.isPending} empty={!items.isPending && (items.data?.length ?? 0) === 0} emptyTitle="No item mappings">
            <table className="sila-table"><thead><tr><th>POS item</th><th>Recipe</th></tr></thead>
              <tbody>{items.data?.map((row) => <tr key={row.id}><td>{row.posItemCode} {row.posItemDescription}</td><td>{row.recipeCode}</td></tr>)}</tbody></table>
          </SilaDataTable>
        </>
      )}
    </>
  );
}

type RecipeTrackerRow = {
  id: string; sourceTransactionId: string; sourceLineNumber?: number; businessDate?: string;
  locationName?: string; locationCode?: string; locationType?: string; outlet?: string;
  posItemCode?: string; recipeCode?: string; recipeVersionNumber?: number; quantitySold?: number;
  sapMovementType?: string; materialDocument?: string; status: string; canReprocess?: boolean;
  step1Status?: string; step1Message?: string; step2Status?: string; step2Message?: string;
  erpHttpStatus?: number; erpResponse?: string; failureMessage?: string; integrationSystem?: string;
};

type RecipeTransactionDetail = {
  header: RecipeTrackerRow;
  lines: Array<{ id: string; materialCode?: string; description?: string; consumedQuantity?: number; uom?: string }>;
  timeline: Array<{ id: string; step: string; status: string; detail?: string; createdAt: string }>;
  failureCode?: string; failureMessage?: string; failedStep?: string; erpRequest?: string; erpResponse?: string; erpHttpStatus?: number;
};

function canReprocessRow(row: RecipeTrackerRow | undefined | null) {
  if (!row) return false;
  const status = (row.status ?? '').toUpperCase();
  if (status === 'POSTED' || status === 'POSTING') return false;
  if (row.canReprocess) return true;
  const step1 = (row.step1Status ?? '').toUpperCase();
  const step2 = (row.step2Status ?? '').toUpperCase();
  return step1 === 'FAILED' || step2 === 'FAILED' || step2 === 'UNKNOWN';
}

function reprocessOutcome(detail: RecipeTransactionDetail): { tone: 'good' | 'bad'; text: string } {
  const header = detail.header;
  if ((header.status ?? '').toUpperCase() === 'POSTED') {
    return { tone: 'good', text: header.step2Message || 'Reprocess succeeded. SAP accepted the inventory adjustment.' };
  }
  const step1 = (header.step1Status ?? '').toUpperCase();
  const step2 = (header.step2Status ?? '').toUpperCase();
  if (step1 === 'FAILED') {
    return { tone: 'bad', text: `Reprocess failed. Step 1: ${header.step1Message || detail.failureMessage || 'Local inventory was not updated.'}` };
  }
  if (step2 === 'FAILED' || step2 === 'UNKNOWN') {
    return { tone: 'bad', text: `Reprocess failed. Step 2: ${header.step2Message || detail.failureMessage || 'ERP UPDATE_STOCK did not complete.'}` };
  }
  return { tone: 'bad', text: detail.failureMessage || 'Reprocess did not complete posting.' };
}

function apiErrorText(error: unknown) {
  const data = (error as { data?: { message?: string } | null }).data;
  const message = data?.message || (error instanceof Error ? error.message : 'Reprocess failed.');
  return message.toLowerCase().startsWith('reprocess') ? message : `Reprocess failed. ${message}`;
}

export function RecipeTransactionsPage() {
  const { organizationId } = useOrg();
  const client = useQueryClient();
  const [filters, setFilters] = useState({ businessDate: '', outlet: '', posCode: '', material: '', status: '', transactionId: '' });
  const [info, setInfo] = useState<RecipeTrackerRow | null>(null);
  const [notice, setNotice] = useState<{ tone: 'good' | 'bad'; text: string } | null>(null);
  const query = useQuery({
    queryKey: ['recipe-transactions', organizationId, filters],
    queryFn: () => {
      const params = new URLSearchParams({ organizationId: organizationId ?? '' });
      if (filters.status) params.set('status', filters.status);
      if (filters.businessDate) params.set('businessDate', filters.businessDate);
      if (filters.outlet) params.set('outlet', filters.outlet);
      if (filters.posCode) params.set('posCode', filters.posCode);
      if (filters.material) params.set('material', filters.material);
      if (filters.transactionId) params.set('transactionId', filters.transactionId);
      return api<RecipeTrackerRow[]>(`/api/v1/recipe-management/transactions?${params.toString()}`);
    },
    enabled: Boolean(organizationId), retry: false,
  });
  const reprocess = useMutation({
    mutationFn: (id: string) => api<RecipeTransactionDetail>(`/api/v1/recipe-management/transactions/${id}/reprocess?organizationId=${organizationId}`, { method: 'POST' }),
    onSuccess: (detail) => {
      client.invalidateQueries({ queryKey: ['recipe-transactions', organizationId] });
      setInfo(detail.header);
      setNotice(reprocessOutcome(detail));
    },
    onError: (error) => {
      client.invalidateQueries({ queryKey: ['recipe-transactions', organizationId] });
      setNotice({ tone: 'bad', text: apiErrorText(error) });
    },
  });
  return (
    <>
      <SilaPageHeader eyebrow="Recipe Management" title="Transaction Tracker" description="Step 1 updates SILA inventory from the sales file or POS API. Step 2 calls ERP UPDATE_STOCK. Reprocess only failed or not-updated rows. Open the I icon for the success or failure message." />
      {notice && <p className={`sila-muted-copy sila-integration-notice sila-integration-notice--${notice.tone}`} role="status" data-testid="status-txn-reprocess" style={{ margin: '-8px 0 16px', whiteSpace: 'pre-wrap' }}>{notice.text}</p>}
      <div className="sila-filter-row" style={{ marginBottom: 16 }}>
        <input className="sila-input" type="date" value={filters.businessDate} onChange={(event) => setFilters({ ...filters, businessDate: event.target.value })} />
        <input className="sila-input" placeholder="Outlet" value={filters.outlet} onChange={(event) => setFilters({ ...filters, outlet: event.target.value })} />
        <input className="sila-input" placeholder="POS Code" value={filters.posCode} onChange={(event) => setFilters({ ...filters, posCode: event.target.value })} />
        <input className="sila-input" placeholder="Material" value={filters.material} onChange={(event) => setFilters({ ...filters, material: event.target.value })} />
        <input className="sila-input" placeholder="Transaction ID" value={filters.transactionId} onChange={(event) => setFilters({ ...filters, transactionId: event.target.value })} />
        <select className="sila-select" value={filters.status} onChange={(event) => setFilters({ ...filters, status: event.target.value })}>
          <option value="">Status</option>
          {['RECEIVED', 'VALIDATING', 'RECIPE_MATCHED', 'RECIPE_EXPLODED', 'READY_TO_POST', 'POSTING', 'POSTED', 'FAILED', 'POSTING_UNKNOWN'].map((status) => <option key={status}>{status}</option>)}
        </select>
      </div>
      <SilaDataTable loading={query.isPending} empty={!query.isPending && (query.data?.length ?? 0) === 0} emptyTitle="No POS consumption yet">
        <table className="sila-table"><thead><tr><th>POS txn</th><th>Location</th><th>POS Code</th><th>Qty</th><th>Step 1 · SILA inventory</th><th>Step 2 · ERP</th><th>Status</th><th>Message</th><th>Reprocess</th></tr></thead>
          <tbody>{query.data?.map((row) => (
            <tr key={row.id}>
              <td><Link href={`/recipe-management/transactions/${row.id}`}>{row.sourceTransactionId}</Link>{row.sourceLineNumber ? ` / ${row.sourceLineNumber}` : ''}</td>
              <td>{row.locationName ?? row.outlet ?? '—'}{row.locationCode ? ` (${row.locationCode})` : ''}</td>
              <td>{row.posItemCode ?? '—'}</td>
              <td>{row.quantitySold}</td>
              <td><StatusBadge value={row.step1Status ?? 'FAILED'} /></td>
              <td><StatusBadge value={row.step2Status ?? 'NOT_STARTED'} />{row.materialDocument ? ` ${row.materialDocument}` : ''}</td>
              <td><StatusBadge value={row.status} /></td>
              <td>
                <button className="sila-button sila-button--quiet" type="button" aria-label="Show posting message" data-testid={`button-txn-info-${row.id}`} onClick={() => setInfo(row)}><Info size={15} /></button>
              </td>
              <td>
                {canReprocessRow(row) ? <button className="sila-button sila-button--primary" type="button" data-testid={`button-txn-reprocess-${row.id}`} disabled={reprocess.isPending} onClick={() => reprocess.mutate(row.id)}>Reprocess</button> : '—'}
              </td>
            </tr>
          ))}</tbody>
        </table>
      </SilaDataTable>
      {info && (
        <div className="sila-drawer-backdrop" data-testid="txn-info-panel" onClick={() => setInfo(null)}>
          <aside className="sila-drawer" onClick={(event) => event.stopPropagation()}>
            <h3>Posting message · {info.sourceTransactionId}</h3>
            <p className="sila-muted-copy">{info.locationType ?? 'Location'} {info.locationName ?? info.outlet}{info.locationCode ? ` (${info.locationCode})` : ''}</p>
            <h4 style={{ marginTop: 16 }}>Step 1 · Local inventory</h4>
            <p><StatusBadge value={info.step1Status} /></p>
            <p style={{ whiteSpace: 'pre-wrap' }}>{info.step1Message}</p>
            <h4 style={{ marginTop: 16 }}>Step 2 · ERP inventory adjustment</h4>
            <p><StatusBadge value={info.step2Status} />{info.erpHttpStatus ? ` · HTTP ${info.erpHttpStatus}` : ''}{info.integrationSystem ? ` · ${info.integrationSystem}` : ''}</p>
            <p style={{ whiteSpace: 'pre-wrap' }}>{info.step2Message || info.failureMessage || 'No ERP response yet.'}</p>
            {notice && <p className={`sila-muted-copy sila-integration-notice sila-integration-notice--${notice.tone}`} role="status" style={{ marginTop: 16, whiteSpace: 'pre-wrap' }}>{notice.text}</p>}
            {canReprocessRow(info) && (
              <button className="sila-button sila-button--primary" type="button" style={{ marginTop: 16 }} disabled={reprocess.isPending} onClick={() => reprocess.mutate(info.id)}>Reprocess</button>
            )}
            <button className="sila-button" type="button" style={{ marginTop: 12, marginLeft: canReprocessRow(info) ? 8 : 0 }} onClick={() => setInfo(null)}>Close</button>
          </aside>
        </div>
      )}
    </>
  );
}

export function RecipeTransactionDetailPage() {
  const { organizationId } = useOrg();
  const params = useParams<{ id: string }>();
  const client = useQueryClient();
  const [infoOpen, setInfoOpen] = useState(false);
  const [notice, setNotice] = useState<{ tone: 'good' | 'bad'; text: string } | null>(null);
  const query = useQuery({
    queryKey: ['recipe-transaction', organizationId, params.id],
    queryFn: () => api<RecipeTransactionDetail>(`/api/v1/recipe-management/transactions/${params.id}?organizationId=${organizationId}`),
    enabled: Boolean(organizationId && params.id), retry: false,
  });
  const reprocess = useMutation({
    mutationFn: () => api<RecipeTransactionDetail>(`/api/v1/recipe-management/transactions/${params.id}/reprocess?organizationId=${organizationId}`, { method: 'POST' }),
    onSuccess: (detail) => {
      client.setQueryData(['recipe-transaction', organizationId, params.id], detail);
      client.invalidateQueries({ queryKey: ['recipe-transaction', organizationId, params.id] });
      setNotice(reprocessOutcome(detail));
    },
    onError: (error) => {
      client.invalidateQueries({ queryKey: ['recipe-transaction', organizationId, params.id] });
      setNotice({ tone: 'bad', text: apiErrorText(error) });
    },
  });
  const data = query.data;
  const header = data?.header;
  return (
    <>
      <SilaPageHeader eyebrow="Recipe Management" title="Transaction detail" description="Step 1 updates local inventory. Step 2 posts SAP UPDATE_STOCK. Use the I icon for the ERP response."
        actions={<>
          <button className="sila-button" type="button" aria-label="Show posting message" data-testid="button-txn-detail-info" onClick={() => setInfoOpen(true)}><Info size={15} /> Message</button>
          {canReprocessRow(header) ? <button className="sila-button sila-button--primary" type="button" onClick={() => reprocess.mutate()}>Reprocess</button> : null}
        </>} />
      {notice && <p className={`sila-muted-copy sila-integration-notice sila-integration-notice--${notice.tone}`} role="status" data-testid="status-txn-detail-reprocess" style={{ margin: '-8px 0 16px', whiteSpace: 'pre-wrap' }}>{notice.text}</p>}
      {header && (
        <p className="sila-muted-copy" style={{ marginBottom: 16 }}>
          {header.locationType ?? 'Location'} {header.locationName ?? header.outlet}
          {header.locationCode ? ` (${header.locationCode})` : ''} · Qty {header.quantitySold} · {header.status}
        </p>
      )}
      <section className="sila-card" style={{ padding: 18, marginBottom: 18 }}>
        <h3>Processing steps</h3>
        <p><strong>Step 1 · SILA inventory</strong> · <StatusBadge value={header?.step1Status} /></p>
        <p className="sila-muted-copy">{header?.step1Message}</p>
        <p style={{ marginTop: 12 }}><strong>Step 2 · ERP UPDATE_STOCK</strong> · <StatusBadge value={header?.step2Status} /></p>
        <p className="sila-muted-copy" style={{ whiteSpace: 'pre-wrap' }}>{header?.step2Message}</p>
      </section>
      {data?.failureMessage && <p role="status">Failed step {data.failedStep}: {data.failureCode} — {data.failureMessage}</p>}
      <section className="sila-card" style={{ padding: 18, marginBottom: 18 }}>
        <h3>Timeline</h3>
        <ol>{data?.timeline?.map((event) => <li key={event.id}>{event.createdAt} · {event.step} · {event.status} · {event.detail}</li>)}</ol>
      </section>
      <SilaDataTable loading={query.isPending} empty={!query.isPending && (data?.lines.length ?? 0) === 0} emptyTitle="No exploded ingredients">
        <table className="sila-table"><thead><tr><th>Material</th><th>Description</th><th>Consumed</th><th>UOM</th></tr></thead>
          <tbody>{data?.lines?.map((line) => <tr key={line.id}><td>{line.materialCode}</td><td>{line.description}</td><td>{line.consumedQuantity}</td><td>{line.uom}</td></tr>)}</tbody>
        </table>
      </SilaDataTable>
      {infoOpen && header && (
        <div className="sila-drawer-backdrop" data-testid="txn-detail-info-panel" onClick={() => setInfoOpen(false)}>
          <aside className="sila-drawer" onClick={(event) => event.stopPropagation()}>
            <h3>ERP response</h3>
            <p><StatusBadge value={header.step2Status} />{data?.erpHttpStatus ? ` · HTTP ${data.erpHttpStatus}` : ''}</p>
            <p style={{ whiteSpace: 'pre-wrap' }}>{header.step2Message || data?.failureMessage || 'No ERP response yet.'}</p>
            {data?.erpResponse && <pre style={{ whiteSpace: 'pre-wrap', marginTop: 12 }}>{data.erpResponse}</pre>}
            {notice && <p className={`sila-muted-copy sila-integration-notice sila-integration-notice--${notice.tone}`} role="status" style={{ marginTop: 16, whiteSpace: 'pre-wrap' }}>{notice.text}</p>}
            {canReprocessRow(header) && <button className="sila-button sila-button--primary" type="button" style={{ marginTop: 16 }} onClick={() => reprocess.mutate()}>Reprocess</button>}
            <button className="sila-button" type="button" style={{ marginTop: 12 }} onClick={() => setInfoOpen(false)}>Close</button>
          </aside>
        </div>
      )}
    </>
  );
}

export function RecipeMasterDataPage() {
  const [kind, setKind] = useState('materials');
  return (
    <>
      <SilaPageHeader eyebrow="Administration" title="Master Data" description="Material, Family, Category, UOM, conversion, valuation, and POS mapping. Locations are maintained in Location Master." />
      <div className="sila-filter-row">
        <select className="sila-select" data-testid="select-master-data-kind" value={kind} onChange={(event) => setKind(event.target.value)}>
          <option value="materials">Material Master</option>
          <option value="families">Family</option>
          <option value="categories">Category</option>
          <option value="uoms">UOM</option>
          <option value="conversions">Material UOM Conversion</option>
          <option value="valuations">Material Valuation</option>
          <option value="pos-items">POS Item Mapping</option>
        </select>
        <Link href="/admin/locations" className="sila-button">Open Location Master</Link>
      </div>
      {kind === 'materials' && <RecipeMaterialsPage embedded />}
      {kind === 'families' && <RecipeCodeNameMaster kind="families" title="Family Master" description="Controlled family values for Recipe Create/Edit." />}
      {kind === 'categories' && <RecipeCodeNameMaster kind="categories" title="Category Master" description="Controlled category values for Recipe Create/Edit." />}
      {kind === 'uoms' && <RecipeUomMaster />}
      {kind === 'conversions' && <RecipeConversionMaster />}
      {kind === 'valuations' && <RecipeValuationExplorer />}
      {(kind === 'pos-items' || kind === 'outlets') && (
        <section className="sila-card" style={{ padding: 18 }}>
          <p className="sila-muted-copy">POS item and outlet / venue mapping stay on POS Integration so existing mapping and menu-item tools are not duplicated.</p>
          <Link href="/recipe-management/pos" className="sila-button sila-button--primary">Open POS Integration</Link>
        </section>
      )}
    </>
  );
}

function RecipeUomMaster() {
  const { organizationId } = useOrg();
  const client = useQueryClient();
  const [form, setForm] = useState({ code: '', name: '', dimension: 'OTHER', status: 'ACTIVE' });
  const query = useQuery({
    queryKey: ['recipe-uoms-all', organizationId],
    queryFn: () => api<Array<{ id: string; code: string; name: string; dimension: string; status: string }>>(`/api/v1/recipe-management/uoms?organizationId=${organizationId}`),
    enabled: Boolean(organizationId), retry: false,
  });
  const save = useMutation({
    mutationFn: () => api(`/api/v1/recipe-management/uoms?organizationId=${organizationId}`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(form) }),
    onSuccess: () => { setForm({ code: '', name: '', dimension: 'OTHER', status: 'ACTIVE' }); client.invalidateQueries({ queryKey: ['recipe-uoms-all', organizationId] }); },
  });
  return (
    <>
      <div className="sila-filter-row">
        <input className="sila-input" placeholder="Code" value={form.code} onChange={(event) => setForm({ ...form, code: event.target.value })} />
        <input className="sila-input" placeholder="Name" value={form.name} onChange={(event) => setForm({ ...form, name: event.target.value })} />
        <select className="sila-select" value={form.dimension} onChange={(event) => setForm({ ...form, dimension: event.target.value })}>
          <option>MASS</option><option>VOLUME</option><option>COUNT</option><option>PACK</option><option>SERVING</option><option>OTHER</option>
        </select>
        <select className="sila-select" value={form.status} onChange={(event) => setForm({ ...form, status: event.target.value })}>
          <option value="ACTIVE">Active</option><option value="INACTIVE">Inactive</option>
        </select>
        <button className="sila-button sila-button--primary" type="button" onClick={() => save.mutate()}>Save</button>
      </div>
      <SilaDataTable loading={query.isPending} empty={!query.isPending && (query.data?.length ?? 0) === 0} emptyTitle="No UOMs">
        <table className="sila-table"><thead><tr><th>Code</th><th>Name</th><th>Dimension</th><th>Status</th></tr></thead>
          <tbody>{query.data?.map((row) => <tr key={row.id}><td>{row.code}</td><td>{row.name}</td><td>{row.dimension}</td><td><StatusBadge value={row.status} /></td></tr>)}</tbody></table>
      </SilaDataTable>
    </>
  );
}

function RecipeConversionMaster() {
  const { organizationId } = useOrg();
  const client = useQueryClient();
  const [queryText, setQueryText] = useState('');
  const [form, setForm] = useState({ materialId: '', fromUom: '', toUom: '', numerator: '1', denominator: '1', packSize: '', packUom: '' });
  const query = useQuery({
    queryKey: ['recipe-uom-conversions', organizationId, queryText],
    queryFn: () => api<{ items: Array<{ id: string; materialId: string; materialCode: string; fromUom: string; toUom: string; numerator: number; denominator: number; packSize?: number; packUom?: string; isActive: boolean }> }>(`/api/v1/recipe-management/uom-conversions?organizationId=${organizationId}&query=${encodeURIComponent(queryText)}&pageSize=50`),
    enabled: Boolean(organizationId), retry: false,
  });
  const save = useMutation({
    mutationFn: () => api(`/api/v1/recipe-management/uom-conversions?organizationId=${organizationId}`, {
      method: 'PUT', headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        materialId: form.materialId, fromUom: form.fromUom, toUom: form.toUom,
        numerator: Number(form.numerator), denominator: Number(form.denominator),
        packSize: form.packSize ? Number(form.packSize) : null, packUom: form.packUom || null, isActive: true,
      }),
    }),
    onSuccess: () => client.invalidateQueries({ queryKey: ['recipe-uom-conversions', organizationId] }),
  });
  return (
    <>
      <div className="sila-filter-row">
        <input className="sila-input" placeholder="Search Material ID" value={queryText} onChange={(event) => setQueryText(event.target.value)} />
        <input className="sila-input" placeholder="Material GUID" value={form.materialId} onChange={(event) => setForm({ ...form, materialId: event.target.value })} />
        <input className="sila-input" placeholder="From UOM" value={form.fromUom} onChange={(event) => setForm({ ...form, fromUom: event.target.value })} />
        <input className="sila-input" placeholder="To UOM" value={form.toUom} onChange={(event) => setForm({ ...form, toUom: event.target.value })} />
        <input className="sila-input" placeholder="Numerator" value={form.numerator} onChange={(event) => setForm({ ...form, numerator: event.target.value })} />
        <input className="sila-input" placeholder="Denominator" value={form.denominator} onChange={(event) => setForm({ ...form, denominator: event.target.value })} />
        <input className="sila-input" placeholder="Pack size" value={form.packSize} onChange={(event) => setForm({ ...form, packSize: event.target.value })} />
        <input className="sila-input" placeholder="Pack UOM" value={form.packUom} onChange={(event) => setForm({ ...form, packUom: event.target.value })} />
        <button className="sila-button sila-button--primary" type="button" onClick={() => save.mutate()}>Save</button>
      </div>
      <SilaDataTable loading={query.isPending} empty={!query.isPending && (query.data?.items.length ?? 0) === 0} emptyTitle="No conversions">
        <table className="sila-table"><thead><tr><th>Material</th><th>From</th><th>To</th><th>Numerator</th><th>Denominator</th><th>Pack</th><th>Active</th></tr></thead>
          <tbody>{query.data?.items.map((row) => (
            <tr key={row.id}><td>{row.materialCode}</td><td>{row.fromUom}</td><td>{row.toUom}</td><td>{row.numerator}</td><td>{row.denominator}</td><td>{row.packSize} {row.packUom}</td><td>{row.isActive ? 'Y' : 'N'}</td></tr>
          ))}</tbody></table>
      </SilaDataTable>
    </>
  );
}

function RecipeValuationExplorer() {
  const { organizationId } = useOrg();
  const [search, setSearch] = useState('');
  const [priceMaterialId, setPriceMaterialId] = useState<string | null>(null);
  const query = useQuery({
    queryKey: ['recipe-valuations', organizationId, search],
    queryFn: () => api<Array<MaterialPriceRow>>(`/api/v1/recipe-management/materials?organizationId=${organizationId}&query=${encodeURIComponent(search)}`),
    enabled: Boolean(organizationId), retry: false,
  });
  const rows = (query.data ?? []).flatMap((item) => (item.valuations?.length ? item.valuations : [{ valuationArea: '—' }]).map((valuation) => ({ ...item, ...valuation })));
  return (
    <>
      <div className="sila-filter-row">
        <input className="sila-input" placeholder="Search Material ID or name" value={search} onChange={(event) => setSearch(event.target.value)} />
      </div>
      <SilaDataTable loading={query.isPending} empty={!query.isPending && rows.length === 0} emptyTitle="No valuations">
        <table className="sila-table"><thead><tr><th>Material ID</th><th>Description</th><th>Valuation Area / Plant</th><th>Price Control</th><th>Unit Price</th><th>Price UOM</th><th>Currency</th><th>Price Status</th><th className="sila-table__actions">Actions</th></tr></thead>
          <tbody>{rows.map((row, index) => (
            <tr key={`${row.materialCode}-${index}`}><td>{row.materialCode}</td><td>{row.description || row.name}</td><td>{row.valuationArea}</td><td>{row.priceControl}</td>
              <td>{unitPriceDisplay(row.priceStatus || (row.hasValidUnitPrice ? 'PRICE_APPROVED' : 'PRICE_MISSING'), row.approvedUnitPrice ?? row.unitCost ?? null, row.proposedUnitPrice ?? null, row.currency || 'AED', row.priceUom || row.baseUom)}</td>
              <td>{row.priceUom || row.baseUom}</td><td>{row.currency}</td>
              <td>{row.priceStatusLabel || priceStatusLabel(row.priceStatus)}</td>
              <td className="sila-table__actions">
                <button className="sila-button sila-button--primary" type="button" disabled={!row.id || Boolean(row.hasPendingPriceChange)} onClick={() => setPriceMaterialId(row.id)}>Update Unit Price</button>
              </td></tr>
          ))}</tbody></table>
      </SilaDataTable>
      {priceMaterialId && organizationId && (
        <MaterialPricePanel organizationId={organizationId} materialId={priceMaterialId}
          onClose={() => setPriceMaterialId(null)}
          onSubmitted={() => { setPriceMaterialId(null); query.refetch(); }} />
      )}
    </>
  );
}

export function RecipeNewPage() { return <RecipeEditorPage create />; }
