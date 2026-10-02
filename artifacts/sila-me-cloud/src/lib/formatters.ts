export function formatDate(value?: string | null) {
  if (!value) return '—';
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return value;
  return new Intl.DateTimeFormat('en-GB', { day: '2-digit', month: 'short', year: 'numeric' }).format(date);
}

export function formatDateTime(value?: string | null) {
  if (!value) return '—';
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return value;
  return new Intl.DateTimeFormat('en-GB', { day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' }).format(date);
}

export function formatMoney(value?: number | null, currency = 'AED') {
  if (value === null || value === undefined || Number.isNaN(value)) return '—';
  return new Intl.NumberFormat('en-AE', { style: 'currency', currency, maximumFractionDigits: 2 }).format(value);
}

export function formatCompactMoney(value?: number | null, currency = 'AED') {
  if (value === null || value === undefined || Number.isNaN(value)) return 'Not available';
  const abs = Math.abs(value);
  const body = abs >= 1_000_000_000
    ? `${(value / 1_000_000_000).toFixed(1).replace(/\.0$/, '')}B`
    : abs >= 1_000_000
      ? `${(value / 1_000_000).toFixed(1).replace(/\.0$/, '')}M`
      : abs >= 10_000
        ? `${(value / 1_000).toFixed(1).replace(/\.0$/, '')}K`
        : new Intl.NumberFormat('en-AE', { maximumFractionDigits: 0 }).format(value);
  return `${currency} ${body}`;
}

export function formatNumber(value?: number | null, maximumFractionDigits = 2) {
  if (value === null || value === undefined || Number.isNaN(value)) return '—';
  return new Intl.NumberFormat('en-AE', { maximumFractionDigits }).format(value);
}

export function humanizeStatus(value?: string | null) {
  if (!value) return 'Unknown';
  return value.toLowerCase().split('_').map((word) => word.charAt(0).toUpperCase() + word.slice(1)).join(' ');
}

export function statusTone(value?: string | null) {
  const status = value?.toUpperCase() ?? '';
  if (['POSTED', 'CLOSED', 'GRN_POSTED', 'ACTIVE', 'FULL_EXTRACTION_COMPLETE', 'PROCESSED'].includes(status)) return 'good';
  if (['FAILED', 'CANCELLED', 'OCR_FAILED', 'INACTIVE'].includes(status)) return 'bad';
  if (['READY_FOR_GRN', 'READY_TO_POST', 'REVIEW_REQUIRED', 'PARTIALLY_RECEIVED', 'PROCESSING', 'READING'].includes(status)) return 'warn';
  return 'blue';
}