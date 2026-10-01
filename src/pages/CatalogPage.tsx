import { useEffect, useState } from 'react'
import { Navigate } from 'react-router-dom'
import { getSupplierCatalog, searchBuyerCatalog, type CatalogItem } from '../api'
import { useAuth } from '../auth'
import { formatMoney } from '../format'

function asList(payload: CatalogItem[] | { data?: CatalogItem[]; catalogs?: CatalogItem[] } | null): CatalogItem[] {
  if (!payload) return []
  if (Array.isArray(payload)) return payload
  if (Array.isArray(payload.data)) return payload.data
  if (Array.isArray(payload.catalogs)) return payload.catalogs
  return []
}

export default function CatalogPage() {
  const { session } = useAuth()
  const [query, setQuery] = useState('')
  const [items, setItems] = useState<CatalogItem[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!session || session.role === 'platform') return
    let active = true
    const handle = window.setTimeout(() => {
      setLoading(true)
      const load = session.role === 'buyer' ? searchBuyerCatalog(query) : getSupplierCatalog()
      load
        .then((rows) => {
          if (!active) return
          const list = asList(rows)
          const needle = query.trim().toLowerCase()
          setItems(session.role === 'supplier' && needle
            ? list.filter((item) => `${item.catalogName} ${item.description} ${item.supplierName}`.toLowerCase().includes(needle))
            : list)
        })
        .catch((err: unknown) => {
          if (active) setError(err instanceof Error ? err.message : 'Could not load the catalog.')
        })
        .finally(() => {
          if (active) setLoading(false)
        })
    }, session.role === 'buyer' ? 250 : 0)
    return () => {
      active = false
      window.clearTimeout(handle)
    }
  }, [session, query])

  if (!session) return null
  if (session.role === 'platform') return <Navigate to="/home" replace />

  return (
    <main className="screen">
      <header className="topbar">
        <div>
          <p className="page-kicker">Catalog</p>
          <h1>{session.role === 'buyer' ? 'Products' : 'Your catalog'}</h1>
        </div>
      </header>
      <div className="search">
        <input
          placeholder={session.role === 'buyer' ? 'Search products or suppliers' : 'Filter your items'}
          value={query}
          onChange={(event) => setQuery(event.target.value)}
          aria-label="Search catalog"
        />
      </div>
      {error && <div className="alert">{error}</div>}
      {loading ? (
        <div className="loading"><div className="spinner" />Loading catalog…</div>
      ) : (
        <div className="stack">
          {items.map((item, index) => (
            <article className="list-card" key={item.catalogId || item.id || index}>
              <div className="meta">
                <span>{item.supplierName || item.segmentTitle || item.catalogType || 'Catalog'}</span>
              </div>
              <div className="card-title">{item.catalogName || 'Untitled item'}</div>
              {item.description && <p className="sub">{item.description}</p>}
              <div className="row" style={{ marginTop: '0.45rem' }}>
                <span className="muted">{[item.commodityTitle, item.unitOfMeasure].filter(Boolean).join(' · ') || ' '}</span>
                <span className="price">{formatMoney(item.price, item.currency)}</span>
              </div>
            </article>
          ))}
          {items.length === 0 && <div className="empty">Nothing in the catalog for that search.</div>}
        </div>
      )}
    </main>
  )
}
