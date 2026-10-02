/**
 * PROTECTED SILA INVOICE RECEIVING FLOW — See docs/PROTECTED_INVOICE_FLOW.md.
 * Supplier Master resolution and Open PO matching. Do not modify from unrelated feature work.
 */
import type { AdvancedInvoiceExtractionResponse, PurchaseOrder, Supplier } from '@workspace/api-client-react';
import type { BasicFields } from '../providers/scan-session';

export type PurchaseOrderSelectionError =
  | 'PO_SUPPLIER_MISMATCH'
  | 'PO_NOT_OPEN'
  | 'PO_NOT_GR_ELIGIBLE'
  | 'PO_SCOPE_MISMATCH';

export type AuthoritativeSupplier = {
  internalSupplierId: string;
  supplierId: string;
  supplierName: string;
  supplierLegalName?: string | null;
  trn?: string | null;
};

export type OcrSupplierCandidate = {
  raw: string;
  supplierId: string;
  supplierName: string;
};

export type InvoiceMatchLine = {
  lineNumber?: number;
  description?: string | null;
  quantity?: number | null;
  unitPrice?: number | null;
  lineAmount?: number | null;
  materialCode?: string | null;
  poItemNumber?: string | null;
  uom?: string | null;
};

export type PurchaseOrderMatch = {
  po: PurchaseOrder;
  score: number;
  band: 'exact' | 'strong' | 'partial' | 'low';
  reasons: string[];
  matchedLineCount: number;
  invoiceLineCount: number;
};

export function normalizeSupplierName(value: string) {
  return value
    .trim()
    .toUpperCase()
    .replace(/[^A-Z0-9]+/g, ' ')
    .replace(/\bL\s+L\s+C\b/g, 'LLC')
    .replace(/\s+/g, ' ')
    .trim();
}

export function normalizeDescription(value: string) {
  return value
    .trim()
    .toUpperCase()
    .replace(/[“”]/g, '"')
    .replace(/[‘’]/g, "'")
    .replace(/[^A-Z0-9]+/g, ' ')
    .replace(/\s+/g, ' ')
    .trim();
}

export function invoiceLinesFromAdvanced(advanced?: AdvancedInvoiceExtractionResponse | null): InvoiceMatchLine[] {
  return (advanced?.lines ?? []).map((line) => ({
    lineNumber: line.lineNumber,
    description: asText(line.description?.value),
    quantity: asNumber(line.quantity?.value),
    unitPrice: asNumber(line.unitPrice?.value),
    lineAmount: asNumber(line.grossAmount?.value ?? line.netAmount?.value),
    materialCode: asText(line.supplierItemCode?.value),
    poItemNumber: asText(line.poItemNumber?.value),
    uom: asText(line.uom?.value),
  })).filter((line) => line.description || line.quantity != null || line.materialCode);
}

function asText(value: unknown) {
  return value == null ? '' : String(value).trim();
}

function asNumber(value: unknown) {
  if (typeof value === 'number' && Number.isFinite(value)) return value;
  if (typeof value === 'string' && value.trim()) {
    const parsed = Number(value);
    return Number.isFinite(parsed) ? parsed : null;
  }
  return null;
}

