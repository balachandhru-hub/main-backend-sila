import { useEffect, useMemo, useState } from 'react';
import { Save, Settings2 } from 'lucide-react';
import {
  getGetAccessContextQueryKey,
  getGetInvoiceOcrConfigurationQueryKey,
  useGetAccessContext,
  useGetInvoiceOcrConfiguration,
  useUpdateInvoiceOcrConfiguration,
} from '@workspace/api-client-react';
import type { InvoiceOcrConfigurationInput } from '@workspace/api-client-react';
import { SilaPageHeader } from '@/components/sila-ui';

const request = { credentials: 'include' as const };

const defaults: InvoiceOcrConfigurationInput = {
  mobileBasicOcrEnabled: true,
  automaticBackendFallbackEnabled: true,
  minimumMobileConfidence: 0.75,
  requireSupplierName: true,
  requireInvoiceNumber: true,
  requirePurchaseOrderNumber: true,
  requireInvoiceAmount: true,
  requireInvoiceDate: false,
  requireCurrency: false,
  requireSupplierTrn: false,
  backendProvider: 'BUILT_IN_ADVANCED',
  alwaysBackendOnReread: true,
  detailedLineExtractionEnabled: true,
  supplierMasterValidationEnabled: true,
  purchaseOrderValidationEnabled: true,
  financialReconciliationEnabled: true,
  amountTolerance: 0.05,
  backendTimeoutSeconds: 60,
  backendRetryCount: 1,
  reuseCachedOcr: true,
};

const policyFields: Array<[keyof InvoiceOcrConfigurationInput, string]> = [
  ['requireSupplierName', 'Supplier name'],
  ['requireInvoiceNumber', 'Invoice number'],
  ['requirePurchaseOrderNumber', 'Purchase-order number'],
  ['requireInvoiceAmount', 'Invoice amount'],
  ['requireInvoiceDate', 'Invoice date'],
  ['requireCurrency', 'Currency'],
  ['requireSupplierTrn', 'Supplier TRN'],
];

