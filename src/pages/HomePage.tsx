import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import {
  getBuyerAnalytics,
  getBuyerRfqs,
  getPerson,
  getSupplierAnalytics,
  getSupplierRfqs,
  type BuyerAnalytics,
  type PersonDetail,
  type RfqListItem,
  type SupplierAnalytics,
} from '../api'
import { useAuth } from '../auth'
import { formatDate } from '../format'

function Kpi({ value, label }: { value: number | string; label: string }) {
  return (
    <div className="kpi">
      <b>{value}</b>
      <span>{label}</span>
    </div>
  )
}

export default function HomePage() {
  const { session } = useAuth()
  const [person, setPerson] = useState<PersonDetail | null>(null)
  const [buyer, setBuyer] = useState<BuyerAnalytics | null>(null)
  const [supplier, setSupplier] = useState<SupplierAnalytics | null>(null)
  const [rfqs, setRfqs] = useState<RfqListItem[]>([])
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    if (!session) return
    let active = true
    setLoading(true)
    const work = (async () => {
      const who = await getPerson().catch(() => null)
      if (!active) return
      setPerson(who)
      if (session.role === 'buyer' && session.buyerId) {
        const [analytics, list] = await Promise.all([
          getBuyerAnalytics().catch(() => null),
          getBuyerRfqs(session.buyerId, 0, 5).catch(() => [] as RfqListItem[]),
        ])
        if (!active) return
        setBuyer(analytics)
        setRfqs(Array.isArray(list) ? list : [])
      } else if (session.role === 'supplier' && session.supplierId) {
        const [analytics, list] = await Promise.all([
          getSupplierAnalytics().catch(() => null),
          getSupplierRfqs(session.supplierId, 0, 5).catch(() => [] as RfqListItem[]),
        ])
        if (!active) return
        setSupplier(analytics)
        setRfqs(Array.isArray(list) ? list : [])
      }
    })()
    work.catch((err: unknown) => {
      if (active) setError(err instanceof Error ? err.message : 'Could not load the home screen.')
    }).finally(() => {
      if (active) setLoading(false)
    })
    return () => { active = false }
  }, [session])

  if (!session) return null
  const name = person?.name || person?.organizationName || session.roleLabel

  return (
    <main className="screen">
      <header className="topbar">
        <img src="/sila-logo.png" alt="" />
        <div className="grow">
          <p className="page-kicker">SILA</p>
          <h1>{session.role === 'platform' ? 'Account' : 'Today'}</h1>
        </div>
        <span className="role-pill">{session.roleLabel}</span>
      </header>
      <p className="sub" style={{ marginTop: '-0.4rem', marginBottom: '0.9rem' }}>Hello, {name}</p>
      {error && <div className="alert">{error}</div>}
      {loading && <div className="loading"><div className="spinner" />Loading your workspace…</div>}

      {session.role === 'buyer' && buyer && (
        <>
          <div className="kpi-grid">
            <Kpi value={buyer.kpis.liveRfqs} label="Live RFQs" />
            <Kpi value={buyer.kpis.closingThisWeek} label="Closing this week" />
            <Kpi value={buyer.kpis.awaitingAward} label="Awaiting award" />
            <Kpi value={buyer.kpis.suppliersEngaged} label="Suppliers engaged" />
            <Kpi value={buyer.kpis.awardedRfqs} label="Awarded" />
            <Kpi value={buyer.kpis.activeContracts} label="Active contracts" />
          </div>
          {buyer.statusBreakdown?.length > 0 && (
            <div className="chips" style={{ marginTop: '0.75rem' }}>
              {buyer.statusBreakdown.map((item) => (
                <span className="chip" key={item.key}>{item.label} <strong>{item.count}</strong></span>
              ))}
            </div>
          )}
        </>
      )}

      {session.role === 'supplier' && supplier && (
        <>
          <div className="kpi-grid">
            <Kpi value={supplier.kpis.openForBidding} label="Open to quote" />
            <Kpi value={supplier.kpis.actionRequired} label="Action required" />
            <Kpi value={supplier.kpis.quotationsSubmitted} label="Quotes sent" />
            <Kpi value={supplier.kpis.awaitingDecision} label="Awaiting decision" />
            <Kpi value={supplier.kpis.rfqsWon} label="Won" />
            <Kpi value={`${supplier.kpis.winRate}%`} label="Win rate" />
          </div>
          {supplier.stageBreakdown?.length > 0 && (
            <div className="chips" style={{ marginTop: '0.75rem' }}>
              {supplier.stageBreakdown.map((item) => (
                <span className="chip" key={item.key}>{item.label} <strong>{item.count}</strong></span>
              ))}
            </div>
          )}
        </>
      )}

      {session.role === 'platform' && (
        <section className="card">
          <h2>Administration stays on the desktop portal</h2>
          <p className="sub">
            User setup, approvals, contracts and templates are unchanged on the full SILA site.
            This phone view is for buyers and suppliers working RFQs, quotations and the catalog.
          </p>
          {person && (
            <div className="stack" style={{ marginTop: '0.8rem' }}>
              <div className="row"><span className="muted">Name</span><span>{person.name || '—'}</span></div>
              <div className="row"><span className="muted">Organization</span><span>{person.organizationName || '—'}</span></div>
              <div className="row"><span className="muted">Email</span><span>{person.email || '—'}</span></div>
            </div>
          )}
        </section>
      )}

      {(session.role === 'buyer' || session.role === 'supplier') && !loading && (
        <>
          <div className="section-head">
            <h2>{session.role === 'buyer' ? 'Recent RFQs' : 'Invitations'}</h2>
            <Link to="/rfqs">View all</Link>
          </div>
          <div className="stack">
            {(buyer?.upcomingDeadlines?.length
              ? buyer.upcomingDeadlines.map((item) => ({
                  id: item.rfqId,
                  number: item.rfqNumber,
                  title: item.title,
                  meta: item.department || `${item.invitedSuppliers} suppliers`,
                  date: item.endDate,
                }))
              : supplier?.upcomingDeadlines?.length
                ? supplier.upcomingDeadlines.map((item) => ({
                    id: item.buyerRfqId || item.supplierRfqId,
                    number: item.rfqNumber,
                    title: item.title,
                    meta: item.buyerName,
                    date: item.endDate,
                  }))
                : rfqs.map((item) => ({
                    id: item.rfqId,
                    number: item.rfqNumber,
                    title: item.title,
                    meta: item.organizationName || item.deliveryLocation || '',
                    date: item.endDate,
                  }))
            ).slice(0, 5).map((item) => (
              <Link className="list-card" key={item.id} to={`/rfqs/${item.id}`}>
                <div className="meta"><span className="ref">{item.number}</span><span>{item.meta}</span></div>
                <div className="card-title">{item.title}</div>
                <div className="meta">Closes {formatDate(item.date)}</div>
              </Link>
            ))}
            {!buyer?.upcomingDeadlines?.length && !supplier?.upcomingDeadlines?.length && rfqs.length === 0 && (
              <div className="empty">No RFQs yet.</div>
            )}
          </div>
        </>
      )}
    </main>
  )
}
