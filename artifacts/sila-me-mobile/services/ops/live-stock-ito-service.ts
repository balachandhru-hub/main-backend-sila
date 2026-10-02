import { customFetch } from '@workspace/api-client-react';
import { capabilityMessage, isConnected } from './capabilities';
import type { OpsResult } from './index';
import type { ItoDraft } from './ito-models';

export type LiveStockMaterial = {
  id: string;
  materialCode: string;
  name: string;
  description: string;
  category?: string | null;
  materialGroup?: string | null;
  type?: string | null;
  supplierName?: string | null;
  supplierId?: string | null;
  supplierMaterialNumber?: string | null;
  barcode?: string | null;
  brand?: string | null;
  uom: string;
  totalAvailable: number | null;
  locationCount: number | null;
  lastUpdatedAt?: string | null;
  unitCost?: number | null;
  currency?: string | null;
  priceStatus?: string | null;
};

export type LiveStockBalance = {
  organizationId: string;
  organizationName: string;
  unitId: string;
  unitName: string;
  unitCode: string;
  availableQty: number | null;
  onHandQty?: number | null;
  reservedQty?: number | null;
  inTransitQty?: number | null;
  transferableQty?: number | null;
  locationType?: string | null;
  isCurrentLocation?: boolean;
};

export type LiveStockAvailabilityFilter = 'IN_STOCK' | 'LOW_STOCK' | 'OUT_OF_STOCK' | 'IN_TRANSIT';
export type LiveStockFilters = {
  propertyId?: string | null;
  unitId?: string | null;
  category?: string | null;
  materialGroup?: string | null;
  type?: string | null;
  supplier?: string | null;
  availability?: LiveStockAvailabilityFilter | null;
  batch?: string | null;
  expiry?: string | null;
};

export type LiveDecision = {
  material: LiveStockMaterial;
  balances: LiveStockBalance[];
  requiredQty: number;
  localAvailable: number;
  shortage: number;
  recommendation: string;
  recommendationReason: string;
  nextActions: string[];
};

export type ItoListItem = {
  id: string;
  itoNumber: string;
  mode: string;
  fromLocation: string;
  toLocation: string;
  transferRelationship: string;
  status: string;
  requestedAt: string;
  totalValue: number;
};

export type ItoDetailApi = {
  id: string;
  itoNumber: string;
  status: string;
  mode: string;
  transferRelationship: string;
  fromInventoryLocationId: string;
  fromLocation: string;
  fromType: string;
  toInventoryLocationId: string;
  toLocation: string;
  toType: string;
  reason?: string | null;
  lines: Array<{ id: string; materialId: string; materialCode: string; description: string; requestedQty: number; approvedQty: number; dispatchedQty: number; receivedQty: number; uom: string; sourceAvailable?: number | null; sourceAfter?: number | null }>;
  approvals: Array<{ id: string; side: string; status: string; availableQty?: number | null; requestedQty?: number | null; stockAfter?: number | null }>;
  events: Array<{ id: string; action: string; comment?: string | null; createdAt: string }>;
  allowedActions: string[];
};

export type MobileHome = {
  myLocationId?: string | null;
  myLocationName?: string | null;
  myLocationType?: string | null;
  availableItems: number;
  lowStock: number;
  incoming: number;
  myActions: number;
  incomingTransfers: number;
  awaitingReceipt: number;
  approvals: number;
  criticalAlerts: number;
};

export type InventoryLocationOption = {
  id: string;
  locationCode: string;
  locationName: string;
  locationType: string;
  transferEnabled: boolean;
};

function notImplemented<T>(capability: 'liveStockSearch' | 'internalTransferOrder' | 'itoApproval' | 'itoDispatch' | 'itoReceipt'): OpsResult<T> {
  return { status: 'NOT_IMPLEMENTED', capability, message: capabilityMessage(capability) };
}

async function json<T>(url: string, init?: RequestInit): Promise<T> {
  return customFetch<T>(url, { ...init, responseType: 'json' });
}

export function normalizeSearchText(value: string) {
  return value.trim().toLowerCase().replace(/\s+/g, ' ');
}