export default function DocumentExtraction() {
  const context = useGetAccessContext({ request, query: { queryKey: getGetAccessContextQueryKey(), retry: false } });
  const organizationId = context.data?.organizations[0]?.id;
  const config = useGetInvoiceOcrConfiguration(
    organizationId ? { organizationId } : undefined,
    { request, query: { queryKey: getGetInvoiceOcrConfigurationQueryKey(organizationId ? { organizationId } : undefined), enabled: Boolean(organizationId), retry: false } },
  );
  const update = useUpdateInvoiceOcrConfiguration({ request });
  const [form, setForm] = useState<InvoiceOcrConfigurationInput>(defaults);
  const [message, setMessage] = useState('');
  const loadedConfig = useMemo(() => config.data ? {
    mobileBasicOcrEnabled: config.data.mobileBasicOcrEnabled,
    automaticBackendFallbackEnabled: config.data.automaticBackendFallbackEnabled,
    minimumMobileConfidence: config.data.minimumMobileConfidence,
    requireSupplierName: config.data.requireSupplierName,
    requireInvoiceNumber: config.data.requireInvoiceNumber,
    requirePurchaseOrderNumber: config.data.requirePurchaseOrderNumber,
    requireInvoiceAmount: config.data.requireInvoiceAmount,
    requireInvoiceDate: config.data.requireInvoiceDate,
    requireCurrency: config.data.requireCurrency,
    requireSupplierTrn: config.data.requireSupplierTrn,
    backendProvider: config.data.backendProvider,
    alwaysBackendOnReread: config.data.alwaysBackendOnReread,
    detailedLineExtractionEnabled: config.data.detailedLineExtractionEnabled,
    supplierMasterValidationEnabled: config.data.supplierMasterValidationEnabled,
    purchaseOrderValidationEnabled: config.data.purchaseOrderValidationEnabled,
    financialReconciliationEnabled: config.data.financialReconciliationEnabled,
    amountTolerance: config.data.amountTolerance,
    backendTimeoutSeconds: config.data.backendTimeoutSeconds,
    backendRetryCount: config.data.backendRetryCount,
    reuseCachedOcr: config.data.reuseCachedOcr,
  } : null, [config.data]);

  useEffect(() => {
    if (loadedConfig) setForm(loadedConfig);
  }, [loadedConfig]);

  const setBoolean = (key: keyof InvoiceOcrConfigurationInput, value: boolean) =>
    setForm(current => ({ ...current, [key]: value }));
  const save = () => {
    setMessage('');
    update.mutate({ data: form, params: organizationId ? { organizationId } : undefined }, {
      onSuccess: () => setMessage('Invoice OCR policy saved. New scans will use this organization policy.'),
      onError: () => setMessage('The OCR policy could not be saved. Check your configuration permission and try again.'),
    });
  };

  return (
    <>
      <SilaPageHeader
        eyebrow="Administration / Document extraction"
        title="Invoice OCR policy"
        description="Control when Mobile Basic OCR hands an invoice to backend Advanced OCR. The backend remains authoritative for fallback and re-read decisions."
        actions={<span className="sila-status sila-status--good" data-testid="status-advanced-ocr-connected">Backend OCR connected</span>}
      />
      {message && <p role="status" style={{ color: message.startsWith('Invoice') ? 'var(--sila-blue)' : 'var(--sila-red, #b42318)', fontSize: 12 }}>{message}</p>}
      <section className="sila-card" style={{ padding: 22, display: 'grid', gap: 24 }} data-testid="section-invoice-ocr-policy">
        <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
          <Settings2 size={17} color="var(--sila-blue)" />
          <div><strong>Two-level extraction</strong><p style={{ margin: '4px 0 0', color: 'var(--sila-muted)', fontSize: 12 }}>Basic OCR remains fast on the device. Advanced OCR uses the authoritative PDF and creates a versioned extraction run.</p></div>
        </div>
        <div className="sila-form-grid">
          <label className="sila-form-field"><span>Backend provider</span><select className="sila-select" value={form.backendProvider} onChange={event => setForm(current => ({ ...current, backendProvider: event.target.value }))}><option value="BUILT_IN_ADVANCED">Built-in Advanced OCR</option></select></label>
          <label className="sila-form-field"><span>Minimum Mobile confidence</span><input className="sila-input" type="number" min="0" max="1" step="0.01" value={form.minimumMobileConfidence} onChange={event => setForm(current => ({ ...current, minimumMobileConfidence: Number(event.target.value) }))} /></label>
          <label className="sila-form-field"><span>Amount tolerance</span><input className="sila-input" type="number" min="0" step="0.01" value={form.amountTolerance} onChange={event => setForm(current => ({ ...current, amountTolerance: Number(event.target.value) }))} /></label>
          <label className="sila-form-field"><span>Backend timeout (seconds)</span><input className="sila-input" type="number" min="1" max="600" value={form.backendTimeoutSeconds} onChange={event => setForm(current => ({ ...current, backendTimeoutSeconds: Number(event.target.value) }))} /></label>
        </div>
        <div style={{ display: 'grid', gap: 10 }}>
          {[
            ['mobileBasicOcrEnabled', 'Enable Mobile Basic OCR'],
            ['automaticBackendFallbackEnabled', 'Automatically fall back to backend Advanced OCR when policy checks fail'],
            ['alwaysBackendOnReread', 'Always use backend Advanced OCR for re-read'],
            ['detailedLineExtractionEnabled', 'Extract invoice lines and line-level confidence'],
            ['supplierMasterValidationEnabled', 'Validate supplier candidates against organization master data'],
            ['purchaseOrderValidationEnabled', 'Validate purchase-order candidates against organization data'],
            ['financialReconciliationEnabled', 'Require financial reconciliation before marking extraction complete'],
            ['reuseCachedOcr', 'Reuse an exact document-hash extraction when available'],
          ].map(([key, label]) => <label key={key} style={{ display: 'flex', gap: 10, alignItems: 'center', fontSize: 13 }}><input type="checkbox" checked={Boolean(form[key as keyof InvoiceOcrConfigurationInput])} onChange={event => setBoolean(key as keyof InvoiceOcrConfigurationInput, event.target.checked)} />{label}</label>)}
        </div>
        <div>
          <strong style={{ fontSize: 13 }}>Required Mobile fields</strong>
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: 14, marginTop: 12 }}>
            {policyFields.map(([key, label]) => <label key={key} style={{ display: 'flex', gap: 7, alignItems: 'center', fontSize: 12 }}><input type="checkbox" checked={Boolean(form[key])} onChange={event => setBoolean(key, event.target.checked)} />{label}</label>)}
          </div>
        </div>
        <div><button className="sila-button sila-button--primary" type="button" onClick={save} disabled={update.isPending || !organizationId}><Save size={14} />{update.isPending ? 'Saving…' : 'Save OCR policy'}</button>{config.data && <span style={{ color: 'var(--sila-muted)', fontSize: 11, marginLeft: 12 }}>Version {config.data.version}</span>}</div>
      </section>
    </>
  );
}