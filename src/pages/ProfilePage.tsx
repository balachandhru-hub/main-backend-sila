import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { getBuyerProfile, getPerson, getSupplierProfile, type OrgProfile, type PersonDetail } from '../api'
import { useAuth } from '../auth'
import { titleCase } from '../format'

export default function ProfilePage() {
  const { session, signOut } = useAuth()
  const navigate = useNavigate()
  const [person, setPerson] = useState<PersonDetail | null>(null)
  const [org, setOrg] = useState<OrgProfile | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    if (!session) return
    let active = true
    getPerson().then((value) => { if (active) setPerson(value) }).catch(() => undefined)
    const profile = session.role === 'buyer'
      ? getBuyerProfile()
      : session.role === 'supplier'
        ? getSupplierProfile()
        : Promise.resolve(null)
    profile
      .then((value) => { if (active) setOrg(value) })
      .catch((err: unknown) => { if (active) setError(err instanceof Error ? err.message : 'Could not load the profile.') })
    return () => { active = false }
  }, [session])

  if (!session) return null
  const business = org?.businessProfile
  const address = [business?.addressLine1, business?.city, business?.state, business?.pinCode, business?.country]
    .filter(Boolean)
    .join(', ')

  return (
    <main className="screen">
      <header className="topbar">
        <img src="/sila-logo.png" alt="" />
        <div className="grow">
          <p className="page-kicker">Account</p>
          <h1>{person?.name || session.roleLabel}</h1>
        </div>
      </header>
      {error && <div className="alert">{error}</div>}
      <section className="card stack">
        <div className="row"><span className="muted">Role</span><span>{person?.roleName || session.roleLabel}</span></div>
        <div className="row"><span className="muted">Username</span><span>{person?.userName || '—'}</span></div>
        <div className="row"><span className="muted">Email</span><span>{person?.email || business?.email || '—'}</span></div>
        <div className="row"><span className="muted">Phone</span><span>{person?.phone || business?.phone || '—'}</span></div>
      </section>
      {business && (
        <section className="card stack" style={{ marginTop: '0.75rem' }}>
          <h2>{business.organizationName || person?.organizationName || 'Organization'}</h2>
          {business.status && <span className="badge">{titleCase(business.status)}</span>}
          {address && <p className="sub">{address}</p>}
          {business.industry && <div className="row"><span className="muted">Industry</span><span>{business.industry}</span></div>}
          {business.website && <div className="row"><span className="muted">Website</span><span>{business.website}</span></div>}
          {business.description && <p className="sub">{business.description}</p>}
        </section>
      )}
      <button
        className="btn ghost"
        style={{ marginTop: '1rem' }}
        type="button"
        disabled={busy}
        onClick={() => {
          setBusy(true)
          signOut().finally(() => navigate('/', { replace: true }))
        }}
      >
        Sign out
      </button>
    </main>
  )
}
