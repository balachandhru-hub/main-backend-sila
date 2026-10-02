import { expect, test, type Page, type Route } from '@playwright/test';

const fixtureCredentials = {
  email: 'cloud-admin@example.test',
  password: 'development-password',
};

const currentUser = {
  id: 'fixture-cloud-user',
  displayName: 'Fixture Cloud Admin',
  email: fixtureCredentials.email,
  application: 'CLOUD',
  status: 'ACTIVE',
};

const accessContext = {
  user: currentUser,
  organizations: [
    {
      id: 'fixture-property',
      code: 'FIXTURE',
      name: 'Fixture Property',
      kind: 'CUSTOMER',
      status: 'ACTIVE',
    },
  ],
  units: [
    {
      id: 'fixture-store',
      organizationId: 'fixture-property',
      code: 'STORE-01',
      name: 'Fixture Store',
      kind: 'STORE',
      status: 'ACTIVE',
    },
  ],
  roles: [],
  permissions: [
    { id: 'permission-admin', key: 'ADMIN', name: 'Administration' },
    { id: 'permission-user', key: 'ADMIN_USERS', name: 'Users' },
    { id: 'permission-role', key: 'ADMIN_ROLES', name: 'Roles' },
    { id: 'permission-org', key: 'ADMIN_ORGANIZATION', name: 'Organization' },
    { id: 'permission-location', key: 'ADMIN_LOCATIONS', name: 'Locations' },
    { id: 'permission-extraction', key: 'ADMIN_EXTRACTION', name: 'Extraction' },
    { id: 'permission-integration', key: 'ADMIN_INTEGRATIONS', name: 'Integrations' },
    { id: 'permission-configuration', key: 'ADMIN_CONFIGURATION', name: 'Configuration' },
    { id: 'permission-audit', key: 'ADMIN_AUDIT', name: 'Audit' },
  ],
};

const sessionResponse = { user: currentUser };

const invoiceOcrConfiguration = {
  id: 'fixture-ocr-configuration',
  organizationId: 'fixture-property',
  mobileBasicOcrEnabled: true,
  automaticBackendFallbackEnabled: true,
  minimumMobileConfidence: 0.75,
  requireSupplierName: true,
  requireInvoiceNumber: true,
  requirePurchaseOrderNumber: true,
  requireInvoiceAmount: true,
  requireInvoiceDate: false,
  requireCurrency: false,
  requireSupplierTrn: false,
  backendProvider: 'BUILT_IN_ADVANCED',
  alwaysBackendOnReread: true,
  detailedLineExtractionEnabled: true,
  supplierMasterValidationEnabled: true,
  purchaseOrderValidationEnabled: true,
  financialReconciliationEnabled: true,
  amountTolerance: 0.05,
  backendTimeoutSeconds: 60,
  backendRetryCount: 1,
  reuseCachedOcr: true,
  version: 4,
  updatedAt: '2026-09-10T08:00:00.000Z',
};

