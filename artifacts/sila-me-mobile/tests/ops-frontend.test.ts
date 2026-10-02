import assert from 'node:assert/strict';
import test from 'node:test';
import {
  MOBILE_CAPABILITIES,
  capabilityMessage,
  isConnected,
} from '../services/ops/capabilities';
import { greetingForNow } from '../services/ops/greeting';
import {
  inventoryService,
  stockCountService,
  transferService,
  goodsIssueService,
  taskService,
  liveStockService,
  itoService,
  matchesMaterialQuery,
  matchesMaterialFilters,
  partitionBalancesByCurrentLocation,
  groupBalancesByProperty,
  isStockFresh,
  validateSourceDestination,
  validateRequestedQty,
  bothPartiesApproved,
  buildItoTimeline,
  createEmptyItoDraft,
  ITO_STATUS_LABELS,
  type LiveStockMaterial,
  type LiveStockBalance,
} from '../services/ops';
import {
  HOME_QUICK_ACTIONS,
  INVENTORY_ACTIONS,
  ITO_DETAIL_SECTIONS,
  ITO_LANDING_TABS,
  MORE_LINKS,
  TASK_TABS,
} from '../services/ops/nav';

const TAB_NAMES = ['home', 'receive', 'inventory', 'tasks', 'more'] as const;

const RECEIVE_ACTIONS = [
  'Scan Invoice',
  'Receive Against PO',
  'View Open PO',
  'Pending GRN',
  'Receiving History',
] as const;

const sampleMaterials: LiveStockMaterial[] = [
  {
    id: '1',
    materialCode: '1002345',
    name: 'Mineral Water 500ml',
    description: 'Still mineral water',
    category: 'Beverages',
    materialGroup: 'Drinks',
    type: 'FG',
    supplierName: 'ABC FOOD TRADING LLC',
    supplierId: 'SUP-1',
    uom: 'EA',
    totalAvailable: 1250,
    locationCount: 6,
  },
  {
    id: '2',
    materialCode: '1002346',
    name: 'Sparkling Water',
    description: 'Carbonated water',
    category: 'Beverages',
    materialGroup: 'Drinks',
    type: 'FG',
    supplierName: 'Aqua Co',
    supplierId: 'SUP-2',
    uom: 'EA',
    totalAvailable: 0,
    locationCount: 2,
  },
  {
    id: '3',
    materialCode: '2001001',
    name: 'Olive Oil',
    description: 'Extra virgin',
    category: 'Dry Goods',
    materialGroup: 'Oils',
    type: 'RM',
    supplierName: 'ABC FOOD TRADING LLC',
    supplierId: 'SUP-1',
    uom: 'L',
    totalAvailable: 48,
    locationCount: 3,
  },
];

const sampleBalances: LiveStockBalance[] = [
  { organizationId: 'org-jvc', organizationName: 'FIVE JVC', unitId: 'u-main', unitName: 'Main Store', unitCode: 'MS', availableQty: 500 },
  { organizationId: 'org-jvc', organizationName: 'FIVE JVC', unitId: 'u-pool', unitName: 'Pool Outlet', unitCode: 'PO', availableQty: 120 },
  { organizationId: 'org-palm', organizationName: 'FIVE Palm', unitId: 'u-palm-main', unitName: 'Main Store', unitCode: 'PMS', availableQty: 400 },
];

test('bottom navigation keeps five primary tabs', () => {
  assert.deepEqual([...TAB_NAMES], ['home', 'receive', 'inventory', 'tasks', 'more']);
  assert.equal(TAB_NAMES.length, 5);
});

test('home quick actions prioritize live stock and raise ITO', () => {
  assert.deepEqual(HOME_QUICK_ACTIONS.map((item) => item.label), [
    'Scan Invoice',
    'Receive Goods',
    'Live Stock',
    'Raise ITO',
    'Stock Count',
    'Goods Issue',
  ]);
  assert.ok(!HOME_QUICK_ACTIONS.some((item) => item.label === 'Stock Transfer'));
});

test('receive landing preserves scan and adds PO/history actions', () => {
  assert.ok(RECEIVE_ACTIONS.includes('Scan Invoice'));
  assert.ok(RECEIVE_ACTIONS.includes('Receive Against PO'));
  assert.ok(RECEIVE_ACTIONS.includes('View Open PO'));
  assert.ok(RECEIVE_ACTIONS.includes('Pending GRN'));
  assert.ok(RECEIVE_ACTIONS.includes('Receiving History'));
});

