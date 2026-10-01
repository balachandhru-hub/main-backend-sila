const API_KEY = import.meta.env.VITE_API_KEY

export class ApiError extends Error {
  status: number
  constructor(message: string, status: number) {
    super(message)
    this.status = status
  }
}

let refreshInflight: Promise<boolean> | null = null

function refreshSession(): Promise<boolean> {
  if (!refreshInflight) {
    refreshInflight = fetch('/api/v1/identity/auth/refresh-token', {
      method: 'POST',
      credentials: 'include',
    })
      .then((response) => response.ok)
      .finally(() => {
        refreshInflight = null
      })
  }
  return refreshInflight
}

function messageFrom(body: unknown, fallback: string): string {
  if (body && typeof body === 'object') {
    const record = body as { message?: string; description?: string }
    return record.message || record.description || fallback
  }
  return fallback
}

export async function api<T>(
  path: string,
  init: RequestInit = {},
  options?: { apiKey?: boolean; retry?: boolean },
): Promise<T> {
  const headers = new Headers(init.headers)
  if (init.body && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json')
  }
  if (options?.apiKey !== false && API_KEY) {
    headers.set('X-API-Key', API_KEY)
  }

  const response = await fetch(path, { ...init, headers, credentials: 'include' })
  if (response.status === 204) return null as T

  const text = await response.text()
  let body: unknown = null
  if (text) {
    try {
      body = JSON.parse(text)
    } catch {
      body = { message: text }
    }
  }

  const isAuthCall = path.includes('/api/v1/identity/auth/')
  if (response.status === 401 && !isAuthCall && options?.retry !== false) {
    const refreshed = await refreshSession()
    if (refreshed) return api<T>(path, init, { ...options, retry: false })
    window.dispatchEvent(new CustomEvent('session:expired'))
  }

  if (!response.ok) {
    throw new ApiError(messageFrom(body, `Request failed (${response.status})`), response.status)
  }

  return body as T
}

export interface TokenClaims {
  userId: string
  roleId: string
  personId: string
  organizationId: string
  buyerId?: string | null
  supplierId?: string | null
  organizationType?: number | string | null
  permissions?: string[]
}

export function login(userName: string, password: string) {
  return api<{ message?: string }>('/api/v1/identity/auth/login', {
    method: 'POST',
    body: JSON.stringify({ userName, password }),
  }, { apiKey: false })
}

export function logout() {
  return api('/api/v1/identity/auth/logout', { method: 'PUT' }).catch(() => undefined)
}

export function getClaims() {
  return api<TokenClaims>('/api/v1/identity/token-claim')
}

export interface PersonDetail {
  personId?: string
  name?: string
  email?: string
  phone?: string
  userName?: string
  roleName?: string
  organizationName?: string
  organizationEmail?: string
  addressLine?: string
  country?: string
}

export function getPerson() {
  return api<PersonDetail>('/api/v1/identity/person-detail')
}

export interface Breakdown {
  key: string
  label: string
  count: number
  pendingCount?: number
}

export interface BuyerAnalytics {
  kpis: {
    totalRfqs: number
    liveRfqs: number
    closingThisWeek: number
    awaitingAward: number
    awardedRfqs: number
    awardRate: number
    suppliersEngaged: number
    contracts: number
    activeContracts: number
  }
  statusBreakdown: Breakdown[]
  upcomingDeadlines: {
    rfqId: string
    rfqNumber: string
    title: string
    department: string
    endDate: string
    invitedSuppliers: number
  }[]
}

export interface SupplierAnalytics {
  kpis: {
    invitations: number
    openForBidding: number
    actionRequired: number
    quotationsSubmitted: number
    awaitingDecision: number
    rfqsWon: number
    rfqsNotAwarded: number
    winRate: number
  }
  stageBreakdown: Breakdown[]
  upcomingDeadlines: {
    supplierRfqId: string
    buyerRfqId: string
    rfqNumber: string
    title: string
    buyerName: string
    endDate: string
    quotationSubmitted: boolean
  }[]
}

export function getBuyerAnalytics() {
  return api<BuyerAnalytics>('/api/v1/buyer/dashboard-analytics')
}

export function getSupplierAnalytics() {
  return api<SupplierAnalytics>('/api/v1/supplier/dashboard-analytics')
}

export interface RfqListItem {
  rfqId: string
  rfqNumber: string
  title: string
  endDate?: string | null
  deliveryLocation?: string | null
  organizationName?: string | null
  supplierRFQId?: string | null
  status?: string | null
}

