import { useLocalSearchParams, useRouter } from 'expo-router';
import React, { useState } from 'react';
import { Pressable, Text } from 'react-native';
import { ReceiveShell, Panel, PrimaryButton, StatusPill } from '@/components/receive-ui';
import { useColors } from '@/hooks/useColors';
import { useQuery } from '@tanstack/react-query';
import { customFetch, getInvoice } from '@workspace/api-client-react';
export default function PurchaseOrderMatch() {
  const colors = useColors(); const router = useRouter(); const { invoiceId } = useLocalSearchParams<{ invoiceId: string }>(); const [busy, setBusy] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const invoice = useQuery({ queryKey: ['receive-invoice-po', invoiceId], queryFn: () => getInvoice(invoiceId ?? ''), enabled: Boolean(invoiceId) });
  const supplierId = (invoice.data as (typeof invoice.data & { supplierId?: string }) | undefined)?.supplierId;
  const candidates = useQuery({ queryKey: ['receive-supplier-candidates', invoiceId], queryFn: () => customFetch<{ candidates: Array<{ supplierId: string; supplierCode: string; name: string; confidence: number; matchReason: string }> }>(`/api/v1/invoices/${invoiceId}/supplier-candidates`, { responseType: 'json' }), enabled: Boolean(invoiceId && invoice.data && !invoice.data.supplierCode), retry: false });
  const purchaseOrders = useQuery({ queryKey: ['receive-open-pos', supplierId, invoice.data?.organizationId], queryFn: () => customFetch<Array<{ id: string; poNumber: string; supplierName: string; status: string; poDate?: string | null; currency: string; totalAmount?: number | null; items: Array<unknown> }>>(`/api/v1/purchase-orders/search?organizationId=${invoice.data?.organizationId ?? ''}&supplierId=${supplierId ?? ''}&openOnly=true`, { responseType: 'json' }), enabled: Boolean(supplierId && invoice.data?.organizationId), retry: false });
  const selectSupplier = async (supplierId: string) => {
    setBusy(true);
    setErrorMessage(null);
    try { await customFetch(`/api/v1/invoices/${invoiceId}/match-supplier`, { method: 'POST', responseType: 'json', body: JSON.stringify({ supplierId }), headers: { 'Content-Type': 'application/json' } }); await invoice.refetch(); await candidates.refetch(); }
    catch (error) { setErrorMessage(error instanceof Error ? error.message : 'The supplier could not be selected.'); }
    finally { setBusy(false); }
  };
  const selectPo = async (purchaseOrderId: string) => {
    setBusy(true);
    setErrorMessage(null);
    try { await customFetch(`/api/v1/invoices/${invoiceId}/match-po`, { method: 'POST', responseType: 'json', body: JSON.stringify({ purchaseOrderId }), headers: { 'Content-Type': 'application/json' } }); await invoice.refetch(); }
    catch (error) { setErrorMessage(error instanceof Error ? error.message : 'The purchase order could not be selected.'); }
    finally { setBusy(false); }
  };
  if (invoice.isPending) return <ReceiveShell title="Purchase order match"><Text style={{ color: colors.mutedForeground }}>Loading match…</Text></ReceiveShell>;
  const noPurchaseOrder = Boolean(invoice.data && !invoice.data.purchaseOrderNumber && !invoice.data.purchaseOrderId);
  return <ReceiveShell title="Purchase order match" fallback="/receive/invoice-review">
    <StatusPill label={invoice.data?.purchaseOrderId ? 'MATCHED' : 'REVIEW_REQUIRED'} tone={invoice.data?.purchaseOrderId ? 'success' : 'warning'} />
    <Panel>
      <Text style={{ color: colors.foreground, fontFamily: 'Inter_600SemiBold' }}>{invoice.data?.supplierCode ? `Supplier confirmed · ${invoice.data.supplierCode}` : 'Confirm supplier'}</Text>
      <Text style={{ color: colors.mutedForeground, marginTop: 8, fontFamily: 'Inter_400Regular' }}>Only active, unblocked suppliers and supplier-specific open POs can be used for receiving.</Text>
      {!invoice.data?.supplierCode && (candidates.data?.candidates ?? []).map(candidate => <Pressable key={candidate.supplierId} disabled={busy} onPress={() => void selectSupplier(candidate.supplierId)} style={{ marginTop: 14, padding: 12, borderWidth: 1, borderColor: colors.border, borderRadius: 8 }}><Text style={{ color: colors.foreground, fontFamily: 'Inter_600SemiBold' }}>{candidate.supplierCode} · {candidate.name}</Text><Text style={{ color: colors.mutedForeground, marginTop: 4 }}>{candidate.matchReason} match · {Math.round(candidate.confidence * 100)}%</Text></Pressable>)}
      {!invoice.data?.supplierCode && candidates.data?.candidates?.length === 0 && <Text style={{ color: colors.error, marginTop: 14 }}>No authoritative supplier match was found. Ask an administrator to add or alias this supplier.</Text>}
       {noPurchaseOrder && invoice.data?.supplierId && <Text style={{ color: colors.mutedForeground, marginTop: 14 }}>This invoice was saved without a purchase order. It is ready for review, but it cannot create a goods receipt until a PO is selected.</Text>}
       {errorMessage && <Text style={{ color: colors.error, marginTop: 14 }}>{errorMessage}</Text>}
    </Panel>
    {invoice.data?.supplierCode && <Panel><Text style={{ color: colors.foreground, fontFamily: 'Inter_600SemiBold' }}>Select an eligible open PO</Text>{(purchaseOrders.data ?? []).map(po => <Pressable key={po.id} disabled={busy} onPress={() => void selectPo(po.id)} style={{ marginTop: 14, padding: 12, borderWidth: 1, borderColor: colors.border, borderRadius: 8 }}><Text style={{ color: colors.foreground, fontFamily: 'Inter_600SemiBold' }}>{po.poNumber}</Text><Text style={{ color: colors.mutedForeground, marginTop: 4 }}>{po.supplierName} · {po.status}</Text></Pressable>)}{purchaseOrders.data?.length === 0 && <Text style={{ color: colors.mutedForeground, marginTop: 12 }}>No open PO with remaining quantity is available for this supplier and store.</Text>}</Panel>}
     <PrimaryButton label={noPurchaseOrder ? 'Finish review' : 'Review invoice lines'} onPress={() => noPurchaseOrder ? router.replace('/receive') : router.push({ pathname: '/receive/line-match', params: { invoiceId } })} disabled={(noPurchaseOrder ? !invoice.data?.supplierId : !invoice.data?.purchaseOrderId) || busy} />
  </ReceiveShell>;
}