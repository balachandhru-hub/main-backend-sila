import { useEffect, useState } from 'react';
import { ArrowLeft, FileText, RefreshCw, Save, ScanLine } from 'lucide-react';
import { Link, useParams } from 'wouter';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { getGetDocumentQueryKey, getGetInvoiceExtractionHistoryQueryKey, getGetInvoiceExtractionQueryKey, getGetInvoiceQueryKey, useAdvancedReread, useGetDocument, useGetInvoice, useGetInvoiceBasicExtraction, useGetInvoiceExtraction, useGetInvoiceExtractionHistory, useProcessInvoice, useReprocessInvoice, useUpdateInvoice } from '@workspace/api-client-react';
import { SilaPageHeader, SilaDataTable, StatusBadge, QueryError, EmptyState } from '@/components/sila-ui';
import { ExtractionCenter } from '@/components/extraction-center';
import { formatDate, formatDateTime, formatMoney, formatNumber } from '@/lib/formatters';
const request = { credentials: 'include' as const };

type Transfer = {
  destinationId: string;
  provider: string;
  status: string;
  resolutionSource?: string | null;
  folderPath?: string | null;
  externalFileName?: string | null;
  externalWebUrl?: string | null;
  attemptCount: number;
  nextAttemptAt?: string | null;
  completedAt?: string | null;
  lastErrorCode?: string | null;
  lastErrorMessage?: string | null;
};

function TransferMonitoring({ documentId }: { documentId: string }) {
  const [retryingId, setRetryingId] = useState('');
  const transfers = useQuery({
    queryKey: ['document-transfers', documentId],
    enabled: Boolean(documentId),
    queryFn: async () => {
      const response = await fetch(`/api/v1/documents/${documentId}/transfers`, { credentials: 'include' });
      if (!response.ok) throw new Error('Transfer status could not be loaded.');
      return (await response.json()) as { transfers: Transfer[] };
    },
    refetchInterval: 10000,
  });
  const retry = async (transferId: string) => {
    setRetryingId(transferId);
    try {
      await fetch(`/api/v1/document-transfers/${transferId}/retry`, { method: 'POST', credentials: 'include' });
      await transfers.refetch();
    } finally {
      setRetryingId('');
    }
  };
  const rows = transfers.data?.transfers ?? [];
  return <section className="sila-card sila-detail-section" data-testid="section-document-transfers">
    <h2 className="sila-detail-section__title"><span style={{ color: 'var(--sila-ink)', fontWeight: 700 }}>External document storage</span><span>{transfers.isPending ? 'Loading' : `${rows.length} route${rows.length === 1 ? '' : 's'}`}</span></h2>
    {transfers.isError ? <p style={{ color: 'var(--sila-muted)', fontSize: 12 }}>Transfer status is unavailable.</p> : rows.length === 0 ? <p style={{ color: 'var(--sila-muted)', fontSize: 12 }}>No validated external destination was selected for this document. The SILA ME PDF remains available.</p> : rows.map((transfer) => <div className="sila-side-list" key={transfer.destinationId} style={{ marginBottom: 10 }}>
      <div className="sila-side-list__row"><span className="sila-side-list__label">Provider / scope</span><strong className="sila-side-list__value">{transfer.provider} · {transfer.resolutionSource ?? 'Configured'}</strong></div>
      <div className="sila-side-list__row"><span className="sila-side-list__label">Status</span><StatusBadge value={transfer.status} /></div>
      <div className="sila-side-list__row"><span className="sila-side-list__label">Folder</span><strong className="sila-side-list__value">{transfer.folderPath ?? 'Configured destination'}</strong></div>
      {transfer.externalWebUrl && <div className="sila-side-list__row"><span className="sila-side-list__label">Stored file</span><a className="sila-side-list__value" href={transfer.externalWebUrl} target="_blank" rel="noreferrer">{transfer.externalFileName ?? 'Open in SharePoint'}</a></div>}
      {transfer.lastErrorMessage && <p style={{ color: 'var(--sila-red, #b42318)', fontSize: 12, margin: '8px 0' }}>{transfer.lastErrorMessage}</p>}
      {(transfer.status === 'FAILED' || transfer.status === 'FAILED_AUTHENTICATION' || transfer.status === 'RETRY_PENDING') && <button className="sila-button" type="button" onClick={() => retry(transfer.destinationId)} disabled={retryingId === transfer.destinationId}>{retryingId === transfer.destinationId ? 'Retrying…' : 'Retry transfer'}</button>}
    </div>)}
  </section>;
}

