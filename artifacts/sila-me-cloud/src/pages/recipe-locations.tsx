import { useRef, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Download, UploadCloud } from 'lucide-react';
import { customFetch, getGetAccessContextQueryKey, useGetAccessContext } from '@workspace/api-client-react';
import { SilaDataTable, SilaPageHeader, StatusBadge } from '@/components/sila-ui';

const request = { credentials: 'include' as const };
async function api<T>(url: string, init?: RequestInit): Promise<T> {
  return customFetch<T>(url, { ...request, ...init, responseType: 'json' });
}
function orgId(organizations: Array<{ id: string; code: string; name: string }> | undefined) {
  return organizations?.find((item) => item.code.toUpperCase() === 'FIVE')?.id
    ?? organizations?.find((item) => item.name === 'Five Hotels and Resorts')?.id
    ?? organizations?.[0]?.id;
}

type LocationRow = { id: string; kind: string; code: string; name: string; description?: string; status: string };
type RecipeRow = { id: string; recipeCode: string; title: string; status: string };

export function RecipeLocationsPage({ embedded = false }: { embedded?: boolean } = {}) {
  const context = useGetAccessContext({ request, query: { queryKey: getGetAccessContextQueryKey(), retry: false } });
  const organizationId = orgId(context.data?.organizations);
  const client = useQueryClient();
  const fileRef = useRef<HTMLInputElement>(null);
  const pendingFile = useRef<File | null>(null);
  const [form, setForm] = useState({ kind: 'OUTLET', code: '', name: '', description: '', status: 'ACTIVE' });
  const [kindFilter, setKindFilter] = useState('');
  const [selectedId, setSelectedId] = useState('');
  const [assignRecipeId, setAssignRecipeId] = useState('');
  const [preview, setPreview] = useState<{ fileName: string; totalRows: number; validRows: number; invalidRows: number } | null>(null);
  const query = useQuery({
    queryKey: ['recipe-locations', organizationId, kindFilter],
    queryFn: () => api<LocationRow[]>(`/api/v1/recipe-management/locations?organizationId=${organizationId}${kindFilter ? `&kind=${kindFilter}` : ''}`),
    enabled: Boolean(organizationId), retry: false,
  });
  const recipes = useQuery({
    queryKey: ['recipes', organizationId],
    queryFn: () => api<RecipeRow[]>(`/api/v1/recipe-management/recipes?organizationId=${organizationId}`),
    enabled: Boolean(organizationId), retry: false,
  });
  const assigned = useQuery({
    queryKey: ['location-recipes', organizationId, selectedId],
    queryFn: () => api<RecipeRow[]>(`/api/v1/recipe-management/locations/${selectedId}/recipes?organizationId=${organizationId}`),
    enabled: Boolean(organizationId && selectedId), retry: false,
  });
  const save = useMutation({
    mutationFn: () => api(`/api/v1/recipe-management/locations?organizationId=${organizationId}`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(form) }),
    onSuccess: () => {
      setForm({ kind: form.kind, code: '', name: '', description: '', status: 'ACTIVE' });
      client.invalidateQueries({ queryKey: ['recipe-locations', organizationId] });
    },
  });
  const previewFile = useMutation({
    mutationFn: async (file: File) => {
      const body = new FormData();
      body.append('file', file);
      return api<NonNullable<typeof preview>>(`/api/v1/recipe-management/locations/import/preview?organizationId=${organizationId}`, { method: 'POST', body });
    },
    onSuccess: setPreview,
  });
  const commitFile = useMutation({
    mutationFn: async (file: File) => {
      const body = new FormData();
      body.append('file', file);
      return api(`/api/v1/recipe-management/locations/import?organizationId=${organizationId}`, { method: 'POST', body });
    },
    onSuccess: () => { setPreview(null); query.refetch(); },
  });
  const assign = useMutation({
    mutationFn: () => api(`/api/v1/recipe-management/locations/${selectedId}/recipes?organizationId=${organizationId}`, {
      method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ recipeId: assignRecipeId }),
    }),
    onSuccess: () => { setAssignRecipeId(''); assigned.refetch(); },
  });
  const remove = useMutation({
    mutationFn: (recipeId: string) => api(`/api/v1/recipe-management/locations/${selectedId}/recipes/${recipeId}?organizationId=${organizationId}`, { method: 'DELETE' }),
    onSuccess: () => assigned.refetch(),
  });
  const selected = query.data?.find((item) => item.id === selectedId);
  const header = (
    <SilaPageHeader eyebrow="Recipe Management" title="Outlet / Store / Venue Master" description="Maintain locations like Material Master: screen, Excel, and database. Assign recipes to each outlet, store, or venue."
      actions={<>
        <button className="sila-button" type="button" onClick={() => fileRef.current?.click()}><UploadCloud size={14} /> Upload Excel</button>
        <a className="sila-button" href={`/api/v1/recipe-management/locations/export?organizationId=${organizationId}`}><Download size={14} /> Download</a>
        <a className="sila-button" href={`/api/v1/recipe-management/locations/template?organizationId=${organizationId}`}><Download size={14} /> Download Template</a>
      </>} />
  );
  return (
    <>
      {!embedded && header}
      {embedded && <h3 className="sila-recipe-panel__title">Outlet / Store / Venue Master</h3>}
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
        <select className="sila-select" value={kindFilter} onChange={(event) => setKindFilter(event.target.value)} data-testid="filter-location-kind">
          <option value="">All kinds</option>
          <option value="OUTLET">Outlet</option>
          <option value="STORE">Store</option>
          <option value="VENUE">Venue</option>
        </select>
        <select className="sila-select" value={form.kind} onChange={(event) => setForm({ ...form, kind: event.target.value })} data-testid="select-location-kind">
          <option value="OUTLET">Outlet</option>
          <option value="STORE">Store</option>
          <option value="VENUE">Venue</option>
        </select>
        <input className="sila-input" placeholder="Code" value={form.code} onChange={(event) => setForm({ ...form, code: event.target.value })} data-testid="input-location-code" />
        <input className="sila-input" placeholder="Name" value={form.name} onChange={(event) => setForm({ ...form, name: event.target.value })} data-testid="input-location-name" />
        <input className="sila-input" placeholder="Description" value={form.description} onChange={(event) => setForm({ ...form, description: event.target.value })} />
        <select className="sila-select" value={form.status} onChange={(event) => setForm({ ...form, status: event.target.value })}>
          <option value="ACTIVE">Active</option>
          <option value="INACTIVE">Inactive</option>
        </select>
        <button className="sila-button sila-button--primary" type="button" onClick={() => save.mutate()} data-testid="button-save-location">Save</button>
      </div>
      <SilaDataTable loading={query.isPending} empty={!query.isPending && (query.data?.length ?? 0) === 0} emptyTitle="No locations">
        <table className="sila-table"><thead><tr><th>Kind</th><th>Code</th><th>Name</th><th>Description</th><th>Status</th></tr></thead>
          <tbody>{query.data?.map((row) => (
            <tr key={row.id} onClick={() => setSelectedId(row.id)} style={{ cursor: 'pointer', background: selectedId === row.id ? 'var(--sila-surface-muted, #f4f1ea)' : undefined }}>
              <td>{row.kind}</td><td>{row.code}</td><td>{row.name}</td><td>{row.description}</td><td><StatusBadge value={row.status} /></td>
            </tr>
          ))}</tbody>
        </table>
      </SilaDataTable>
      {selected && (
        <section className="sila-card" style={{ padding: 18, marginTop: 18 }} data-testid="location-recipe-assignment">
          <h3>Recipe assignment · {selected.kind} {selected.name}</h3>
          <div className="sila-filter-row">
            <select className="sila-select" value={assignRecipeId} onChange={(event) => setAssignRecipeId(event.target.value)} data-testid="select-assign-recipe">
              <option value="">Select recipe</option>
              {recipes.data?.map((item) => <option key={item.id} value={item.id}>{item.recipeCode} · {item.title}</option>)}
            </select>
            <button className="sila-button sila-button--primary" type="button" disabled={!assignRecipeId || assign.isPending} onClick={() => assign.mutate()} data-testid="button-assign-recipe">Assign recipe</button>
          </div>
          <table className="sila-table"><thead><tr><th>RecipeID</th><th>Title</th><th>Status</th><th></th></tr></thead>
            <tbody>
              {(assigned.data ?? []).length === 0 && <tr><td colSpan={4} className="sila-muted-copy">No recipes assigned yet.</td></tr>}
              {assigned.data?.map((row) => (
                <tr key={row.id}>
                  <td>{row.recipeCode}</td><td>{row.title}</td><td><StatusBadge value={row.status} /></td>
                  <td><button className="sila-button" type="button" onClick={() => remove.mutate(row.id)}>Remove</button></td>
                </tr>
              ))}
            </tbody>
          </table>
        </section>
      )}
    </>
  );
}
