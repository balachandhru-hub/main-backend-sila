import { Link } from 'wouter';
import { Search, ShoppingCart } from 'lucide-react';
import { useMemo, useState } from 'react';
import { getGetAccessContextQueryKey, getSearchPurchaseOrdersQueryKey, useGetAccessContext, useSearchPurchaseOrders } from '@workspace/api-client-react';
import { SilaPageHeader, SilaDataTable, StatusBadge, QueryError } from '@/components/sila-ui';
import { MasterDataExcelPanel } from '@/components/master-data-excel-panel';
import { formatDate, formatMoney, formatNumber } from '@/lib/formatters';
const request = { credentials: 'include' as const };

export default function PurchaseOrders() {
  const access = useGetAccessContext({ request, query: { queryKey: getGetAccessContextQueryKey(), retry: false } });
  const organizationId = access.data?.organizations[0]?.id;
  const units = access.data?.units ?? [];
  const [search, setSearch] = useState('');
  const [entityCode, setEntityCode] = useState('');
  const [operatingUnitId, setOperatingUnitId] = useState('');
  const [openOnly, setOpenOnly] = useState(false);
  const filters = {
    query: search || undefined,
    organizationId,
    entityCode: entityCode || undefined,
    operatingUnitId: operatingUnitId || undefined,
    openOnly,
  };
  const query = useSearchPurchaseOrders(filters, { request, query: { queryKey: getSearchPurchaseOrdersQueryKey(filters), retry: false, enabled: Boolean(organizationId) } });
  const rows = useMemo(() => query.data ?? [], [query.data]);
  return <><SilaPageHeader eyebrow="Receiving / Procurement" title="Purchase orders" description="Follow every order from committed quantity through actual receipt." actions={<button className="sila-button sila-button--primary" type="button" disabled data-testid="button-create-purchase-order"><ShoppingCart size={14} /> New purchase order</button>} />
  <MasterDataExcelPanel organizationId={organizationId} entityCode={entityCode} kind="PURCHASE_ORDERS" onImported={() => query.refetch()} />
  <div className="sila-filter-row"><input className="sila-input" value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Search PO number or supplier" aria-label="Search purchase orders" data-testid="input-search-purchase-orders" /><input className="sila-input" value={entityCode} onChange={(event) => setEntityCode(event.target.value)} placeholder="Entity code" aria-label="Filter purchase orders by entity" /><select className="sila-select" value={operatingUnitId} onChange={(event) => setOperatingUnitId(event.target.value)} aria-label="Filter purchase orders by operating unit"><option value="">All operating units</option>{units.map((unit) => <option value={unit.id} key={unit.id}>{unit.name}</option>)}</select><label className="sila-checkbox"><input type="checkbox" checked={openOnly} onChange={(event) => setOpenOnly(event.target.checked)} /> Open only</label><button className="sila-button" type="button" onClick={() => query.refetch()} data-testid="button-search-purchase-orders"><Search size={14} /> Search</button></div>{query.isError ? <QueryError onRetry={() => query.refetch()} /> : <SilaDataTable loading={query.isPending} empty={!query.isPending && rows.length === 0} emptyTitle="No purchase orders found" emptyDescription="Orders created in the connected procurement service will appear here."><table className="sila-table"><thead><tr><th>PO number</th><th>Entity</th><th>Supplier</th><th>Order date</th><th>Expected delivery</th><th className="sila-table__right">Total</th><th>Status</th></tr></thead><tbody>{rows.map((po) => { const scopedEntityCode = po.entityCode ?? 'DEFAULT'; return <tr key={po.id}><td><Link className="sila-table__primary" href={`/receiving/purchase-orders/${encodeURIComponent(po.poNumber)}?organizationId=${po.organizationId}&entityCode=${encodeURIComponent(scopedEntityCode)}`} data-testid={`link-purchase-order-${po.id}`}>{po.poNumber}</Link><span className="sila-table__secondary">{po.items.length} line items</span></td><td>{scopedEntityCode}</td><td>{po.supplierName}</td><td>{formatDate(po.poDate)}</td><td>{formatDate(po.deliveryDate)}</td><td className="sila-table__right">{po.totalAmount == null ? '—' : formatMoney(po.totalAmount, po.currency)}<span className="sila-table__secondary">{po.totalOrderedQuantity == null ? '' : `${formatNumber(po.totalOrderedQuantity)} ordered`}</span></td><td><StatusBadge value={po.status} /></td></tr>; })}</tbody></table></SilaDataTable>}</>;
}