const connectedDetailFixtures = {
  invoice: {
    id: 'fixture-invoice',
    documentId: 'fixture-document',
    invoiceNumber: 'INV-FIXTURE-001',
    invoiceDate: '2026-09-10T00:00:00.000Z',
    supplierName: 'Fixture Foods Supplier',
    supplierTaxNumber: 'TRN-FIXTURE',
    purchaseOrderNumber: 'PO-FIXTURE-001',
    purchaseOrderId: 'fixture-po',
    currency: 'AED',
    netAmount: 100,
    taxAmount: 5,
    grossAmount: 105,
    invoiceType: 'MATERIAL',
    status: 'PO_MATCHED',
    overallConfidence: 0.98,
    organizationId: 'fixture-property',
    operatingUnitId: 'fixture-store',
    operatingUnitName: 'Fixture Store',
    supplierCode: 'SUP-FIXTURE',
    goodsReceiptId: 'fixture-grn',
    lines: [
      {
        id: 'fixture-invoice-line',
        lineNumber: 1,
        supplierMaterialCode: 'RICE-001',
        materialId: 'fixture-material',
        description: 'Basmati rice',
        quantity: 10,
        uom: 'KG',
        unitPrice: 10,
        taxRate: 5,
        taxAmount: 5,
        lineAmount: 100,
        confidence: 0.98,
        matchStatus: 'MATCHED',
        purchaseOrderItemId: 'fixture-po-item',
      },
    ],
    createdAt: '2026-09-10T08:00:00.000Z',
    updatedAt: '2026-09-10T08:05:00.000Z',
  },
  document: {
    id: 'fixture-document',
    filename: 'fixture-invoice.pdf',
    contentType: 'application/pdf',
    fileSizeBytes: 1024,
    pageCount: 1,
    sourceChannel: 'CLOUD_UPLOAD',
    status: 'FULL_EXTRACTION_COMPLETE',
    createdAt: '2026-09-10T08:00:00.000Z',
    invoiceId: 'fixture-invoice',
  },
  basicExtraction: {
    documentId: 'fixture-document',
    supplierName: 'Fixture Foods Supplier',
    supplierTrn: 'TRN-FIXTURE',
    supplierInvoiceNumber: 'INV-FIXTURE-001',
    invoiceDate: '2026-09-10T00:00:00.000Z',
    purchaseOrderNumber: 'PO-FIXTURE-001',
    invoiceGross: 105,
    currency: 'AED',
    status: 'COMPLETE',
  },
  extraction: {
    header: {
      documentId: 'fixture-document',
      supplierName: 'Fixture Foods Supplier',
      supplierTrn: 'TRN-FIXTURE',
      supplierInvoiceNumber: 'INV-FIXTURE-001',
      invoiceDate: '2026-09-10T00:00:00.000Z',
      purchaseOrderNumber: 'PO-FIXTURE-001',
      invoiceGross: 105,
      invoiceNet: 100,
      currency: 'AED',
    },
    lines: [
      {
        itemSkuId: 'fixture-material',
        itemAmount: 105,
        itemNet: 100,
        itemDescription: 'Basmati rice',
        lineItemNumber: '1',
        purchaseOrderItemId: 'fixture-po-item',
      },
    ],
    provider: 'fixture',
    extractionMethod: 'fixture',
    confidence: 0.98,
    fallbackUsed: false,
    status: 'COMPLETE',
    completedAt: '2026-09-10T08:05:00.000Z',
  },
  grn: {
    id: 'fixture-grn',
    grnNumber: 'GRN-FIXTURE-001',
    status: 'POSTED',
    purchaseOrderId: 'fixture-po',
    purchaseOrderNumber: 'PO-FIXTURE-001',
    invoiceId: 'fixture-invoice',
    invoiceNumber: 'INV-FIXTURE-001',
    supplierId: 'fixture-supplier',
    supplierName: 'Fixture Foods Supplier',
    organizationId: 'fixture-property',
    operatingUnitId: 'fixture-store',
    operatingUnitName: 'Fixture Store',
    receiptDate: '2026-09-11T00:00:00.000Z',
    createdAt: '2026-09-11T08:00:00.000Z',
    postedAt: '2026-09-11T08:05:00.000Z',
    lines: [
      {
        id: 'fixture-grn-line',
        purchaseOrderItemId: 'fixture-po-item',
        purchaseOrderLineNumber: 1,
        materialCode: 'RICE-001',
        description: 'Basmati rice',
        openQuantityBefore: 10,
        invoiceQuantity: 10,
        receivedQuantity: 10,
        acceptedQuantity: 10,
        damagedQuantity: 0,
        rejectedQuantity: 0,
        uom: 'KG',
        batchNumber: 'BATCH-FIXTURE',
        expiryDate: '2027-09-11T00:00:00.000Z',
      },
    ],
  },
  purchaseOrder: {
    id: 'fixture-po',
    poNumber: 'PO-FIXTURE-001',
    poDate: '2026-09-08T00:00:00.000Z',
    deliveryDate: '2026-09-11T00:00:00.000Z',
    currency: 'AED',
    status: 'PARTIALLY_RECEIVED',
    organizationId: 'fixture-property',
    operatingUnitId: 'fixture-store',
    operatingUnitName: 'Fixture Store',
    supplierId: 'fixture-supplier',
    supplierName: 'Fixture Foods Supplier',
    items: [
      {
        id: 'fixture-po-item',
        lineNumber: 1,
        materialId: 'fixture-material',
        materialCode: 'RICE-001',
        description: 'Basmati rice',
        orderedQuantity: 20,
        receivedQuantity: 10,
        openQuantity: 10,
        uom: 'KG',
        unitPrice: 10,
        status: 'PARTIALLY_RECEIVED',
      },
    ],
  },
};

