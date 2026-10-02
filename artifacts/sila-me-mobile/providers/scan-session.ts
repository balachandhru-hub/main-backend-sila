/**
 * PROTECTED SILA INVOICE RECEIVING FLOW — See docs/PROTECTED_INVOICE_FLOW.md.
 * Scan session OCR merge types. Re-read must reuse this session, not create a new document.
 */
import type { AdvancedInvoiceExtractionResponse } from '@workspace/api-client-react';

export type ScanPage = {
  id: string;
  originalImageUri: string;
  processedImageUri: string;
  thumbnailUri?: string;
  width: number;
  height: number;
  rotation: 0 | 90 | 180 | 270;
  ocrText?: string;
  createdAt: string;
};

export type BasicFields = {
  supplierName: string;
  supplierTrn: string;
  supplierInvoiceNumber: string;
  invoiceDate: string;
  purchaseOrderNumber: string;
  invoiceGross: string;
  currency: string;
};

export type OcrStatus = 'idle' | 'reading' | 'extracting' | 'partial' | 'complete' | 'unavailable' | 'failed';

export type OcrExtractionStatus = 'SUCCESS' | 'PARTIAL' | 'FAILED';

export type OcrDiagnostics = {
  requestId: string;
  mobileStatus?: OcrExtractionStatus;
  mobileFields?: string[];
  fallbackDecision?: string;
  fallbackTrigger?: string;
  backendStatus?: string;
  backendProvider?: string;
  backendConfidence?: number | null;
  backendFieldCount?: number;
  backendLineCount?: number;
  mergeStatus?: string;
  finalStatus?: OcrExtractionStatus;
  finalMissingFields?: string[];
  effectiveConfiguration?: {
    automaticBackendFallbackEnabled?: boolean;
    minimumMobileConfidence?: number;
    version?: number;
  };
};

export type OcrResult = {
  pageTexts: Array<{ id: string; text: string }>;
  rawText: string;
  basic: BasicFields;
  available: boolean;
  status: OcrExtractionStatus;
  error?: string;
  advanced?: AdvancedInvoiceExtractionResponse;
  diagnostics?: OcrDiagnostics;
};

export type InvoiceOcrLine = {
  lineNumber: number;
  description: string;
  quantity?: number | null;
  uom?: string | null;
  unitPrice?: number | null;
  lineAmount?: number | null;
  netAmount?: number | null;
  taxAmount?: number | null;
  materialCode?: string | null;
  poItemNumber?: string | null;
};

export type ScanSession = {
  id: string;
  ocrRequestId?: string;
  pages: ScanPage[];
  pdfUri?: string;
  pdfSize?: number;
  pdfDirty: boolean;
  basic: BasicFields;
  basicExtraction: BasicFields;
  manualFields: string[];
  ocrLines: InvoiceOcrLine[];
  savedInvoiceId?: string;
  saving: boolean;
  ocrStatus: OcrStatus;
  ocrPage?: number;
  ocrText?: string;
  ocrAvailable?: boolean;
  ocrError?: string;
  ocrDiagnostics?: OcrDiagnostics;
};

export const emptyBasic: BasicFields = {
  supplierName: '',
  supplierTrn: '',
  supplierInvoiceNumber: '',
  invoiceDate: '',
  purchaseOrderNumber: '',
  invoiceGross: '',
  currency: '',
};

export function evaluateBasicSufficiency(basic: BasicFields, required?: {
  requirePurchaseOrderNumber?: boolean;
  requireInvoiceDate?: boolean;
  requireCurrency?: boolean;
  requireSupplierTrn?: boolean;
}) {
  const requiredKeys: Array<keyof BasicFields> = ['supplierName', 'supplierInvoiceNumber'];
  if (required?.requirePurchaseOrderNumber ?? true) requiredKeys.push('purchaseOrderNumber');
  requiredKeys.push('invoiceGross');
  if (required?.requireInvoiceDate) requiredKeys.push('invoiceDate');
  if (required?.requireCurrency) requiredKeys.push('currency');
  if (required?.requireSupplierTrn) requiredKeys.push('supplierTrn');
  const missingFields = requiredKeys.filter(key => !basic[key].trim());
  return {
    status: missingFields.length === 0 ? 'SUCCESS' as const : 'PARTIAL' as const,
    missingFields,
  };
}

export function createScanSession(): ScanSession {
  return {
    id: `scan-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`,
    ocrRequestId: createOcrRequestId(),
    pages: [],
    pdfDirty: true,
    basic: { ...emptyBasic },
    basicExtraction: { ...emptyBasic },
    manualFields: [],
    ocrLines: [],
    saving: false,
    ocrStatus: 'idle',
  };
}

