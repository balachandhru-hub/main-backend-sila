import { Bell, ChevronDown, ChevronLeft, CircleHelp, LayoutDashboard, Menu, PanelLeftClose, PanelLeftOpen, Settings2, ShieldCheck, Users, Warehouse, ReceiptText, Truck, FileText, ClipboardCheck, BarChart3, Database, ChefHat, ShoppingCart, ClipboardList, PackageCheck, Boxes, X, SlidersHorizontal, UtensilsCrossed, ListChecks } from 'lucide-react';
import { useCallback, useEffect, useMemo, useState, createContext, useContext, type ReactNode } from 'react';
import { Link, useLocation } from 'wouter';
import { useQueryClient } from '@tanstack/react-query';
import { getGetAccessContextQueryKey, getGetCloudSessionQueryKey, getGetCurrentUserQueryKey, setUnauthorizedHandler, useCloudLogout, useGetAccessContext, useGetCloudSession } from '@workspace/api-client-react';
import type { AccessContext } from '@workspace/api-client-react';
import { CustomerBrandMark, SilaPoweredMark } from '@/components/branding';
import { hasPermission } from '@/lib/permissions';

type AccessValue = { accessContext?: AccessContext; accessPending: boolean };
const AccessContextStore = createContext<AccessValue>({ accessPending: true });
export function useAccessContext() { return useContext(AccessContextStore); }

type NavItem = { label: string; href: string; icon: typeof LayoutDashboard; permission?: string };
const navigation: Array<{ label: string; items: NavItem[] }> = [
  { label: 'Workspace', items: [{ label: 'Dashboard', href: '/dashboard', icon: LayoutDashboard }] },
  { label: 'Recipe Management', items: [
    { label: 'Dashboard', href: '/recipe-management/dashboard', icon: LayoutDashboard },
    { label: 'Recipes', href: '/recipe-management/recipes', icon: UtensilsCrossed },
    { label: 'Recipe Approvals', href: '/recipe-management/approvals', icon: ClipboardCheck },
    { label: 'POS Integration', href: '/recipe-management/pos', icon: Settings2 },
    { label: 'Transaction Tracker', href: '/recipe-management/transactions', icon: ListChecks },
  ] },
  { label: 'Menu Engineering', items: [
    { label: 'Menu planning', href: '/menu-engineering/menu-planning', icon: ChefHat }, { label: 'Recipes', href: '/menu-engineering/recipes', icon: ClipboardList }, { label: 'Ingredients', href: '/menu-engineering/ingredients', icon: Boxes }, { label: 'Portion planning', href: '/menu-engineering/portion-planning', icon: PackageCheck }, { label: 'Demand forecast', href: '/menu-engineering/demand-forecast', icon: BarChart3 }, { label: 'Menu performance', href: '/menu-engineering/menu-performance', icon: BarChart3 },
  ] },
  { label: 'Inventory', items: [
    { label: 'Dashboard', href: '/inventory/dashboard', icon: LayoutDashboard },
    { label: 'Live Inventory', href: '/inventory/live', icon: Warehouse },
    { label: 'Internal Transfers', href: '/inventory/transfers', icon: Truck },
    { label: 'Goods Receipt', href: '/inventory/goods-receipt', icon: PackageCheck },
    { label: 'Goods Issue', href: '/inventory/goods-issue', icon: ReceiptText },
    { label: 'Stock Count', href: '/inventory/count', icon: ClipboardCheck },
    { label: 'Shortage & Enquiries', href: '/inventory/shortages', icon: CircleHelp },
    { label: 'Waste & Damage', href: '/inventory/damage-waste', icon: X },
    { label: 'Inventory Transactions', href: '/inventory/transactions', icon: ListChecks },
  ] },
  { label: 'Receiving', items: [
    { label: 'Receive', href: '/receiving/receive', icon: PackageCheck }, { label: 'Invoices', href: '/receiving/invoices', icon: ReceiptText }, { label: 'Goods receipts', href: '/receiving/goods-receipts', icon: ClipboardCheck }, { label: 'Exceptions', href: '/receiving/exceptions', icon: CircleHelp },
  ] },
  { label: 'Purchasing', items: [
    { label: 'Requirements', href: '/purchasing/requirements', icon: ClipboardList }, { label: 'Suggestions', href: '/purchasing/suggestions', icon: ShoppingCart }, { label: 'Purchase orders', href: '/purchasing/purchase-orders', icon: Truck },
  ] },
  { label: 'Documents', items: [
    { label: 'Inbox', href: '/documents/inbox', icon: FileText }, { label: 'Invoice capture', href: '/documents/invoice-capture', icon: ReceiptText }, { label: 'Extraction', href: '/documents/extraction', icon: Database }, { label: 'Archive', href: '/documents/archive', icon: FileText },
  ] },
  { label: 'Control', items: [{ label: 'Approvals', href: '/approvals', icon: ShieldCheck }, { label: 'Analytics', href: '/analytics', icon: BarChart3 }] },
  { label: 'Administration', items: [
    { label: 'Users', href: '/admin/users', icon: Users, permission: 'user' },
    { label: 'Roles', href: '/admin/roles', icon: ShieldCheck, permission: 'role' },
    { label: 'Organization', href: '/admin/organization', icon: Warehouse, permission: 'organization' },
    { label: 'Location Master', href: '/admin/locations', icon: Warehouse },
    { label: 'Material Master', href: '/admin/materials', icon: PackageCheck },
    { label: 'Recipe Master Data', href: '/admin/recipe-master-data', icon: Database },
    { label: 'Company codes', href: '/admin/company-codes', icon: Database },
    { label: 'Properties', href: '/admin/properties', icon: Warehouse, permission: 'organization' },
    { label: 'Suppliers', href: '/admin/suppliers', icon: Users },
    { label: 'Workflows & configuration', href: '/admin/configuration', icon: SlidersHorizontal, permission: 'configuration' },
    { label: 'Integrations', href: '/admin/integrations', icon: Settings2, permission: 'integration' },
    { label: 'Document extraction', href: '/admin/document-extraction', icon: Database, permission: 'extraction' },
    { label: 'Licenses', href: '/admin/licenses', icon: ShieldCheck, permission: 'user' },
    { label: 'Audit', href: '/admin/audit', icon: FileText, permission: 'audit' },
  ] },
];

