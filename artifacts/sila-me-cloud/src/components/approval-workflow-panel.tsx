import { useMemo, useState } from 'react';
import { Plus, RefreshCw, Trash2 } from 'lucide-react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { customFetch } from '@workspace/api-client-react';
import { StatusBadge } from '@/components/sila-ui';

const request = { credentials: 'include' as const };

type Level = { level: number; roleKey: string; label: string };
type Condition = { fieldKey: string; operator: string; value: number };
type Row = {
  id: string; name: string; approvalType: string; action: string; scopeKind: string; scopeValue?: string | null;
  isActive: boolean; conditions: Condition[]; levels: Level[];
};
type Catalog = {
  approvalTypes: Array<{ key: string; label: string; runtimeNote: string; fields: Array<{ key: string; label: string }> }>;
  actions: string[];
  scopeKinds: string[];
  operators: string[];
  roles: Array<{ key: string; name: string }>;
};
type ScopeOption = { value: string; label: string };

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

function emptyForm() {
  return {
    name: '',
    approvalType: 'RECIPE',
    action: 'CREATE',
    scopeKind: 'ALL',
    scopeValue: '',
    isActive: true,
    conditions: [] as Condition[],
    levels: [{ level: 1, roleKey: '', label: 'Level 1' }] as Level[],
  };
}

