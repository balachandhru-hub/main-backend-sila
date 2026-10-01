import { Navigate, Route, Routes } from 'react-router-dom'
import { useAuth } from './auth'
import Shell from './Shell'
import LoginPage from './pages/LoginPage'
import HomePage from './pages/HomePage'
import RfqListPage from './pages/RfqListPage'
import RfqDetailPage from './pages/RfqDetailPage'
import CatalogPage from './pages/CatalogPage'
import ProfilePage from './pages/ProfilePage'

export default function App() {
  const { session, ready } = useAuth()

  return (
    <Routes>
      <Route
        path="/"
        element={
          !ready ? (
            <div className="app-frame"><div className="loading"><div className="spinner" />Checking your session…</div></div>
          ) : session ? (
            <Navigate to="/home" replace />
          ) : (
            <LoginPage />
          )
        }
      />
      <Route element={<Shell />}>
        <Route path="/home" element={<HomePage />} />
        <Route path="/rfqs" element={<RfqListPage />} />
        <Route path="/rfqs/:rfqId" element={<RfqDetailPage />} />
        <Route path="/catalog" element={<CatalogPage />} />
        <Route path="/profile" element={<ProfilePage />} />
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}
