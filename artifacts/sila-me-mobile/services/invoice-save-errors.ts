/** PROTECTED SILA INVOICE RECEIVING FLOW — See docs/PROTECTED_INVOICE_FLOW.md. */
export type InvoiceSaveApiError = {
  code: string;
  message: string;
  missingFields?: string[] | null;
  existingInvoiceId?: string | null;
  duplicateType?: string | null;
};

export type InvoiceSaveRecovery =
  | { kind: 'missing-fields'; error: InvoiceSaveApiError; stayOnScan: true }
  | { kind: 'duplicate'; error: InvoiceSaveApiError; stayOnScan: true }
  | null;

export function readInvoiceSaveApiError(error: unknown): InvoiceSaveApiError | null {
  if (!error || typeof error !== 'object' || !('data' in error)) return null;
  const data = (error as { data?: unknown }).data;
  if (!data || typeof data !== 'object' || !('code' in data) || !('message' in data)) return null;

  const candidate = data as { code?: unknown; message?: unknown };
  return typeof candidate.code === 'string' && typeof candidate.message === 'string'
    ? data as InvoiceSaveApiError
    : null;
}

export function getInvoiceSaveRecovery(error: unknown): InvoiceSaveRecovery {
  const apiError = readInvoiceSaveApiError(error);
  if (!apiError) return null;
            if (apiError.code === 'INVOICE_REVIEW_FIELDS_REQUIRED' || apiError.code === 'SUPPLIER_REQUIRED') {
    return { kind: 'missing-fields', error: apiError, stayOnScan: true };
  }
  if (apiError.code === 'DUPLICATE_INVOICE') {
    return { kind: 'duplicate', error: apiError, stayOnScan: true };
  }
  return null;
}