/**
 * PROTECTED SILA INVOICE RECEIVING FLOW — See docs/PROTECTED_INVOICE_FLOW.md.
 * Save and Continue: persist reviewed invoice, navigate only to Finalize GRN. Do not POST GRN.
 */
import type { Document, PurchaseOrder, Supplier } from '@workspace/api-client-react';
import type { BasicFields } from '../providers/scan-session';
import { getInvoiceSaveRecovery, readInvoiceSaveApiError } from './invoice-save-errors';
import { INVOICE_REVIEW_NEXT_PATH } from './invoice-receiving-contract';
import {
  invoiceReviewBlockers,
  persistedSupplierName,
  resolveCanonicalCustomerOrganization,
} from './invoice-review-state';

export const INVOICE_REVIEW_BLOCKER_MESSAGES: Record<string, string> = {
  AUTHORITATIVE_SUPPLIER_REQUIRED: 'Supplier is required',
  SUPPLIER_REQUIRED: 'Supplier is required',
  INVOICE_NUMBER_REQUIRED: 'Invoice Number is required',
  INVOICE_GROSS_REQUIRED: 'Gross Amount is required',
  PURCHASE_ORDER_REQUIRED: 'Purchase Order is required',
  INVOICE_LINES_REQUIRE_REVIEW: 'Invoice lines require review',
};

export type InvoiceSaveScopeOrganization = { id: string; code: string; name: string };
export type InvoiceSaveScopeUnit = { id: string; organizationId: string };

export type InvoiceSaveNavigation = {
  pathname: typeof INVOICE_REVIEW_NEXT_PATH;
  params: {
    invoiceId: string;
    purchaseOrderId: string;
    documentId: string;
  };
};

export type InvoiceSaveRequestFields = {
  organizationId: string;
  operatingUnitId?: string;
  sourceChannel: 'MOBILE_SCANNER';
  pageCount: number;
  scanSessionId: string;
  ocrRequestId?: string | null;
  manualEditedFieldsJson: string;
  supplierName: string | null;
  supplierId: string;
  supplierTrn: string | null;
  supplierInvoiceNumber: string | null;
  invoiceDate: string | null;
  purchaseOrderNumber: string | null;
  noPurchaseOrder: boolean;
  invoiceGross: number | null;
  currency: string | null;
  deferFullExtraction: true;
  invoiceLinesJson: string | null;
};

export type InvoiceSaveOutcome =
  | { kind: 'ignored' }
  | { kind: 'blocked'; blockers: string[]; requestSent: false }
  | { kind: 'saved'; document: Document; navigation: InvoiceSaveNavigation; requestSent: true }
  | { kind: 'error'; message: string; code?: string; requestSent: true; duplicateInvoiceId?: string | null };

export type InvoiceSaveTrace = (event: string, details?: Record<string, unknown>) => void;

export function formatInvoiceReviewBlockers(
  fields: BasicFields,
  authoritativeSupplierId: string | null,
  selectedPo: PurchaseOrder | null,
  noPurchaseOrder: boolean,
) {
  return invoiceReviewBlockers(fields, authoritativeSupplierId, selectedPo, noPurchaseOrder)
    .map((code) => INVOICE_REVIEW_BLOCKER_MESSAGES[code] ?? code);
}

export function resolveInvoiceSaveOrganization(
  organizations: InvoiceSaveScopeOrganization[],
  scoped: InvoiceSaveScopeOrganization | null | undefined,
  tenantCode: string,
  tenantName: string,
) {
  return resolveCanonicalCustomerOrganization(organizations, tenantCode, tenantName)
    ?? (scoped && resolveCanonicalCustomerOrganization([scoped], tenantCode, tenantName))
    ?? null;
}

export function resolveInvoiceSaveOperatingUnit(
  organizationId: string | undefined,
  unit: InvoiceSaveScopeUnit | null | undefined,
  units: InvoiceSaveScopeUnit[] = [],
) {
  if (!organizationId) return undefined;
  if (unit?.id && (!unit.organizationId || unit.organizationId === organizationId)) return unit.id;
  return units.find((item) => item.organizationId === organizationId)?.id
    ?? units[0]?.id;
}

export function resolveInvoiceOcrScope(
  organizations: InvoiceSaveScopeOrganization[],
  units: InvoiceSaveScopeUnit[],
  tenantCode: string,
  tenantName: string,
  scoped?: { organization?: InvoiceSaveScopeOrganization | null; unit?: InvoiceSaveScopeUnit | null },
) {
  const organization = resolveInvoiceSaveOrganization(
    organizations,
    scoped?.organization,
    tenantCode,
    tenantName,
  );
  const matchingUnit = scoped?.unit && organization && scoped.unit.organizationId === organization.id
    ? scoped.unit
    : units.find((unit) => unit.organizationId === organization?.id) ?? null;
  return {
    organization,
    organizationId: organization?.id,
    operatingUnitId: resolveInvoiceSaveOperatingUnit(organization?.id, matchingUnit) ?? null,
  };
}

