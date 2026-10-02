/**
 * PROTECTED SILA INVOICE RECEIVING FLOW
 * See docs/PROTECTED_INVOICE_FLOW.md.
 * Do not modify this contract/behavior from unrelated feature work.
 *
 * Frozen Mobile Invoice Review field set. Internal OCR may change;
 * these DTO names and meanings must not be renamed or dropped arbitrarily.
 */
export type {
  AdvancedInvoiceExtractionResponse,
  AdvancedInvoiceHeader,
  AdvancedInvoiceLine,
  AdvancedInvoiceValidation,
  BasicOcrResponse,
  Document,
  Invoice,
  InvoiceLine,
  PurchaseOrder,
  Supplier,
} from '@workspace/api-client-react';

/** Save and Continue must navigate here only. It must not POST GRN. */
export const INVOICE_REVIEW_NEXT_PATH = '/receive/finalize-grn' as const;

export const INVOICE_REVIEW_CONTRACT_FIELDS = {
  document: [
    'id',
    'invoiceId',
    'filename',
    'contentType',
    'fileSizeBytes',
    'pageCount',
    'sourceChannel',
    'status',
    'createdAt',
    'saveStatus',
    'nextStep',
    'message',
  ],
  supplier: ['id', 'supplierCode', 'name', 'legalName', 'taxNumber', 'aliases'],
  invoice: [
    'id',
    'documentId',
    'invoiceNumber',
    'invoiceDate',
    'supplierId',
    'supplierCode',
    'supplierName',
    'supplierTaxNumber',
    'purchaseOrderNumber',
    'purchaseOrderId',
    'currency',
    'netAmount',
    'taxAmount',
    'grossAmount',
    'goodsReceiptId',
    'lines',
  ],
  invoiceLine: [
    'lineNumber',
    'supplierMaterialCode',
    'description',
    'quantity',
    'uom',
    'unitPrice',
    'taxRate',
    'taxAmount',
    'lineAmount',
    'purchaseOrderItemId',
    'matchStatus',
  ],
  matching: ['supplierMatched', 'purchaseOrderMatched'],
} as const;