export function isSupplierIdLabel(value: string) {
  const text = value.trim();
  return /^(?:supplier\s*)?(?:id|code)\s*[:#=.–—-]?\s*[A-Z0-9-]{4,}$/i.test(text)
    || /^(?:supplier\s*)?(?:id|code)$/i.test(text);
}

export function isSupplierNameNoise(value: string) {
  const text = String(value ?? '').trim();
  if (!text) return true;
  return /^(?:tax\s+)?invoice(?:\s+(?:date|number|no\.?|#|id))?$/i.test(text)
    || /^(date|invoice\s+date|invoice\s+number|po\s+(?:number|no\.?|date)|purchase\s+order)\b/i.test(text)
    || /^(currency|description|quantity|unit\s+price|amount|subtotal|total|tax|vat|trn|bill\s+to|ship\s+to|name)$/i.test(text)
    || /^\d{1,2}[./-]\d{1,2}[./-]\d{2,4}$/.test(text)
    || /^grand(\s+total)?\b/i.test(text);
}

export function extractOcrSupplierCandidate(ocrSupplier: string): OcrSupplierCandidate {
  const raw = String(ocrSupplier ?? '').replace(/\u00a0/g, ' ').replace(/[：﹕]/g, ':').replace(/\s+/g, ' ').trim();
  const labelled = raw.match(/(?:supplier|vendor)\s+(?:id|code)\s*[:#=.–—-]*\s*([A-Z0-9-]{4,})/i)
    ?? raw.match(/^(?:id|code)\s*[:#=.–—-]*\s*([A-Z0-9-]{4,})$/i)
    ?? raw.match(/\b(?:id|code)\s*[:#=.–—-]+\s*([A-Z0-9-]{4,})\b/i);
  const codeOnly = /^[A-Z0-9-]{4,}$/i.test(raw) && !/[A-Z]{3,}/i.test(raw.replace(/\d/g, '')) ? raw : '';
  const supplierId = (labelled?.[1] ?? codeOnly).trim();
  const remainder = labelled ? raw.replace(labelled[0], ' ').replace(/\s+/g, ' ').trim() : raw;
  const leftoverIsLabel = !remainder
    || isSupplierIdLabel(remainder)
    || /^(?:supplier|vendor)?\s*(?:id|code)\b/i.test(remainder)
    || /^(?:id|code)\s*$/i.test(remainder)
    || (supplierId && remainder.replace(/[^A-Z0-9]/gi, '').toUpperCase() === supplierId.toUpperCase());
  const supplierName = leftoverIsLabel ? '' : remainder.replace(/^(?:supplier|vendor)?\s*(?:id|code)\s*[:#=.–—-]*\s*/i, '').trim();
  return { raw, supplierId, supplierName };
}

/** UI-boundary value for OCR SUPPLIER. Must never render labelled IDs. */
export function ocrSupplierScreenValue(ocrSupplier: string | null | undefined) {
  const candidate = extractOcrSupplierCandidate(String(ocrSupplier ?? ''));
  const value = (candidate.supplierName || candidate.supplierId || '').trim();
  if (!value || isSupplierNameNoise(value)) return candidate.supplierId || '';
  if (/\bid\s*[:#]/i.test(value) || /^(?:supplier|vendor)?\s*(?:id|code)\b/i.test(value) || isSupplierIdLabel(value)) {
    return candidate.supplierId || value.replace(/^(?:supplier|vendor)?\s*(?:id|code)\s*[:#=.–—-]*\s*/i, '').trim();
  }
  return value;
}

export function shouldRenderPurchaseOrderSelector(selectedSupplierId: string | null | undefined) {
  return Boolean(selectedSupplierId);
}

export function formatOcrSupplierDisplay(ocrSupplier: string) {
  return ocrSupplierScreenValue(ocrSupplier);
}

export function openPoLookupEntityCode(entityCode?: string | null) {
  if (!entityCode) return undefined;
  const normalized = entityCode.trim();
  if (!normalized || /^(all|default)$/i.test(normalized)) return undefined;
  return normalized;
}

export function persistedSupplierName(ocrSupplier: string, master?: Supplier | null) {
  if (master?.name.trim() && !isSupplierIdLabel(master.name) && !/\bid\s*[:#]/i.test(master.name)) return master.name.trim();
  return ocrSupplierScreenValue(ocrSupplier);
}

export function toAuthoritativeSupplier(supplier: Supplier): AuthoritativeSupplier {
  return {
    internalSupplierId: supplier.id,
    supplierId: supplier.supplierCode,
    supplierName: supplier.name,
    supplierLegalName: supplier.legalName,
    trn: supplier.trn ?? supplier.taxNumber,
  };
}

function pickCanonicalSupplier(matches: Supplier[]) {
  const rank = (supplier: Supplier) => {
    const entity = (supplier.entityCode ?? '').trim().toUpperCase();
    if (entity === 'DEFAULT' || entity === 'ALL') return 0;
    return 1;
  };
  return [...matches].sort((left, right) =>
    rank(left) - rank(right) || left.id.localeCompare(right.id))[0] ?? null;
}

export function findUniqueSupplierMatch(ocrSupplierName: string, suppliers: Supplier[], ocrTrn?: string | null) {
  const candidate = extractOcrSupplierCandidate(ocrSupplierName);
  if (candidate.supplierId) {
    const idMatches = suppliers.filter((supplier) =>
      supplier.supplierCode.trim().toUpperCase() === candidate.supplierId.toUpperCase());
    if (idMatches.length === 1) return idMatches[0];
    if (idMatches.length > 1) {
      const codes = new Set(idMatches.map((supplier) => supplier.supplierCode.trim().toUpperCase()));
      return codes.size === 1 ? pickCanonicalSupplier(idMatches) : null;
    }
  }

  const trnDigits = String(ocrTrn ?? '').replace(/\D/g, '');
  if (trnDigits.length >= 8) {
    const trnMatches = suppliers.filter((supplier) =>
      String(supplier.trn ?? supplier.taxNumber ?? '').replace(/\D/g, '') === trnDigits);
    if (trnMatches.length === 1) return trnMatches[0];
  }

  const nameSources = [candidate.supplierName, candidate.raw].filter((value) => value.trim().length > 0);
  for (const source of nameSources) {
    const normalizedOcr = normalizeSupplierName(source);
    if (!normalizedOcr || isSupplierIdLabel(source) || isSupplierNameNoise(source) || /^(SUPPLIER )?(ID|CODE) /.test(normalizedOcr)) continue;
    const matches = suppliers.filter((supplier) =>
      normalizeSupplierName(supplier.name) === normalizedOcr ||
      normalizeSupplierName(supplier.legalName ?? '') === normalizedOcr ||
      normalizeSupplierName(supplier.searchName ?? '') === normalizedOcr ||
      (supplier.aliases ?? []).some((alias) => normalizeSupplierName(alias) === normalizedOcr));
    if (matches.length === 1) return matches[0];
  }
  return null;
}

export function isGrEligibleOpenPurchaseOrder(po: PurchaseOrder) {
  if (po.status === 'CLOSED' || po.status === 'CANCELLED') return false;
  return po.items.some((item) =>
    item.goodsReceiptExpected !== false &&
    !item.deletionIndicator &&
    !item.deliveryCompleted &&
    Number(item.openQuantity) > 0 &&
    item.status !== 'CLOSED' &&
    item.status !== 'CANCELLED');
}

export function normalizePurchaseOrderNumber(value: string) {
  return value
    .replace(/\s+/g, '')
    .replace(/^PO[:#-]*/i, '')
    .replace(/[^\w]/g, '')
    .trim()
    .toUpperCase();
}

export function purchaseOrderNumberOf(po: Pick<PurchaseOrder, 'poNumber'> | { poNumber?: string; PoNumber?: string }) {
  return normalizePurchaseOrderNumber(po.poNumber ?? ('PoNumber' in po ? po.PoNumber ?? '' : ''));
}

export function mergeOpenPurchaseOrders(openPos: PurchaseOrder[], extra?: PurchaseOrder | null) {
  if (!extra) return openPos;
  if (openPos.some((po) => po.id === extra.id || purchaseOrderNumberOf(po) === purchaseOrderNumberOf(extra))) {
    return openPos;
  }
  return [extra, ...openPos];
}

export function resolveCanonicalCustomerOrganization<T extends { code: string; name: string }>(
  organizations: T[],
  tenantCode: string,
  tenantName: string,
): T | null {
  const code = tenantCode.trim().toUpperCase();
  return organizations.find((item) => item.code.toUpperCase() === code)
    ?? organizations.find((item) => item.name === tenantName)
    ?? null;
}

export function validatePurchaseOrderSelection(
  po: PurchaseOrder,
  supplierId: string,
  _organizationId: string,
  _entityCode?: string | null,
): PurchaseOrderSelectionError | null {
  if (po.supplierId && supplierId && po.supplierId !== supplierId) return 'PO_SUPPLIER_MISMATCH';
  if (po.status === 'CLOSED' || po.status === 'CANCELLED') return 'PO_NOT_OPEN';
  if ((po.items?.length ?? 0) > 0 && !isGrEligibleOpenPurchaseOrder(po)) return 'PO_NOT_GR_ELIGIBLE';
  return null;
}

export function findOcrPurchaseOrder(
  ocrPurchaseOrderNumber: string,
  purchaseOrders: PurchaseOrder[],
  supplierId: string,
  _organizationId: string,
  _entityCode?: string | null,
) {
  const normalized = normalizePurchaseOrderNumber(ocrPurchaseOrderNumber);
  if (!normalized) return null;
  const numbered = purchaseOrders.filter((po) => purchaseOrderNumberOf(po) === normalized);
  if (numbered.length === 0) return null;
  const forSupplier = numbered.filter((po) =>
    !supplierId || !po.supplierId || po.supplierId === supplierId || po.erpSupplierId === supplierId);
  const pool = forSupplier.length > 0 ? forSupplier : numbered;
  return pool.find((po) =>
    po.status !== 'CLOSED' && po.status !== 'CANCELLED' &&
    ((po.items?.length ?? 0) === 0 || isGrEligibleOpenPurchaseOrder(po)))
    ?? pool.find((po) => po.status !== 'CLOSED' && po.status !== 'CANCELLED')
    ?? null;
}

function numeric(value: number | string | null | undefined) {
  if (value == null || value === '') return null;
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : null;
}

function descriptionsMatch(invoice: string, po: string) {
  const left = normalizeDescription(invoice);
  const right = normalizeDescription(po);
  if (!left || !right) return false;
  if (left === right) return true;
  return left.length >= 8 && (left.includes(right) || right.includes(left));
}

function amountsClose(left: number, right: number) {
  const tolerance = Math.max(0.05, Math.abs(right) * 0.02);
  return Math.abs(left - right) <= tolerance;
}

export function scorePurchaseOrderMatch(
  po: PurchaseOrder,
  invoiceLines: InvoiceMatchLine[],
  invoiceGross: string | number | null | undefined,
  invoiceCurrency: string | null | undefined,
): PurchaseOrderMatch {
  const eligible = po.items.filter((item) =>
    item.goodsReceiptExpected !== false &&
    !item.deletionIndicator &&
    !item.deliveryCompleted &&
    Number(item.openQuantity) > 0);
  const reasons: string[] = [];
  let score = 0;
  let matchedLineCount = 0;
  const used = new Set<string>();

  for (const line of invoiceLines) {
    let bestItem: (typeof eligible)[number] | undefined;
    let best = 0;
    let reason = '';
    for (const item of eligible) {
      if (used.has(item.id)) continue;
      let lineScore = 0;
      let lineReason = '';
      const material = (line.materialCode ?? '').trim().toUpperCase();
      if (material && item.materialCode.trim().toUpperCase() === material) {
        lineScore += 40;
        lineReason = 'Material ID';
      }
      if (line.poItemNumber && String(item.itemNumber ?? item.lineNumber) === String(line.poItemNumber).trim()) {
        lineScore += 35;
        lineReason = lineReason || 'PO item';
      }
      if (line.description && descriptionsMatch(line.description, item.description)) {
        lineScore += 25;
        lineReason = lineReason || 'Description';
      }
      const invoiceQty = numeric(line.quantity);
      const unitPrice = numeric(line.unitPrice) ?? numeric(item.unitPrice);
      const poPrice = numeric(item.unitPrice);
      if (unitPrice != null && poPrice != null && amountsClose(unitPrice, poPrice)) {
        lineScore += 20;
        lineReason = lineReason || 'Unit price';
      }
      const expectedAmount = invoiceQty != null && poPrice != null ? invoiceQty * poPrice : null;
      const invoiceAmount = numeric(line.lineAmount);
      if (expectedAmount != null && invoiceAmount != null && amountsClose(invoiceAmount, expectedAmount)) {
        lineScore += 20;
        lineReason = lineReason || 'Line amount';
      } else if (invoiceAmount != null && numeric(item.itemAmount) != null && amountsClose(invoiceAmount, numeric(item.itemAmount)!)) {
        lineScore += 8;
      }
      if (invoiceQty != null && invoiceQty > 0 && invoiceQty <= Number(item.openQuantity)) {
        lineScore += 8;
        lineReason = lineReason || 'Quantity';
      }
      if (lineScore > best) {
        best = lineScore;
        bestItem = item;
        reason = lineReason;
      }
    }
    if (bestItem && best >= 25) {
      used.add(bestItem.id);
      matchedLineCount += 1;
      score += best;
      if (reason) reasons.push(reason);
    }
  }

  const gross = numeric(invoiceGross);
  const poTotal = numeric(po.totalAmount);
  const openAmount = eligible.reduce((sum, item) => {
    const price = numeric(item.unitPrice) ?? 0;
    return sum + Number(item.openQuantity) * price;
  }, 0);
  if (gross != null && gross > 0) {
    if (poTotal != null && amountsClose(gross, poTotal)) {
      score += 10;
      reasons.push('Invoice total');
    } else if (openAmount > 0 && gross <= openAmount * 1.02) {
      score += 6;
      reasons.push('Open amount');
    }
  }
  if (invoiceCurrency && po.currency && invoiceCurrency.trim().toUpperCase() === po.currency.trim().toUpperCase()) {
    score += 5;
    reasons.push('Currency');
  }

  const invoiceLineCount = invoiceLines.length;
  const band: PurchaseOrderMatch['band'] =
    matchedLineCount > 0 && invoiceLineCount > 0 && matchedLineCount === invoiceLineCount && score >= 50
      ? 'strong'
      : matchedLineCount > 0
        ? 'partial'
        : score >= 10
          ? 'low'
          : 'low';
  return { po, score, band, reasons: Array.from(new Set(reasons)), matchedLineCount, invoiceLineCount };
}

export function rankOpenPurchaseOrders(
  purchaseOrders: PurchaseOrder[],
  supplierId: string,
  organizationId: string,
  entityCode: string | null | undefined,
  invoiceLines: InvoiceMatchLine[],
  invoiceGross: string | number | null | undefined,
  invoiceCurrency: string | null | undefined,
  ocrPurchaseOrderNumber?: string,
): PurchaseOrderMatch[] {
  const eligible = purchaseOrders.filter((po) =>
    validatePurchaseOrderSelection(po, supplierId, organizationId, entityCode) === null);
  const exact = ocrPurchaseOrderNumber
    ? findOcrPurchaseOrder(ocrPurchaseOrderNumber, purchaseOrders, supplierId, organizationId, entityCode)
    : null;
  const rankedPool = exact && !eligible.some((po) => po.id === exact.id) ? [...eligible, exact] : eligible;
  return rankedPool
    .map((po) => {
      const scored = scorePurchaseOrderMatch(po, invoiceLines, invoiceGross, invoiceCurrency);
      if (exact && po.id === exact.id) {
        return {
          ...scored,
          score: scored.score + 1000,
          band: 'exact' as const,
          reasons: ['Exact OCR PO number', ...scored.reasons],
        };
      }
      return scored;
    })
    .sort((left, right) => right.score - left.score || left.po.poNumber.localeCompare(right.po.poNumber));
}

export function autoSelectPurchaseOrder(ranked: PurchaseOrderMatch[]) {
  if (ranked.length === 0) return null;
  const [best, second] = ranked;
  if (best.band === 'exact') return best.po;
  if (best.score >= 70 && (!second || best.score - second.score >= 20)) return best.po;
  if (best.band === 'strong' && (!second || second.band !== 'strong' || best.score - second.score >= 20)) return best.po;
  if (ranked.length === 1 && best.matchedLineCount > 0 && best.score >= 40) return best.po;
  return null;
}

export function formatPurchaseOrderMatch(match: PurchaseOrderMatch) {
  if (match.band === 'exact') return 'MATCHED FROM INVOICE';
  if (match.invoiceLineCount > 0 && match.matchedLineCount > 0) {
    return `${match.matchedLineCount}/${match.invoiceLineCount} Invoice Lines Matched`;
  }
  if (match.band === 'strong') return 'Strong Match';
  if (match.band === 'partial') return 'Partial Match';
  return 'Low Match';
}

export function invoiceReviewOcrBanner(
  ocrStatus: 'idle' | 'reading' | 'extracting' | 'partial' | 'complete' | 'unavailable' | 'failed',
  basic: { supplierName: string; supplierInvoiceNumber: string; purchaseOrderNumber: string; invoiceGross: string; invoiceDate: string; currency: string },
  ocrText?: string | null,
) {
  if (ocrStatus === 'idle' || ocrStatus === 'reading' || ocrStatus === 'extracting') return null;
  const extractedCount = [
    basic.supplierName,
    basic.supplierInvoiceNumber,
    basic.purchaseOrderNumber,
    basic.invoiceGross,
    basic.invoiceDate,
    basic.currency,
  ].filter((value) => value.trim()).length;
  if (extractedCount > 0) {
    return ocrStatus === 'partial'
      ? { tone: 'warning' as const, text: "Some invoice details could not be read automatically. We've filled in what we found — please review the highlighted fields." }
      : { tone: 'ok' as const, text: 'Automatically extracted — please review.' };
  }
  if (String(ocrText ?? '').trim()) {
    return { tone: 'ok' as const, text: 'Automatically extracted — please review.' };
  }
  return {
    tone: 'error' as const,
    text: "We couldn't read this invoice automatically. Your scanned document is safe. You can try Re-read Invoice or enter the details manually.",
  };
}

export function shouldKeepAdvancedResult(status: string | undefined, nativeRawText: string) {
  const value = String(status ?? '').toUpperCase();
  if (value === 'FAILED') return false;
  if (value === 'SKIPPED') return Boolean(nativeRawText.trim());
  return true;
}

export function ocrTextFromExtraction(rawText: string | null | undefined, basic: { supplierName: string; supplierInvoiceNumber: string; purchaseOrderNumber: string; invoiceGross: string }) {
  if (String(rawText ?? '').trim()) return String(rawText);
  return [basic.supplierName, basic.supplierInvoiceNumber, basic.purchaseOrderNumber, basic.invoiceGross]
    .map((value) => value.trim())
    .filter(Boolean)
    .join('\n');
}

export function defaultPhysicalReceivedQuantity(invoiceQuantity: number | null | undefined, openQuantity: number) {
  if (invoiceQuantity == null || !Number.isFinite(Number(invoiceQuantity)) || Number(invoiceQuantity) < 0) {
    return { value: '', overDelivery: false };
  }
  const qty = Number(invoiceQuantity);
  if (qty > openQuantity) return { value: '', overDelivery: true };
  return { value: String(qty), overDelivery: false };
}

export function invoiceReviewBlockers(
  fields: BasicFields,
  authoritativeSupplierId: string | null,
  selectedPo: PurchaseOrder | null,
  noPurchaseOrder: boolean,
) {
  const missing: string[] = [];
  if (!authoritativeSupplierId) missing.push('AUTHORITATIVE_SUPPLIER_REQUIRED');
  if (!fields.supplierInvoiceNumber.trim()) missing.push('INVOICE_NUMBER_REQUIRED');
  if (!fields.invoiceGross.trim()) missing.push('INVOICE_GROSS_REQUIRED');
  if (!noPurchaseOrder && selectedPo === null) missing.push('PURCHASE_ORDER_REQUIRED');
  return missing;
}

export function isInvoiceReviewReady(
  fields: BasicFields,
  authoritativeSupplierId: string | null,
  selectedPo: PurchaseOrder | null,
  noPurchaseOrder: boolean,
) {
  return invoiceReviewBlockers(fields, authoritativeSupplierId, selectedPo, noPurchaseOrder).length === 0;
}

export function invoiceQuantityForPoItem(
  lines: Array<{ purchaseOrderItemId?: string | null; lineNumber?: number | null; poItemNumber?: string | null; quantity?: number | null }>,
  item: { id: string; lineNumber?: number | null; itemNumber?: string | null },
) {
  const byLink = lines.find((line) => line.purchaseOrderItemId === item.id);
  if (byLink?.quantity != null) return byLink.quantity;
  const itemNumber = String(item.itemNumber ?? item.lineNumber ?? '');
  const byNumber = lines.find((line) => {
    const lineNumber = String(line.lineNumber ?? '');
    const poItem = String(line.poItemNumber ?? '');
    return (itemNumber && (lineNumber === itemNumber || poItem === itemNumber));
  });
  return byNumber?.quantity ?? null;
}

export function assertForcedAdvancedReady(options?: { forceAdvanced?: boolean; pdfUri?: string; organizationId?: string }) {
  if (!options?.forceAdvanced) return;
  if (!options.pdfUri) throw new Error('REREAD_DOCUMENT_MISSING');
  if (!options.organizationId) throw new Error('BACKEND_OCR_SCOPE_UNAVAILABLE');
}

export function mergeRereadBasicFields<T extends Record<string, string>>(
  extracted: T,
  confirmed: T,
  confirmedKeys: string[],
) {
  const next = { ...extracted };
  for (const key of confirmedKeys) {
    if (key in confirmed) next[key as keyof T] = confirmed[key as keyof T];
  }
  if ('supplierName' in next) {
    Object.assign(next, { supplierName: ocrSupplierScreenValue(String((next as Record<string, string>).supplierName ?? '')) });
  }
  return next;
}
