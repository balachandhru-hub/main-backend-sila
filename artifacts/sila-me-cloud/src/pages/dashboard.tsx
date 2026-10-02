import { Activity, ArrowRight, ArrowUpRight, BarChart3, ChefHat, ClipboardList, PackageCheck, ReceiptText, ShieldCheck, ShoppingCart, Warehouse } from 'lucide-react';
import { Link } from 'wouter';
import { getGetGrnsQueryKey, getGetInvoicesQueryKey, getHealthCheckQueryKey, useGetGrns, useGetInvoices, useHealthCheck } from '@workspace/api-client-react';
import { SilaPageHeader, StatusBadge, QueryError } from '@/components/sila-ui';
import { useAccessContext } from '@/components/sila-layout';
import { formatDate, formatMoney } from '@/lib/formatters';

const request = { credentials: 'include' as const };

export default function Dashboard() {
  const { accessContext } = useAccessContext();
  const health = useHealthCheck({ request, query: { queryKey: getHealthCheckQueryKey(), refetchInterval: 30000, retry: 1 } });
  const invoices = useGetInvoices({ request, query: { queryKey: getGetInvoicesQueryKey(), retry: false } });
  const grns = useGetGrns({ request, query: { queryKey: getGetGrnsQueryKey(), retry: false } });
  const invoiceRows = invoices.data ?? [];
  const grnRows = grns.data ?? [];
  const firstName = accessContext?.user.displayName.split(' ')[0] ?? 'operator';
  return (
    <>
      <SilaPageHeader eyebrow="Operations cockpit" title={`Good morning, ${firstName}`} description="A focused view of what needs attention across receiving, invoice control, and access." actions={<span className={`sila-status ${health.isError ? 'sila-status--bad' : 'sila-status--good'}`} data-testid="status-backend-health"><Activity size={12} />{health.isPending ? 'Checking connection' : health.isError ? 'Connection unavailable' : 'Backend connected'}</span>} />
      {health.isError && <QueryError onRetry={() => health.refetch()} />}
      <div className="sila-kpi-grid" data-testid="dashboard-kpis">
        <article className="sila-card sila-kpi"><span className="sila-kpi__label">Invoices in control</span><strong className="sila-kpi__value" data-testid="metric-invoice-count">{invoices.isPending ? '—' : invoiceRows.length}</strong><span className="sila-kpi__note">Captured and routed for action</span></article>
        <article className="sila-card sila-kpi"><span className="sila-kpi__label">Ready for GRN</span><strong className="sila-kpi__value" data-testid="metric-ready-grn">{invoices.isPending ? '—' : invoiceRows.filter((invoice) => invoice.status === 'READY_FOR_GRN').length}</strong><span className="sila-kpi__note">Material invoices awaiting receipt</span></article>
        <article className="sila-card sila-kpi"><span className="sila-kpi__label">Goods receipts</span><strong className="sila-kpi__value" data-testid="metric-grn-count">{grns.isPending ? '—' : grnRows.length}</strong><span className="sila-kpi__note">Receipts recorded in Cloud</span></article>
        <article className="sila-card sila-kpi"><span className="sila-kpi__label">Access scope</span><strong className="sila-kpi__value" data-testid="metric-unit-count">{accessContext?.units.length ?? '—'}</strong><span className="sila-kpi__note">Operating units in context</span></article>
      </div>
      <div className="sila-detail-grid">
        <section className="sila-card sila-detail-section" data-testid="dashboard-invoice-queue">
          <h2 className="sila-detail-section__title"><span style={{ color: 'var(--sila-ink)', fontSize: 14, fontWeight: 700 }}><ReceiptText size={16} style={{ verticalAlign: 'middle', marginRight: 8 }} />Invoice control queue</span><Link className="sila-button sila-button--quiet" href="/receiving/invoices" data-testid="link-dashboard-invoices">View all <ArrowUpRight size={14} /></Link></h2>
          {invoices.isPending ? <div className="sila-loading-row" /> : <div className="sila-side-list">{invoiceRows.slice(0, 4).map((invoice) => <div className="sila-side-list__row" key={invoice.id}><div><Link className="sila-table__primary" href={`/receiving/invoices/${invoice.id}`} data-testid={`link-dashboard-invoice-${invoice.id}`}>{invoice.invoiceNumber}</Link><span className="sila-table__secondary">{invoice.supplierName ?? 'Supplier pending'} · {formatDate(invoice.invoiceDate)}</span></div><StatusBadge value={invoice.status} /></div>)}{!invoiceRows.length && <p style={{ color: 'var(--sila-muted)', fontSize: 12 }} data-testid="text-dashboard-invoice-empty">No invoices uploaded yet.</p>}</div>}
        </section>
        <section className="sila-card sila-detail-section" data-testid="dashboard-receiving-queue">
          <h2 className="sila-detail-section__title"><span style={{ color: 'var(--sila-ink)', fontSize: 14, fontWeight: 700 }}><PackageCheck size={16} style={{ verticalAlign: 'middle', marginRight: 8 }} />Receiving activity</span><Link className="sila-button sila-button--quiet" href="/receiving/goods-receipts" data-testid="link-dashboard-grns">View all <ArrowUpRight size={14} /></Link></h2>
          {grns.isPending ? <div className="sila-loading-row" /> : <div className="sila-side-list">{grnRows.slice(0, 4).map((grn) => <div className="sila-side-list__row" key={grn.id}><div><Link className="sila-table__primary" href={`/receiving/goods-receipts/${grn.id}`} data-testid={`link-dashboard-grn-${grn.id}`}>{grn.grnNumber}</Link><span className="sila-table__secondary">{grn.supplierName} · {formatDate(grn.receiptDate)}</span></div><StatusBadge value={grn.status} /></div>)}{!grnRows.length && <p style={{ color: 'var(--sila-muted)', fontSize: 12 }} data-testid="text-dashboard-grn-empty">No receipts posted yet.</p>}</div>}
        </section>
      </div>
      <section className="sila-card sila-detail-section" style={{ marginTop: 18 }} data-testid="dashboard-operating-signal">
        <h2 className="sila-detail-section__title"><span style={{ color: 'var(--sila-ink)', fontSize: 14, fontWeight: 700 }}><ShieldCheck size={16} style={{ verticalAlign: 'middle', marginRight: 8 }} />Control-room signal</span><span>{health.data?.status ?? 'Monitoring'}</span></h2>
        <div className="sila-meta-grid"><div><span className="sila-meta-label">Session</span><strong className="sila-meta-value" data-testid="text-session-status">Authenticated</strong></div><div><span className="sila-meta-label">Organizations</span><strong className="sila-meta-value" data-testid="text-organization-count">{accessContext?.organizations.length ?? '—'}</strong></div><div><span className="sila-meta-label">Invoice exposure</span><strong className="sila-meta-value" data-testid="text-invoice-exposure">{formatMoney(invoiceRows.reduce((total, invoice) => total + (invoice.grossAmount ?? 0), 0), invoiceRows[0]?.currency ?? 'AED')}</strong></div></div>
      </section>
      <section className="sila-card sila-flow-card" data-testid="dashboard-operational-flow">
        <div className="sila-flow-card__heading">
          <div><p className="sila-page-header__eyebrow">The operating chain</p><h2>From plan to proof</h2><p>Follow the handoff across the operation. Connected stages use live Cloud records; future stages stay explicitly unclaimed.</p></div>
          <span className="sila-status sila-status--blue" data-testid="status-flow-context">{accessContext?.units.length ? `${accessContext.units.length} units in scope` : 'Operating context'}</span>
        </div>
        <div className="sila-flow" data-testid="dashboard-flow-stages">
          {[
            { label: 'Menu plan', note: 'API integration pending', href: '/menu-engineering/menu-planning', icon: ChefHat, pending: true },
            { label: 'Requirements', note: 'API integration pending', href: '/purchasing/requirements', icon: ClipboardList, pending: true },
            { label: 'Stock', note: 'Inventory Control Center', href: '/inventory/dashboard', icon: Warehouse, pending: false },
            { label: 'Purchase orders', note: 'Connected page', href: '/purchasing/purchase-orders', icon: ShoppingCart, pending: false },
            { label: 'Receiving', note: `${grnRows.length} live receipts`, href: '/receiving/goods-receipts', icon: PackageCheck, pending: false },
            { label: 'Analytics', note: 'API integration pending', href: '/analytics', icon: BarChart3, pending: true },
          ].map((stage, index, stages) => {
            const Icon = stage.icon;
            return <div className="sila-flow__item" key={stage.href}>
              <Link href={stage.href} className={`sila-flow__stage ${stage.pending ? 'sila-flow__stage--pending' : ''}`} data-testid={`link-flow-stage-${stage.label.toLowerCase().replace(/[^a-z0-9]+/g, '-')}`}>
                <span className="sila-flow__icon"><Icon size={16} /></span><strong data-testid={`text-flow-stage-${index}`}>{stage.label}</strong><span className="sila-flow__note" data-testid={`status-flow-stage-${index}`}>{stage.note}</span>
              </Link>
              {index < stages.length - 1 && <ArrowRight className="sila-flow__arrow" size={15} />}
            </div>;
          })}
        </div>
      </section>
      <section className="sila-card sila-detail-section sila-action-strip" data-testid="dashboard-quick-actions">
        <div><p className="sila-page-header__eyebrow">Next moves</p><h2>Keep the shift moving</h2></div>
        <div className="sila-action-links">
          <Link href="/receiving/invoices" className="sila-button sila-button--primary" data-testid="link-dashboard-action-invoices"><ReceiptText size={14} /> Review invoices</Link>
          <Link href="/receiving/goods-receipts" className="sila-button" data-testid="link-dashboard-action-grns"><PackageCheck size={14} /> Post a receipt</Link>
          <Link href="/inventory/dashboard" className="sila-button" data-testid="link-dashboard-action-inventory"><Warehouse size={14} /> Inventory Control Center</Link>
          <Link href="/purchasing/purchase-orders" className="sila-button" data-testid="link-dashboard-action-purchase-orders"><ShoppingCart size={14} /> Open purchase orders</Link>
        </div>
      </section>
    </>
  );
}