import { NavLink, Navigate, Outlet } from 'react-router-dom'
import { useAuth } from './auth'
import { IconDoc, IconGrid, IconHome, IconUser } from './icons'

const links = [
  { to: '/home', label: 'Home', icon: <IconHome /> },
  { to: '/invoices', label: 'Invoices', icon: <IconDoc /> },
  { to: '/receive', label: 'GRN', icon: <IconGrid /> },
  { to: '/orders', label: 'POs', icon: <IconUser /> },
]

export default function Shell() {
  const { session, ready } = useAuth()
  if (!ready) {
    return (
      <div className="app-frame">
        <div className="loading"><div className="spinner" />Checking the SILA gateway…</div>
      </div>
    )
  }
  if (!session) return <Navigate to="/" replace />

  return (
    <div className="app-frame">
      <Outlet />
      <nav className="nav">
        {links.map((link) => (
          <NavLink key={link.to} to={link.to} className={({ isActive }) => (isActive ? 'active' : undefined)}>
            {link.icon}
            {link.label}
          </NavLink>
        ))}
      </nav>
    </div>
  )
}
