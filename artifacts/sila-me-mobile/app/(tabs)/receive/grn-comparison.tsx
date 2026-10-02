import { useLocalSearchParams, useRouter } from 'expo-router';
import React, { useMemo } from 'react';
import { Text, View } from 'react-native';
import { useQuery } from '@tanstack/react-query';
import { getInvoice, getPurchaseOrder } from '@workspace/api-client-react';
import { ReceiveShell, Panel, PrimaryButton, formatQuantity } from '@/components/receive-ui';
import { useColors } from '@/hooks/useColors';
import { defaultPhysicalReceivedQuantity, formatOcrSupplierDisplay, invoiceQuantityForPoItem, isSupplierIdLabel } from '@/services/invoice-review-state';

export default function GrnComparison() {
  const colors = useColors();
  const router = useRouter();
  const { invoiceId, purchaseOrderId, documentId } = useLocalSearchParams<{ invoiceId: string; purchaseOrderId?: string; documentId?: string }>();
  const invoice = useQuery({ queryKey: ['receive-comparison-invoice', invoiceId], queryFn: () => getInvoice(invoiceId ?? ''), enabled: Boolean(invoiceId) });
  const poNumber = invoice.data?.purchaseOrderNumber;
  const po = useQuery({
    queryKey: ['receive-comparison-po', purchaseOrderId || poNumber, invoice.data?.organizationId],
    queryFn: () => getPurchaseOrder(poNumber ?? '', { organizationId: invoice.data?.organizationId }),
    enabled: Boolean(poNumber && invoice.data?.organizationId),
  });
  const eligible = useMemo(() => (po.data?.items ?? []).filter(item =>
    item.goodsReceiptExpected && !item.deletionIndicator && !item.deliveryCompleted && Number(item.openQuantity) > 0), [po.data]);
  const invoiceQty = (item: { id: string; lineNumber?: number | null; itemNumber?: string | null }) =>
    invoiceQuantityForPoItem(invoice.data?.lines ?? [], item);
  const serviceOnly = Boolean(po.data && po.data.items.length > 0 && po.data.items.every(item => !item.goodsReceiptExpected));
  if (invoice.isPending || (poNumber && po.isPending)) return <ReceiveShell title="GRN validation"><Text style={{ color: colors.mutedForeground }}>Loading PO comparison…</Text></ReceiveShell>;
  if (!invoice.data) return <ReceiveShell title="GRN validation"><Text style={{ color: colors.error }}>Invoice could not be loaded.</Text></ReceiveShell>;
  if (!po.data) {
    return <ReceiveShell title="GRN validation" fallback="/receive/invoice-review">
      <Text style={{ color: colors.mutedForeground, fontFamily: 'Inter_400Regular', marginBottom: 14 }}>No open purchase order is linked. The invoice is saved; goods receipt is not available yet.</Text>
      <PrimaryButton label="Select purchase order" onPress={() => router.push({ pathname: '/receive/po-match', params: { invoiceId } })} />
    </ReceiveShell>;
  }
  if (serviceOnly) {
    return <ReceiveShell title="GRN validation" fallback="/receive/invoice-review">
      <Panel>
        <Text style={{ color: colors.foreground, fontFamily: 'Inter_700Bold', fontSize: 16 }}>Service PO — goods receipt is not available in SILA ME.</Text>
        <Text style={{ color: colors.mutedForeground, marginTop: 10 }}>Invoice {invoice.data.invoiceNumber} remains saved. GRN status: NOT_APPLICABLE.</Text>
      </Panel>
      <PrimaryButton label="Done" onPress={() => router.replace('/receive')} />
    </ReceiveShell>;
  }
  const supplierName = invoice.data.supplierName && !isSupplierIdLabel(invoice.data.supplierName)
    ? invoice.data.supplierName
    : (formatOcrSupplierDisplay(invoice.data.supplierName ?? '') || invoice.data.supplierCode || '—');
  return <ReceiveShell title="GRN validation" fallback="/receive/invoice-review">
    <Panel>
      <Text style={{ color: colors.mutedForeground, fontFamily: 'Inter_600SemiBold', fontSize: 10, letterSpacing: 0.8 }}>SUPPLIER</Text>
      <Text style={{ color: colors.foreground, fontFamily: 'Inter_700Bold', fontSize: 16 }}>{supplierName}</Text>
      <Text style={{ color: colors.foreground, fontFamily: 'Inter_600SemiBold', marginTop: 4 }}>{invoice.data.supplierCode}</Text>
      <Text style={{ color: colors.mutedForeground, marginTop: 10 }}>Invoice {invoice.data.invoiceNumber}</Text>
      <Text style={{ color: colors.mutedForeground, marginTop: 4 }}>Purchase Order {po.data.poNumber}</Text>
      <Text style={{ color: colors.mutedForeground, marginTop: 4 }}>Company Code {po.data.companyCode ?? '—'}</Text>
      <Text style={{ color: colors.mutedForeground, marginTop: 4 }}>Invoice Date {invoice.data.invoiceDate ?? '—'}</Text>
      <Text style={{ color: colors.mutedForeground, marginTop: 4 }}>{invoice.data.currency ?? po.data.currency} · Gross {formatQuantity(invoice.data.grossAmount)}</Text>
    </Panel>
    {eligible.map(item => {
      const qty = invoiceQty(item);
      const prefill = defaultPhysicalReceivedQuantity(qty ?? null, Number(item.openQuantity));
      return (
      <Panel key={item.id}>
        <Text style={{ color: colors.foreground, fontFamily: 'Inter_700Bold' }}>PO Item {item.itemNumber ?? item.lineNumber} · {item.materialCode}</Text>
        <Text style={{ color: colors.mutedForeground, marginTop: 6 }}>{item.description}</Text>
        <View style={{ marginTop: 10, gap: 4 }}>
          <Text style={{ color: colors.foreground }}>PO Ordered Qty {formatQuantity(item.orderedQuantity)} {item.uom}</Text>
          <Text style={{ color: colors.foreground }}>Previously Received Qty {formatQuantity(item.receivedQuantity)} {item.uom}</Text>
          <Text style={{ color: colors.primary, fontFamily: 'Inter_600SemiBold' }}>PO Open Qty {formatQuantity(item.openQuantity)} {item.uom}</Text>
          <Text style={{ color: colors.mutedForeground }}>Invoice Qty {formatQuantity(qty)} {item.uom}</Text>
          <Text style={{ color: colors.foreground }}>Physical Received Qty {prefill.value || '—'} {item.uom} (prefill, editable next)</Text>
          {prefill.overDelivery ? <Text style={{ color: colors.error }}>OVER_DELIVERY_REVIEW_REQUIRED</Text> : null}
        </View>
      </Panel>
      );
    })}
    {eligible.length === 0 && <Text style={{ color: colors.error, marginBottom: 12 }}>No GR-eligible open items remain on this purchase order.</Text>}
    <PrimaryButton
      label="Confirm physical quantities"
      onPress={() => router.push({ pathname: '/receive/finalize-grn', params: { invoiceId, purchaseOrderId: po.data.id, documentId: documentId ?? invoice.data.documentId } })}
      disabled={eligible.length === 0}
    />
  </ReceiveShell>;
}
