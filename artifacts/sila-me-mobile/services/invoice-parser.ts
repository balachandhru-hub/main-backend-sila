/**
 * PROTECTED SILA INVOICE RECEIVING FLOW — See docs/PROTECTED_INVOICE_FLOW.md.
 * Mobile OCR candidate scoring for header fields.
 */
import type { BasicFields } from '@/providers/scan-session';
import { formatOcrSupplierDisplay } from './invoice-review-state';

const CURRENCY_CODES = ['AED', 'USD', 'EUR', 'GBP', 'SAR', 'QAR', 'OMR', 'BHD', 'KWD', 'MYR', 'INR'];
const BUSINESS_NAME_PATTERN = /\b(llc|l\.l\.c|trading|general trading|company|corp|corporation|industries|enterprise|limited|ltd|gmbh|fze|fzc)\b/i;
const BUYER_LINE_PATTERN = /^(bill\s+to|ship\s+to|sold\s+to|customer|buyer|delivery\s+to|deliver\s+to)\b/i;
const REJECT_SUPPLIER_PATTERN = /\b(bill\s+to|ship\s+to|customer|buyer|deliver\s+to|tax\s+invoice|purchase\s+order|invoice\s+(?:date|number|no\.?)|credit\s+note|supplier\s+(?:id|code))\b/i;
const SUPPLIER_ID_LABEL = /^(?:supplier\s*)?(?:id|code)\s*[:#=-]?\s*([A-Z0-9-]{4,})$/i;
const WEAK_TOTAL_PATTERN = /\b(subtotal|sub-total|vat\b|tax\b|discount|unit\s+price|line\s+total|quantity|previous\s+balance)\b/i;
const STRONG_TOTAL_PATTERN = /\b(grand\s+total|invoice\s+total|total\s+amount|amount\s+due|total\s+due|net\s+payable|total\s+payable|total\s+incl(?:uding)?\s+vat|invoice\s+amount|gross\s+amount)\b/i;

type Scored = { value: string; score: number; page: number };

function normaliseText(text: string) {
  return text.normalize('NFKC').replace(/\u00a0/g, ' ').replace(/[ \t]+/g, ' ');
}

function cleanValue(value: string) {
  return value
    .replace(/^[\s:#=\-–—]+/, '')
    .replace(/\s+/g, ' ')
    .trim();
}

function linesWithPages(text: string) {
  let page = 1;
  return normaliseText(text).split(/\r?\n/).map(line => {
    const value = line.trim();
    const marker = value.match(/^---\s*PAGE\s+(\d+)\s*---$/i);
    if (marker) page = Number(marker[1]);
    return { value, page };
  }).filter(line => line.value && !/^---\s*PAGE\s+\d+\s*---$/i.test(line.value));
}

function best(candidates: Scored[]) {
  return candidates.sort((a, b) => b.score - a.score)[0]?.value ?? '';
}

function extractAfterLabel(lines: Array<{ value: string; page: number }>, labels: RegExp[], extras?: (line: { value: string; page: number }, value: string) => number) {
  const candidates: Scored[] = [];
  lines.forEach((line, index) => {
    const label = labels.find(pattern => pattern.test(line.value));
    if (!label) return;
    const match = line.value.match(label);
    const sameLine = match ? cleanValue(line.value.slice((match.index ?? 0) + match[0].length)) : '';
    const nextLine = cleanValue(lines[index + 1]?.value ?? '');
    const value = sameLine || nextLine;
    if (!value || BUYER_LINE_PATTERN.test(value)) return;
    const score = (line.page === 1 ? 20 : 0) + (sameLine ? 10 : 0) + (extras?.(line, value) ?? 0);
    candidates.push({ value, page: line.page, score });
  });
  return best(candidates);
}

function normaliseDate(value: string) {
  const iso = value.match(/\b(\d{4})[./-](\d{1,2})[./-](\d{1,2})\b/);
  if (iso) {
    return `${iso[1]}-${iso[2].padStart(2, '0')}-${iso[3].padStart(2, '0')}`;
  }
  const numeric = value.match(/\b(\d{1,2})[./-](\d{1,2})[./-](\d{2,4})\b/);
  if (numeric) {
    const year = numeric[3].length === 2 ? `20${numeric[3]}` : numeric[3];
    return `${year}-${numeric[2].padStart(2, '0')}-${numeric[1].padStart(2, '0')}`;
  }
  const named = value.match(/\b(\d{1,2})[\s-]+(Jan(?:uary)?|Feb(?:ruary)?|Mar(?:ch)?|Apr(?:il)?|May|Jun(?:e)?|Jul(?:y)?|Aug(?:ust)?|Sep(?:t(?:ember)?)?|Oct(?:ober)?|Nov(?:ember)?|Dec(?:ember)?)[,\s-]+(\d{4})\b/i);
  if (!named) return '';
  const months = ['jan', 'feb', 'mar', 'apr', 'may', 'jun', 'jul', 'aug', 'sep', 'oct', 'nov', 'dec'];
  const month = months.findIndex(item => named[2].toLowerCase().startsWith(item)) + 1;
  return `${named[3]}-${String(month).padStart(2, '0')}-${named[1].padStart(2, '0')}`;
}

function numericValue(value: string) {
  const raw = value.match(/\d[\d.,\s]*\d|\d/g)?.at(-1)?.replace(/\s/g, '') ?? '';
  if (!raw) return '';

  const lastComma = raw.lastIndexOf(',');
  const lastDot = raw.lastIndexOf('.');
  const decimalSeparator = lastComma > lastDot ? ',' : '.';
  const decimalIndex = raw.lastIndexOf(decimalSeparator);
  const hasDecimal = decimalIndex >= 0 && raw.length - decimalIndex - 1 > 0 && raw.length - decimalIndex - 1 <= 2;

  if (!hasDecimal) return raw.replace(/[.,]/g, '');
  return `${raw.slice(0, decimalIndex).replace(/[.,]/g, '')}.${raw.slice(decimalIndex + 1)}`;
}

function extractTrn(lines: Array<{ value: string; page: number }>) {
  const candidates: Array<{ value: string; score: number }> = [];
  lines.forEach((line, index) => {
    if (!/(?:\btrn\b|tax\s+registration|vat\s+registration|vat\s+reg|tax\s+reg)/i.test(line.value)) return;
    const nearby = `${line.value} ${lines[index + 1]?.value ?? ''}`.replace(/[Oo]/g, '0').replace(/[Il]/g, '1');
    const match = nearby.match(/\b\d[\d\s-]{13,}\d\b/);
    if (match) candidates.push({ value: match[0].replace(/\D/g, ''), score: line.page === 1 ? 20 : 0 });
  });
  return candidates.sort((a, b) => b.score - a.score)[0]?.value ?? '';
}

function extractSupplierCode(lines: Array<{ value: string; page: number }>) {
  for (const line of lines) {
    const labelled = line.value.match(/(?:supplier\s*)?(?:id|code)\s*[:#=-]?\s*([A-Z0-9-]{4,})/i)
      ?? line.value.match(/^(?:id|code)\s*[:#=-]?\s*([A-Z0-9-]{4,})$/i);
    if (labelled?.[1]) return labelled[1];
  }
  return '';
}

function isSupplierNameNoise(value: string) {
  const text = value.trim();
  if (!text) return true;
  return /^(?:tax\s+)?invoice(?:\s+(?:date|number|no\.?|#|id))?$/i.test(text)
    || /^(date|invoice\s+date|invoice\s+number|po\s+(?:number|no\.?|date)|purchase\s+order)\b/i.test(text)
    || /^(currency|description|quantity|unit\s+price|amount|subtotal|total|tax|vat|trn|bill\s+to|ship\s+to|name)$/i.test(text)
    || /^\d{1,2}[./-]\d{1,2}[./-]\d{2,4}$/.test(text)
    || /^grand(\s+total)?\b/i.test(text)
    || REJECT_SUPPLIER_PATTERN.test(text)
    || BUYER_LINE_PATTERN.test(text)
    || SUPPLIER_ID_LABEL.test(text)
    || /^(name|address|email|phone|number|no\.?|#)$/i.test(text);
}

function extractSupplier(lines: Array<{ value: string; page: number }>) {
  for (let index = 0; index < lines.length; index += 1) {
    const match = lines[index].value.match(/^\s*(?:supplier\s+name|vendor\s+name|issued\s+by|sold\s+by|from)\s*[:\-]\s*(.*)$/i)
      ?? lines[index].value.match(/^\s*(?:supplier|vendor)\s*[:\-]?\s*(.*)$/i);
    if (!match) continue;
    for (let offset = 0; offset <= 4; offset += 1) {
      const candidate = cleanValue(offset === 0 ? match[1] : (lines[index + offset]?.value ?? ''));
      if (!candidate || isSupplierNameNoise(candidate) || BUYER_LINE_PATTERN.test(candidate) || SUPPLIER_ID_LABEL.test(candidate)) continue;
      if (!BUSINESS_NAME_PATTERN.test(candidate) && (candidate.match(/[A-Za-z]/g) ?? []).length < 3) continue;
      return candidate;
    }
  }
  const pageOne = lines.filter(line => line.page === 1);
  const top = pageOne.slice(0, Math.max(8, Math.ceil(pageOne.length * 0.35)));
  const named = top
    .map(line => line.value)
    .find(line => BUSINESS_NAME_PATTERN.test(line) && !isSupplierNameNoise(line) && !BUYER_LINE_PATTERN.test(line) && !SUPPLIER_ID_LABEL.test(line) && !/\d{8,}/.test(line));
  if (named) return named;
  return extractSupplierCode(lines);
}

function extractCurrency(text: string, gross: string) {
  const source = `${gross} ${text}`;
  const code = source.match(new RegExp(`\\b(${CURRENCY_CODES.join('|')})\\b`, 'i'))?.[1];
  if (code) return code.toUpperCase();
  if (source.includes('د.إ')) return 'AED';
  return '';
}

function extractGross(lines: Array<{ value: string; page: number }>) {
  const labelled = extractAfterLabel(lines, [
    /grand\s+total\b/i,
    /total\s+(?:including|incl(?:uding)?)\s+vat\b/i,
    /total\s+payable\b/i,
    /net\s+payable\b/i,
    /invoice\s+total\b/i,
    /invoice\s+amount\b/i,
    /total\s+amount\b/i,
    /gross\s+amount\b/i,
    /amount\s+due\b/i,
    /total\s+due\b/i,
    /^\s*total\b(?!\s*(?:qty|quantity|units))/i,
  ], (line, value) => (STRONG_TOTAL_PATTERN.test(line.value) || /^\s*total\b/i.test(line.value) ? 25 : 0) + (WEAK_TOTAL_PATTERN.test(line.value) || WEAK_TOTAL_PATTERN.test(value) ? -40 : 0));
  if (labelled && !WEAK_TOTAL_PATTERN.test(labelled)) return labelled;
  const scored: Scored[] = [];
  lines.forEach(line => {
    if (!STRONG_TOTAL_PATTERN.test(line.value) || WEAK_TOTAL_PATTERN.test(line.value)) return;
    const amount = numericValue(line.value);
    if (amount) scored.push({ value: line.value, page: line.page, score: 30 + (line.page > 1 ? 5 : 0) });
  });
  return best(scored);
}

function looksLikeInvoiceNumber(value: string) {
  const cleaned = cleanValue(value).replace(/\s+/g, '');
  if (cleaned.length < 3 || cleaned.length > 48) return false;
  if (normaliseDate(value)) return false;
  const digits = cleaned.replace(/\D/g, '');
  if (digits.length === 15 && !/[A-Za-z]/.test(cleaned)) return false;
  if (/^(PO|P\.O|TRN|VAT|GRN|DATE|TOTAL|GROSS)/i.test(cleaned)) return false;
  if (/\b(po number|purchase order|trn|vat|gross|total|date)\b/i.test(value) && !/\binv/i.test(value)) return false;
  return /\d/.test(cleaned) && /^[A-Z0-9][A-Z0-9._/\-]{2,}$/i.test(cleaned);
}

function extractInvoiceNumber(lines: Array<{ value: string; page: number }>) {
  const labels = [
    /supplier\s+invoice\s*(?:number|no\.?|#)/i,
    /tax\s+invoice\s*(?:number|no\.?|#)/i,
    /(?:tax\s+)?invoice\s*(?:number|no\.?|ref(?:erence)?|#)/i,
    /\binvoice\s*#/i,
    /\binv(?:oice)?\s*(?:number|no\.?|#)/i,
    /document\s*(?:number|no\.?|#)/i,
    /invoice\s+id\b/i,
  ];
  const candidates: Scored[] = [];
  lines.forEach((line, index) => {
    const label = labels.find(pattern => pattern.test(line.value));
    if (!label) return;
    const match = line.value.match(label);
    const sameLine = match ? cleanValue(line.value.slice((match.index ?? 0) + match[0].length)) : '';
    const nextLine = cleanValue(lines[index + 1]?.value ?? '');
    [sameLine, nextLine].forEach((raw, offset) => {
      if (!raw || !looksLikeInvoiceNumber(raw)) return;
      const cleaned = raw.replace(/\s+/g, '').replace(/^[:#=\-–—]+/, '');
      if (!looksLikeInvoiceNumber(cleaned)) return;
      candidates.push({
        value: cleaned,
        page: line.page,
        score: (line.page === 1 ? 24 : 0)
          + (offset === 0 ? 14 : 4)
          + (/inv/i.test(cleaned) ? 12 : 0)
          + (/test-inv/i.test(cleaned) ? 6 : 0)
          + (/\d/.test(cleaned) ? 6 : 0)
          + (/supplier\s+invoice|tax\s+invoice/i.test(line.value) ? 8 : 0),
      });
    });
  });
  return best(candidates);
}

function extractHeadingInvoiceNumber(lines: Array<{ value: string; page: number }>) {
  const candidates: Scored[] = [];
  lines.forEach((line, index) => {
    if (/^(?:tax\s+)?invoice\s*$/i.test(line.value)) {
      const next = cleanValue(lines[index + 1]?.value ?? '').replace(/\s+/g, '');
      if (looksLikeInvoiceNumber(next) && /[A-Z]/i.test(next) && !/^45\d{8}$/.test(next)) {
        candidates.push({ value: next, page: line.page, score: 30 });
      }
    }
    const token = cleanValue(line.value).replace(/\s+/g, '');
    if (
      looksLikeInvoiceNumber(token)
      && /[A-Z]/i.test(token)
      && !/^45\d{8}$/.test(token)
      && !/\b(invoice\s+date|po\s+number|purchase\s+order|supplier|bill\s+to|currency)\b/i.test(line.value)
    ) {
      candidates.push({ value: token, page: line.page, score: (line.page === 1 ? 12 : 0) + (/invoice/i.test(token) ? 10 : 0) });
    }
  });
  return best(candidates);
}

export function parseMobileBasicFields(text: string): BasicFields {
  const lines = linesWithPages(text);
  const invoiceNumber = extractInvoiceNumber(lines) || extractHeadingInvoiceNumber(lines);
  const date = extractAfterLabel(lines, [/tax\s+invoice\s+date\b/i, /invoice\s+date\b/i, /^\s*date\s*[:=\-]/i]);
  const po = extractAfterLabel(lines, [
    /purchase\s+order(?:\s+(?:number|no\.?))?\b/i,
    /purchase\s+ref(?:erence)?\b/i,
    /order\s+reference\b/i,
    /\b(?:your|customer|buyer)\s+po\b/i,
    /^\s*p\.?o\.?(?:\s*(?:number|no\.?))?\s*[:\-]/i,
    /\bpo(?:\s*(?:number|no\.?|#))?\b/i,
  ], (_line, value) => (/\b(invoice|trn|vat)\b/i.test(value) ? -25 : 10));
  const grossLabel = extractGross(lines);
  const gross = numericValue(grossLabel);

  return {
    supplierName: formatOcrSupplierDisplay(extractSupplier(lines)),
    supplierTrn: extractTrn(lines),
    supplierInvoiceNumber: invoiceNumber,
    invoiceDate: normaliseDate(date),
    purchaseOrderNumber: po,
    invoiceGross: gross,
    currency: extractCurrency(text, grossLabel),
  };
}

export function parseMobileAmountExtras(text: string) {
  const lines = linesWithPages(text);
  const net = numericValue(extractAfterLabel(lines, [
    /\bsub[- ]?total\b/i,
    /\bnet\s+amount\b/i,
    /\bnet\s+total\b/i,
  ], (line, value) => (WEAK_TOTAL_PATTERN.test(line.value) ? 8 : 0) + (STRONG_TOTAL_PATTERN.test(value) ? -20 : 0)));
  const tax = numericValue(extractAfterLabel(lines, [
    /\bvat\s+amount\b/i,
    /\btax\s+amount\b/i,
    /\btotal\s+vat\b/i,
    /^\s*vat\s*[:\-]/i,
    /^\s*tax\s*[:\-]/i,
  ], (line) => (STRONG_TOTAL_PATTERN.test(line.value) ? -25 : 5)));
  return { netAmount: net, taxAmount: tax };
}
