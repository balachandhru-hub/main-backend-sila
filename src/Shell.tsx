import { NavLink, Navigate, Outlet } from 'react-router-dom'
import { useAuth } from './auth'
import { IconDoc, IconGrid, IconHome, IconUser } from './icons'

const links = [
  { to: '/home', label: 'Home', icon: <IconHome /> },
  { to: '/rfqs', label: 'RFQs', icon: <IconDoc /> },
  { to: '/catalog', label: 'Catalog', icon: <IconGrid /> },
  { to: '/profile', label: 'Profile', icon: <IconUser /> },
]

export default function Shell() {
  const { session, ready } = useAuth()
  if (!ready) {
    return (
      <div className="app-frame">
        <div className="loading"><div className="spinner" />Checking your session…</div>
      </div>
    )
  }
  if (!session) return <Navigate to="/" replace />

  const showWork = session.role === 'buyer' || session.role === 'supplier'
  const items = showWork ? links : links.filter((link) => link.to === '/home' || link.to === '/profile')

  return (
    <div className="app-frame">
      <Outlet />
      <nav className="nav" style={{ gridTemplateColumns: `repeat(${items.length}, 1fr)` }}>
        {items.map((link) => (
          <NavLink key={link.to} to={link.to} className={({ isActive }) => (isActive ? 'active' : undefined)}>
            {link.icon}
            {link.label}
          </NavLink>
        ))}
      </nav>
    </div>
  )
}
