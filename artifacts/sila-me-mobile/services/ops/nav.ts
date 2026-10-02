export const HOME_QUICK_ACTIONS = [
  { testID: 'qa-scan-invoice', icon: 'camera', label: 'Scan Invoice', href: '/receive/scan' },
  { testID: 'qa-receive-goods', icon: 'inbox', label: 'Receive Goods', href: '/receive/against-po' },
  { testID: 'qa-live-stock', icon: 'package', label: 'Live Stock', href: '/inventory/live-stock' },
  { testID: 'qa-raise-ito', icon: 'shuffle', label: 'Raise ITO', href: '/inventory/ito/new' },
  { testID: 'qa-stock-count', icon: 'clipboard', label: 'Stock Count', href: '/inventory/count' },
  { testID: 'qa-goods-issue', icon: 'send', label: 'Goods Issue', href: '/inventory/issue' },
] as const;

export const INVENTORY_ACTIONS = [
  { title: 'Live Inventory', href: '/inventory/live-stock' },
  { title: 'Quick Transfer', href: '/inventory/quick-transfer' },
  { title: 'Transfers', href: '/inventory/ito' },
  { title: 'Receiving', href: '/receive/against-po' },
  { title: 'Stock Count', href: '/inventory/count' },
  { title: 'Goods Issue', href: '/inventory/issue' },
  { title: 'Waste & Damage', href: '/inventory/damage' },
  { title: 'Transactions', href: '/inventory/movements' },
] as const;

export const MORE_LINKS = [
  { title: 'Documents', href: '/documents' },
  { title: 'Suppliers', href: '/suppliers' },
  { title: 'Purchase Orders', href: '/purchase-orders' },
  { title: 'GRN History', href: '/receive/history' },
  { title: 'Internal Transfer History', href: '/inventory/ito/history' },
  { title: 'Movement History', href: '/inventory/movements' },
  { title: 'Batch & Expiry', href: '/inventory/batches' },
  { title: 'Profile', href: '/profile' },
  { title: 'Settings', href: '/settings' },
  { title: 'Help', href: '/help' },
  { title: 'About SILA ME', href: '/about' },
] as const;

export const TASK_TABS = ['MY TASKS', 'ITO APPROVALS', 'APPROVALS', 'EXCEPTIONS', 'COMPLETED'] as const;

export const ITO_LANDING_TABS = ['MY REQUESTS', 'REQUIRES MY APPROVAL', 'IN TRANSIT', 'AWAITING RECEIPT', 'COMPLETED'] as const;

export const ITO_DETAIL_SECTIONS = ['OVERVIEW', 'ITEMS', 'APPROVALS', 'DISPATCH', 'RECEIPT', 'TIMELINE'] as const;
