import { FileQuestion, LoaderCircle } from 'lucide-react';
import type { ReactNode } from 'react';
import { humanizeStatus, statusTone } from '@/lib/formatters';

export function StatusBadge({ value }: { value?: string | null }) {
  return <span className={`sila-status sila-status--${statusTone(value)}`} data-testid={`status-badge-${value ?? 'unknown'}`}>{humanizeStatus(value)}</span>;
}

export function SilaPageHeader({ eyebrow = 'SILA ME Cloud', title, description, actions }: { eyebrow?: string; title: string; description?: string; actions?: ReactNode }) {
  return (
    <div className="sila-page-header" data-testid="page-header">
      <div className="sila-page-header__copy">
        <p className="sila-page-header__eyebrow">{eyebrow}</p>
        <h1 data-testid="text-page-title">{title}</h1>
        {description && <p data-testid="text-page-description">{description}</p>}
      </div>
      {actions && <div className="sila-page-header__actions" data-testid="page-header-actions">{actions}</div>}
    </div>
  );
}

export function SilaDataTable({ children, loading = false, empty = false, emptyTitle = 'No records found', emptyDescription = 'There is nothing to show for this context yet.' }: { children?: ReactNode; loading?: boolean; empty?: boolean; emptyTitle?: string; emptyDescription?: string }) {
  if (loading) return <div className="sila-card sila-table-wrap" data-testid="table-loading"><div className="sila-loading-row" /><div className="sila-loading-row" /><div className="sila-loading-row" /><div className="sila-loading-row" /></div>;
  if (empty) return <EmptyState title={emptyTitle} description={emptyDescription} />;
  return <div className="sila-card sila-table-wrap" data-testid="data-table">{children}</div>;
}

export function EmptyState({ title, description, action }: { title: string; description: string; action?: ReactNode }) {
  return (
    <div className="sila-card sila-empty" data-testid="empty-state">
      <div className="sila-empty__icon"><FileQuestion size={19} /></div>
      <h3 data-testid="text-empty-title">{title}</h3>
      <p data-testid="text-empty-description">{description}</p>
      {action && <div style={{ marginTop: 16 }}>{action}</div>}
    </div>
  );
}

export function QueryError({ onRetry }: { onRetry?: () => void }) {
  return (
    <div className="sila-card sila-empty" data-testid="error-state">
      <div className="sila-empty__icon"><FileQuestion size={19} /></div>
      <h3>We could not load this workspace view</h3>
      <p>The session may have expired or the service is temporarily unavailable.</p>
      {onRetry && <button className="sila-button sila-button--primary" type="button" onClick={onRetry} data-testid="button-retry-query"><LoaderCircle size={14} /> Try again</button>}
    </div>
  );
}

export function DevPendingMarker() {
  return import.meta.env.DEV ? <span className="sila-dev-marker" data-testid="status-api-pending">API integration pending</span> : null;
}