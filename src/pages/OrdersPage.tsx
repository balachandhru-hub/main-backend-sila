import { useEffect, useState } from 'react'
import { getPurchaseOrder, searchPurchaseOrders, type PurchaseOrder } from '../api'
import { formatMoney, titleCase } from '../format'

export default function OrdersPage() {
  const [query, setQuery] = useState('')
  const [rows, setRows] = useState<PurchaseOrder[]>([])
  const [open, setOpen] = useState<PurchaseOrder | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    let active = true
    const handle = window.setTimeout(() => {
      setLoading(true)
      searchPurchaseOrders(query)
        .then((list) => { if (active) setRows(Array.isArray(list) ? list : []) })
        .catch((err: unknown) => { if (active) setError(err instanceof Error ? err.message : 'Could not search purchase orders.') })
        .finally(() => { if (active) setLoading(false) })
    }, 250)
    return () => { active = false; window.clearTimeout(handle) }
  }, [query])

  return (
    <main className="screen">
      <header className="topbar">
        <div>
          <p className="page-kicker">ERP</p>
          <h1>Purchase orders</h1>
        </div>
      </header>
      <div className="search">
        <input placeholder="PO number or supplier" value={query} aria-label="Search purchase orders" onChange={(e) => setQuery(e.target.value)} />
      </div>
      {error && <div className="alert">{error}</div>}
      {loading ? <div className="loading"><div className="spinner" />Searching…</div> : (
        <div className="stack">
          {rows.map((item) => (
            <button
              className="list-card"
              key={item.id}
              type="button"
              onClick={() => {
                getPurchaseOrder(item.poNumber).then(setOpen).catch((err: unknown) => setError(err instanceof Error ? err.message : 'Could not open the PO.'))
              }}
            >
              <div className="meta"><span className="ref">{item.poNumber}</span><span className="badge">{titleCase(item.status)}</span></div>
              <div className="card-title">{item.supplierName}</div>
              <div className="meta"><span>{item.operatingUnitName || ' '}</span><span>{formatMoney(item.totalAmount, item.currency)}</span></div>
            </button>
          ))}
          {rows.length === 0 && <div className="empty">No open purchase orders.</div>}
        </div>
      )}
      {open && (
        <section className="card" style={{ marginTop: '0.8rem' }}>
          <div className="section-head" style={{ marginTop: 0 }}>
            <h2>{open.poNumber}</h2>
            <button className="text-btn" type="button" onClick={() => setOpen(null)}>Close</button>
          </div>
          <p className="sub">{open.supplierName}</p>
          {(open.items || []).map((item) => (
            <div className="line" key={item.id}>
              <div>
                <div>{item.description || item.materialCode}</div>
                <div className="meta">Open {item.openQuantity} {item.uom} · received {item.receivedQuantity}</div>
              </div>
            </div>
          ))}
        </section>
      )}
    </main>
  )
}
