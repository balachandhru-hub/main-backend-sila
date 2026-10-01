import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { getInvoices, type Invoice } from '../api'
import { formatDate, formatMoney, titleCase } from '../format'

export default function InvoicesPage() {
  const [rows, setRows] = useState<Invoice[]>([])
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    let active = true
    getInvoices()
      .then((list) => { if (active) setRows(Array.isArray(list) ? list : []) })
      .catch((err: unknown) => { if (active) setError(err instanceof Error ? err.message : 'Could not load invoices.') })
      .finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [])

  return (
    <main className="screen">
      <header className="topbar">
        <div className="grow">
          <p className="page-kicker">Documents</p>
          <h1>Invoices</h1>
        </div>
        <Link className="text-btn" to="/invoices/upload">Upload</Link>
      </header>
      {error && <div className="alert">{error}</div>}
      {loading ? <div className="loading"><div className="spinner" />Loading invoices…</div> : (
        <div className="stack">
          {rows.map((item) => (
            <Link className="list-card" key={item.id} to={`/invoices/${item.id}`}>
              <div className="meta">
                <span className="ref">{item.invoiceNumber || 'No number'}</span>
                <span className="badge">{titleCase(item.status)}</span>
              </div>
              <div className="card-title">{item.supplierName || 'Supplier not set'}</div>
              <div className="meta">
                <span>{item.purchaseOrderNumber ? `PO ${item.purchaseOrderNumber}` : 'No PO'}</span>
                <span>{formatMoney(item.grossAmount, item.currency)} · {formatDate(item.invoiceDate || item.createdAt)}</span>
              </div>
            </Link>
          ))}
          {rows.length === 0 && <div className="empty">No invoices on this gateway yet.</div>}
        </div>
      )}
    </main>
  )
}
