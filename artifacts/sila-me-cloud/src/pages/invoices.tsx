import { Link } from 'wouter';
import { FileUp, Search } from 'lucide-react';
import { useRef, useState } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { getGetInvoicesQueryKey, useGetInvoices, useUploadInvoiceDocument } from '@workspace/api-client-react';
import { SilaPageHeader, SilaDataTable, StatusBadge, QueryError } from '@/components/sila-ui';
import { useAccessContext } from '@/components/sila-layout';
import { formatDate, formatMoney } from '@/lib/formatters';
const request = { credentials: 'include' as const };

export default function Invoices() {
  const { accessContext } = useAccessContext();
  const queryClient = useQueryClient();
  const query = useGetInvoices({ request, query: { queryKey: getGetInvoicesQueryKey(), retry: false } });
  const upload = useUploadInvoiceDocument({ request });
  const inputRef = useRef<HTMLInputElement>(null);
  const [search, setSearch] = useState('');
  const [message, setMessage] = useState('');
  const rows = (query.data ?? []).filter((invoice) => `${invoice.invoiceNumber} ${invoice.supplierName ?? ''}`.toLowerCase().includes(search.toLowerCase()));
  const chooseFile = (file?: File) => {
    const organizationId = accessContext?.organizations[0]?.id;
    if (!file || !organizationId) { setMessage('Select an organization context before uploading.'); return; }
    upload.mutate({ data: { organizationId, operatingUnitId: accessContext?.units[0]?.id ?? null, sourceChannel: 'CLOUD_UPLOAD', pageCount: 1, deferFullExtraction: false, file } }, { onSuccess: () => { setMessage('Invoice uploaded. Extraction is now processing.'); queryClient.invalidateQueries({ queryKey: getGetInvoicesQueryKey() }); }, onError: () => setMessage('Invoice upload failed. Check the document and your Cloud permission.') });
  };
  return <><SilaPageHeader eyebrow="Receiving / Documents" title="Invoices" description="A single control queue for uploaded documents, extraction, matching, and receipt readiness." actions={<><input ref={inputRef} type="file" accept="application/pdf,image/*" hidden onChange={(event) => chooseFile(event.target.files?.[0])} data-testid="input-upload-invoice-file" /><button className="sila-button sila-button--primary" type="button" onClick={() => inputRef.current?.click()} disabled={upload.isPending} data-testid="button-upload-invoice"><FileUp size={14} />{upload.isPending ? 'Uploading' : 'Upload invoice'}</button></>} />{message && <p style={{ margin: '-12px 0 18px', color: 'var(--sila-blue)', fontSize: 12 }} role="status" data-testid="status-invoice-upload">{message}</p>}<div className="sila-filter-row"><input className="sila-input" value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Search invoice or supplier" aria-label="Search invoices" data-testid="input-search-invoices" /><button className="sila-button" type="button" data-testid="button-search-invoices"><Search size={14} /> Search</button></div>{query.isError ? <QueryError onRetry={() => query.refetch()} /> : <SilaDataTable loading={query.isPending} empty={!query.isPending && rows.length === 0} emptyTitle={search ? 'No matching invoices' : 'No invoices captured'} emptyDescription="Cloud-uploaded invoices and Mobile capture activity will appear in this queue."><table className="sila-table"><thead><tr><th>Invoice</th><th>Supplier</th><th>Date</th><th>PO reference</th><th className="sila-table__right">Gross</th><th>Type</th><th>Status</th></tr></thead><tbody>{rows.map((invoice) => <tr key={invoice.id}><td><Link className="sila-table__primary" href={`/receiving/invoices/${invoice.id}`} data-testid={`link-invoice-${invoice.id}`}>{invoice.invoiceNumber}</Link><span className="sila-table__secondary">{formatDate(invoice.createdAt)}</span></td><td>{invoice.supplierName ?? 'Supplier pending'}</td><td>{formatDate(invoice.invoiceDate)}</td><td>{invoice.purchaseOrderNumber ?? 'Not matched'}</td><td className="sila-table__right">{formatMoney(invoice.grossAmount, invoice.currency ?? 'AED')}</td><td>{invoice.invoiceType}</td><td><StatusBadge value={invoice.status} /></td></tr>)}</tbody></table></SilaDataTable>}</>;
}