export function matchesMaterialQuery(material: LiveStockMaterial, query: string) {
  const q = normalizeSearchText(query);
  if (!q) return true;
  const haystack = normalizeSearchText([
    material.materialCode, material.name, material.description, material.category, material.materialGroup,
    material.type, material.supplierName, material.supplierId, material.supplierMaterialNumber, material.barcode, material.brand,
  ].filter(Boolean).join(' '));
  return haystack.includes(q);
}

export function matchesMaterialFilters(material: LiveStockMaterial, filters: LiveStockFilters) {
  if (filters.category && normalizeSearchText(material.category ?? '') !== normalizeSearchText(filters.category)) return false;
  if (filters.materialGroup && normalizeSearchText(material.materialGroup ?? '') !== normalizeSearchText(filters.materialGroup)) return false;
  if (filters.type && normalizeSearchText(material.type ?? '') !== normalizeSearchText(filters.type)) return false;
  if (filters.supplier) {
    const supplierHaystack = normalizeSearchText(`${material.supplierName ?? ''} ${material.supplierId ?? ''}`);
    if (!supplierHaystack.includes(normalizeSearchText(filters.supplier))) return false;
  }
  if (filters.availability === 'IN_STOCK' && !(material.totalAvailable != null && material.totalAvailable > 0)) return false;
  if (filters.availability === 'OUT_OF_STOCK' && material.totalAvailable !== 0) return false;
  if (filters.availability === 'LOW_STOCK' && !(material.totalAvailable != null && material.totalAvailable > 0 && material.totalAvailable <= 10)) return false;
  return true;
}

export function partitionBalancesByCurrentLocation(balances: LiveStockBalance[], currentUnitId?: string | null) {
  const tagged = balances.map((row) => ({ ...row, isCurrentLocation: Boolean(currentUnitId && row.unitId === currentUnitId) }));
  return { current: tagged.find((row) => row.isCurrentLocation) ?? null, others: tagged.filter((row) => !row.isCurrentLocation) };
}

export function groupBalancesByProperty(balances: LiveStockBalance[]) {
  const map = new Map<string, { name: string; rows: LiveStockBalance[]; total: number | null }>();
  for (const row of balances) {
    const current = map.get(row.organizationId) ?? { name: row.organizationName, rows: [], total: 0 };
    current.rows.push(row);
    if (row.availableQty != null && current.total != null) current.total += row.availableQty;
    else current.total = null;
    map.set(row.organizationId, current);
  }
  return [...map.entries()];
}

export function isStockFresh(lastUpdatedAt?: string | null, now = Date.now(), maxAgeMs = 15 * 60 * 1000) {
  if (!lastUpdatedAt) return false;
  const ts = Date.parse(lastUpdatedAt);
  if (Number.isNaN(ts)) return false;
  return now - ts <= maxAgeMs;
}

function toMaterial(item: { id: string; materialCode: string; description: string; baseUom: string; unitCost?: number | null; currency?: string | null; priceStatus?: string | null; materialGroup?: string | null; category?: string | null; totalAvailable: number }): LiveStockMaterial {
  return {
    id: item.id, materialCode: item.materialCode, name: item.description, description: item.description, uom: item.baseUom,
    materialGroup: item.materialGroup, category: item.category, unitCost: item.unitCost, currency: item.currency,
    priceStatus: item.priceStatus, totalAvailable: item.totalAvailable, locationCount: null,
  };
}