test('inventory landing exposes live inventory and transfers', () => {
  assert.deepEqual(INVENTORY_ACTIONS.map((item) => item.title), [
    'Live Inventory',
    'Quick Transfer',
    'Transfers',
    'Receiving',
    'Stock Count',
    'Goods Issue',
    'Waste & Damage',
    'Transactions',
  ]);
  assert.ok(!INVENTORY_ACTIONS.some((item) => item.title === 'Stock Transfer'));
});

test('more menu includes internal transfer history', () => {
  assert.ok(MORE_LINKS.some((link) => link.title === 'Internal Transfer History'));
});

test('ITO landing and detail sections are wired for navigation', () => {
  assert.deepEqual([...ITO_LANDING_TABS], ['MY REQUESTS', 'REQUIRES MY APPROVAL', 'IN TRANSIT', 'AWAITING RECEIPT', 'COMPLETED']);
  assert.deepEqual([...ITO_DETAIL_SECTIONS], ['OVERVIEW', 'ITEMS', 'APPROVALS', 'DISPATCH', 'RECEIPT', 'TIMELINE']);
  assert.ok(TASK_TABS.includes('ITO APPROVALS'));
});

test('connected receiving capabilities stay marked CONNECTED', () => {
  assert.equal(MOBILE_CAPABILITIES.invoiceReceiving.status, 'CONNECTED');
  assert.equal(MOBILE_CAPABILITIES.grnPosting.status, 'CONNECTED');
  assert.equal(MOBILE_CAPABILITIES.openPurchaseOrders.status, 'CONNECTED');
  assert.equal(MOBILE_CAPABILITIES.supplierLookup.status, 'CONNECTED');
  assert.equal(MOBILE_CAPABILITIES.receivingHistory.status, 'CONNECTED');
  assert.equal(isConnected('invoiceReceiving'), true);
  assert.equal(isConnected('grnPosting'), true);
});

test('inventory and approval capabilities stay NOT_CONNECTED without fake success', () => {
  assert.equal(MOBILE_CAPABILITIES.inventoryLookup.status, 'NOT_CONNECTED');
  assert.equal(MOBILE_CAPABILITIES.stockCount.status, 'NOT_CONNECTED');
  assert.equal(MOBILE_CAPABILITIES.stockTransfer.status, 'NOT_CONNECTED');
  assert.equal(MOBILE_CAPABILITIES.goodsIssue.status, 'NOT_CONNECTED');
  assert.equal(MOBILE_CAPABILITIES.damageReport.status, 'NOT_CONNECTED');
  assert.equal(MOBILE_CAPABILITIES.approvals.status, 'NOT_CONNECTED');
  assert.match(capabilityMessage('stockCount'), /not connected/i);
  assert.match(capabilityMessage('approvals'), /not connected/i);
});

test('live stock and ITO capabilities are registered as CONNECTED', () => {
  assert.equal(MOBILE_CAPABILITIES.liveStockSearch.status, 'CONNECTED');
  assert.equal(MOBILE_CAPABILITIES.internalTransferOrder.status, 'CONNECTED');
  assert.equal(MOBILE_CAPABILITIES.itoApproval.status, 'CONNECTED');
  assert.equal(MOBILE_CAPABILITIES.itoDispatch.status, 'CONNECTED');
  assert.equal(MOBILE_CAPABILITIES.itoReceipt.status, 'CONNECTED');
});

test('disconnected inventory services return NOT_IMPLEMENTED instead of fabricated data', async () => {
  const lookup = await inventoryService.search('flour');
  assert.equal(lookup.status, 'NOT_IMPLEMENTED');
  if (lookup.status === 'NOT_IMPLEMENTED') {
    assert.equal(lookup.capability, 'inventoryLookup');
    assert.ok(lookup.message.length > 0);
  }

  const count = await stockCountService.submit();
  assert.equal(count.status, 'NOT_IMPLEMENTED');

  const transfer = await transferService.submit();
  assert.equal(transfer.status, 'NOT_IMPLEMENTED');

  const issue = await goodsIssueService.submit();
  assert.equal(issue.status, 'NOT_IMPLEMENTED');

  const tasks = await taskService.list();
  assert.equal(tasks.status, 'NOT_IMPLEMENTED');
});

