import { type ReactNode, useEffect } from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { TooltipProvider } from '@/components/ui/tooltip';
import { Toaster } from '@/components/ui/toaster';
import { ErrorBoundary } from '@/components/error-boundary';
import { SilaCloudLayout } from '@/components/sila-layout';
import { RoutePermissionGuard } from '@/components/permission';
import Dashboard from '@/pages/dashboard';
import Login from '@/pages/login';
import PlatformLaunch from '@/pages/platform-launch';
import PlatformLogin from '@/pages/platform-login';
import OrganizationsPage from '@/pages/organizations';
import OrganizationDetailPage from '@/pages/organization-detail';
import PlatformUsersPage from '@/pages/platform-users';
import PlatformSimpleList from '@/pages/platform-lists';
import TenantLicensesPage from '@/pages/tenant-licenses';
import NotFound from '@/pages/not-found';
import PurchaseOrders from '@/pages/purchase-orders';
import PurchaseOrderDetail from '@/pages/purchase-order-detail';
import Invoices from '@/pages/invoices';
import InvoiceDetail from '@/pages/invoice-detail';
import Grns from '@/pages/grns';
import GrnDetail from '@/pages/grn-detail';
import UsersPage from '@/pages/users';
import UserDetailPage from '@/pages/user-detail';
import RolesPermissionsPage from '@/pages/roles-permissions';
import OrganizationPage from '@/pages/organization';
import DocumentExtractionPage from '@/pages/document-extraction';
import IntegrationsPage from '@/pages/integrations';
import SuppliersPage from '@/pages/suppliers';
import { CompanyCodesPage, PropertyMasterPage } from '@/pages/operational-masters';
import * as FutureRoutes from '@/pages/future-routes';
import * as RecipeManagement from '@/pages/recipe-management';
import * as InventoryFoundation from '@/pages/inventory-foundation';
import { Route, Switch, Router as WouterRouter, useLocation } from 'wouter';
import { customerBasePath, resolveCustomerRouteSlug } from '@/lib/tenant-route';

const queryClient = new QueryClient();

function RoutedErrorBoundary({ children }: { children: ReactNode }) {
  const [location] = useLocation();
  return <ErrorBoundary resetKey={location}>{children}</ErrorBoundary>;
}

function Shell({ children, permission }: { children: ReactNode; permission?: string }) {
  return <SilaCloudLayout><RoutePermissionGuard permission={permission}>{children}</RoutePermissionGuard></SilaCloudLayout>;
}

function Home() {
  const [, setLocation] = useLocation();
  useEffect(() => {
    if (resolveCustomerRouteSlug()) {
      void fetch('/api/auth/cloud/session', { credentials: 'include' })
        .then((response) => setLocation(response.ok ? '/dashboard' : '/login'))
        .catch(() => setLocation('/login'));
      return;
    }
    void fetch('/api/platform/session', { credentials: 'include' })
      .then((response) => {
        setLocation(response.ok ? '/organizations' : '/platform/login');
      })
      .catch(() => setLocation('/platform/login'));
  }, [setLocation]);
  return null;
}

function DashboardRoute() { return <Shell><Dashboard /></Shell>; }
function PurchaseOrdersRoute() { return <Shell><PurchaseOrders /></Shell>; }
function PurchaseOrderDetailRoute() { return <Shell><PurchaseOrderDetail /></Shell>; }
function InvoicesRoute() { return <Shell><Invoices /></Shell>; }
function InvoiceDetailRoute() { return <Shell><InvoiceDetail /></Shell>; }
function GrnsRoute() { return <Shell><Grns /></Shell>; }
function GrnDetailRoute() { return <Shell><GrnDetail /></Shell>; }
function UsersRoute() { return <Shell permission="user"><UsersPage /></Shell>; }
function OrganizationRoute() { return <Shell permission="organization"><OrganizationPage /></Shell>; }
function FutureRoute({ children, permission }: { children: ReactNode; permission?: string }) { return <Shell permission={permission}>{children}</Shell>; }

function Redirect({ to }: { to: string }) {
  const [, setLocation] = useLocation();
  useEffect(() => { setLocation(to); }, [setLocation, to]);
  return null;
}