const apiPendingRoutes = [
  '/menu-engineering/menu-planning',
  '/menu-engineering/recipes',
  '/menu-engineering/recipes/fixture-recipe',
  '/menu-engineering/ingredients',
  '/menu-engineering/portion-planning',
  '/menu-engineering/demand-forecast',
  '/menu-engineering/menu-performance',
  '/inventory/dashboard',
  '/inventory/live',
  '/inventory/stock',
  '/inventory/stock/fixture-stock',
  '/inventory/count',
  '/inventory/count/fixture-count',
  '/inventory/transfers',
  '/inventory/goods-receipt',
  '/inventory/goods-issue',
  '/inventory/damage-waste',
  '/inventory/transactions',
  '/inventory/batches',
  '/purchasing/requirements',
  '/purchasing/suggestions',
  '/purchasing/suppliers',
  '/receiving/receive',
  '/receiving/exceptions',
  '/documents/inbox',
  '/documents/invoice-capture',
  '/documents/archive',
  '/approvals',
  '/analytics',
  '/analytics/consumption',
  '/analytics/food-cost',
  '/analytics/menu-performance',
  '/analytics/inventory-variance',
  '/analytics/waste',
  '/analytics/purchases',
  '/master-data/materials',
  '/master-data/ingredients',
  '/master-data/recipes',
  '/master-data/menu-items',
  '/master-data/suppliers',
  '/master-data/uom',
  '/master-data/categories',
  '/admin/properties',
  '/admin/integrations',
  '/admin/configuration',
  '/admin/audit',
];

async function json(route: Route, body: unknown, status = 200, headers?: Record<string, string>) {
  await route.fulfill({
    status,
    contentType: 'application/json',
    headers,
    body: JSON.stringify(body),
  });
}

async function installCloudFixture(page: Page) {
  let loggedIn = false;
  let sessionExpired = false;
  let expireNextProtectedRequest = false;
  let expireNextProtectedMutation = false;
  let permissions = accessContext.permissions;

  await page.route('**/api/**', async (route) => {
    const request = route.request();
    const pathname = new URL(request.url()).pathname;
    const hasSessionCookie = request.headers().cookie?.includes('sila_me_session=');
    const authenticated = !sessionExpired && (loggedIn || Boolean(hasSessionCookie));

    if (pathname === '/api/auth/cloud/login' && request.method() === 'POST') {
      const body = request.postDataJSON() as { email?: string; password?: string };
      if (body.email !== fixtureCredentials.email || body.password !== fixtureCredentials.password) {
        await json(route, { code: 'INVALID_CREDENTIALS', message: 'Invalid credentials.' }, 401);
        return;
      }

      loggedIn = true;
      sessionExpired = false;
      await json(route, sessionResponse, 200, {
        'set-cookie': 'sila_me_session=fixture-session; Path=/; HttpOnly; SameSite=Lax',
      });
      return;
    }

    if (pathname === '/api/auth/cloud/logout' && request.method() === 'POST') {
      loggedIn = false;
      await route.fulfill({
        status: 204,
        headers: {
          'set-cookie': 'sila_me_session=; Max-Age=0; Path=/; HttpOnly; SameSite=Lax',
        },
      });
      return;
    }

    if (pathname === '/api/auth/cloud/session' && request.method() === 'GET') {
      await json(
        route,
        authenticated ? sessionResponse : { code: 'SESSION_INVALID', message: 'Your session is no longer valid.' },
        authenticated ? 200 : 401,
      );
      return;
    }

    if (expireNextProtectedRequest) {
      expireNextProtectedRequest = false;
      await json(route, { code: 'SESSION_INVALID', message: 'Your session is no longer valid.' }, 401);
      return;
    }

    if (expireNextProtectedMutation && ['POST', 'PATCH', 'PUT'].includes(request.method())) {
      expireNextProtectedMutation = false;
      await json(route, { code: 'SESSION_INVALID', message: 'Your session is no longer valid.' }, 401);
      return;
    }

    if (!authenticated) {
      await json(route, { code: 'SESSION_INVALID', message: 'Your session is no longer valid.' }, 401);
      return;
    }

    if (pathname === '/api/v1/access/context') {
      await json(route, { ...accessContext, permissions });
      return;
    }

    if (pathname === '/api/v1/configuration/invoice-ocr') {
      await json(route, invoiceOcrConfiguration);
      return;
    }

    if (pathname === '/api/v1/health') {
      await json(route, { status: 'Healthy' });
      return;
    }

    if (pathname === '/api/v1/invoices/fixture-invoice') {
      await json(route, connectedDetailFixtures.invoice);
      return;
    }

    if (pathname === '/api/v1/invoices/fixture-invoice/basic-extraction') {
      await json(route, connectedDetailFixtures.basicExtraction);
      return;
    }

    if (pathname === '/api/v1/invoices/fixture-invoice/extraction') {
      await json(route, connectedDetailFixtures.extraction);
      return;
    }

    if (pathname === '/api/v1/documents/fixture-document') {
      await json(route, connectedDetailFixtures.document);
      return;
    }

    if (pathname === '/api/v1/documents/fixture-document/content') {
      await route.fulfill({
        status: 200,
        contentType: 'application/pdf',
        body: '%PDF-1.4\n% fixture invoice document\n',
      });
      return;
    }

    if (pathname === '/api/v1/grns/fixture-grn') {
      await json(route, connectedDetailFixtures.grn);
      return;
    }

    if (pathname === '/api/v1/purchase-orders/PO-FIXTURE-001') {
      await json(route, connectedDetailFixtures.purchaseOrder);
      return;
    }

    if (pathname === '/api/v1/invoices' || pathname === '/api/v1/grns') {
      await json(route, []);
      return;
    }

    await json(route, []);
  });

  return {
    expireSession() {
      sessionExpired = true;
    },
    expireNextProtectedRequest() {
      expireNextProtectedRequest = true;
    },
    expireNextProtectedMutation() {
      expireNextProtectedMutation = true;
    },
    setPermissions(nextPermissions: typeof accessContext.permissions) {
      permissions = nextPermissions;
    },
  };
}

