import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { getGrns, getInvoices, getPerson, type GoodsReceipt, type Invoice, type PersonDetail } from '../api'
import { useAuth } from '../auth'
import { formatDate, titleCase } from '../format'

export default function HomePage() {
  const { session, signOut } = useAuth()
  const [person, setPerson] = useState<PersonDetail | null>(null)
  const [invoices, setInvoices] = useState<Invoice[]>([])
  const [grns, setGrns] = useState<GoodsReceipt[]>([])
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let active = true
    Promise.all([
      getPerson().catch(() => null),
      getInvoices().catch((err: unknown) => {
        if (active) setError(err instanceof Error ? err.message : 'Could not load invoices.')
        return [] as Invoice[]
      }),
      getGrns().catch(() => [] as GoodsReceipt[]),
    ]).then(([who, invoiceRows, grnRows]) => {
      if (!active) return
      setPerson(who)
      setInvoices(Array.isArray(invoiceRows) ? invoiceRows : [])
      setGrns(Array.isArray(grnRows) ? grnRows : [])
    })
    return () => { active = false }
  }, [])

  const ready = invoices.filter((item) => item.status === 'READY_FOR_GRN').length

  return (
    <main className="screen">
      <header className="topbar">
        <img src="/sila-logo.png" alt="" />
        <div className="grow">
          <p className="page-kicker">SILA Store</p>
          <h1>{person?.name || session?.roleLabel || 'Store'}</h1>
        </div>
        <button className="text-btn" type="button" onClick={() => signOut()}>Out</button>
      </header>
      <p className="sub" style={{ marginTop: '-0.35rem', marginBottom: '0.8rem' }}>
        {person?.organizationName || 'This gateway'} · invoice upload, review, and GRN
      </p>
      {error && <div className="alert">{error}</div>}
      <div className="kpi-grid" style={{ marginBottom: '0.8rem' }}>
        <div className="kpi"><b>{invoices.length}</b><span>Invoices</span></div>
        <div className="kpi"><b>{ready}</b><span>Ready for GRN</span></div>
        <div className="kpi"><b>{grns.length}</b><span>Goods receipts</span></div>
        <div className="kpi"><b>{grns.filter((item) => item.status === 'POSTED').length}</b><span>Posted</span></div>
      </div>
      <div className="tiles">
        <Link className="tile" to="/invoices/upload"><strong>Upload invoice</strong><span>Camera or file, then the gateway stores it</span></Link>
        <Link className="tile" to="/invoices"><strong>Invoices</strong><span>Review extraction and status</span></Link>
        <Link className="tile" to="/receive"><strong>Receive goods</strong><span>Validate and post a GRN</span></Link>
        <Link className="tile" to="/orders"><strong>Purchase orders</strong><span>Open POs on this backend</span></Link>
      </div>
      <div className="section-head"><h2>Latest receipts</h2><Link to="/receive">All</Link></div>
      <div className="stack">
        {grns.slice(0, 4).map((item) => (
          <div className="list-card" key={item.id}>
            <div className="meta"><span className="ref">{item.grnNumber}</span><span className="badge">{titleCase(item.status)}</span></div>
            <div className="card-title">{item.supplierName || item.purchaseOrderNumber}</div>
            <div className="meta"><span>PO {item.purchaseOrderNumber}</span><span>{formatDate(item.receiptDate)}</span></div>
          </div>
        ))}
        {grns.length === 0 && <div className="empty">No goods receipts yet.</div>}
      </div>
    </main>
  )
}