function Router() {
  return (
    <RoutedErrorBoundary>
      <Switch>
        <Route path="/login" component={Login} />
        <Route path="/platform-launch" component={PlatformLaunch} />
        <Route path="/platform/login" component={PlatformLogin} />
        <Route path="/organizations/:tenantId" component={OrganizationDetailPage} />
        <Route path="/organizations" component={OrganizationsPage} />
        <Route path="/platform/customers/:tenantId" component={OrganizationDetailPage} />
        <Route path="/platform/customers" component={OrganizationsPage} />
        <Route path="/platform/users" component={PlatformUsersPage} />
        <Route path="/platform/audit" component={() => <PlatformSimpleList path="/api/platform/audit" title="Platform audit" />} />
        <Route path="/platform/settings" component={() => <PlatformSimpleList path="/api/platform/session" title="Settings" />} />
        <Route path="/platform" component={OrganizationsPage} />
        <Route path="/dashboard" component={DashboardRoute} />
        <Route path="/recipe-management/dashboard" component={() => <Shell><RecipeManagement.RecipeDashboardPage /></Shell>} />
        <Route path="/recipe-management/master-data" component={() => <Redirect to="/admin/recipe-master-data" />} />
        <Route path="/recipe-management/materials" component={() => <Redirect to="/admin/materials" />} />
        <Route path="/recipe-management/locations" component={() => <Redirect to="/admin/locations" />} />
        <Route path="/recipe-management/families" component={() => <Redirect to="/admin/recipe-master-data" />} />
        <Route path="/recipe-management/categories" component={() => <Redirect to="/admin/recipe-master-data" />} />
        <Route path="/recipe-management/recipes/new" component={() => <Shell><RecipeManagement.RecipeNewPage /></Shell>} />
        <Route path="/recipe-management/recipes/:id" component={() => <Shell><RecipeManagement.RecipeEditorPage /></Shell>} />
        <Route path="/recipe-management/recipes" component={() => <Shell><RecipeManagement.RecipesPage /></Shell>} />
        <Route path="/recipe-management/approvals" component={() => <Shell><RecipeManagement.RecipeApprovalsPage /></Shell>} />
        <Route path="/recipe-management/pos" component={() => <Shell><RecipeManagement.RecipePosIntegrationPage /></Shell>} />
        <Route path="/recipe-management/transactions/:id" component={() => <Shell><RecipeManagement.RecipeTransactionDetailPage /></Shell>} />
        <Route path="/recipe-management/transactions" component={() => <Shell><RecipeManagement.RecipeTransactionsPage /></Shell>} />
        <Route path="/menu-engineering/menu-planning" component={() => <FutureRoute><FutureRoutes.MenuPlanning /></FutureRoute>} />
        <Route path="/menu-engineering/recipes/:id" component={() => <FutureRoute><FutureRoutes.RecipeDetail /></FutureRoute>} />
        <Route path="/menu-engineering/recipes" component={() => <FutureRoute><FutureRoutes.Recipes /></FutureRoute>} />
        <Route path="/menu-engineering/ingredients" component={() => <FutureRoute><FutureRoutes.Ingredients /></FutureRoute>} />
        <Route path="/menu-engineering/portion-planning" component={() => <FutureRoute><FutureRoutes.PortionPlanning /></FutureRoute>} />
        <Route path="/menu-engineering/demand-forecast" component={() => <FutureRoute><FutureRoutes.DemandForecast /></FutureRoute>} />
        <Route path="/menu-engineering/menu-performance" component={() => <FutureRoute><FutureRoutes.MenuPerformance /></FutureRoute>} />
        <Route path="/menu-engineering/menus" component={() => <FutureRoute><FutureRoutes.MenuPlanning /></FutureRoute>} />
        <Route path="/menu-engineering/performance" component={() => <FutureRoute><FutureRoutes.MenuPerformance /></FutureRoute>} />
        <Route path="/menu-engineering/costing" component={() => <FutureRoute><FutureRoutes.Costing /></FutureRoute>} />
        <Route path="/inventory/dashboard" component={() => <Shell><InventoryFoundation.InventoryDashboardPage /></Shell>} />
        <Route path="/inventory/live" component={() => <Shell><InventoryFoundation.LiveInventoryPage /></Shell>} />
        <Route path="/inventory/transfers/new" component={() => <Shell><InventoryFoundation.InventoryTransferNewPage /></Shell>} />
        <Route path="/inventory/transfers/:id" component={() => <Shell><InventoryFoundation.InventoryTransferDetailPage /></Shell>} />
        <Route path="/inventory/transfers" component={() => <Shell><InventoryFoundation.InventoryTransfersPage /></Shell>} />
        <Route path="/inventory/stock/:id" component={() => <FutureRoute><FutureRoutes.StockOverview /></FutureRoute>} />
        <Route path="/inventory/stock" component={() => <FutureRoute><FutureRoutes.StockOverview /></FutureRoute>} />
        <Route path="/inventory/goods-receipt" component={() => <FutureRoute><FutureRoutes.GoodsReceiptPlaceholder /></FutureRoute>} />
        <Route path="/inventory/count/:id" component={() => <FutureRoute><FutureRoutes.InventoryCountDetail /></FutureRoute>} />
        <Route path="/inventory/count" component={() => <FutureRoute><FutureRoutes.InventoryCount /></FutureRoute>} />
        <Route path="/inventory/counts" component={() => <FutureRoute><FutureRoutes.InventoryCount /></FutureRoute>} />
        <Route path="/inventory/goods-issue" component={() => <FutureRoute><FutureRoutes.GoodsIssue /></FutureRoute>} />
        <Route path="/inventory/damage-waste" component={() => <FutureRoute><FutureRoutes.DamageWriteOff /></FutureRoute>} />
        <Route path="/inventory/waste" component={() => <FutureRoute><FutureRoutes.DamageWriteOff /></FutureRoute>} />
        <Route path="/inventory/damage" component={() => <FutureRoute><FutureRoutes.DamageWriteOff /></FutureRoute>} />
        <Route path="/inventory/transactions" component={() => <FutureRoute><FutureRoutes.InventoryTransactions /></FutureRoute>} />
        <Route path="/inventory/batches" component={() => <FutureRoute><FutureRoutes.BatchExpiry /></FutureRoute>} />
        <Route path="/inventory/adjustments" component={() => <FutureRoute><FutureRoutes.StockAdjustments /></FutureRoute>} />
        <Route path="/inventory" component={() => <Shell><InventoryFoundation.InventoryDashboardPage /></Shell>} />
        <Route path="/purchasing/requirements" component={() => <FutureRoute><FutureRoutes.MaterialRequirements /></FutureRoute>} />
        <Route path="/purchasing/suggestions" component={() => <FutureRoute><FutureRoutes.PurchaseSuggestions /></FutureRoute>} />
        <Route path="/purchasing/purchase-orders/:id" component={PurchaseOrderDetailRoute} />
        <Route path="/purchasing/purchase-orders" component={PurchaseOrdersRoute} />
        <Route path="/purchasing/suppliers" component={() => <Redirect to="/admin/suppliers" />} />
        <Route path="/receiving/purchase-orders/:poNumber" component={PurchaseOrderDetailRoute} />
        <Route path="/receiving/purchase-orders" component={PurchaseOrdersRoute} />
        <Route path="/receiving/receive" component={() => <FutureRoute><FutureRoutes.Receive /></FutureRoute>} />
        <Route path="/receiving/invoices/:id" component={InvoiceDetailRoute} />
        <Route path="/receiving/invoices" component={InvoicesRoute} />
        <Route path="/receiving/goods-receipts/:id" component={GrnDetailRoute} />
        <Route path="/receiving/goods-receipts" component={GrnsRoute} />
        <Route path="/receiving/exceptions" component={() => <FutureRoute><FutureRoutes.ReceivingExceptions /></FutureRoute>} />
        <Route path="/procurement/material-requirements" component={() => <FutureRoute><FutureRoutes.MaterialRequirements /></FutureRoute>} />
        <Route path="/procurement/purchase-suggestions" component={() => <FutureRoute><FutureRoutes.PurchaseSuggestions /></FutureRoute>} />
        <Route path="/procurement/supplier-comparison" component={() => <FutureRoute><FutureRoutes.SupplierComparison /></FutureRoute>} />
        <Route path="/procurement/reorder-planning" component={() => <FutureRoute><FutureRoutes.ReorderPlanning /></FutureRoute>} />
        <Route path="/documents/inbox" component={() => <FutureRoute><FutureRoutes.DocumentInbox /></FutureRoute>} />
        <Route path="/documents/invoice-capture" component={() => <FutureRoute><FutureRoutes.InvoiceCapture /></FutureRoute>} />
        <Route path="/documents/extraction" component={() => <Shell permission="configuration"><DocumentExtractionPage /></Shell>} />
        <Route path="/documents/archive" component={() => <FutureRoute><FutureRoutes.DocumentArchive /></FutureRoute>} />
        <Route path="/documents/delivery-notes" component={() => <FutureRoute><FutureRoutes.DeliveryNotes /></FutureRoute>} />
        <Route path="/documents/extraction-results" component={() => <FutureRoute><FutureRoutes.ExtractionResults /></FutureRoute>} />
        <Route path="/approvals" component={() => <FutureRoute><FutureRoutes.Approvals /></FutureRoute>} />
        <Route path="/analytics/consumption" component={() => <FutureRoute><FutureRoutes.Analytics /></FutureRoute>} />
        <Route path="/analytics/food-cost" component={() => <FutureRoute><FutureRoutes.Analytics /></FutureRoute>} />
        <Route path="/analytics/menu-performance" component={() => <FutureRoute><FutureRoutes.MenuPerformance /></FutureRoute>} />
        <Route path="/analytics/inventory-variance" component={() => <FutureRoute><FutureRoutes.Analytics /></FutureRoute>} />
        <Route path="/analytics/waste" component={() => <FutureRoute><FutureRoutes.Analytics /></FutureRoute>} />
        <Route path="/analytics/purchases" component={() => <FutureRoute><FutureRoutes.Analytics /></FutureRoute>} />
        <Route path="/analytics" component={() => <FutureRoute><FutureRoutes.Analytics /></FutureRoute>} />
        <Route path="/master-data/company-codes" component={() => <Redirect to="/admin/company-codes" />} />
        <Route path="/master-data/properties" component={() => <Redirect to="/admin/properties" />} />
        <Route path="/master-data/plants" component={() => <Redirect to="/admin/locations" />} />
        <Route path="/master-data/storage-locations" component={() => <Redirect to="/admin/locations" />} />
        <Route path="/master-data/materials" component={() => <Redirect to="/admin/materials" />} />
        <Route path="/master-data/ingredients" component={() => <FutureRoute><FutureRoutes.MasterIngredients /></FutureRoute>} />
        <Route path="/master-data/recipes" component={() => <FutureRoute><FutureRoutes.MasterRecipes /></FutureRoute>} />
        <Route path="/master-data/menu-items" component={() => <FutureRoute><FutureRoutes.MenuItems /></FutureRoute>} />
        <Route path="/master-data/suppliers" component={() => <Redirect to="/admin/suppliers" />} />
        <Route path="/master-data/uom" component={() => <Redirect to="/admin/recipe-master-data" />} />
        <Route path="/master-data/categories" component={() => <Redirect to="/admin/recipe-master-data" />} />
        <Route path="/master-data/inventory-locations" component={() => <Redirect to="/admin/locations" />} />
        <Route path="/master-data/locations" component={() => <Redirect to="/admin/locations" />} />
        <Route path="/master-data" component={() => <Redirect to="/admin/recipe-master-data" />} />
        <Route path="/admin/users/:userId" component={() => <Shell permission="user"><UserDetailPage /></Shell>} />
        <Route path="/admin/users" component={UsersRoute} />
        <Route path="/admin/locations" component={() => <Shell><InventoryFoundation.InventoryLocationsPage /></Shell>} />
        <Route path="/admin/materials" component={() => <Shell><RecipeManagement.RecipeMaterialsPage /></Shell>} />
        <Route path="/admin/recipe-master-data" component={() => <Shell><RecipeManagement.RecipeMasterDataPage /></Shell>} />
        <Route path="/admin/company-codes" component={() => <Shell><CompanyCodesPage /></Shell>} />
        <Route path="/admin/suppliers" component={() => <Shell><SuppliersPage /></Shell>} />
        <Route path="/admin/properties" component={() => <Shell permission="organization"><PropertyMasterPage /></Shell>} />
        <Route path="/admin/organization" component={OrganizationRoute} />
        <Route path="/admin/roles" component={() => <Shell permission="role"><RolesPermissionsPage /></Shell>} />
        <Route path="/admin/document-extraction" component={() => <Shell permission="configuration"><DocumentExtractionPage /></Shell>} />
        <Route path="/admin/integrations" component={() => <FutureRoute permission="integration"><IntegrationsPage /></FutureRoute>} />
        <Route path="/admin/configuration" component={() => <FutureRoute permission="configuration"><FutureRoutes.Configurations /></FutureRoute>} />
        <Route path="/admin/configurations" component={() => <FutureRoute permission="configuration"><FutureRoutes.Configurations /></FutureRoute>} />
        <Route path="/admin/audit" component={() => <FutureRoute permission="audit"><FutureRoutes.AuditLogs /></FutureRoute>} />
        <Route path="/admin/licenses" component={() => <Shell permission="user"><TenantLicensesPage /></Shell>} />
        <Route path="/" component={Home} />
        <Route component={NotFound} />
      </Switch>
    </RoutedErrorBoundary>
  );
}

function LegacyFiveTestRedirect() {
  useEffect(() => {
    const first = window.location.pathname.split('/').filter(Boolean)[0];
    if (first?.toLowerCase() !== 'five-test') return;
    const rest = window.location.pathname.replace(/^\/five-test/i, '') || '/';
    window.location.replace(`/five${rest}${window.location.search}${window.location.hash}`);
  }, []);
  return null;
}

function App() {
  return <QueryClientProvider client={queryClient}><TooltipProvider><LegacyFiveTestRedirect /><WouterRouter base={customerBasePath() || import.meta.env.BASE_URL.replace(/\/$/, '')}><Router /></WouterRouter><Toaster /></TooltipProvider></QueryClientProvider>;
}

export default App;