export type CapabilityStatus = 'CONNECTED' | 'PARTIAL' | 'NOT_CONNECTED';

export type MobileCapabilityKey =
  | 'invoiceReceiving'
  | 'grnPosting'
  | 'openPurchaseOrders'
  | 'supplierLookup'
  | 'receivingHistory'
  | 'inventoryLookup'
  | 'liveStockSearch'
  | 'stockCount'
  | 'stockTransfer'
  | 'internalTransferOrder'
  | 'itoApproval'
  | 'itoDispatch'
  | 'itoReceipt'
  | 'goodsIssue'
  | 'damageReport'
  | 'batchExpiry'
  | 'movementHistory'
  | 'documents'
  | 'approvals'
  | 'exceptions'
  | 'globalSearch';

export type MobileCapability = {
  key: MobileCapabilityKey;
  label: string;
  status: CapabilityStatus;
  endpoint?: string;
  message?: string;
};

export const MOBILE_CAPABILITIES: Record<MobileCapabilityKey, MobileCapability> = {
  invoiceReceiving: {
    key: 'invoiceReceiving',
    label: 'Invoice receiving',
    status: 'CONNECTED',
    endpoint: 'POST /api/v1/documents/invoices',
  },
  grnPosting: {
    key: 'grnPosting',
    label: 'GRN posting',
    status: 'CONNECTED',
    endpoint: 'POST /api/v1/grns',
  },
  openPurchaseOrders: {
    key: 'openPurchaseOrders',
    label: 'Open purchase orders',
    status: 'CONNECTED',
    endpoint: 'GET /api/v1/purchase-orders/search?openOnly=true',
  },
  supplierLookup: {
    key: 'supplierLookup',
    label: 'Supplier lookup',
    status: 'CONNECTED',
    endpoint: 'GET /api/v1/master-data/suppliers',
  },
  receivingHistory: {
    key: 'receivingHistory',
    label: 'Receiving history',
    status: 'CONNECTED',
    endpoint: 'GET /api/v1/grns',
  },
  inventoryLookup: {
    key: 'inventoryLookup',
    label: 'Stock lookup',
    status: 'NOT_CONNECTED',
    message: 'Backend connection is coming in the next implementation phase.',
  },
  liveStockSearch: {
    key: 'liveStockSearch',
    label: 'Live stock search',
    status: 'CONNECTED',
    endpoint: 'GET /api/v1/inventory/live/search',
  },
  stockCount: {
    key: 'stockCount',
    label: 'Stock count',
    status: 'NOT_CONNECTED',
    message: 'Stock count posting is not connected yet.',
  },
  stockTransfer: {
    key: 'stockTransfer',
    label: 'Stock transfer',
    status: 'NOT_CONNECTED',
    message: 'Use Internal Transfer Order for controlled internal movements.',
  },
  internalTransferOrder: {
    key: 'internalTransferOrder',
    label: 'Internal transfer order',
    status: 'CONNECTED',
    endpoint: 'POST /api/v1/inventory/transfers',
  },
  itoApproval: {
    key: 'itoApproval',
    label: 'ITO approval',
    status: 'CONNECTED',
    endpoint: 'POST /api/v1/inventory/transfers/{id}/approve',
  },
  itoDispatch: {
    key: 'itoDispatch',
    label: 'ITO dispatch',
    status: 'CONNECTED',
    endpoint: 'POST /api/v1/inventory/transfers/{id}/dispatch',
  },
  itoReceipt: {
    key: 'itoReceipt',
    label: 'ITO receipt',
    status: 'CONNECTED',
    endpoint: 'POST /api/v1/inventory/transfers/{id}/receive',
  },
  goodsIssue: {
    key: 'goodsIssue',
    label: 'Goods issue',
    status: 'NOT_CONNECTED',
    message: 'Backend connection is coming in the next implementation phase.',
  },
  damageReport: {
    key: 'damageReport',
    label: 'Damage / rejection',
    status: 'NOT_CONNECTED',
    message: 'Backend connection is coming in the next implementation phase.',
  },
  batchExpiry: {
    key: 'batchExpiry',
    label: 'Batch & expiry',
    status: 'NOT_CONNECTED',
    message: 'Backend connection is coming in the next implementation phase.',
  },
  movementHistory: {
    key: 'movementHistory',
    label: 'Movement history',
    status: 'NOT_CONNECTED',
    message: 'Backend connection is coming in the next implementation phase.',
  },
  documents: {
    key: 'documents',
    label: 'Documents',
    status: 'PARTIAL',
    endpoint: 'GET /api/v1/documents/{id}/transfers',
    message: 'Document browsing is limited until the documents catalog API is available.',
  },
  approvals: {
    key: 'approvals',
    label: 'Approvals',
    status: 'NOT_CONNECTED',
    message: 'Approval service is not connected yet.',
  },
  exceptions: {
    key: 'exceptions',
    label: 'Exceptions',
    status: 'PARTIAL',
    endpoint: 'GET /api/v1/grns',
    message: 'Only GRN posting failures from receiving history are available today.',
  },
  globalSearch: {
    key: 'globalSearch',
    label: 'Global search',
    status: 'PARTIAL',
    endpoint: 'GET /api/v1/master-data/suppliers, GET /api/v1/purchase-orders/search, GET /api/v1/grns',
  },
};

export function isConnected(key: MobileCapabilityKey) {
  return MOBILE_CAPABILITIES[key].status === 'CONNECTED';
}

export function capabilityMessage(key: MobileCapabilityKey) {
  return MOBILE_CAPABILITIES[key].message ?? 'Backend connection is coming in the next implementation phase.';
}
