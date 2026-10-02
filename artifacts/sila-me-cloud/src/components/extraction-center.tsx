import { useEffect, useState } from 'react';
import {
  getGetExtractionAgentsQueryKey,
  getGetInvoiceExtractionQueryKey,
  getGetDocumentQueryKey,
  useGetDocument,
  useCreateExtractionAgent,
  useGetExtractionAgents,
  useGetInvoiceExtraction,
  useTestExtractionAgent,
  useUpdateInvoice,
} from '@workspace/api-client-react';
import type { Invoice } from '@workspace/api-client-react';

type Props = {
  invoices: Invoice[];
};

const requestOptions = { credentials: 'include' as const };

export function ExtractionCenter({ invoices }: Props) {
  const [selectedId, setSelectedId] = useState(invoices[0]?.id ?? '');
  const [message, setMessage] = useState('');
  const [agentName, setAgentName] = useState('Built-in invoice reader');
  const [agentEndpoint, setAgentEndpoint] = useState('');
  const [agentReference, setAgentReference] = useState('');
  const [agentActive, setAgentActive] = useState(false);
  const [invoiceNumber, setInvoiceNumber] = useState('');
  const [supplierTrn, setSupplierTrn] = useState('');

  useEffect(() => {
    if (!selectedId && invoices[0]) setSelectedId(invoices[0].id);
  }, [invoices, selectedId]);

  const extraction = useGetInvoiceExtraction(selectedId, {
    request: requestOptions,
    query: {
      queryKey: getGetInvoiceExtractionQueryKey(selectedId),
      enabled: Boolean(selectedId),
      retry: false,
    },
  });
  const documentId = invoices.find((invoice) => invoice.id === selectedId)?.documentId ?? '';
  const document = useGetDocument(documentId, {
    request: requestOptions,
    query: { queryKey: getGetDocumentQueryKey(documentId), enabled: Boolean(documentId), retry: false },
  });
  const documentContentUrl = documentId ? `/api/v1/documents/${documentId}/content` : '';
  const agents = useGetExtractionAgents(undefined, {
    request: requestOptions,
    query: { queryKey: getGetExtractionAgentsQueryKey(), retry: false },
  });
  const createAgent = useCreateExtractionAgent({ request: requestOptions });
  const updateInvoice = useUpdateInvoice({ request: requestOptions });
  const testAgent = useTestExtractionAgent({ request: requestOptions });

  useEffect(() => {
    const header = extraction.data?.header;
    setInvoiceNumber(header?.supplierInvoiceNumber ?? '');
    setSupplierTrn(header?.supplierTrn ?? '');
  }, [extraction.data]);

  const saveCorrection = () => {
    if (!selectedId || !invoiceNumber.trim()) return;
    updateInvoice.mutate(
      { id: selectedId, data: { invoiceNumber: invoiceNumber.trim(), supplierTaxNumber: supplierTrn.trim() || null } },
      {
        onSuccess: () => {
          setMessage('Correction saved and recorded in the audit trail.');
          extraction.refetch();
        },
        onError: () => setMessage('The correction could not be saved. Check your Cloud permissions.'),
      },
    );
  };

  const saveAgent = () => {
    createAgent.mutate(
      {
        data: {
          name: agentName,
          documentType: 'INVOICE',
          providerType: 'CUSTOM_REST',
          endpointUrl: agentEndpoint || null,
          authenticationType: agentReference ? 'API_KEY' : 'NONE',
          credentialReference: agentReference || null,
          priority: 100,
          isActive: agentActive,
        },
      },
      {
        onSuccess: () => {
          setMessage('Provider configuration saved. Credentials are stored as references only.');
          agents.refetch();
        },
        onError: () => setMessage('The provider configuration could not be saved.'),
      },
    );
  };

  return (
    <section className="extraction-center" aria-label="Invoice extraction">
      <div className="access-panel__heading">
        <div>
          <p className="eyebrow"><span className="eyebrow__mark" />Extraction control</p>
          <h2>Review what the reader found.</h2>
        </div>
        <span className="access-panel__badge">PHASE 3.1</span>
      </div>
      <div className="extraction-center__grid">
        <div className="extraction-card">
          <span className="card-label">FULL EXTRACTION</span>
          <select value={selectedId} onChange={(event) => setSelectedId(event.target.value)} aria-label="Select invoice">
            <option value="">Select an invoice</option>
            {invoices.map((invoice) => <option key={invoice.id} value={invoice.id}>{invoice.invoiceNumber} · {invoice.supplierName ?? 'Supplier pending'}</option>)}
          </select>
          {extraction.isPending && <p className="extraction-muted">Loading extracted fields…</p>}
          {extraction.data && (
            <>
              <div className="extraction-meta">
                <span>{extraction.data.provider}</span>
                <span>{extraction.data.fallbackUsed ? 'Built-in fallback used' : extraction.data.extractionMethod}</span>
                <span>{extraction.data.confidence ? `${Math.round(extraction.data.confidence * 100)}% confidence` : 'Confidence pending'}</span>
                {document.data && <span>{document.data.sourceChannel.replaceAll('_', ' ')} · {document.data.pageCount ?? 1} page{document.data.pageCount === 1 ? '' : 's'} · Mobile basic extraction captured</span>}
              </div>
              <div className="extraction-fields">
                <label><span>Invoice number</span><input value={invoiceNumber} onChange={(event) => setInvoiceNumber(event.target.value)} /></label>
                <label><span>Supplier TRN</span><input value={supplierTrn} onChange={(event) => setSupplierTrn(event.target.value)} /></label>
              </div>
              <button className="admin-form__submit" type="button" onClick={saveCorrection} disabled={updateInvoice.isPending || !invoiceNumber.trim()}>Save correction</button>
              <div className="extraction-lines">
                {extraction.data.lines.map((line, index) => <div key={`${line.lineItemNumber ?? index}`}><strong>{line.itemSkuId ?? 'Unmatched item'}</strong><span>{line.itemDescription ?? 'No description'} · {line.itemAmount ?? '—'}</span></div>)}
              </div>
            </>
          )}
        </div>
        {document.data && documentContentUrl && (
          <div className="extraction-card document-preview-card">
            <div className="document-preview-card__heading">
              <div>
                <span className="card-label">STORED DOCUMENT</span>
                <p className="extraction-muted">The original uploaded PDF is shown here without rebuilding or flattening its pages.</p>
              </div>
              <a href={documentContentUrl} target="_blank" rel="noreferrer">Open PDF</a>
            </div>
            <iframe
              className="document-preview"
              src={documentContentUrl}
               title={`Stored invoice document ${document.data.filename}`}
            />
          </div>
        )}
        <div className="extraction-card">
          <span className="card-label">PROVIDER CONFIGURATION</span>
          <p className="extraction-muted">Built-in extraction is the default. Add an external provider only when the organization needs one.</p>
          <div className="extraction-fields">
            <label><span>Name</span><input value={agentName} onChange={(event) => setAgentName(event.target.value)} /></label>
            <label><span>Endpoint URL</span><input value={agentEndpoint} onChange={(event) => setAgentEndpoint(event.target.value)} placeholder="https://provider.example/extract" /></label>
            <label><span>Credential reference</span><input value={agentReference} onChange={(event) => setAgentReference(event.target.value)} placeholder="secret://invoice-ocr-token" /></label>
            <label className="extraction-toggle"><input type="checkbox" checked={agentActive} onChange={(event) => setAgentActive(event.target.checked)} /> Use this provider for invoices</label>
          </div>
          <button className="admin-form__submit" type="button" onClick={saveAgent} disabled={createAgent.isPending}>Save provider</button>
          <div className="extraction-agent-list">
            {agents.data?.map((agent) => <div key={agent.id}><div><strong>{agent.name}</strong><span>{agent.providerType} · {agent.credentialMask ?? 'No credential'}</span></div><button type="button" onClick={() => testAgent.mutate({ id: agent.id }, { onSuccess: (result) => setMessage(result.message) })}>Test</button></div>)}
          </div>
        </div>
      </div>
      {message && <p className="extraction-message" role="status">{message}</p>}
    </section>
  );
}