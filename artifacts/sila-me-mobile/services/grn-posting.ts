import { customFetch, type GoodsReceipt, type GrnLine } from '@workspace/api-client-react';

const request = { credentials: 'include' as const };

/** Downstream of Invoice Review. Starts from saved InvoiceId / GoodsReceiptId. See docs/PROTECTED_INVOICE_FLOW.md. */

export type FinalizeGoodsReceipt = GoodsReceipt & {
  companyCode?: string | null;
  externalReference?: string | null;
  asnReference?: string | null;
  externalSystem?: string | null;
};

export async function prepareGrn(input: { invoiceId: string; purchaseOrderId?: string }) {
  return customFetch<FinalizeGoodsReceipt>('/api/v1/grns/prepare', {
    ...request,
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
    responseType: 'json',
  });
}

export async function postGoodsReceipt(
  goodsReceiptId: string,
  lines: Array<{ purchaseOrderItemId: string; receivedQuantity: number; acceptedQuantity: number; damagedQuantity: number; rejectedQuantity: number }>,
) {
  return customFetch<FinalizeGoodsReceipt>(`/api/v1/goods-receipts/${goodsReceiptId}/post`, {
    ...request,
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ lines }),
    responseType: 'json',
  });
}

export function lineNumberLabel(line: GrnLine) {
  return line.purchaseOrderLineNumber;
}

export function erpRequestPayloadText(erpResponseJson?: string | null) {
  if (!erpResponseJson?.trim()) return undefined;
  try {
    const parsed = JSON.parse(erpResponseJson) as {
      url?: unknown;
      Url?: unknown;
      soapAction?: unknown;
      SoapAction?: unknown;
      requestXml?: unknown;
      RequestXml?: unknown;
      requestBody?: unknown;
      RequestBody?: unknown;
    };
    if (parsed && typeof parsed === 'object') {
      const requestXml = [parsed.requestXml, parsed.RequestXml, parsed.requestBody, parsed.RequestBody]
        .find((value): value is string => typeof value === 'string' && value.trim().length > 0);
      if (requestXml) {
        const url = typeof parsed.url === 'string' ? parsed.url : typeof parsed.Url === 'string' ? parsed.Url : null;
        const soapAction = typeof parsed.soapAction === 'string' ? parsed.soapAction : typeof parsed.SoapAction === 'string' ? parsed.SoapAction : null;
        return [url ? `URL: ${url}` : null, soapAction ? `SOAPAction: ${soapAction}` : null, requestXml]
          .filter(Boolean)
          .join('\n\n');
      }
    }
    return JSON.stringify(parsed, null, 2);
  } catch {
    return erpResponseJson;
  }
}