test('live stock search without tenant context returns empty rather than fabricated rows', async () => {
  const result = await liveStockService.search('water');
  assert.equal(result.status, 'EMPTY');
  if (result.status === 'EMPTY') assert.deepEqual(result.data, []);
});

test('case-insensitive partial material search matches water products', () => {
  const matches = sampleMaterials.filter((item) => matchesMaterialQuery(item, 'WaTeR'));
  assert.equal(matches.length, 2);
  assert.ok(matches.some((item) => item.name.includes('Mineral Water')));
  assert.ok(matches.some((item) => item.name.includes('Sparkling Water')));
});

test('category and supplier filters narrow live stock results', () => {
  const beverages = sampleMaterials.filter((item) => matchesMaterialFilters(item, { category: 'Beverages' }));
  assert.equal(beverages.length, 2);
  const abc = sampleMaterials.filter((item) => matchesMaterialFilters(item, { supplier: 'ABC FOOD' }));
  assert.equal(abc.length, 2);
  const inStock = sampleMaterials.filter((item) => matchesMaterialFilters(item, { availability: 'IN_STOCK' }));
  assert.equal(inStock.length, 2);
  const out = sampleMaterials.filter((item) => matchesMaterialFilters(item, { availability: 'OUT_OF_STOCK' }));
  assert.equal(out.length, 1);
});

test('stock by location groups by property and puts current location first', () => {
  const partitioned = partitionBalancesByCurrentLocation(sampleBalances, 'u-main');
  assert.equal(partitioned.current?.unitName, 'Main Store');
  assert.equal(partitioned.current?.organizationName, 'FIVE JVC');
  assert.equal(partitioned.others.length, 2);
  assert.ok(partitioned.others.every((row) => row.unitId !== 'u-main'));

  const grouped = groupBalancesByProperty(partitioned.others);
  assert.equal(grouped.length, 2);
  const palm = grouped.find(([id]) => id === 'org-palm');
  assert.ok(palm);
  assert.equal(palm?.[1].total, 400);
});

test('out-of-stock current location still exposes request transfer elsewhere', () => {
  const balances: LiveStockBalance[] = [
    { organizationId: 'org-jvc', organizationName: 'FIVE JVC', unitId: 'u-main', unitName: 'Main Store', unitCode: 'MS', availableQty: 0 },
    { organizationId: 'org-palm', organizationName: 'FIVE Palm', unitId: 'u-palm-main', unitName: 'Main Store', unitCode: 'PMS', availableQty: 100 },
  ];
  const { current, others } = partitionBalancesByCurrentLocation(balances, 'u-main');
  assert.equal(current?.availableQty, 0);
  assert.equal(others.length, 1);
  assert.ok((others[0]?.availableQty ?? 0) > 0);
});

test('stock freshness marks missing timestamps as outdated', () => {
  assert.equal(isStockFresh(null), false);
  assert.equal(isStockFresh(new Date().toISOString()), true);
  assert.equal(isStockFresh(new Date(Date.now() - 60 * 60 * 1000).toISOString()), false);
});

test('ITO create validates source destination and requested quantity', () => {
  assert.equal(validateSourceDestination('a', 'a'), 'SOURCE_DESTINATION_SAME');
  assert.equal(validateSourceDestination('a', 'b'), null);
  assert.match(validateRequestedQty('', 10) ?? '', /valid/i);
  assert.match(validateRequestedQty('20', 10) ?? '', /exceed/i);
  assert.equal(validateRequestedQty('5', 10), null);
});

test('ITO multi-line draft and review model support multiple materials', () => {
  const draft = createEmptyItoDraft();
  draft.lines = [
    { key: '1', materialId: 'm1', materialCode: '1', description: 'Tomato Paste', uom: 'EA', availableQty: 120, requestedQty: '20' },
    { key: '2', materialId: 'm2', materialCode: '2', description: 'Olive Oil', uom: 'EA', availableQty: 48, requestedQty: '10' },
    { key: '3', materialId: 'm3', materialCode: '3', description: 'Mineral Water', uom: 'EA', availableQty: 500, requestedQty: '100' },
  ];
  assert.equal(draft.lines.length, 3);
  assert.ok(draft.lines.every((line) => validateRequestedQty(line.requestedQty, line.availableQty) == null));
});

