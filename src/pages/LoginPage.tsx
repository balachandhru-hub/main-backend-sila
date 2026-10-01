import { FormEvent, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { ApiError } from '../api'
import { useAuth } from '../auth'

export default function LoginPage() {
  const { signIn } = useAuth()
  const navigate = useNavigate()
  const [userName, setUserName] = useState('')
  const [password, setPassword] = useState('')
  const [show, setShow] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  async function onSubmit(event: FormEvent) {
    event.preventDefault()
    setError(null)
    if (!userName.trim() || !password) {
      setError('Username and password are required.')
      return
    }
    setBusy(true)
    try {
      await signIn(userName.trim(), password)
      navigate('/home', { replace: true })
    } catch (err) {
      setError(err instanceof ApiError || err instanceof Error ? err.message : 'Sign-in failed.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="app-frame">
      <main className="screen solo">
        <div className="brand-bar">
          <img src="/sila-logo.png" alt="SILA" />
        </div>
        <section className="hero">
          <div className="kicker">SILA Strategic Procurement Suite</div>
          <h1 style={{ color: 'white', marginTop: '0.45rem' }}>Sourcing, on your phone</h1>
          <p>The same buyer and supplier portal, laid out for a small screen. Sign in with your SILA account.</p>
        </section>
        <form className="card" onSubmit={onSubmit}>
          <h2>Sign in</h2>
          <p className="sub" style={{ marginBottom: '1rem' }}>Use the username from the desktop portal.</p>
          {error && <div className="alert">{error}</div>}
          <div className="field">
            <label htmlFor="user">Username</label>
            <input id="user" autoComplete="username" value={userName} onChange={(e) => setUserName(e.target.value)} />
          </div>
          <div className="field">
            <label htmlFor="pass">Password</label>
            <input
              id="pass"
              type={show ? 'text' : 'password'}
              autoComplete="current-password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
            />
          </div>
          <button type="button" className="text-btn" onClick={() => setShow((value) => !value)} style={{ marginBottom: '0.9rem' }}>
            {show ? 'Hide password' : 'Show password'}
          </button>
          <button className="btn" type="submit" disabled={busy}>{busy ? 'Signing in…' : 'Sign in'}</button>
        </form>
      </main>
    </div>
  )
}