export function getBuyerRfqs(buyerId: string, index: number, limit: number) {
  return api<RfqListItem[]>('/api/v1/buyer/rfq-master-data', {
    method: 'POST',
    body: JSON.stringify({ buyerId, index, limit }),
  })
}

export function getSupplierRfqs(supplierId: string, index: number, limit: number) {
  return api<RfqListItem[]>('/api/v1/supplier/rfq-master-data', {
    method: 'POST',
    body: JSON.stringify({ supplierId, index, limit }),
  })
}

export interface RfqLine {
  id?: string
  buyerRFQItemId?: string
  supplierRFQItemId?: string
  supplierRFQId?: string
  description?: string
  quantity?: number
  uom?: string
  materialCode?: string
}

export interface QuotationLine {
  id?: string
  supplierRFQItemId?: string
  buyerRFQItemId?: string
  quotedPrice?: number
}

export interface Quotation {
  totalPrice?: number | null
  deliveryCharge?: number | null
  tax?: number | null
  discount?: number | null
  deliveryType?: string | null
  status?: string | null
  supplierName?: string | null
  supplierId?: string | null
  quotationId?: string | null
  id?: string
  isLead?: boolean
  isAwarded?: boolean
  supplierQuotationItems?: QuotationLine[] | null
}

export interface RfqDetail {
  title?: string
  description?: string
  department?: string
  region?: string
  currency?: string
  deliveryLocation?: string
  startDate?: string
  endDate?: string
  deliveryTargetDate?: string
  budget?: number
  status?: string
  addLotOption?: boolean
  buyerName?: string
  items?: RfqLine[]
  supplierIds?: { supplierId: string; supplierName: string }[]
  supplierQuotation?: Quotation[]
  supplierQuotationItems?: QuotationLine[]
}

export function getBuyerRfq(rfqId: string) {
  return api<RfqDetail>(`/api/v1/buyer/rfq-by-id?rfqId=${encodeURIComponent(rfqId)}`)
}

export function getSupplierRfq(rfqId: string) {
  return api<RfqDetail>(`/api/v1/supplier/rfq-by-id?rfqId=${encodeURIComponent(rfqId)}`)
}

export interface QuotePayload {
  supplierQuotationId?: string | null
  supplierRFQId?: string | null
  totalPrice: number
  deliveryCharge: number
  deliveryType: string
  discount: number
  discountType: string
  tax: number
  taxType: string
  temporaryVerificationToken?: string
  items: {
    supplierRFQItemId?: string | null
    buyerRFQItemId: string
    quotedPrice: number
    deliveryCharge?: number
    deliveryType?: string
    discount?: number
    discountType?: string
    tax?: number
    taxType?: string
    isLineitemAvailable?: boolean
  }[]
}

export function sendSupplierOtp() {
  return api('/api/v1/supplier/send-otp', { method: 'POST' })
}

export function verifySupplierOtp(otp: string) {
  return api<{ token?: string; message?: string }>('/api/v1/supplier/verify-otp', {
    method: 'POST',
    body: JSON.stringify({ otp }),
  })
}

export function submitQuotation(payload: QuotePayload) {
  return api('/api/v1/supplier/quotation', {
    method: 'PUT',
    body: JSON.stringify(payload),
  })
}

export interface CatalogItem {
  id?: string
  catalogId?: string
  catalogName?: string
  description?: string
  price?: number
  currency?: string
  unitOfMeasure?: string
  supplierName?: string
  segmentTitle?: string
  familyTitle?: string
  commodityTitle?: string
  catalogType?: string
}

export function searchBuyerCatalog(search: string) {
  const params = new URLSearchParams({ index: '0', limit: '30' })
  if (search.trim()) params.set('search', search.trim())
  return api<CatalogItem[]>(`/api/v1/supplier/buyer-catalog?${params}`)
}

export function getSupplierCatalog() {
  return api<CatalogItem[] | { data?: CatalogItem[]; catalogs?: CatalogItem[] }>('/api/v1/supplier/catalog')
}

export interface OrgProfile {
  id?: string
  businessProfile?: {
    organizationName?: string
    email?: string
    phone?: string
    country?: string
    addressLine1?: string
    city?: string
    state?: string
    pinCode?: string
    industry?: string
    website?: string
    description?: string
    status?: string
  }
}

export function getBuyerProfile() {
  return api<OrgProfile | null>('/api/v1/buyer/profile')
}

export function getSupplierProfile() {
  return api<OrgProfile | null>('/api/v1/supplier/profile')
}