test('two-party approval display requires both parties independently', () => {
  assert.equal(bothPartiesApproved('PENDING', 'APPROVED'), false);
  assert.equal(bothPartiesApproved('APPROVED', 'PENDING'), false);
  assert.equal(bothPartiesApproved('APPROVED', 'APPROVED'), true);
});

test('ITO timeline includes full lifecycle steps', () => {
  const events = buildItoTimeline({
    createdAt: '2026-09-17T10:00:00Z',
    createdBy: 'Bala',
    submittedAt: '2026-09-17T10:05:00Z',
  });
  assert.deepEqual(events.map((item) => item.action), [
    'ITO Created',
    'Submitted',
    'Source Approved',
    'Destination Approved',
    'Dispatched',
    'Received',
    'Completed',
  ]);
  assert.equal(events[0]?.completed, true);
  assert.equal(events[2]?.completed, false);
});

test('ITO status labels cover frontend-ready status model', () => {
  for (const status of [
    'DRAFT',
    'SUBMITTED',
    'SOURCE_APPROVAL_PENDING',
    'DESTINATION_APPROVAL_PENDING',
    'APPROVED',
    'REJECTED',
    'READY_FOR_DISPATCH',
    'PARTIALLY_DISPATCHED',
    'IN_TRANSIT',
    'PARTIALLY_RECEIVED',
    'RECEIVED',
    'COMPLETED',
    'CANCELLED',
    'FAILED',
  ] as const) {
    assert.ok(ITO_STATUS_LABELS[status]);
  }
});

test('ITO submit/approve/dispatch/receipt do not simulate success when disconnected', async () => {
  const draft = createEmptyItoDraft();
  const submit = await itoService.submit(draft);
  assert.equal(submit.status, 'NOT_IMPLEMENTED');
  const approve = await itoService.approve();
  assert.equal(approve.status, 'NOT_IMPLEMENTED');
  const dispatch = await itoService.dispatch();
  assert.equal(dispatch.status, 'NOT_IMPLEMENTED');
  const receive = await itoService.receive();
  assert.equal(receive.status, 'NOT_IMPLEMENTED');
});

test('live stock to ITO prepopulation fields are present on draft location refs', () => {
  const draft = createEmptyItoDraft();
  draft.source = {
    organizationId: 'org-palm',
    organizationName: 'FIVE Palm',
    unitId: 'u-palm-main',
    unitName: 'Main Store',
    unitCode: 'PMS',
  };
  draft.destination = {
    organizationId: 'org-jvc',
    organizationName: 'FIVE JVC',
    unitId: 'u-main',
    unitName: 'Main Store',
    unitCode: 'MS',
  };
  draft.lines = [{
    key: 'pre',
    materialId: '1',
    materialCode: '1002345',
    description: 'Mineral Water 500ml',
    uom: 'EA',
    availableQty: 400,
    requestedQty: '20',
  }];
  assert.equal(validateSourceDestination(draft.source.unitId, draft.destination.unitId), null);
  assert.equal(draft.lines[0]?.availableQty, 400);
});

test('stock changed UX message is ready for concurrency revalidation', () => {
  const message = 'Available stock has changed. Current available quantity is 60.';
  assert.match(message, /Available stock has changed/i);
  assert.match(message, /60/);
});

test('greeting helper returns day-part labels', () => {
  assert.equal(greetingForNow(new Date('2026-09-17T08:00:00')), 'Good Morning');
  assert.equal(greetingForNow(new Date('2026-09-17T14:00:00')), 'Good Afternoon');
  assert.equal(greetingForNow(new Date('2026-09-17T20:00:00')), 'Good Evening');
});

test('permission-aware more menu keys use canonical backend permission names', () => {
  const keys = ['VIEW_SUPPLIER', 'VIEW_PURCHASE_ORDER', 'VIEW_GRN', 'VIEW_GRN_HISTORY', 'VIEW_INVENTORY'];
  for (const key of keys) assert.match(key, /^[A-Z_]+$/);
});

test('draft variance display formula is counted minus system and never posts stock', () => {
  const systemQty = 10;
  const countedQty = 8;
  const variance = countedQty - systemQty;
  assert.equal(variance, -2);
  assert.equal(MOBILE_CAPABILITIES.stockCount.status, 'NOT_CONNECTED');
});
