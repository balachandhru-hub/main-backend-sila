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
    const record = body as { message?: string; description?: string; title?: string }
    return record.message || record.description || record.title || fallback
  }
  if (typeof body === 'string' && body.trim()) return body
  return fallback
}

export async function api<T>(
  path: string,
  init: RequestInit = {},
  options?: { apiKey?: boolean; retry?: boolean },
): Promise<T> {
  const headers = new Headers(init.headers)
  const isForm = typeof FormData !== 'undefined' && init.body instanceof FormData
  if (init.body && !isForm && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json')
  }
  if (options?.apiKey !== false && API_KEY) {
    headers.set('X-API-Key', API_KEY)
  }

  let response: Response
  try {
    response = await fetch(path, { ...init, headers, credentials: 'include' })
  } catch {
    throw new ApiError(
      'The SILA gateway is not reachable. This app calls the one backend on port 8000, not sila-api.chervicaon.com.',
      0,
    )
  }

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
    const fallback = response.status >= 500
      ? 'The SILA gateway on port 8000 did not answer. Start this backend, then sign in again.'
      : `Request failed (${response.status})`
    throw new ApiError(messageFrom(body, fallback), response.status)
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
  name?: string
  email?: string
  phone?: string
  userName?: string
  roleName?: string
  organizationName?: string
}

export function getPerson() {
  return api<PersonDetail>('/api/v1/identity/person-detail')
}

export interface OrgUnit {
  id: string
  code: string
  name: string
  kind?: string
  status?: string
}

export interface InvoiceLine {
  id: string
  lineNumber: number
  description: string
  quantity?: number | null
  uom?: string | null
  unitPrice?: number | null
  lineAmount?: number | null
  matchStatus?: string
  purchaseOrderItemId?: string | null
}

export interface Invoice {
  id: string
  documentId: string
  invoiceNumber: string
  invoiceDate?: string | null
  supplierName?: string | null
  purchaseOrderNumber?: string | null
  purchaseOrderId?: string | null
  currency?: string | null
  grossAmount?: number | null
  status?: string
  operatingUnitId?: string | null
  operatingUnitName?: string | null
  goodsReceiptId?: string | null
  lines?: InvoiceLine[]
  createdAt?: string
}

export interface PoItem {
  id: string
  lineNumber: number
  materialCode: string
  description: string
  orderedQuantity: number
  receivedQuantity: number
  openQuantity: number
  uom: string
}

export interface PurchaseOrder {
  id: string
  poNumber: string
  supplierName: string
  status?: string
  currency?: string
  operatingUnitId?: string | null
  operatingUnitName?: string | null
  totalAmount?: number | null
  items?: PoItem[]
}

export interface GoodsReceipt {
  id: string
  grnNumber: string
  status?: string
  purchaseOrderNumber: string
  invoiceNumber?: string | null
  supplierName: string
  operatingUnitName?: string
  receiptDate?: string
  failureMessage?: string | null
}

export interface DocumentResult {
  id: string
  filename: string
  invoiceId: string
  status?: string
  message?: string | null
  nextStep?: string
}

export function getUnits() {
  return api<OrgUnit[]>('/api/v1/operations/units')
}

export function getInvoices() {
  return api<Invoice[]>('/api/v1/operations/invoices')
}

export function getInvoice(id: string) {
  return api<Invoice>(`/api/v1/operations/invoices/${id}`)
}

export function uploadInvoice(form: FormData) {
  return api<DocumentResult>('/api/v1/operations/documents/invoices', {
    method: 'POST',
    body: form,
  })
}

export function searchPurchaseOrders(query: string) {
  const params = new URLSearchParams({ openOnly: 'true' })
  if (query.trim()) params.set('query', query.trim())
  return api<PurchaseOrder[]>(`/api/v1/operations/purchase-orders/search?${params}`)
}

export function getPurchaseOrder(poNumber: string) {
  return api<PurchaseOrder>(`/api/v1/operations/purchase-orders/${encodeURIComponent(poNumber)}`)
}

export function getGrns() {
  return api<GoodsReceipt[]>('/api/v1/operations/grns')
}

export interface GrnLineInput {
  purchaseOrderItemId: string
  receivedQuantity: number
  acceptedQuantity: number
  damagedQuantity: number
  rejectedQuantity: number
}

export function validateGrn(body: {
  invoiceId: string
  purchaseOrderId: string
  operatingUnitId: string
  lines: GrnLineInput[]
}) {
  return api<{ valid: boolean; message?: string; errors?: string[] }>('/api/v1/operations/grns/validate', {
    method: 'POST',
    body: JSON.stringify(body),
  })
}

export function postGrn(body: {
  invoiceId: string
  purchaseOrderId: string
  operatingUnitId: string
  lines: GrnLineInput[]
}) {
  return api<GoodsReceipt>('/api/v1/operations/grns', {
    method: 'POST',
    headers: { 'Idempotency-Key': crypto.randomUUID() },
    body: JSON.stringify({ ...body, receiptDate: new Date().toISOString() }),
  })
}
