import { FormEvent, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { uploadInvoice } from '../api'

export default function InvoiceUploadPage() {
  const navigate = useNavigate()
  const [file, setFile] = useState<File | null>(null)
  const [supplierName, setSupplierName] = useState('')
  const [invoiceNumber, setInvoiceNumber] = useState('')
  const [poNumber, setPoNumber] = useState('')
  const [gross, setGross] = useState('')
  const [currency, setCurrency] = useState('AED')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  async function onSubmit(event: FormEvent) {
    event.preventDefault()
    if (!file) {
      setError('Choose an invoice photo or PDF.')
      return
    }
    setBusy(true)
    setError(null)
    try {
      const form = new FormData()
      form.set('file', file)
      form.set('sourceChannel', 'MOBILE_UPLOAD')
      form.set('pageCount', '1')
      if (supplierName.trim()) form.set('supplierName', supplierName.trim())
      if (invoiceNumber.trim()) form.set('supplierInvoiceNumber', invoiceNumber.trim())
      if (poNumber.trim()) form.set('purchaseOrderNumber', poNumber.trim())
      if (gross.trim()) form.set('invoiceGross', gross.trim())
      if (currency.trim()) form.set('currency', currency.trim())
      const saved = await uploadInvoice(form)
      navigate(saved.invoiceId ? `/invoices/${saved.invoiceId}` : '/invoices', { replace: true })
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Upload failed.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <main className="screen">
      <header className="topbar">
        <Link className="icon-btn" to="/invoices" aria-label="Back to invoices">←</Link>
        <div className="grow">
          <p className="page-kicker">Scan</p>
          <h1>Upload invoice</h1>
        </div>
      </header>
      <form className="card" onSubmit={onSubmit}>
        {error && <div className="alert">{error}</div>}
        <div className="field file">
          <label htmlFor="file">Photo or PDF</label>
          <input id="file" type="file" accept="image/*,application/pdf" capture="environment" onChange={(e) => setFile(e.target.files?.[0] ?? null)} />
        </div>
        <div className="field">
          <label htmlFor="supplier">Supplier</label>
          <input id="supplier" value={supplierName} onChange={(e) => setSupplierName(e.target.value)} />
        </div>
        <div className="field">
          <label htmlFor="number">Invoice number</label>
          <input id="number" value={invoiceNumber} onChange={(e) => setInvoiceNumber(e.target.value)} />
        </div>
        <div className="field">
          <label htmlFor="po">Purchase order</label>
          <input id="po" value={poNumber} onChange={(e) => setPoNumber(e.target.value)} />
        </div>
        <div className="field">
          <label htmlFor="gross">Gross amount</label>
          <input id="gross" inputMode="decimal" value={gross} onChange={(e) => setGross(e.target.value)} />
        </div>
        <div className="field">
          <label htmlFor="currency">Currency</label>
          <input id="currency" value={currency} onChange={(e) => setCurrency(e.target.value)} />
        </div>
        <button className="btn" type="submit" disabled={busy}>{busy ? 'Uploading…' : 'Send to this backend'}</button>
      </form>
    </main>
  )
}