export function invoiceSaveNavigation(document: Pick<Document, 'id' | 'invoiceId'>, purchaseOrderId?: string | null): InvoiceSaveNavigation {
  return {
    pathname: INVOICE_REVIEW_NEXT_PATH,
    params: {
      invoiceId: document.invoiceId,
      purchaseOrderId: purchaseOrderId ?? '',
      documentId: document.id,
    },
  };
}

export function isoInvoiceDate(value: string | null | undefined) {
  const raw = (value ?? '').trim();
  if (!raw) return null;
  if (/^\d{4}-\d{2}-\d{2}$/.test(raw)) return raw;
  const iso = raw.match(/^(\d{4})[./-](\d{1,2})[./-](\d{1,2})$/);
  if (iso) return `${iso[1]}-${iso[2].padStart(2, '0')}-${iso[3].padStart(2, '0')}`;
  const numeric = raw.match(/^(\d{1,2})[./-](\d{1,2})[./-](\d{2,4})$/);
  if (numeric) {
    const year = numeric[3].length === 2 ? `20${numeric[3]}` : numeric[3];
    return `${year}-${numeric[2].padStart(2, '0')}-${numeric[1].padStart(2, '0')}`;
  }
  const parsed = Date.parse(raw);
  if (!Number.isFinite(parsed)) return null;
  return new Date(parsed).toISOString().slice(0, 10);
}

export function buildInvoiceSaveRequest(input: {
  fields: BasicFields;
  selectedSupplier: Supplier | null;
  selectedSupplierId: string;
  selectedPo: PurchaseOrder | null;
  noPurchaseOrder: boolean;
  organizationId: string;
  operatingUnitId?: string;
  pageCount: number;
  scanSessionId: string;
  ocrRequestId?: string | null;
  manualFields: string[];
  invoiceLinesJson: string | null;
}): InvoiceSaveRequestFields {
  return {
    organizationId: input.organizationId,
    operatingUnitId: input.operatingUnitId,
    sourceChannel: 'MOBILE_SCANNER',
    pageCount: input.pageCount,
    scanSessionId: input.scanSessionId,
    ocrRequestId: input.ocrRequestId,
    manualEditedFieldsJson: JSON.stringify(input.manualFields),
    supplierName: persistedSupplierName(input.fields.supplierName, input.selectedSupplier) || null,
    supplierId: input.selectedSupplierId,
    supplierTrn: input.fields.supplierTrn || null,
    supplierInvoiceNumber: input.fields.supplierInvoiceNumber || null,
    invoiceDate: isoInvoiceDate(input.fields.invoiceDate),
    purchaseOrderNumber: input.noPurchaseOrder ? null : (input.selectedPo?.poNumber ?? (input.fields.purchaseOrderNumber || null)),
    noPurchaseOrder: input.noPurchaseOrder,
    invoiceGross: input.fields.invoiceGross ? Number(input.fields.invoiceGross) : null,
    currency: input.fields.currency || null,
    deferFullExtraction: true,
    invoiceLinesJson: input.invoiceLinesJson,
  };
}

export function invoiceReviewHeaderActions(width: number, rereading: boolean) {
  const compact = width < 720;
  return {
    compact,
    view: {
      testID: 'view-document',
      icon: 'file-text' as const,
      label: compact ? 'View' : 'VIEW DOCUMENT',
      accessibilityLabel: 'View document',
    },
    reread: {
      testID: 'reread-invoice',
      icon: 'refresh-cw' as const,
      label: rereading ? (compact ? 'Re-reading…' : 'RE-READING...') : (compact ? 'Re-read' : 'RE-READ INVOICE'),
      accessibilityLabel: rereading ? 'Re-reading invoice' : 'Re-read invoice',
    },
  };
}

