import { useEffect, useMemo, useState } from 'react'
import { Link, Navigate } from 'react-router-dom'
import { getBuyerRfqs, getSupplierRfqs, type RfqListItem } from '../api'
import { useAuth } from '../auth'
import { formatDate, titleCase } from '../format'

const PAGE = 20

export default function RfqListPage() {
  const { session } = useAuth()
  const [items, setItems] = useState<RfqListItem[]>([])
  const [query, setQuery] = useState('')
  const [page, setPage] = useState(0)
  const [hasMore, setHasMore] = useState(false)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const orgId = session?.role === 'buyer' ? session.buyerId : session?.supplierId

  useEffect(() => {
    if (!session || session.role === 'platform' || !orgId) return
    let active = true
    setLoading(true)
    const load = session.role === 'buyer'
      ? getBuyerRfqs(orgId, 0, PAGE)
      : getSupplierRfqs(orgId, 0, PAGE)
    load
      .then((rows) => {
        if (!active) return
        const list = Array.isArray(rows) ? rows : []
        setItems(list)
        setPage(0)
        setHasMore(list.length === PAGE)
      })
      .catch((err: unknown) => {
        if (active) setError(err instanceof Error ? err.message : 'Could not load RFQs.')
      })
      .finally(() => {
        if (active) setLoading(false)
      })
    return () => { active = false }
  }, [session, orgId])

  async function loadMore() {
    if (!session || !orgId) return
    const next = page + 1
    const rows = session.role === 'buyer'
      ? await getBuyerRfqs(orgId, next, PAGE)
      : await getSupplierRfqs(orgId, next, PAGE)
    const list = Array.isArray(rows) ? rows : []
    setItems((current) => [...current, ...list])
    setPage(next)
    setHasMore(list.length === PAGE)
  }

  const visible = useMemo(() => {
    const needle = query.trim().toLowerCase()
    if (!needle) return items
    return items.filter((item) =>
      [item.rfqNumber, item.title, item.organizationName, item.deliveryLocation, item.status]
        .filter(Boolean)
        .some((value) => String(value).toLowerCase().includes(needle)),
    )
  }, [items, query])

  if (!session) return null
  if (session.role === 'platform') return <Navigate to="/home" replace />

  return (
    <main className="screen">
      <header className="topbar">
        <div>
          <p className="page-kicker">{session.role === 'buyer' ? 'Sourcing' : 'Bidding'}</p>
          <h1>{session.role === 'buyer' ? 'Your RFQs' : 'Invited RFQs'}</h1>
        </div>
      </header>
      <div className="search">
        <input
          placeholder="Search number, title, location"
          value={query}
          onChange={(event) => setQuery(event.target.value)}
          aria-label="Search RFQs"
        />
      </div>
      {error && <div className="alert">{error}</div>}
      {loading ? (
        <div className="loading"><div className="spinner" />Loading RFQs…</div>
      ) : (
        <div className="stack">
          {visible.map((item) => (
            <Link className="list-card" key={item.rfqId} to={`/rfqs/${item.rfqId}`}>
              <div className="meta">
                <span className="ref">{item.rfqNumber}</span>
                {item.status && <span className="badge">{titleCase(item.status)}</span>}
              </div>
              <div className="card-title">{item.title}</div>
              <div className="meta">
                <span>{item.organizationName || item.deliveryLocation || '—'}</span>
                <span>Closes {formatDate(item.endDate)}</span>
              </div>
            </Link>
          ))}
          {visible.length === 0 && <div className="empty">No RFQs match.</div>}
          {hasMore && !query && (
            <button className="btn secondary" type="button" onClick={() => loadMore().catch((err: unknown) => setError(err instanceof Error ? err.message : 'Could not load more.'))}>
              Load more
            </button>
          )}
        </div>
      )}
    </main>
  )
}
