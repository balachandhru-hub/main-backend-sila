import { createRoot } from 'react-dom/client';

import { setRouteSlug } from '@workspace/api-client-react';

import App from './App';
import { ErrorBoundary } from '@/components/error-boundary';

import { installTenantRouteFetch, resolveCustomerRouteSlug } from '@/lib/tenant-route';

import './index.css';

if (new URLSearchParams(window.location.search).get('embed') === '1') {
  sessionStorage.setItem('sila-embed', '1');
}
if (sessionStorage.getItem('sila-embed') === '1') {
  document.documentElement.classList.add('sila-embed');
}

installTenantRouteFetch();
setRouteSlug(resolveCustomerRouteSlug());

createRoot(document.getElementById('root')!, {
  // Keeps caught errors off reportError(), which would raise the dev overlay.
  onCaughtError: (error, errorInfo) => {
    console.error(error, errorInfo.componentStack);
  },
}).render(
  <ErrorBoundary>
    <App />
  </ErrorBoundary>,
);
