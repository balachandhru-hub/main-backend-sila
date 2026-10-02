import {
  getInvoice,
  getPurchaseOrder,
  getGrn,
  listSuppliers,
  searchPurchaseOrders,
  getGrns,
  type GoodsReceipt,
  type PurchaseOrder,
  type Supplier,
} from '@workspace/api-client-react';
import { capabilityMessage, isConnected, type MobileCapabilityKey } from './capabilities';

export type OpsResult<T> =
  | { status: 'OK'; data: T }
  | { status: 'NOT_IMPLEMENTED'; capability: MobileCapabilityKey; message: string }
  | { status: 'EMPTY'; data: T }
  | { status: 'ERROR'; message: string };

function notImplemented<T>(capability: MobileCapabilityKey): OpsResult<T> {
  return { status: 'NOT_IMPLEMENTED', capability, message: capabilityMessage(capability) };
}

export const supplierService = {
  async search(organizationId: string, query: string): Promise<OpsResult<Supplier[]>> {
    if (!organizationId) return { status: 'EMPTY', data: [] };
    try {
      const data = await listSuppliers({ organizationId, query: query.trim() || undefined, status: 'ACTIVE' });
      return data.length ? { status: 'OK', data } : { status: 'EMPTY', data };
    } catch {
      return { status: 'ERROR', message: 'Supplier search is temporarily unavailable.' };
    }
  },
};

export const purchaseOrderService = {
  async searchOpen(params: {
    organizationId?: string;
    supplierId?: string | null;
    query?: string;
  }): Promise<OpsResult<PurchaseOrder[]>> {
    try {
      const data = await searchPurchaseOrders({
        organizationId: params.organizationId,
        supplierId: params.supplierId ?? undefined,
        query: params.query || undefined,
        openOnly: true,
      });
      return data.length ? { status: 'OK', data } : { status: 'EMPTY', data };
    } catch {
      return { status: 'ERROR', message: 'Open purchase orders could not be loaded.' };
    }
  },
  async getByNumber(poNumber: string, organizationId?: string): Promise<OpsResult<PurchaseOrder>> {
    try {
      const data = await getPurchaseOrder(poNumber, { organizationId });
      return { status: 'OK', data };
    } catch {
      return { status: 'ERROR', message: 'The purchase order could not be loaded.' };
    }
  },
};

export const grnService = {
  async list(): Promise<OpsResult<GoodsReceipt[]>> {
    try {
      const data = await getGrns();
      return data.length ? { status: 'OK', data } : { status: 'EMPTY', data };
    } catch {
      return { status: 'ERROR', message: 'Receiving history is temporarily unavailable.' };
    }
  },
  async get(id: string): Promise<OpsResult<GoodsReceipt>> {
    try {
      return { status: 'OK', data: await getGrn(id) };
    } catch {
      return { status: 'ERROR', message: 'The goods receipt could not be loaded.' };
    }
  },
  async pending(): Promise<OpsResult<GoodsReceipt[]>> {
    const listed = await this.list();
    if (listed.status !== 'OK' && listed.status !== 'EMPTY') return listed;
    const pending = (listed.status === 'OK' || listed.status === 'EMPTY' ? listed.data : []).filter((item) =>
      ['DRAFT', 'READY_TO_POST', 'POSTING', 'FAILED', 'UNKNOWN'].includes(item.status) ||
      item.status === 'FAILED' ||
      item.status === 'UNKNOWN' ||
      item.status === 'POSTING');
    return pending.length ? { status: 'OK', data: pending } : { status: 'EMPTY', data: pending };
  },
};

export const invoiceService = {
  async get(id: string) {
    try {
      return { status: 'OK' as const, data: await getInvoice(id) };
    } catch {
      return { status: 'ERROR' as const, message: 'The invoice could not be loaded.' };
    }
  },
};

export type InventoryMaterial = {
  id: string;
  materialCode: string;
  description: string;
  availableQty: number | null;
  uom: string;
  location?: string | null;
  batch?: string | null;
  expiryDate?: string | null;
};

export const inventoryService = {
  async search(_query: string): Promise<OpsResult<InventoryMaterial[]>> {
    if (!isConnected('inventoryLookup')) return notImplemented('inventoryLookup');
    return { status: 'EMPTY', data: [] };
  },
  async get(_id: string): Promise<OpsResult<InventoryMaterial>> {
    if (!isConnected('inventoryLookup')) return notImplemented('inventoryLookup');
    return { status: 'ERROR', message: 'Material detail is not available.' };
  },
};

export const stockCountService = {
  async list() {
    return notImplemented<'list'>('stockCount');
  },
  async submit() {
    return notImplemented<'submit'>('stockCount');
  },
};

export const transferService = {
  async submit() {
    return notImplemented<'submit'>('stockTransfer');
  },
};

export const goodsIssueService = {
  async submit() {
    return notImplemented<'submit'>('goodsIssue');
  },
};

export const documentService = {
  async list() {
    return notImplemented<'list'>('documents');
  },
};

export const taskService = {
  async list() {
    return notImplemented<'list'>('approvals');
  },
};

export { liveStockService, itoService } from './live-stock-ito-service';
export type { LiveStockMaterial, LiveStockBalance, LiveStockFilters, LiveStockAvailabilityFilter } from './live-stock-ito-service';
export {
  matchesMaterialQuery,
  matchesMaterialFilters,
  normalizeSearchText,
  partitionBalancesByCurrentLocation,
  groupBalancesByProperty,
  isStockFresh,
} from './live-stock-ito-service';
export * from './ito-models';
