import { Navigate, Route, Routes } from 'react-router-dom'
import { useAuth } from './auth'
import Shell from './Shell'
import LoginPage from './pages/LoginPage'
import HomePage from './pages/HomePage'
import InvoicesPage from './pages/InvoicesPage'
import InvoiceUploadPage from './pages/InvoiceUploadPage'
import InvoiceDetailPage from './pages/InvoiceDetailPage'
import ReceivePage from './pages/ReceivePage'
import OrdersPage from './pages/OrdersPage'

export default function App() {
  const { session, ready } = useAuth()

  return (
    <Routes>
      <Route
        path="/"
        element={
          !ready ? (
            <div className="app-frame"><div className="loading"><div className="spinner" />Checking the SILA gateway…</div></div>
          ) : session ? (
            <Navigate to="/home" replace />
          ) : (
            <LoginPage />
          )
        }
      />
      <Route element={<Shell />}>
        <Route path="/home" element={<HomePage />} />
        <Route path="/invoices" element={<InvoicesPage />} />
        <Route path="/invoices/upload" element={<InvoiceUploadPage />} />
        <Route path="/invoices/:invoiceId" element={<InvoiceDetailPage />} />
        <Route path="/receive" element={<ReceivePage />} />
        <Route path="/orders" element={<OrdersPage />} />
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}