export async function performInvoiceReviewSave(input: {
  saving: boolean;
  fields: BasicFields;
  selectedSupplier: Supplier | null;
  selectedSupplierId: string | null;
  selectedPo: PurchaseOrder | null;
  noPurchaseOrder: boolean;
  organization: InvoiceSaveScopeOrganization | null;
  operatingUnitId?: string;
  pdfUri?: string | null;
  pageCount: number;
  scanSessionId: string;
  ocrRequestId?: string | null;
  manualFields: string[];
  invoiceLinesJson: string | null;
  idempotencyKey: string;
  upload: (request: InvoiceSaveRequestFields & { file: Blob }, options: { headers: { 'Idempotency-Key': string } }) => Promise<Document>;
  readPdf: (uri: string) => Promise<Blob>;
  trace?: InvoiceSaveTrace;
}): Promise<InvoiceSaveOutcome> {
  if (input.saving) return { kind: 'ignored' };
  input.trace?.('SAVE_CLICKED', {
    tenant: 'five',
    environment: 'TEST',
    invoiceNumber: input.fields.supplierInvoiceNumber,
    documentId: input.scanSessionId,
    authoritativeSupplierInternalId: input.selectedSupplier?.id ?? input.selectedSupplierId,
    authoritativeSupplierId: input.selectedSupplier?.supplierCode ?? null,
    authoritativeSupplierName: input.selectedSupplier?.name ?? null,
    selectedPurchaseOrderId: input.selectedPo?.id ?? null,
    selectedPurchaseOrderNumber: input.selectedPo?.poNumber ?? null,
    ocrPurchaseOrderNumber: input.fields.purchaseOrderNumber,
    invoiceLineCount: input.invoiceLinesJson ? 1 : 0,
    isInvoiceReviewReady: formatInvoiceReviewBlockers(input.fields, input.selectedSupplierId, input.selectedPo, input.noPurchaseOrder).length === 0,
    validationErrors: formatInvoiceReviewBlockers(input.fields, input.selectedSupplierId, input.selectedPo, input.noPurchaseOrder),
  });

  const blockers = formatInvoiceReviewBlockers(input.fields, input.selectedSupplierId, input.selectedPo, input.noPurchaseOrder);
  if (blockers.length > 0) {
    return { kind: 'blocked', blockers, requestSent: false };
  }
  if (!input.organization) {
    return { kind: 'blocked', blockers: ['FIVE TEST organization context is missing. The invoice was not saved.'], requestSent: false };
  }
  if (!input.pdfUri) {
    return { kind: 'blocked', blockers: ['Create the PDF before saving.'], requestSent: false };
  }
  if (!input.selectedSupplierId) {
    return { kind: 'blocked', blockers: ['Supplier is required'], requestSent: false };
  }

  input.trace?.('SAVE_REQUEST_STARTED', { method: 'POST', path: '/api/v1/documents/invoices' });
  try {
    const blob = await input.readPdf(input.pdfUri);
    const request = {
      ...buildInvoiceSaveRequest({
        fields: input.fields,
        selectedSupplier: input.selectedSupplier,
        selectedSupplierId: input.selectedSupplierId,
        selectedPo: input.selectedPo,
        noPurchaseOrder: input.noPurchaseOrder,
        organizationId: input.organization.id,
        operatingUnitId: input.operatingUnitId,
        pageCount: input.pageCount,
        scanSessionId: input.scanSessionId,
        ocrRequestId: input.ocrRequestId,
        manualFields: input.manualFields,
        invoiceLinesJson: input.invoiceLinesJson,
      }),
      file: new Blob([blob], { type: 'application/pdf' }),
    };
    const document = await input.upload(request, { headers: { 'Idempotency-Key': input.idempotencyKey } });
    if (!document.invoiceId || document.invoiceId.startsWith('00000000')) {
      return {
        kind: 'error',
        requestSent: true,
        message: document.message || 'The invoice was saved but InvoiceId was not returned.',
      };
    }
    input.trace?.('SAVE_SUCCEEDED', {
      httpStatus: 200,
      invoiceId: document.invoiceId,
      purchaseOrderId: input.selectedPo?.id ?? null,
      navigation: '/receive/finalize-grn',
    });
    return {
      kind: 'saved',
      document,
      navigation: invoiceSaveNavigation(document, input.selectedPo?.id),
      requestSent: true,
    };
  } catch (error) {
    const recovery = getInvoiceSaveRecovery(error);
    const apiError = recovery?.error ?? readInvoiceSaveApiError(error);
    const status = error && typeof error === 'object' && 'status' in error ? Number((error as { status?: number }).status) : undefined;
    input.trace?.('SAVE_FAILED', {
      httpStatus: status ?? null,
      code: apiError?.code ?? null,
      message: apiError?.message ?? (error instanceof Error ? error.message : 'Save failed'),
    });
    if (recovery?.kind === 'duplicate') {
      return {
        kind: 'error',
        requestSent: true,
        code: apiError?.code,
        duplicateInvoiceId: apiError?.existingInvoiceId ?? null,
        message: `${apiError?.message ?? 'This supplier invoice already exists in SILA ME.'}${apiError?.duplicateType ? ` Match type: ${apiError.duplicateType}.` : ''}${apiError?.existingInvoiceId ? ` Existing invoice: ${apiError.existingInvoiceId}.` : ''} No new invoice was created.`,
      };
    }
    if (recovery?.kind === 'missing-fields') {
      return {
        kind: 'error',
        requestSent: true,
        code: apiError?.code,
        message: apiError?.missingFields?.length
          ? `Complete the required fields: ${apiError.missingFields.join(', ')}.`
          : apiError?.message ?? 'Complete the required invoice fields.',
      };
    }
    return {
      kind: 'error',
      requestSent: true,
      code: apiError?.code,
      message: apiError?.message ?? (error instanceof Error ? error.message : 'Your pages are still available. Check your connection and retry.'),
    };
  }
}

export async function readInvoicePdfBlob(pdfUri: string, fetchImpl: typeof fetch = fetch) {
  const response = await fetchImpl(pdfUri);
  if (!response.ok) throw new Error('The scanned document could not be read for save.');
  return response.blob();
}
