export function formatDate(value?: string | null) {
  if (!value) return 'Open'
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return 'Open'
  return date.toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' })
}

export function formatMoney(amount?: number | null, currency?: string | null) {
  if (amount == null || Number.isNaN(amount)) return '—'
  const code = currency && currency.length === 3 ? currency : undefined
  try {
    return new Intl.NumberFormat(undefined, {
      style: code ? 'currency' : 'decimal',
      currency: code,
      maximumFractionDigits: 2,
    }).format(amount)
  } catch {
    return `${amount}${currency ? ` ${currency}` : ''}`
  }
}

export function titleCase(value?: string | null) {
  if (!value) return '—'
  return value
    .replace(/[_-]+/g, ' ')
    .toLowerCase()
    .replace(/\b\w/g, (letter) => letter.toUpperCase())
}
