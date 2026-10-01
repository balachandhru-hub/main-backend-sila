import { FormEvent, useEffect, useMemo, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import {
  getBuyerRfq,
  getSupplierRfq,
  sendSupplierOtp,
  submitQuotation,
  verifySupplierOtp,
  type QuotePayload,
  type RfqDetail,
} from '../api'
import { useAuth } from '../auth'
import { formatDate, formatMoney, titleCase } from '../format'

const EMPTY_GUID = '00000000-0000-0000-0000-000000000000'
const validId = (id?: string | null) => (id && id !== EMPTY_GUID ? id : null)

export default function RfqDetailPage() {
  const { rfqId = '' } = useParams()
  const { session } = useAuth()
  const [detail, setDetail] = useState<RfqDetail | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)
  const [prices, setPrices] = useState<Record<string, string>>({})
  const [delivery, setDelivery] = useState('0')
  const [tax, setTax] = useState('0')
  const [discount, setDiscount] = useState('0')
  const [otp, setOtp] = useState('')
  const [otpSent, setOtpSent] = useState(false)
  const [token, setToken] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const [notice, setNotice] = useState<string | null>(null)

  useEffect(() => {
    if (!session || !rfqId) return
    let active = true
    setLoading(true)
    const load = session.role === 'supplier' ? getSupplierRfq(rfqId) : getBuyerRfq(rfqId)
    load
      .then((data) => {
        if (!active) return
        setDetail(data)
        const existing = data.supplierQuotation?.[0]
        if (existing) {
          setDelivery(String(existing.deliveryCharge ?? 0))
          setTax(String(existing.tax ?? 0))
          setDiscount(String(existing.discount ?? 0))
        }
        const next: Record<string, string> = {}
        data.items?.forEach((item, index) => {
          const key = item.id || item.buyerRFQItemId || `item-${index}`
          const quoted = data.supplierQuotationItems?.find((line) =>
            line.buyerRFQItemId === item.id || line.buyerRFQItemId === item.buyerRFQItemId,
          )
          if (quoted?.quotedPrice != null) next[key] = String(quoted.quotedPrice)
        })
        setPrices(next)
      })
      .catch((err: unknown) => {
        if (active) setError(err instanceof Error ? err.message : 'Could not open this RFQ.')
      })
      .finally(() => {
        if (active) setLoading(false)
      })
    return () => { active = false }
  }, [session, rfqId])

  const lineTotal = useMemo(() => {
    if (!detail?.items?.length) return 0
    return detail.items.reduce((sum, item, index) => {
      const key = item.id || item.buyerRFQItemId || `item-${index}`
      const price = Number(prices[key] || 0)
      const qty = Number(item.quantity || 1)
      return sum + price * (Number.isFinite(qty) && qty > 0 ? qty : 1)
    }, 0)
  }, [detail, prices])

  const total = Math.max(0, lineTotal + Number(delivery || 0) + Number(tax || 0) - Number(discount || 0))

  async function onQuote(event: FormEvent) {
    event.preventDefault()
    if (!detail || session?.role !== 'supplier') return
    setError(null)
    setNotice(null)
    setBusy(true)
    try {
      let verification = token
      if (!verification) {
        if (!otpSent) {
          await sendSupplierOtp()
          setOtpSent(true)
          setNotice('We sent a code to your account email. Enter it to submit the quotation.')
          return
        }
        if (!otp.trim()) {
          setError('Enter the verification code.')
          return
        }
        const verified = await verifySupplierOtp(otp.trim())
        verification = verified.token || null
        if (!verification) throw new Error('The code was accepted, but no verification token came back.')
        setToken(verification)
      }

      const supplierRFQId = detail.items?.find((item) => validId(item.supplierRFQId))?.supplierRFQId || null
      const payload: QuotePayload = {
        supplierQuotationId: validId(detail.supplierQuotation?.[0]?.quotationId || detail.supplierQuotation?.[0]?.id),
        supplierRFQId: validId(supplierRFQId),
        totalPrice: Number(total.toFixed(2)),
        deliveryCharge: Number(delivery || 0),
        deliveryType: 'AMOUNT',
        discount: Number(discount || 0),
        discountType: 'AMOUNT',
        tax: Number(tax || 0),
        taxType: 'AMOUNT',
        temporaryVerificationToken: verification || undefined,
        items: (detail.items || []).map((item, index) => {
          const key = item.id || item.buyerRFQItemId || `item-${index}`
          return {
            supplierRFQItemId: validId(item.supplierRFQItemId),
            buyerRFQItemId: item.id || item.buyerRFQItemId || '',
            quotedPrice: Number(prices[key] || 0),
            isLineitemAvailable: true,
          }
        }),
      }
      await submitQuotation(payload)
      setNotice('Quotation submitted.')
      const fresh = await getSupplierRfq(rfqId)
      setDetail(fresh)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not submit the quotation.')
    } finally {
      setBusy(false)
    }
  }

  if (!session) return null

  return (
    <main className="screen">
      <header className="topbar">
        <Link className="icon-btn" to="/rfqs" aria-label="Back to RFQs">←</Link>
        <div className="grow">
          <p className="page-kicker">RFQ</p>
          <h1>{detail?.title || 'Details'}</h1>
        </div>
      </header>
      {error && <div className="alert">{error}</div>}
      {notice && <div className="ok">{notice}</div>}
      {loading && <div className="loading"><div className="spinner" />Opening RFQ…</div>}
      {detail && (
        <div className="stack">
          <section className="card">
            <div className="meta">
              {detail.status && <span className="badge">{titleCase(detail.status)}</span>}
              {detail.department && <span>{detail.department}</span>}
            </div>
            {detail.description && <p className="sub">{detail.description}</p>}
            <div className="stack" style={{ marginTop: '0.7rem' }}>
              <div className="row"><span className="muted">Closes</span><span>{formatDate(detail.endDate)}</span></div>
              <div className="row"><span className="muted">Delivery</span><span>{detail.deliveryLocation || '—'}</span></div>
              {detail.deliveryTargetDate && (
                <div className="row"><span className="muted">Target</span><span>{formatDate(detail.deliveryTargetDate)}</span></div>
              )}
              {detail.budget != null && detail.budget > 0 && (
                <div className="row"><span className="muted">Budget</span><span>{formatMoney(detail.budget, detail.currency)}</span></div>
              )}
              {detail.buyerName && <div className="row"><span className="muted">Buyer</span><span>{detail.buyerName}</span></div>}
            </div>
          </section>

          <section className="card">
            <h2>Line items</h2>
            {(detail.items || []).length === 0 && <p className="sub">No line items on this RFQ.</p>}
            {(detail.items || []).map((item, index) => (
              <div className="line" key={item.id || item.buyerRFQItemId || index}>
                <div>
                  <div className="card-title" style={{ margin: 0 }}>{item.description || 'Item'}</div>
                  <div className="meta">{item.quantity ?? '—'} {item.uom} {item.materialCode ? `· ${item.materialCode}` : ''}</div>
                </div>
              </div>
            ))}
          </section>

          {session.role === 'buyer' && (
            <section className="card">
              <h2>Quotations</h2>
              {(detail.supplierQuotation || []).length === 0 && <p className="sub">No quotations yet.</p>}
              <div className="stack" style={{ marginTop: '0.6rem' }}>
                {(detail.supplierQuotation || []).map((quote, index) => (
                  <div key={quote.quotationId || quote.supplierId || index}>
                    <div className="row">
                      <strong>{quote.supplierName || 'Supplier'}</strong>
                      <span className="price">{formatMoney(quote.totalPrice, detail.currency)}</span>
                    </div>
                    <div className="meta">
                      {quote.status && <span className="badge">{titleCase(quote.status)}</span>}
                      {quote.isLead && <span className="badge lead">Lead</span>}
                      {quote.isAwarded && <span className="badge live">Awarded</span>}
                    </div>
                  </div>
                ))}
              </div>
              {(detail.supplierIds || []).length > 0 && (
                <p className="sub">Invited: {detail.supplierIds!.map((supplier) => supplier.supplierName).join(', ')}</p>
              )}
            </section>
          )}

          {session.role === 'supplier' && (
            <form className="card" onSubmit={onQuote}>
              <h2>Your quotation</h2>
              <p className="sub">Enter a unit price for each line. Totals use quantity × price, plus delivery and tax, minus discount.</p>
              {(detail.items || []).map((item, index) => {
                const key = item.id || item.buyerRFQItemId || `item-${index}`
                return (
                  <div className="line" key={key}>
                    <div>
                      <div>{item.description || 'Item'}</div>
                      <div className="meta">{item.quantity ?? 1} {item.uom}</div>
                    </div>
                    <input
                      inputMode="decimal"
                      aria-label={`Price for ${item.description || 'item'}`}
                      value={prices[key] || ''}
                      placeholder="0.00"
                      onChange={(event) => setPrices((current) => ({ ...current, [key]: event.target.value }))}
                    />
                  </div>
                )
              })}
              <div className="field" style={{ marginTop: '0.8rem' }}>
                <label htmlFor="delivery">Delivery charge</label>
                <input id="delivery" inputMode="decimal" value={delivery} onChange={(e) => setDelivery(e.target.value)} />
              </div>
              <div className="field">
                <label htmlFor="tax">Tax amount</label>
                <input id="tax" inputMode="decimal" value={tax} onChange={(e) => setTax(e.target.value)} />
              </div>
              <div className="field">
                <label htmlFor="discount">Discount amount</label>
                <input id="discount" inputMode="decimal" value={discount} onChange={(e) => setDiscount(e.target.value)} />
              </div>
              <div className="row" style={{ marginBottom: '0.8rem' }}>
                <span>Total</span>
                <strong>{formatMoney(total, detail.currency)}</strong>
              </div>
              {otpSent && !token && (
                <div className="field">
                  <label htmlFor="otp">Email code</label>
                  <input id="otp" inputMode="numeric" autoComplete="one-time-code" value={otp} onChange={(e) => setOtp(e.target.value)} />
                </div>
              )}
              <button className="btn" type="submit" disabled={busy}>
                {busy ? 'Working…' : token || !otpSent ? 'Submit quotation' : 'Verify and submit'}
              </button>
            </form>
          )}
        </div>
      )}
    </main>
  )
}
