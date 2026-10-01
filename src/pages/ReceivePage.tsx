import { FormEvent, useEffect, useState } from 'react'
import { getGrns, getInvoices, getPurchaseOrder, getUnits, postGrn, validateGrn, type GoodsReceipt, type Invoice, type OrgUnit, type PurchaseOrder } from '../api'
import { formatDate, titleCase } from '../format'

export default function ReceivePage() {
  const [units, setUnits] = useState<OrgUnit[]>([])
  const [invoices, setInvoices] = useState<Invoice[]>([])
  const [grns, setGrns] = useState<GoodsReceipt[]>([])
  const [unitId, setUnitId] = useState('')
  const [invoiceId, setInvoiceId] = useState('')
  const [poNumber, setPoNumber] = useState('')
  const [order, setOrder] = useState<PurchaseOrder | null>(null)
  const [qty, setQty] = useState<Record<string, string>>({})
  const [error, setError] = useState<string | null>(null)
  const [notice, setNotice] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  function loadLists() {
    getUnits().then((rows) => {
      const list = Array.isArray(rows) ? rows : []
      setUnits(list)
      setUnitId((current) => current || list[0]?.id || '')
    }).catch(() => undefined)
    getInvoices().then((rows) => setInvoices(Array.isArray(rows) ? rows : [])).catch(() => undefined)
    getGrns().then((rows) => setGrns(Array.isArray(rows) ? rows : [])).catch(() => undefined)
  }

  useEffect(() => { loadLists() }, [])

  async function loadPo() {
    setError(null)
    const found = await getPurchaseOrder(poNumber.trim())
    setOrder(found)
    const next: Record<string, string> = {}
    found.items?.forEach((item) => { next[item.id] = String(item.openQuantity ?? 0) })
    setQty(next)
    if (!unitId && found.operatingUnitId) setUnitId(found.operatingUnitId)
  }

  async function onSubmit(event: FormEvent) {
    event.preventDefault()
    if (!order || !invoiceId || !unitId) {
      setError('Choose a unit, an invoice, and a purchase order.')
      return
    }
    const lines = (order.items || []).map((item) => {
      const received = Number(qty[item.id] || 0)
      return {
        purchaseOrderItemId: item.id,
        receivedQuantity: received,
        acceptedQuantity: received,
        damagedQuantity: 0,
        rejectedQuantity: 0,
      }
    }).filter((line) => line.receivedQuantity > 0)
    if (lines.length === 0) {
      setError('Enter a received quantity on at least one line.')
      return
    }
    setBusy(true)
    setError(null)
    setNotice(null)
    try {
      const body = { invoiceId, purchaseOrderId: order.id, operatingUnitId: unitId, lines }
      const check = await validateGrn(body)
      if (check && check.valid === false) {
        setError(check.message || (check.errors || []).join(' ') || 'This receipt did not pass validation.')
        return
      }
      const posted = await postGrn(body)
      setNotice(`Posted ${posted.grnNumber} (${titleCase(posted.status)}).`)
      setOrder(null)
      setPoNumber('')
      loadLists()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not post the goods receipt.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <main className="screen">
      <header className="topbar">
        <div>
          <p className="page-kicker">Receive</p>
          <h1>Goods receipt</h1>
        </div>
      </header>
      {error && <div className="alert">{error}</div>}
      {notice && <div className="ok">{notice}</div>}
      <form className="card stack" onSubmit={onSubmit}>
        <div className="field">
          <label htmlFor="unit">Operating unit</label>
          <select id="unit" value={unitId} onChange={(e) => setUnitId(e.target.value)}>
            <option value="">Select a unit</option>
            {units.map((unit) => <option key={unit.id} value={unit.id}>{unit.name} ({unit.code})</option>)}
          </select>
        </div>
        <div className="field">
          <label htmlFor="invoice">Invoice</label>
          <select id="invoice" value={invoiceId} onChange={(e) => setInvoiceId(e.target.value)}>
            <option value="">Select an invoice</option>
            {invoices.map((item) => (
              <option key={item.id} value={item.id}>
                {item.invoiceNumber || item.id.slice(0, 8)} · {titleCase(item.status)}
              </option>
            ))}
          </select>
        </div>
        <div className="field">
          <label htmlFor="po">Purchase order number</label>
          <input id="po" value={poNumber} onChange={(e) => setPoNumber(e.target.value)} />
        </div>
        <button className="btn secondary" type="button" onClick={() => loadPo().catch((err: unknown) => setError(err instanceof Error ? err.message : 'PO not found.'))}>
          Load lines
        </button>
        {order && (order.items || []).map((item) => (
          <div className="line" key={item.id}>
            <div>
              <div>{item.description || item.materialCode}</div>
              <div className="meta">Open {item.openQuantity} {item.uom}</div>
            </div>
            <input
              inputMode="decimal"
              aria-label={`Received quantity for ${item.description || item.materialCode}`}
              value={qty[item.id] ?? ''}
              onChange={(e) => setQty((current) => ({ ...current, [item.id]: e.target.value }))}
            />
          </div>
        ))}
        <button className="btn" type="submit" disabled={busy || !order}>{busy ? 'Posting…' : 'Validate and post GRN'}</button>
      </form>
      <div className="section-head"><h2>Posted receipts</h2></div>
      <div className="stack">
        {grns.map((item) => (
          <div className="list-card" key={item.id}>
            <div className="meta"><span className="ref">{item.grnNumber}</span><span className="badge">{titleCase(item.status)}</span></div>
            <div className="card-title">{item.supplierName}</div>
            <div className="meta"><span>PO {item.purchaseOrderNumber}</span><span>{formatDate(item.receiptDate)}</span></div>
            {item.failureMessage && <p className="sub">{item.failureMessage}</p>}
          </div>
        ))}
        {grns.length === 0 && <div className="empty">No goods receipts yet.</div>}
      </div>
    </main>
  )
}