function initials(displayName: string) {
  return displayName.split(' ').filter(Boolean).slice(0, 2).map((part) => part[0]).join('').toUpperCase();
}

function isUnauthorizedError(error: unknown) {
  return Boolean(error && typeof error === 'object' && 'status' in error && (error as { status?: unknown }).status === 401);
}

export function SilaCloudLayout({ children }: { children: ReactNode }) {
  const [, setLocation] = useLocation();
  const [location] = useLocation();
  const [collapsed, setCollapsed] = useState(false);
  const [openGroups, setOpenGroups] = useState<Record<string, boolean>>({});
  const [mobileOpen, setMobileOpen] = useState(false);
  const [isMobile, setIsMobile] = useState(false);
  const [userMenuOpen, setUserMenuOpen] = useState(false);
  const [headerMessage, setHeaderMessage] = useState('');
  const queryClient = useQueryClient();
  const session = useGetCloudSession({
    request: { credentials: 'include' },
    query: {
      queryKey: getGetCloudSessionQueryKey(),
      retry: (count, error) => (isUnauthorizedError(error) ? false : count < 3),
      staleTime: 5 * 60 * 1000,
    },
  });
  const access = useGetAccessContext({ request: { credentials: 'include' }, query: { queryKey: getGetAccessContextQueryKey(), retry: false, enabled: Boolean(session.data?.user) } });
  const logout = useCloudLogout({ request: { credentials: 'include' } });

  const handleSessionExpired = useCallback(() => {
    if (location === '/login' || window.location.pathname.endsWith('/login')) return;

    queryClient.removeQueries({ queryKey: getGetAccessContextQueryKey() });
    queryClient.removeQueries({ queryKey: getGetCurrentUserQueryKey() });
    queryClient.removeQueries({ queryKey: getGetCloudSessionQueryKey() });
    const returnTo = `${location}${window.location.search}${window.location.hash}`;
    setLocation(`/login?sessionExpired=true&returnTo=${encodeURIComponent(returnTo)}`);
  }, [location, queryClient, setLocation]);

  useEffect(() => {
    setUnauthorizedHandler(handleSessionExpired);
    return () => setUnauthorizedHandler(null);
  }, [handleSessionExpired]);

  useEffect(() => {
    if (!session.isError || !isUnauthorizedError(session.error)) return;
    handleSessionExpired();
  }, [handleSessionExpired, session.error, session.isError]);

  useEffect(() => setMobileOpen(false), [location]);
  useEffect(() => {
    const stored = window.localStorage.getItem('sila-cloud-sidebar-collapsed');
    if (stored) setCollapsed(stored === 'true');
    const storedGroups = window.localStorage.getItem('sila-cloud-sidebar-groups');
    if (storedGroups) {
      try { setOpenGroups(JSON.parse(storedGroups) as Record<string, boolean>); } catch { /* reset invalid local state */ }
    }
  }, []);
  useEffect(() => {
    window.localStorage.setItem('sila-cloud-sidebar-collapsed', String(collapsed));
  }, [collapsed]);
  useEffect(() => {
    window.localStorage.setItem('sila-cloud-sidebar-groups', JSON.stringify(openGroups));
  }, [openGroups]);
  useEffect(() => {
    const updateViewport = () => setIsMobile(window.innerWidth <= 760);
    updateViewport();
    window.addEventListener('resize', updateViewport);
    return () => window.removeEventListener('resize', updateViewport);
  }, []);

  const adminAllowed = useMemo(() => hasPermission(access.data?.permissions, ['user', 'role', 'organization', 'administration', 'admin']), [access.data?.permissions]);
  const visibleNavigation = navigation.map((section) => ({
    ...section,
    items: section.label === 'Administration' && !adminAllowed ? [] : section.items.filter((item) => !item.permission || hasPermission(access.data?.permissions, item.permission)),
  })).filter((section) => section.items.length > 0);

  const returnToPlatform = async () => {
    await fetch('/api/auth/support/end', { method: 'POST', credentials: 'include' });
    queryClient.removeQueries({ queryKey: getGetCloudSessionQueryKey() });
    queryClient.removeQueries({ queryKey: getGetCurrentUserQueryKey() });
    queryClient.removeQueries({ queryKey: getGetAccessContextQueryKey() });
    window.location.assign('/organizations');
  };
  const signOut = () => logout.mutate(undefined, { onSuccess: () => { queryClient.removeQueries({ queryKey: getGetCloudSessionQueryKey() }); queryClient.removeQueries({ queryKey: getGetCurrentUserQueryKey() }); queryClient.removeQueries({ queryKey: getGetAccessContextQueryKey() }); setLocation('/login'); } });
  if (session.isPending) return <div className="dashboard-skeleton" data-testid="loading-session"><div className="skeleton-block skeleton-block--small" /><div className="skeleton-block skeleton-block--title" /><div className="skeleton-grid"><div className="skeleton-block" /><div className="skeleton-block" /></div></div>;
  if (session.isError || !session.data?.user) return null;
  const user = session.data.user as typeof session.data.user & {
    supportSession?: boolean;
    supportUserDisplayName?: string | null;
    environment?: string | null;
    tenantCode?: string | null;
    customerName?: string | null;
    productEntitlements?: string[] | null;
  };
  const organizations = access.data?.organizations ?? [];
  const isTestEnvironment = (user.environment || '').toUpperCase() === 'TEST' || (user.environment || '').toUpperCase() === 'DEVELOPMENT';
  const organizationLabel = user.customerName || user.tenantCode || organizations[0]?.name || 'Customer';

  return (
    <AccessContextStore.Provider value={{ accessContext: access.data, accessPending: access.isPending }}>
      <div className="sila-app">
        <div className="sila-shell">
          <aside className={`sila-sidebar ${collapsed ? 'sila-sidebar--collapsed' : ''} ${mobileOpen ? 'sila-sidebar--mobile-open' : ''}`} data-testid="sidebar-navigation">
            <div className="sila-sidebar__brand">
              <CustomerBrandMark
                compact
                organizationId={organizations[0]?.id}
                logoUrl={organizations[0]?.logoUrl}
                organizationName={organizations[0]?.name}
              />
            </div>
            <nav className="sila-sidebar__nav" aria-label="Cloud navigation">
              {visibleNavigation.map((section) => {
                const isOpen = openGroups[section.label] ?? true;
                return <div className="sila-sidebar__section" key={section.label}>
                <button type="button" className="sila-sidebar__section-label" onClick={() => setOpenGroups((state) => ({ ...state, [section.label]: !isOpen }))} data-testid={`button-toggle-nav-group-${section.label.toLowerCase().replace(/[^a-z0-9]+/g, '-')}`}><span>{section.label}</span><ChevronDown size={12} /></button>
                {isOpen && section.items.map((item) => {
                  const active = location === item.href || (item.href !== '/dashboard' && location.startsWith(`${item.href}/`));
                  const Icon = item.icon;
                  return <Link href={item.href} className={`sila-nav-item ${active ? 'sila-nav-item--active' : ''}`} key={item.href} data-testid={`link-nav-${item.label.toLowerCase().replace(/[^a-z0-9]+/g, '-')}`} onClick={() => setMobileOpen(false)}><Icon className="sila-nav-item__icon" size={16} /><span className="sila-nav-item__label">{item.label}</span></Link>;
                })}
              </div>})}
            </nav>
            <div className="sila-sidebar__footer">
              <SilaPoweredMark compact />
              <div className="sila-sidebar__status" data-testid="status-session"><span className="sila-sidebar__status-dot" /><div><strong>Cloud session</strong><span>Secure and active</span></div></div>
              <button type="button" className="sila-nav-item" onClick={signOut} disabled={logout.isPending} data-testid="button-sidebar-sign-out"><ChevronLeft size={16} /><span className="sila-nav-item__label">{logout.isPending ? 'Signing out' : 'Sign out'}</span></button>
            </div>
          </aside>
          {mobileOpen && <button className="sila-mobile-nav" type="button" aria-label="Close navigation" onClick={() => setMobileOpen(false)} data-testid="button-close-mobile-navigation" />}
          <section className="sila-main">
            {user.supportSession ? (
              <div style={{ background: isTestEnvironment ? '#eff6ff' : '#7f1d1d', borderBottom: '1px solid var(--sila-line)', color: isTestEnvironment ? 'var(--sila-ink)' : '#fff', padding: '10px 16px', fontSize: 13, display: 'flex', justifyContent: 'space-between', gap: 12, alignItems: 'center' }} data-testid="banner-support-session">
                <div>
                  <strong>SILA SUPPORT SESSION</strong>
                  <div>{organizationLabel}</div>
                  <div style={{ fontWeight: 800, letterSpacing: '.04em' }}>{isTestEnvironment ? 'TEST' : 'PRODUCTION'}</div>
                  <div>Signed in via SILA Platform</div>
                </div>
                <button className="sila-button" type="button" onClick={() => void returnToPlatform()} data-testid="button-return-sila-cloud">RETURN TO SILA CLOUD</button>
              </div>
            ) : (
              <div style={{ background: isTestEnvironment ? '#fff7ed' : '#7f1d1d', borderBottom: '1px solid var(--sila-line)', color: isTestEnvironment ? 'var(--sila-ink)' : '#fff', padding: '8px 16px', fontSize: 13, fontWeight: 700 }} data-testid="banner-customer-context">
                {organizationLabel} · {isTestEnvironment ? 'TEST' : 'PRODUCTION'}
              </div>
            )}
            <header className="sila-header">
              <div className="sila-header__left"><button type="button" className="sila-header__menu" onClick={() => isMobile ? setMobileOpen((open) => !open) : setCollapsed((value) => !value)} aria-label="Toggle navigation" data-testid="button-toggle-sidebar">{isMobile ? <Menu size={17} /> : collapsed ? <PanelLeftOpen size={17} /> : <PanelLeftClose size={17} />}</button></div>
              <div className="sila-header__right"><button className="sila-icon-button" type="button" aria-label="View notifications" onClick={() => setHeaderMessage('No new operational notifications.')} data-testid="button-notifications"><Bell size={17} /><span className="sila-notification-dot" /></button><button className="sila-icon-button" type="button" aria-label="Help" onClick={() => setHeaderMessage('Cloud help is available through your workspace administrator.')} data-testid="button-help"><CircleHelp size={17} /></button><div style={{ position: 'relative' }}><button className="sila-user-menu" type="button" onClick={() => setUserMenuOpen((open) => !open)} title="Open user menu" data-testid="button-user-menu"><span className="sila-avatar" data-testid="avatar-current-user">{initials(user.displayName)}</span><span className="sila-user-menu__copy"><strong data-testid="text-current-user">{user.displayName}</strong><span>{user.email}</span></span><ChevronDown size={14} /></button>{userMenuOpen && <div className="sila-card" style={{ position: 'absolute', top: 'calc(100% + 8px)', right: 0, zIndex: 40, minWidth: 150, padding: 6 }} data-testid="user-menu-panel"><button className="sila-button sila-button--quiet" style={{ width: '100%', justifyContent: 'flex-start' }} type="button" onClick={signOut} disabled={logout.isPending} data-testid="button-user-menu-sign-out">Sign out</button></div>}</div>{headerMessage && <button className="sila-dev-marker" type="button" onClick={() => setHeaderMessage('')} data-testid="status-header-message">{headerMessage}</button>}</div>
            </header>
            <div className="sila-content">{children}</div>
          </section>
        </div>
      </div>
    </AccessContextStore.Provider>
  );
}