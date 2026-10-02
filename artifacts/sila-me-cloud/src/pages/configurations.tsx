import { useEffect, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { customFetch, getGetAccessContextQueryKey, useGetAccessContext } from '@workspace/api-client-react';
import { SilaPageHeader } from '@/components/sila-ui';
import { ApprovalWorkflowPanel } from '@/components/approval-workflow-panel';
import { IntegrationRoutePanel } from '@/components/integration-route-panel';

const request = { credentials: 'include' as const };
async function api<T>(url: string, init?: RequestInit): Promise<T> {
  return customFetch<T>(url, { ...request, ...init, responseType: 'json' });
}

type PolicyRow = {
  enabled: boolean; allowedSourceLocationTypes: string[]; allowedDestinationLocationTypes: string[];
  maximumQuantity?: number | null; maximumValue?: number | null; samePropertyAllowed: boolean; crossPropertyAllowed: boolean;
  sourceConfirmationRequired: boolean; destinationConfirmationRequired: boolean; managerNotification: boolean; skipManagerApproval: boolean;
};

function QuickTransferPolicyPanel({ organizationId }: { organizationId: string }) {
  const client = useQueryClient();
  const policy = useQuery({
    queryKey: ['quick-transfer-policy', organizationId],
    queryFn: () => api<PolicyRow>(`/api/v1/inventory/quick-transfer/policy?organizationId=${organizationId}`),
    enabled: Boolean(organizationId), retry: false,
  });
  const [form, setForm] = useState<PolicyRow | null>(null);
  useEffect(() => { if (policy.data) setForm(policy.data); }, [policy.data]);
  const save = useMutation({
    mutationFn: () => api(`/api/v1/inventory/quick-transfer/policy?organizationId=${organizationId}`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(form) }),
    onSuccess: () => client.invalidateQueries({ queryKey: ['quick-transfer-policy'] }),
  });
  if (!form) return null;
  return (
    <>
      <section className="sila-card" style={{ padding: 18, margin: '16px 0' }}>
        <span className="sila-section-kicker">Operating rules</span>
        <h2 style={{ marginTop: 6 }}>Quick Transfer policy</h2>
        <p className="sila-muted-copy">Limits and confirmation rules for inventory Quick Transfer. Location Master remains the shared location list.</p>
      </section>
      <form className="sila-card sila-recipe-panel" onSubmit={(event) => { event.preventDefault(); save.mutate(); }}>
        <div className="sila-form-grid">
          <label className="sila-form-field"><span>Max quantity</span><input className="sila-input" type="number" value={form.maximumQuantity ?? ''} onChange={(event) => setForm({ ...form, maximumQuantity: event.target.value ? Number(event.target.value) : null })} /></label>
          <label className="sila-form-field"><span>Max value</span><input className="sila-input" type="number" value={form.maximumValue ?? ''} onChange={(event) => setForm({ ...form, maximumValue: event.target.value ? Number(event.target.value) : null })} /></label>
        </div>
        <div className="sila-inventory-flags" style={{ marginTop: 12 }}>
          <label><input type="checkbox" checked={form.enabled} onChange={(event) => setForm({ ...form, enabled: event.target.checked })} /> Enabled</label>
          <label><input type="checkbox" checked={form.samePropertyAllowed} onChange={(event) => setForm({ ...form, samePropertyAllowed: event.target.checked })} /> Same property</label>
          <label><input type="checkbox" checked={form.crossPropertyAllowed} onChange={(event) => setForm({ ...form, crossPropertyAllowed: event.target.checked })} /> Cross property</label>
          <label><input type="checkbox" checked={form.sourceConfirmationRequired} onChange={(event) => setForm({ ...form, sourceConfirmationRequired: event.target.checked })} /> Source confirmation</label>
          <label><input type="checkbox" checked={form.destinationConfirmationRequired} onChange={(event) => setForm({ ...form, destinationConfirmationRequired: event.target.checked })} /> Destination confirmation</label>
          <label><input type="checkbox" checked={form.managerNotification} onChange={(event) => setForm({ ...form, managerNotification: event.target.checked })} /> Manager notification</label>
          <label><input type="checkbox" checked={form.skipManagerApproval} onChange={(event) => setForm({ ...form, skipManagerApproval: event.target.checked })} /> Skip manager approval</label>
        </div>
        <div className="sila-integration-actions"><button type="submit" className="sila-button sila-button--primary">Save policy</button></div>
      </form>
    </>
  );
}

export default function ConfigurationsPage() {
  const context = useGetAccessContext({ request, query: { queryKey: getGetAccessContextQueryKey(), retry: false } });
  const organizationId = context.data?.organizations[0]?.id;

  return (
    <>
      <SilaPageHeader
        eyebrow="Administration"
        title="Workflows & configuration"
        description="Organization operating rules: approvals, integration routing, and inventory transfer policy."
      />
      <section className="sila-card sila-recipe-panel" style={{ marginBottom: 16 }}>
        <h3 className="sila-recipe-panel__title">Workflow configuration</h3>
        <p className="sila-muted-copy">
          Configure Recipe, Material Master, Supplier Master, Internal Transfer, GRN, and Invoice approvals in one place.
          Choose approval type, select by Property / Company Code / Outlet / Store, add greater-than or less-than conditions, then set Level 1 approvers and add more levels.
        </p>
      </section>
      {organizationId
        ? <ApprovalWorkflowPanel organizationId={organizationId} />
        : <div className="sila-card" style={{ padding: 22 }}>No organization scope is available for this account.</div>}
      <section className="sila-card sila-recipe-panel" style={{ margin: '16px 0' }}>
        <h3 className="sila-recipe-panel__title">Integration Routing</h3>
        <p className="sila-muted-copy">
          Map an API type and company code to a saved API configuration. Technical connection details stay on Integrations.
        </p>
      </section>
      {organizationId ? <IntegrationRoutePanel organizationId={organizationId} /> : null}
      {organizationId ? <QuickTransferPolicyPanel organizationId={organizationId} /> : null}
    </>
  );
}