export const liveStockService = {
  async search(query: string, organizationId?: string, filters?: LiveStockFilters): Promise<OpsResult<LiveStockMaterial[]>> {
    if (!isConnected('liveStockSearch')) return notImplemented('liveStockSearch');
    if (!organizationId || query.trim().length < 2) return { status: 'EMPTY', data: [] };
    try {
      const params = new URLSearchParams({ organizationId, query: query.trim(), pageSize: '20' });
      if (filters?.materialGroup) params.set('materialGroup', filters.materialGroup);
      if (filters?.category) params.set('category', filters.category);
      const page = await json<{ items: Array<Parameters<typeof toMaterial>[0]> }>(`/api/v1/inventory/live/search?${params}`);
      const data = page.items.map(toMaterial);
      return data.length ? { status: 'OK', data } : { status: 'EMPTY', data };
    } catch {
      return { status: 'ERROR', message: 'Live inventory search is temporarily unavailable.' };
    }
  },
  async getMaterial(id: string, organizationId?: string): Promise<OpsResult<LiveStockMaterial>> {
    const live = await this.live(id, organizationId);
    if (live.status !== 'OK') return live.status === 'EMPTY' ? { status: 'ERROR', message: 'Material detail is not available.' } : live;
    return { status: 'OK', data: live.data.material };
  },
  async balances(materialId: string, organizationId?: string, currentLocationId?: string): Promise<OpsResult<LiveStockBalance[]>> {
    const live = await this.live(materialId, organizationId, 0, currentLocationId);
    if (live.status !== 'OK') return live.status === 'NOT_IMPLEMENTED' ? live : { status: 'EMPTY', data: [] };
    return { status: 'OK', data: live.data.balances };
  },
  async live(materialId: string, organizationId?: string, requiredQty = 0, currentLocationId?: string): Promise<OpsResult<LiveDecision>> {
    if (!isConnected('liveStockSearch')) return notImplemented('liveStockSearch');
    if (!organizationId) return { status: 'ERROR', message: 'Material detail is not available.' };
    try {
      const params = new URLSearchParams({ organizationId, requiredQty: String(requiredQty) });
      if (currentLocationId) params.set('currentLocationId', currentLocationId);
      const detail = await json<{
        material: Parameters<typeof toMaterial>[0];
        availability: Array<{ inventoryLocationId: string; locationName: string; locationCode: string; locationType: string; propertyCode?: string | null; onHandQty: number; reservedQty: number; availableQty: number; inTransitQty: number; transferableQty: number }>;
        requiredQty: number; localAvailable: number; shortage: number; recommendation: string; recommendationReason: string; nextActions: string[];
      }>(`/api/v1/inventory/live/${materialId}?${params}`);
      return {
        status: 'OK',
        data: {
          material: toMaterial(detail.material),
          balances: detail.availability.map((row) => ({
            organizationId: row.propertyCode ?? organizationId, organizationName: row.propertyCode ?? '', unitId: row.inventoryLocationId,
            unitName: row.locationName, unitCode: row.locationCode, availableQty: row.availableQty, onHandQty: row.onHandQty,
            reservedQty: row.reservedQty, inTransitQty: row.inTransitQty, transferableQty: row.transferableQty, locationType: row.locationType,
          })),
          requiredQty: detail.requiredQty, localAvailable: detail.localAvailable, shortage: detail.shortage,
          recommendation: detail.recommendation, recommendationReason: detail.recommendationReason, nextActions: detail.nextActions,
        },
      };
    } catch {
      return { status: 'ERROR', message: 'Live inventory is temporarily unavailable.' };
    }
  },
  async home(organizationId: string): Promise<OpsResult<MobileHome>> {
    try {
      const data = await json<MobileHome>(`/api/v1/inventory/mobile/home?organizationId=${organizationId}`);
      return { status: 'OK', data };
    } catch {
      return { status: 'ERROR', message: 'Inventory home is temporarily unavailable.' };
    }
  },
  async locations(organizationId: string): Promise<OpsResult<InventoryLocationOption[]>> {
    try {
      const data = await json<InventoryLocationOption[]>(`/api/v1/inventory/locations?organizationId=${organizationId}&active=true`);
      return data.length ? { status: 'OK', data } : { status: 'EMPTY', data };
    } catch {
      return { status: 'ERROR', message: 'Locations are temporarily unavailable.' };
    }
  },
};

