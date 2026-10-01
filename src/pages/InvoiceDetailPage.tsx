import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { getInvoice, type Invoice } from '../api'
import { formatDate, formatMoney, titleCase } from '../format'

export default function InvoiceDetailPage() {
  const { invoiceId = '' } = useParams()
  const [invoice, setInvoice] = useState<Invoice | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let active = true
    getInvoice(invoiceId)
      .then((row) => { if (active) setInvoice(row) })
      .catch((err: unknown) => { if (active) setError(err instanceof Error ? err.message : 'Could not open the invoice.') })
    return () => { active = false }
  }, [invoiceId])

  return (
    <main className="screen">
      <header className="topbar">
        <Link className="icon-btn" to="/invoices" aria-label="Back to invoices">←</Link>
        <div className="grow">
          <p className="page-kicker">Invoice</p>
          <h1>{invoice?.invoiceNumber || 'Details'}</h1>
        </div>
      </header>
      {error && <div className="alert">{error}</div>}
      {!invoice && !error && <div className="loading"><div className="spinner" />Opening invoice…</div>}
      {invoice && (
        <div className="stack">
          <section className="card">
            <div className="meta"><span className="badge">{titleCase(invoice.status)}</span></div>
            <div className="stack" style={{ marginTop: '0.6rem' }}>
              <div className="row"><span className="muted">Supplier</span><span>{invoice.supplierName || '—'}</span></div>
              <div className="row"><span className="muted">Date</span><span>{formatDate(invoice.invoiceDate)}</span></div>
              <div className="row"><span className="muted">PO</span><span>{invoice.purchaseOrderNumber || '—'}</span></div>
              <div className="row"><span className="muted">Gross</span><span>{formatMoney(invoice.grossAmount, invoice.currency)}</span></div>
              <div className="row"><span className="muted">Unit</span><span>{invoice.operatingUnitName || '—'}</span></div>
            </div>
          </section>
          <section className="card">
            <h2>Lines</h2>
            {(invoice.lines || []).length === 0 && <p className="sub">No extracted lines yet.</p>}
            {(invoice.lines || []).map((line) => (
              <div className="line" key={line.id}>
                <div>
                  <div className="card-title" style={{ margin: 0 }}>{line.description || `Line ${line.lineNumber}`}</div>
                  <div className="meta">{line.quantity ?? '—'} {line.uom} · {titleCase(line.matchStatus)}</div>
                </div>
                <strong>{formatMoney(line.lineAmount, invoice.currency)}</strong>
              </div>
            ))}
          </section>
          {invoice.status === 'READY_FOR_GRN' && (
            <Link className="btn" to="/receive">Post a GRN for this invoice</Link>
          )}
        </div>
      )}
    </main>
  )
}