export default function InvoiceDetail() {
  const { id = '' } = useParams<{ id: string }>();
  const queryClient = useQueryClient();
  const invoice = useGetInvoice(id, { request, query: { queryKey: getGetInvoiceQueryKey(id), retry: false } });
  const extraction = useGetInvoiceExtraction(id, { request, query: { queryKey: getGetInvoiceExtractionQueryKey(id), retry: false, enabled: Boolean(id) } });
  const history = useGetInvoiceExtractionHistory(id, { request, query: { queryKey: getGetInvoiceExtractionHistoryQueryKey(id), retry: false, enabled: Boolean(id) } });
  const basic = useGetInvoiceBasicExtraction(id, { request, query: { queryKey: ['invoice-basic-extraction', id], retry: false, enabled: Boolean(id) } });
  const documentId = invoice.data?.documentId ?? '';
  const document = useGetDocument(documentId, { request, query: { queryKey: getGetDocumentQueryKey(documentId), retry: false, enabled: Boolean(documentId) } });
  const update = useUpdateInvoice({ request });
  const process = useProcessInvoice({ request });
  const reprocess = useReprocessInvoice({ request });
  const advancedReread = useAdvancedReread({ request });
  const [invoiceNumber, setInvoiceNumber] = useState('');
  const [supplierName, setSupplierName] = useState('');
  const [message, setMessage] = useState('');
  useEffect(() => { if (invoice.data) { setInvoiceNumber(invoice.data.invoiceNumber); setSupplierName(invoice.data.supplierName ?? ''); } }, [invoice.data]);
  if (invoice.isError) return <QueryError onRetry={() => invoice.refetch()} />;
  if (invoice.isPending) return <SilaDataTable loading />;
  const row = invoice.data;
  if (!row) return <EmptyState title="Invoice not found" description="This invoice did not return a detail record." />;
  const saveCorrection = () => update.mutate({ id, data: { invoiceNumber: invoiceNumber.trim(), supplierName: supplierName.trim() || null, poNumber: row.purchaseOrderNumber, invoiceDate: row.invoiceDate, currency: row.currency, netAmount: row.netAmount, taxAmount: row.taxAmount, grossAmount: row.grossAmount, supplierTaxNumber: row.supplierTaxNumber } }, { onSuccess: () => { setMessage('Correction saved to the invoice record.'); queryClient.invalidateQueries({ queryKey: getGetInvoiceQueryKey(id) }); }, onError: () => setMessage('Correction could not be saved.') });
  const validateProcess = () => process.mutate({ id }, { onSuccess: () => { setMessage('Invoice processing started.'); queryClient.invalidateQueries({ queryKey: getGetInvoiceQueryKey(id) }); }, onError: () => setMessage('Invoice processing could not be started.') });
  const runReprocess = () => reprocess.mutate({ id }, { onSuccess: () => { setMessage('Extraction reprocess started.'); extraction.refetch(); }, onError: () => setMessage('Extraction could not be reprocessed.') });
  const runAdvancedReread = () => advancedReread.mutate({ id }, { onSuccess: () => { setMessage('Backend Advanced OCR re-read completed. Manual corrections were preserved.'); invoice.refetch(); extraction.refetch(); history.refetch(); }, onError: () => setMessage('Advanced OCR re-read could not be completed.') });
  return <><SilaPageHeader eyebrow="Receiving / Invoices" title={row.invoiceNumber} description={`${row.supplierName ?? 'Supplier pending'} · captured ${formatDateTime(row.createdAt)}`} actions={<><Link href="/receiving/invoices" className="sila-button" data-testid="link-back-invoices"><ArrowLeft size={14} /> Back to invoices</Link><button className="sila-button" type="button" onClick={validateProcess} disabled={process.isPending} data-testid="button-process-invoice"><ScanLine size={14} />{process.isPending ? 'Processing' : 'Process'}</button><button className="sila-button" type="button" onClick={runReprocess} disabled={reprocess.isPending} data-testid="button-reprocess-invoice"><RefreshCw size={14} /> Reprocess</button><button className="sila-button" type="button" onClick={runAdvancedReread} disabled={advancedReread.isPending} data-testid="button-advanced-reread"><RefreshCw size={14} />{advancedReread.isPending ? 'Re-reading…' : 'Advanced re-read'}</button></>} />{message && <p style={{ margin: '-12px 0 18px', color: 'var(--sila-blue)', fontSize: 12 }} role="status" data-testid="status-invoice-detail">{message}</p>}<div className="sila-detail-grid"><div><section className="sila-card sila-detail-section"><h2 className="sila-detail-section__title"><span style={{ color: 'var(--sila-ink)', fontWeight: 700 }}><FileText size={15} style={{ verticalAlign: 'middle', marginRight: 7 }} />Invoice details</span><StatusBadge value={row.status} /></h2><div className="sila-meta-grid"><div><span className="sila-meta-label">Supplier</span><strong className="sila-meta-value" data-testid="text-invoice-supplier">{row.supplierName ?? 'Pending'}</strong></div><div><span className="sila-meta-label">Invoice date</span><strong className="sila-meta-value">{formatDate(row.invoiceDate)}</strong></div><div><span className="sila-meta-label">Purchase order</span><strong className="sila-meta-value">{row.purchaseOrderNumber ?? 'Not matched'}</strong></div><div><span className="sila-meta-label">Gross amount</span><strong className="sila-meta-value">{formatMoney(row.grossAmount, row.currency ?? 'AED')}</strong></div><div><span className="sila-meta-label">Invoice type</span><strong className="sila-meta-value">{row.invoiceType}</strong></div><div><span className="sila-meta-label">Confidence</span><strong className="sila-meta-value">{row.overallConfidence ? `${Math.round(row.overallConfidence * 100)}%` : 'Pending'}</strong></div></div></section><section className="sila-card sila-detail-section"><h2 className="sila-detail-section__title"><span style={{ color: 'var(--sila-ink)', fontWeight: 700 }}>Invoice lines</span><span>{row.lines.length} lines</span></h2>{row.invoiceType === 'SERVICE' && <p style={{ margin: '0 0 14px', color: 'var(--sila-muted)', fontSize: 12 }} data-testid="text-service-grn-note">Goods receipt not applicable for service invoice.</p>}<SilaDataTable empty={row.lines.length === 0} emptyTitle="No extracted lines" emptyDescription="Lines will appear after extraction completes."><table className="sila-table"><thead><tr><th>Description</th><th>Invoice quantity</th><th>Unit price</th><th>Line amount</th><th>Match</th></tr></thead><tbody>{row.lines.map((line) => <tr key={line.id}><td><strong>{line.description}</strong><span className="sila-table__secondary">{line.supplierMaterialCode ?? 'Material code pending'}</span></td><td>{line.quantity === null || line.quantity === undefined ? '—' : `${formatNumber(line.quantity)} ${line.uom ?? ''}`}</td><td>{formatMoney(line.unitPrice, row.currency ?? 'AED')}</td><td>{formatMoney(line.lineAmount, row.currency ?? 'AED')}</td><td><StatusBadge value={line.matchStatus} /></td></tr>)}</tbody></table></SilaDataTable><p style={{ margin: '12px 0 0', color: 'var(--sila-muted)', fontSize: 11 }}>Invoice quantity is kept distinct from actual received quantity. Actual receipt is recorded on a GRN.</p></section><section className="sila-card sila-detail-section"><h2 className="sila-detail-section__title"><span style={{ color: 'var(--sila-ink)', fontWeight: 700 }}>Correction</span><span>Audit recorded</span></h2><div className="sila-form-grid"><div className="sila-form-field"><label htmlFor="invoice-number">Invoice number</label><input id="invoice-number" value={invoiceNumber} onChange={(event) => setInvoiceNumber(event.target.value)} data-testid="input-correction-invoice-number" /></div><div className="sila-form-field"><label htmlFor="supplier-name">Supplier name</label><input id="supplier-name" value={supplierName} onChange={(event) => setSupplierName(event.target.value)} data-testid="input-correction-supplier-name" /></div><div className="sila-form-actions"><button className="sila-button sila-button--primary" type="button" onClick={saveCorrection} disabled={update.isPending || !invoiceNumber.trim()} data-testid="button-save-invoice-correction"><Save size={14} /> Save correction</button></div></div></section></div><aside><section className="sila-card sila-detail-section"><h2 className="sila-detail-section__title"><span style={{ color: 'var(--sila-ink)', fontWeight: 700 }}>Stored PDF</span><span>{document.data?.pageCount ?? 1} page</span></h2>{document.data ? <><div className="sila-side-list"><div className="sila-side-list__row"><span className="sila-side-list__label">Filename</span><strong className="sila-side-list__value">{document.data.filename}</strong></div><div className="sila-side-list__row"><span className="sila-side-list__label">Source</span><strong className="sila-side-list__value">{document.data.sourceChannel}</strong></div><div className="sila-side-list__row"><span className="sila-side-list__label">Status</span><StatusBadge value={document.data.status} /></div></div><iframe className="sila-pdf-viewer" src={`/api/v1/documents/${documentId}/content`} title={`Stored invoice PDF ${document.data.filename}`} data-testid="viewer-invoice-pdf" /></> : <p style={{ color: 'var(--sila-muted)', fontSize: 12 }} data-testid="text-document-loading">Loading stored document…</p>}</section><section className="sila-card sila-detail-section"><h2 className="sila-detail-section__title"><span style={{ color: 'var(--sila-ink)', fontWeight: 700 }}>Advanced extraction</span><span>{history.isPending ? 'Loading' : `${history.data?.length ?? 0} runs`}</span></h2>{history.isError ? <p style={{ color: 'var(--sila-muted)', fontSize: 12 }}>Extraction history is unavailable.</p> : (history.data ?? []).slice(0, 5).map((run) => <div className="sila-side-list" key={run.id} style={{ marginBottom: 10 }}><div className="sila-side-list__row"><span className="sila-side-list__label">Status / trigger</span><strong className="sila-side-list__value">{run.status} · {run.trigger}</strong></div><div className="sila-side-list__row"><span className="sila-side-list__label">Confidence</span><strong className="sila-side-list__value">{run.confidence === null || run.confidence === undefined ? 'Pending' : `${Math.round(run.confidence * 100)}%`}</strong></div><div className="sila-side-list__row"><span className="sila-side-list__label">Hash / completed</span><strong className="sila-side-list__value">{run.contentHash ? `${run.contentHash.slice(0, 12)}…` : 'Unavailable'} · {formatDateTime(run.processingCompletedAt ?? run.createdAt)}</strong></div></div>)}</section><section className="sila-card sila-detail-section"><h2 className="sila-detail-section__title"><span style={{ color: 'var(--sila-ink)', fontWeight: 700 }}>Extraction signal</span></h2><div className="sila-side-list"><div className="sila-side-list__row"><span className="sila-side-list__label">Full extraction</span><strong className="sila-side-list__value">{extraction.data?.status ?? 'Pending'}</strong></div><div className="sila-side-list__row"><span className="sila-side-list__label">Provider</span><strong className="sila-side-list__value">{extraction.data?.provider ?? basic.data?.status ?? 'Pending'}</strong></div><div className="sila-side-list__row"><span className="sila-side-list__label">Completed</span><strong className="sila-side-list__value">{formatDateTime(extraction.data?.completedAt)}</strong></div></div></section><TransferMonitoring documentId={documentId} /></aside></div><ExtractionCenter invoices={[row]} /></>;
}