export function createOcrRequestId() {
  return `ocr-${Date.now()}-${Math.random().toString(36).slice(2, 10)}`;
}

export function normalizePage(page: Partial<ScanPage> & { uri?: string }): ScanPage {
  const processedImageUri = page.processedImageUri ?? page.uri ?? page.originalImageUri ?? '';
  return {
    id: page.id ?? `page-${Date.now()}-${Math.random().toString(36).slice(2, 7)}`,
    originalImageUri: page.originalImageUri ?? page.uri ?? processedImageUri,
    processedImageUri,
    thumbnailUri: page.thumbnailUri ?? processedImageUri,
    width: page.width ?? 0,
    height: page.height ?? 0,
    rotation: page.rotation ?? 0,
    ocrText: page.ocrText,
    createdAt: page.createdAt ?? new Date().toISOString(),
  };
}

function stripOcrSupplierIdLabel(value: string) {
  const text = String(value ?? '').replace(/\u00a0/g, ' ').replace(/[：﹕]/g, ':').replace(/\s+/g, ' ').trim();
  const labelled = text.match(/(?:supplier|vendor)\s+(?:id|code)\s*[:#=.–—-]*\s*([A-Z0-9-]{4,})/i)
    ?? text.match(/^(?:id|code)\s*[:#=.–—-]*\s*([A-Z0-9-]{4,})$/i)
    ?? text.match(/\b(?:id|code)\s*[:#=.–—-]+\s*([A-Z0-9-]{4,})\b/i);
  if (labelled?.[1]) return labelled[1];
  return text.replace(/^(?:supplier|vendor)?\s*(?:id|code)\s*[:#=.–—-]+\s*/i, '').trim();
}

export function normalizeSession(
  value: Omit<Partial<ScanSession>, 'pages'> & {
    pages?: Array<Partial<ScanPage> & { uri?: string }>;
  },
): ScanSession {
  const basic = { ...emptyBasic, ...(value.basicExtraction ?? value.basic ?? {}) };
  basic.supplierName = stripOcrSupplierIdLabel(basic.supplierName);
  return {
    ...createScanSession(),
    ...value,
    pages: (value.pages ?? []).map(normalizePage),
    pdfDirty: value.pdfDirty ?? !value.pdfUri,
    basic,
    basicExtraction: basic,
    ocrLines: value.ocrLines ?? [],
    saving: Boolean(value.saving),
    ocrStatus: value.ocrStatus ?? 'idle',
    ocrRequestId: value.ocrRequestId ?? createOcrRequestId(),
  };
}

function invalidatePdf(session: ScanSession, pages: ScanPage[]): ScanSession {
  return {
    ...session,
    pages: pages.map(page => ({ ...page, ocrText: undefined })),
    pdfUri: undefined,
    pdfSize: undefined,
    pdfDirty: true,
    basic: { ...emptyBasic },
    basicExtraction: { ...emptyBasic },
    manualFields: [],
    ocrLines: [],
    ocrText: undefined,
    ocrAvailable: undefined,
    ocrError: undefined,
    ocrPage: undefined,
    ocrStatus: 'idle',
  };
}

export function addPage(session: ScanSession, page: ScanPage): ScanSession {
  return invalidatePdf(session, [...session.pages, page]);
}

export function replacePage(session: ScanSession, id: string, page: ScanPage): ScanSession {
  return invalidatePdf(session, session.pages.map(item => item.id === id ? page : item));
}

export function removePage(session: ScanSession, id: string): ScanSession {
  return invalidatePdf(session, session.pages.filter(item => item.id !== id));
}

export function movePage(session: ScanSession, id: string, direction: -1 | 1): ScanSession {
  const index = session.pages.findIndex(item => item.id === id);
  const next = index + direction;
  if (index < 0 || next < 0 || next >= session.pages.length) return session;
  const pages = [...session.pages];
  [pages[index], pages[next]] = [pages[next], pages[index]];
  return invalidatePdf(session, pages);
}

export function setPdf(session: ScanSession, pdfUri: string, pdfSize?: number): ScanSession {
  return { ...session, pdfUri, pdfSize, pdfDirty: false };
}

/**
 * A saved invoice is complete. The next scan must start from a clean
 * session: pages, OCR fields, and manual-edit flags from the invoice that
 * was just saved must not leak into the next scan (which would otherwise
 * merge stale supplier/invoice/PO data with the new document's OCR result).
 */
export function completeSession(savedInvoiceId: string): ScanSession {
  return { ...createScanSession(), savedInvoiceId };
}