export const itoService = {
  async list(organizationId?: string, status?: string): Promise<OpsResult<ItoListItem[]>> {
    if (!isConnected('internalTransferOrder')) return notImplemented('internalTransferOrder');
    if (!organizationId) return { status: 'EMPTY', data: [] };
    try {
      const params = new URLSearchParams({ organizationId });
      if (status) params.set('status', status);
      const data = await json<ItoListItem[]>(`/api/v1/inventory/transfers?${params}`);
      return data.length ? { status: 'OK', data } : { status: 'EMPTY', data };
    } catch {
      return { status: 'ERROR', message: 'Transfers are temporarily unavailable.' };
    }
  },
  async get(id: string, organizationId: string): Promise<OpsResult<ItoDetailApi>> {
    try {
      const data = await json<ItoDetailApi>(`/api/v1/inventory/transfers/${id}?organizationId=${organizationId}`);
      return { status: 'OK', data };
    } catch {
      return { status: 'ERROR', message: 'Transfer detail is temporarily unavailable.' };
    }
  },
  async submit(organizationId: string, body: object): Promise<OpsResult<ItoDetailApi>> {
    if (!isConnected('internalTransferOrder')) return notImplemented('internalTransferOrder');
    try {
      const data = await json<ItoDetailApi>(`/api/v1/inventory/transfers?organizationId=${organizationId}`, {
        method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body),
      });
      return { status: 'OK', data };
    } catch {
      return { status: 'ERROR', message: 'Could not submit the transfer.' };
    }
  },
  async submitDraft(organizationId: string, draft: ItoDraft) {
    if (!draft.source || !draft.destination) return { status: 'ERROR' as const, message: 'Choose from and to locations.' };
    return this.submit(organizationId, {
      mode: 'STANDARD',
      fromInventoryLocationId: draft.source.unitId,
      toInventoryLocationId: draft.destination.unitId,
      reason: draft.reason,
      alreadyCollected: false,
      lines: draft.lines.map((line) => ({ materialId: line.materialId, quantity: Number(line.requestedQty) })),
    });
  },
  async quickGetOne(organizationId: string, materialId: string, fromLocationId: string, toLocationId: string) {
    try {
      const data = await json<ItoDetailApi>(`/api/v1/inventory/transfers/quick?organizationId=${organizationId}&materialId=${materialId}&fromLocationId=${fromLocationId}&toLocationId=${toLocationId}`, { method: 'POST' });
      return { status: 'OK' as const, data };
    } catch {
      return { status: 'ERROR' as const, message: 'GET 1 could not be created.' };
    }
  },
  async act(organizationId: string, id: string, path: 'approve' | 'reject' | 'dispatch' | 'receive' | 'discrepancy' | 'handover' | 'dispute-already-collected', quantity?: number, comment?: string, confirmAlreadyCollected = false) {
    try {
      const data = await json<ItoDetailApi>(`/api/v1/inventory/transfers/${id}/${path}?organizationId=${organizationId}`, {
        method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ quantity: quantity ?? 0, approvedQty: quantity ?? null, comment, confirmAlreadyCollected }),
      });
      return { status: 'OK' as const, data };
    } catch {
      return { status: 'ERROR' as const, message: 'The transfer action could not be completed.' };
    }
  },
  async approve() { return notImplemented<'approve'>('itoApproval'); },
  async dispatch() { return notImplemented<'dispatch'>('itoDispatch'); },
  async receive() { return notImplemented<'receive'>('itoReceipt'); },
};

export const inventoryActionService = {
  async createPr(organizationId: string, materialId: string, quantity: number, locationId?: string) {
    return json(`/api/v1/inventory/purchase-requests?organizationId=${organizationId}`, {
      method: 'POST', headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ materialId, quantity, inventoryLocationId: locationId ?? null, reason: 'Mobile Live Inventory' }),
    });
  },
  async requestCount(organizationId: string, locationId: string, reason: string) {
    return json(`/api/v1/inventory/physical-inventory-requests?organizationId=${organizationId}`, {
      method: 'POST', headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ inventoryLocationId: locationId, reason, surpriseCount: false }),
    });
  },
  async alerts(organizationId: string) {
    return json<Array<{ id: string; kind: string; severity: string; status: string; title?: string | null; recommendedAction?: string | null }>>(`/api/v1/inventory/alerts?organizationId=${organizationId}`);
  },
};