async function signIn(
  page: Page,
  expectedPath = '/dashboard',
  expectedTitle = expectedPath === '/dashboard' ? 'Good morning, Fixture' : 'Recipes',
) {
  if (!page.url().includes('/login')) await page.goto('/login');
  await page.getByTestId('input-email').fill(fixtureCredentials.email);
  await page.getByTestId('input-password').fill(fixtureCredentials.password);
  await page.getByTestId('button-submit-login').click();
  await expect(page).toHaveURL(new RegExp(`${expectedPath.replaceAll('/', '\\/')}$`));
  await expect(page.getByTestId('text-page-title')).toContainText(expectedTitle);
}

test.describe('Cloud authentication and shell regressions', () => {
  test('logs in, restores the session, logs out, and allows a second login', async ({ page }) => {
    await installCloudFixture(page);

    await signIn(page);
    await expect(page.getByTestId('text-current-user')).toHaveText('Fixture Cloud Admin');
    await expect(page.getByTestId('status-session')).toContainText('Secure and active');

    await page.reload();
    await expect(page).toHaveURL(/\/dashboard$/);
    await expect(page.getByTestId('text-session-status')).toHaveText('Authenticated');

    await page.getByTestId('button-sidebar-sign-out').click();
    await expect(page).toHaveURL(/\/login$/);
    await expect(page.getByTestId('button-submit-login')).toBeVisible();
    await expect(page.getByTestId('alert-session-expired')).toHaveCount(0);

    await signIn(page);
    await expect(page).toHaveURL(/\/dashboard$/);
    await expect(page.getByTestId('text-current-user')).toHaveText('Fixture Cloud Admin');
  });

  test('redirects to a usable login form when the active session expires in Cloud', async ({ page }) => {
    const fixture = await installCloudFixture(page);
    await signIn(page);

    fixture.expireSession();
    await page.goto('/menu-engineering/recipes', { waitUntil: 'networkidle' });

    await expect(page).toHaveURL(/\/login\?sessionExpired=true&returnTo=%2Fmenu-engineering%2Frecipes$/);
    await expect(page.getByTestId('button-submit-login')).toBeVisible();
    await expect(page.getByTestId('alert-session-expired')).toHaveText('Your Cloud session expired. Sign in again to continue.');
    await expect(page.getByTestId('input-email')).toBeEditable();
    await expect(page.getByTestId('input-password')).toBeEditable();
    await expect(page.getByTestId('sidebar-navigation')).toHaveCount(0);
    await expect(page.getByTestId('text-current-user')).toHaveCount(0);

    await signIn(page, '/menu-engineering/recipes');
    await expect(page).toHaveURL(/\/menu-engineering\/recipes$/);
    await expect(page.getByTestId('text-current-user')).toHaveText('Fixture Cloud Admin');
  });

  test('ignores invalid or external login return paths', async ({ page }) => {
    await installCloudFixture(page);
    for (const returnTo of ['https://evil.example/account', 'not-a-path']) {
      await page.goto(`/login?returnTo=${encodeURIComponent(returnTo)}`);
      await page.getByTestId('input-email').fill(fixtureCredentials.email);
      await page.getByTestId('input-password').fill(fixtureCredentials.password);
      await page.getByTestId('button-submit-login').click();

      await expect(page).toHaveURL(/\/dashboard$/);
      await expect(page.getByTestId('text-page-title')).toContainText('Good morning, Fixture');
    }
  });

  test('redirects to a usable login form when a protected Cloud resource request expires', async ({ page }) => {
    const fixture = await installCloudFixture(page);
    await signIn(page);

    fixture.expireNextProtectedRequest();
    await page.goto('/menu-engineering/recipes', { waitUntil: 'networkidle' });

    await expect(page).toHaveURL(/\/login\?sessionExpired=true&returnTo=%2Fmenu-engineering%2Frecipes$/);
    await expect(page.getByTestId('button-submit-login')).toBeVisible();
    await expect(page.getByTestId('alert-session-expired')).toHaveText('Your Cloud session expired. Sign in again to continue.');
    await expect(page.getByTestId('input-email')).toBeEditable();
    await expect(page.getByTestId('input-password')).toBeEditable();
    await expect(page.getByTestId('sidebar-navigation')).toHaveCount(0);
    await expect(page.getByTestId('text-current-user')).toHaveCount(0);

    await signIn(page, '/menu-engineering/recipes');
    await expect(page.getByTestId('text-current-user')).toHaveText('Fixture Cloud Admin');
  });

  test('recovers from an expired protected Cloud mutation without an unhandled browser error', async ({ page }) => {
    const fixture = await installCloudFixture(page);
    const pageErrors: string[] = [];
    page.on('pageerror', (error) => pageErrors.push(error.message));

    await signIn(page);
    await page.goto('/admin/organization', { waitUntil: 'networkidle' });
    await expect(page.getByTestId('text-page-title')).toHaveText('Organization structure');
    await page.getByTestId('input-organization-name').fill('Expired save');
    await page.getByTestId('input-organization-code').fill('EXPIRED');

    fixture.expireNextProtectedMutation();
    await page.getByTestId('button-create-organization').click();

    await expect(page).toHaveURL(/\/login\?sessionExpired=true&returnTo=%2Fadmin%2Forganization$/);
    await expect(page.getByTestId('button-submit-login')).toBeVisible();
    await expect(page.getByTestId('alert-session-expired')).toHaveText('Your Cloud session expired. Sign in again to continue.');
    await expect(pageErrors).toEqual([]);

    await signIn(page, '/admin/organization', 'Organization structure');
    await expect(page.getByTestId('input-organization-name')).toHaveValue('');
    await expect(page.getByTestId('input-organization-code')).toHaveValue('');
    await expect(page.getByTestId('text-current-user')).toHaveText('Fixture Cloud Admin');
  });

  test('recovers from an expired OCR policy save without an unhandled browser error', async ({ page }) => {
    const fixture = await installCloudFixture(page);
    const pageErrors: string[] = [];
    page.on('pageerror', (error) => pageErrors.push(error.message));

    await signIn(page);
    await page.goto('/admin/document-extraction', { waitUntil: 'networkidle' });
    await expect(page.getByTestId('text-page-title')).toHaveText('Invoice OCR policy');
    await expect(page.getByRole('button', { name: 'Save OCR policy' })).toBeVisible();
    await expect(page.getByLabel('Minimum Mobile confidence')).toHaveValue('0.75');
    await page.getByLabel('Minimum Mobile confidence').fill('0.88');

    fixture.expireNextProtectedMutation();
    await page.getByRole('button', { name: 'Save OCR policy' }).click();

    await expect(page).toHaveURL(/\/login\?sessionExpired=true&returnTo=%2Fadmin%2Fdocument-extraction$/);
    await expect(page.getByTestId('button-submit-login')).toBeVisible();
    await expect(page.getByTestId('alert-session-expired')).toHaveText('Your Cloud session expired. Sign in again to continue.');
    await expect(pageErrors).toEqual([]);

    await signIn(page, '/admin/document-extraction', 'Invoice OCR policy');
    await expect(page.getByLabel('Minimum Mobile confidence')).toHaveValue('0.75');
    await expect(page.getByRole('button', { name: 'Save OCR policy' })).toBeEnabled();
    await expect(page.getByTestId('text-current-user')).toHaveText('Fixture Cloud Admin');
  });

  test('renders canonical API-pending routes and direct detail refreshes without a blank page', async ({ page }) => {
    test.setTimeout(120_000);
    await installCloudFixture(page);
    await signIn(page);

    for (const route of apiPendingRoutes) {
      await page.goto(route, { waitUntil: 'networkidle' });
      await expect(page.getByTestId('text-page-title'), route).toBeVisible();
      await expect(page.getByTestId('status-api-integration-pending'), route).toHaveText('API integration pending');
      await expect(page.getByTestId('future-module-shell'), route).toBeVisible();
    }

    await page.goto('/menu-engineering/recipes/fixture-recipe');
    await page.reload();
    await expect(page.getByTestId('text-page-title')).toHaveText('Recipe detail');
    await expect(page.getByTestId('status-api-integration-pending')).toBeVisible();
  });

  test('renders the access-denied state when a permission-gated route is not in scope', async ({ page }) => {
    const fixture = await installCloudFixture(page);
    await signIn(page);

    fixture.setPermissions([]);
    await page.goto('/admin/users', { waitUntil: 'networkidle' });
    await expect(page.getByTestId('page-access-denied')).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Access denied' })).toBeVisible();
  });

  test('keeps connected invoice, goods-receipt, and purchase-order details usable after a hard refresh', async ({ page }) => {
    await installCloudFixture(page);
    await signIn(page);

    await page.goto('/receiving/invoices/fixture-invoice', { waitUntil: 'networkidle' });
    await expect(page.getByTestId('text-page-title')).toHaveText('INV-FIXTURE-001');
    await expect(page.getByTestId('text-invoice-supplier')).toHaveText('Fixture Foods Supplier');
    await expect(page.getByTestId('viewer-invoice-pdf')).toBeVisible();
    await page.reload({ waitUntil: 'networkidle' });
    await expect(page.getByTestId('text-page-title')).toHaveText('INV-FIXTURE-001');
    await expect(page.getByTestId('text-invoice-supplier')).toHaveText('Fixture Foods Supplier');

    await page.goto('/receiving/goods-receipts/fixture-grn', { waitUntil: 'networkidle' });
    await expect(page.getByTestId('text-page-title')).toHaveText('GRN-FIXTURE-001');
    await expect(page.getByText('Basmati rice')).toBeVisible();
    await page.reload({ waitUntil: 'networkidle' });
    await expect(page.getByTestId('text-page-title')).toHaveText('GRN-FIXTURE-001');
    await expect(page.getByText('Basmati rice')).toBeVisible();

    await page.goto('/receiving/purchase-orders/PO-FIXTURE-001', { waitUntil: 'networkidle' });
    await expect(page.getByTestId('text-page-title')).toHaveText('PO-FIXTURE-001');
    await expect(page.getByText('Basmati rice')).toBeVisible();
    await page.reload({ waitUntil: 'networkidle' });
    await expect(page.getByTestId('text-page-title')).toHaveText('PO-FIXTURE-001');
    await expect(page.getByText('Basmati rice')).toBeVisible();
  });

  test('supports expanded, collapsed, and mobile drawer navigation', async ({ page }) => {
    await installCloudFixture(page);
    await signIn(page);

    const sidebar = page.getByTestId('sidebar-navigation');
    await expect(sidebar).not.toHaveClass(/sila-sidebar--collapsed/);

    await page.getByTestId('button-toggle-sidebar').click();
    await expect(sidebar).toHaveClass(/sila-sidebar--collapsed/);

    await page.getByTestId('button-toggle-sidebar').click();
    await expect(sidebar).not.toHaveClass(/sila-sidebar--collapsed/);

    await page.setViewportSize({ width: 640, height: 900 });
    await page.getByTestId('button-toggle-sidebar').click();
    await expect(sidebar).toHaveClass(/sila-sidebar--mobile-open/);
    await expect(page.getByTestId('button-close-mobile-navigation')).toBeVisible();

    await page.getByTestId('button-close-mobile-navigation').click();
    await expect(sidebar).not.toHaveClass(/sila-sidebar--mobile-open/);
  });
});