export function ApprovalWorkflowPanel({ organizationId }: { organizationId: string }) {
  const queryClient = useQueryClient();
  const [editing, setEditing] = useState<string | 'new' | null>(null);
  const [form, setForm] = useState(emptyForm());
  const [notice, setNotice] = useState<{ tone: 'good' | 'bad'; text: string }>();
  const [pendingDelete, setPendingDelete] = useState<Row | null>(null);

  const catalog = useQuery({
    queryKey: ['approval-workflow-catalog', organizationId],
    queryFn: () => api<Catalog>(`/api/v1/approval-workflows/catalog?organizationId=${organizationId}`),
  });
  const rows = useQuery({
    queryKey: ['approval-workflows', organizationId],
    queryFn: () => api<Row[]>(`/api/v1/approval-workflows?organizationId=${organizationId}`),
  });
  const scopes = useQuery({
    queryKey: ['approval-workflow-scopes', organizationId, form.scopeKind],
    queryFn: () => api<ScopeOption[]>(`/api/v1/approval-workflows/scope-options?organizationId=${organizationId}&scopeKind=${form.scopeKind}`),
    enabled: form.scopeKind !== 'ALL' && Boolean(editing),
  });

  const typeMeta = useMemo(
    () => catalog.data?.approvalTypes.find((item) => item.key === form.approvalType),
    [catalog.data, form.approvalType],
  );
  const fields = typeMeta?.fields ?? [];

  const save = useMutation({
    mutationFn: () => api<Row>(
      editing && editing !== 'new'
        ? `/api/v1/approval-workflows/${editing}?organizationId=${organizationId}`
        : `/api/v1/approval-workflows?organizationId=${organizationId}`,
      {
        method: editing && editing !== 'new' ? 'PUT' : 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          name: form.name,
          approvalType: form.approvalType,
          action: form.action,
          scopeKind: form.scopeKind,
          scopeValue: form.scopeKind === 'ALL' ? null : form.scopeValue,
          isActive: form.isActive,
          conditions: form.conditions,
          levels: form.levels,
        }),
      },
    ),
    onSuccess: () => {
      setEditing(null);
      setNotice({ tone: 'good', text: 'Workflow configuration saved.' });
      queryClient.invalidateQueries({ queryKey: ['approval-workflows', organizationId] });
    },
    onError: (error) => setNotice({ tone: 'bad', text: errorText(error) }),
  });

  const remove = useMutation({
    mutationFn: (id: string) => api(`/api/v1/approval-workflows/${id}?organizationId=${organizationId}`, { method: 'DELETE' }),
    onSuccess: () => {
      setPendingDelete(null);
      setNotice({ tone: 'good', text: 'Workflow configuration deleted.' });
      queryClient.invalidateQueries({ queryKey: ['approval-workflows', organizationId] });
    },
    onError: (error) => setNotice({ tone: 'bad', text: errorText(error, 'Delete failed.') }),
  });

  function startCreate() {
    setForm(emptyForm());
    setEditing('new');
    setNotice(undefined);
  }

  function startEdit(row: Row) {
    setForm({
      name: row.name,
      approvalType: row.approvalType,
      action: row.action,
      scopeKind: row.scopeKind,
      scopeValue: row.scopeValue ?? '',
      isActive: row.isActive,
      conditions: row.conditions.map((item) => ({ ...item })),
      levels: row.levels.map((item) => ({ ...item })),
    });
    setEditing(row.id);
    setNotice(undefined);
  }

  return (
    <section className="sila-card" style={{ padding: 18 }}>
      <div className="sila-data-actions" style={{ marginBottom: 14 }}>
        <button className="sila-button sila-button--primary" type="button" onClick={startCreate}><Plus size={14} /> Add workflow configuration</button>
        <button className="sila-button" type="button" onClick={() => rows.refetch()}><RefreshCw size={14} /> Refresh</button>
      </div>
      {notice && <p className={`sila-muted-copy sila-integration-notice sila-integration-notice--${notice.tone}`} role="status">{notice.text}</p>}
      {editing && (
        <form className="sila-integration-form" style={{ marginBottom: 18 }} onSubmit={(event) => { event.preventDefault(); save.mutate(); }}>
          <div className="sila-form-grid">
            <label className="sila-form-field"><span>Approval type *</span>
              <select className="sila-select" value={form.approvalType} onChange={(event) => setForm((current) => ({ ...current, approvalType: event.target.value, conditions: [] }))}>
                {(catalog.data?.approvalTypes ?? []).map((item) => <option key={item.key} value={item.key}>{item.label}</option>)}
              </select>
            </label>
            <label className="sila-form-field"><span>Action</span>
              <select className="sila-select" value={form.action} onChange={(event) => setForm((current) => ({ ...current, action: event.target.value }))}>
                {(catalog.data?.actions ?? ['ALL', 'CREATE', 'CHANGE']).map((item) => <option key={item} value={item}>{item}</option>)}
              </select>
            </label>
            <label className="sila-form-field"><span>Name *</span>
              <input className="sila-input" value={form.name} onChange={(event) => setForm((current) => ({ ...current, name: event.target.value }))} placeholder="e.g. Recipe create — all properties" />
            </label>
            <label className="sila-form-field"><span>Status</span>
              <select className="sila-select" value={form.isActive ? 'ACTIVE' : 'INACTIVE'} onChange={(event) => setForm((current) => ({ ...current, isActive: event.target.value === 'ACTIVE' }))}>
                <option value="ACTIVE">ACTIVE</option>
                <option value="INACTIVE">INACTIVE</option>
              </select>
            </label>
            <label className="sila-form-field"><span>Select by *</span>
              <select className="sila-select" value={form.scopeKind} onChange={(event) => setForm((current) => ({ ...current, scopeKind: event.target.value, scopeValue: '' }))}>
                {(catalog.data?.scopeKinds ?? []).map((item) => <option key={item} value={item}>{item.replaceAll('_', ' ')}</option>)}
              </select>
            </label>
            {form.scopeKind !== 'ALL' && (
              <label className="sila-form-field"><span>{form.scopeKind.replaceAll('_', ' ')} *</span>
                <select className="sila-select" value={form.scopeValue} onChange={(event) => setForm((current) => ({ ...current, scopeValue: event.target.value }))}>
                  <option value="">Select</option>
                  {(scopes.data ?? []).map((item) => <option key={item.value} value={item.value}>{item.value} — {item.label}</option>)}
                </select>
              </label>
            )}
          </div>
          {typeMeta && <p className="sila-muted-copy" style={{ marginTop: 10 }}>{typeMeta.runtimeNote}</p>}
          <div style={{ marginTop: 14 }}>
            <strong>Condition</strong>
            <p className="sila-muted-copy">Optional custom field greater than / less than. All listed conditions must match.</p>
            {form.conditions.map((condition, index) => (
              <div className="sila-filter-row" key={`${condition.fieldKey}-${index}`} style={{ marginTop: 8 }}>
                <select className="sila-select" value={condition.fieldKey} onChange={(event) => setForm((current) => {
                  const next = [...current.conditions];
                  next[index] = { ...next[index], fieldKey: event.target.value };
                  return { ...current, conditions: next };
                })}>
                  {fields.map((field) => <option key={field.key} value={field.key}>{field.label}</option>)}
                </select>
                <select className="sila-select" value={condition.operator} onChange={(event) => setForm((current) => {
                  const next = [...current.conditions];
                  next[index] = { ...next[index], operator: event.target.value };
                  return { ...current, conditions: next };
                })}>
                  <option value="GREATER_THAN">Greater than</option>
                  <option value="LESS_THAN">Less than</option>
                </select>
                <input className="sila-input" type="number" value={condition.value} onChange={(event) => setForm((current) => {
                  const next = [...current.conditions];
                  next[index] = { ...next[index], value: Number(event.target.value) };
                  return { ...current, conditions: next };
                })} />
                <button className="sila-button" type="button" onClick={() => setForm((current) => ({ ...current, conditions: current.conditions.filter((_, item) => item !== index) }))}><Trash2 size={14} /></button>
              </div>
            ))}
            <button className="sila-button" type="button" style={{ marginTop: 8 }} onClick={() => setForm((current) => ({
              ...current,
              conditions: [...current.conditions, { fieldKey: fields[0]?.key ?? 'GROSS_AMOUNT', operator: 'GREATER_THAN', value: 0 }],
            }))}>Add condition</button>
          </div>
          <div style={{ marginTop: 16 }}>
            <strong>Approvers</strong>
            <p className="sila-muted-copy">Level 1 is required. Add more levels for sequential approval.</p>
            {form.levels.map((level, index) => (
              <div className="sila-filter-row" key={level.level} style={{ marginTop: 8 }}>
                <span className="sila-muted-copy" style={{ minWidth: 64 }}>Level {index + 1}</span>
                <select className="sila-select" value={level.roleKey} onChange={(event) => setForm((current) => {
                  const next = [...current.levels];
                  const role = catalog.data?.roles.find((item) => item.key === event.target.value);
                  next[index] = { ...next[index], roleKey: event.target.value, label: role?.name ?? next[index].label };
                  return { ...current, levels: next };
                })}>
                  <option value="">Select role</option>
                  {(catalog.data?.roles ?? []).map((role) => <option key={role.key} value={role.key}>{role.name} ({role.key})</option>)}
                </select>
                <input className="sila-input" value={level.label} onChange={(event) => setForm((current) => {
                  const next = [...current.levels];
                  next[index] = { ...next[index], label: event.target.value };
                  return { ...current, levels: next };
                })} placeholder="Label" />
                {index > 0 && <button className="sila-button" type="button" onClick={() => setForm((current) => ({ ...current, levels: current.levels.filter((_, item) => item !== index).map((item, order) => ({ ...item, level: order + 1 })) }))}><Trash2 size={14} /></button>}
              </div>
            ))}
            <button className="sila-button" type="button" style={{ marginTop: 8 }} onClick={() => setForm((current) => ({
              ...current,
              levels: [...current.levels, { level: current.levels.length + 1, roleKey: '', label: `Level ${current.levels.length + 1}` }],
            }))}>Add more levels</button>
          </div>
          <div className="sila-integration-actions" style={{ marginTop: 14 }}>
            <button className="sila-button" type="button" onClick={() => setEditing(null)}>Cancel</button>
            <button className="sila-button sila-button--primary" type="submit" disabled={save.isPending || !form.name || form.levels.some((item) => !item.roleKey)}>
              {save.isPending ? 'Saving…' : 'Save workflow'}
            </button>
          </div>
        </form>
      )}
      <div className="sila-table-wrap">
        <table className="sila-table">
          <thead><tr><th>Approval type</th><th>Action</th><th>Select by</th><th>Condition</th><th>Approvers</th><th>Status</th><th></th></tr></thead>
          <tbody>
            {(rows.data ?? []).length === 0 ? <tr><td colSpan={7} className="sila-muted-copy">No workflow configurations yet. Add one for Recipe, Material Master, Supplier Master, Internal Transfer, GRN, or Invoice.</td></tr> : (rows.data ?? []).map((row) => (
              <tr key={row.id}>
                <td>{row.name}<div className="sila-muted-copy">{row.approvalType.replaceAll('_', ' ')}</div></td>
                <td>{row.action}</td>
                <td>{row.scopeKind === 'ALL' ? 'ALL' : `${row.scopeKind.replaceAll('_', ' ')} ${row.scopeValue ?? ''}`}</td>
                <td>{row.conditions.length === 0 ? '—' : row.conditions.map((item) => `${item.fieldKey} ${item.operator === 'GREATER_THAN' ? '>' : '<'} ${item.value}`).join(', ')}</td>
                <td>{row.levels.map((level) => `L${level.level} ${level.label}`).join(' → ')}</td>
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
            <h2 style={{ marginTop: 0 }}>Delete this workflow configuration?</h2>
            <p>Invoice OCR, GRN posting, and Integration Routing are not changed by this delete.</p>
            <p><strong>{pendingDelete.name}</strong> — {pendingDelete.approvalType}</p>
            <div style={{ display: 'flex', gap: 8, justifyContent: 'flex-end' }}>
              <button type="button" className="sila-button sila-button--quiet" onClick={() => setPendingDelete(null)}>Cancel</button>
              <button type="button" className="sila-button" disabled={remove.isPending} onClick={() => remove.mutate(pendingDelete.id)}>{remove.isPending ? 'Deleting…' : 'Delete'}</button>
            </div>
          </div>
        </div>
      ) : null}
    </section>
  );
}
