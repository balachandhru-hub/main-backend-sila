import { useLocalSearchParams, useRouter } from 'expo-router';
import React, { useEffect, useMemo, useState } from 'react';
import { Alert, StyleSheet, Text, TextInput, View } from 'react-native';
import { getInvoice, getPurchaseOrder, postGrn } from '@workspace/api-client-react';
import type { GrnInput } from '@workspace/api-client-react';
import { ReceiveShell, Panel, PrimaryButton, formatQuantity } from '@/components/receive-ui';
import { useColors } from '@/hooks/useColors';
import { useQuery } from '@tanstack/react-query';
import { defaultPhysicalReceivedQuantity, formatOcrSupplierDisplay, invoiceQuantityForPoItem, isSupplierIdLabel } from '@/services/invoice-review-state';

export default function ReceiveQuantities() {
  const colors = useColors();
  const router = useRouter();
  const { invoiceId } = useLocalSearchParams<{ invoiceId: string }>();
  const invoice = useQuery({ queryKey: ['receive-invoice', invoiceId], queryFn: () => getInvoice(invoiceId ?? ''), enabled: Boolean(invoiceId) });
  const po = useQuery({
    queryKey: ['receive-qty-po', invoice.data?.purchaseOrderNumber, invoice.data?.organizationId],
    queryFn: () => getPurchaseOrder(invoice.data?.purchaseOrderNumber ?? '', { organizationId: invoice.data?.organizationId }),
    enabled: Boolean(invoice.data?.purchaseOrderNumber && invoice.data?.organizationId),
  });
  const [received, setReceived] = useState<Record<string, string>>({});
  const [accepted, setAccepted] = useState<Record<string, string>>({});
  const [damaged, setDamaged] = useState<Record<string, string>>({});
  const [rejected, setRejected] = useState<Record<string, string>>({});
  const [reviewing, setReviewing] = useState(false);
  const [isPosting, setIsPosting] = useState(false);
  const [prefilled, setPrefilled] = useState(false);
  const rows = useMemo(() => (po.data?.items ?? []).filter(item =>
    item.goodsReceiptExpected && !item.deletionIndicator && !item.deliveryCompleted && Number(item.openQuantity) > 0), [po.data]);
  const invoiceQty = (item: { id: string; lineNumber?: number | null; itemNumber?: string | null }) =>
    invoiceQuantityForPoItem(invoice.data?.lines ?? [], item);
  useEffect(() => {
    if (prefilled || !invoice.data || !po.data) return;
    const nextReceived: Record<string, string> = {};
    const nextAccepted: Record<string, string> = {};
    rows.forEach((item) => {
      const prefill = defaultPhysicalReceivedQuantity(invoiceQty(item) ?? null, Number(item.openQuantity));
      if (prefill.value) {
        nextReceived[item.id] = prefill.value;
        nextAccepted[item.id] = prefill.value;
      }
    });
    setReceived(nextReceived);
    setAccepted(nextAccepted);
    setPrefilled(true);
  }, [invoice.data, po.data, prefilled, rows]);
  const physical = (id: string) => Number(received[id] || 0);
  const acceptedQty = (id: string) => Number(accepted[id] || (received[id] ? received[id] : 0));
  const input = useMemo<GrnInput | null>(() => {
    if (!invoice.data?.purchaseOrderId) return null;
    return {
      invoiceId: invoice.data.id,
      purchaseOrderId: invoice.data.purchaseOrderId,
      operatingUnitId: invoice.data.operatingUnitId ?? '',
      lines: rows.map((item) => ({
        purchaseOrderItemId: item.id,
        receivedQuantity: physical(item.id),
        acceptedQuantity: acceptedQty(item.id),
        damagedQuantity: Number(damaged[item.id] ?? 0),
        rejectedQuantity: Number(rejected[item.id] ?? 0),
      })),
    };
  }, [accepted, damaged, invoice.data, received, rejected, rows]);

  const submit = async () => {
    if (!input) return;
    if (input.lines.some((line) => {
      const item = rows.find(row => row.id === line.purchaseOrderItemId);
      return !Number.isFinite(line.receivedQuantity) || line.receivedQuantity < 0 || line.acceptedQuantity < 0
        || line.acceptedQuantity + line.damagedQuantity + line.rejectedQuantity > line.receivedQuantity
        || (item && line.acceptedQuantity > Number(item.openQuantity));
    })) {
      Alert.alert('Check quantities', 'Accepted quantity cannot exceed physically received quantity or PO open quantity.');
      return;
    }
    if (!reviewing) {
      setReviewing(true);
      return;
    }
    setIsPosting(true);
    try {
      const grn = await postGrn(input, { headers: { 'Idempotency-Key': `mobile-grn-${invoiceId}` } });
      if (grn.status === 'POSTED') {
        router.replace({ pathname: '/receive/grn-result', params: { grnId: grn.id } });
        return;
      }
      const missingConfig = grn.failureCode === 'POST_GRN_CONFIGURATION_NOT_FOUND';
      Alert.alert(
        missingConfig ? 'GRN not configured' : 'GRN not posted',
        missingConfig
          ? 'GRN posting is not configured for this organization. Contact your administrator.'
          : (grn.failureMessage ?? 'The receipt was not posted to ERP. Stock was not updated.'),
      );
      if (grn.id) router.replace({ pathname: '/receive/grn-result', params: { grnId: grn.id } });
    } catch (error) {
      const data = error && typeof error === 'object' && 'data' in error ? (error as { data?: { message?: string } }).data : null;
      Alert.alert('GRN not posted', data?.message ?? 'The receipt failed validation. Check the quantities and try again.');
    } finally {
      setIsPosting(false);
    }
  };

  if (invoice.isPending || po.isPending) return <ReceiveShell title="Physical quantities"><Text style={{ color: colors.mutedForeground }}>Loading invoice…</Text></ReceiveShell>;
  if (!invoice.data || !po.data) return <ReceiveShell title="Physical quantities"><Text style={{ color: colors.error }}>Invoice could not be loaded.</Text></ReceiveShell>;
  const plant = rows.find(item => item.plant)?.plant;
  const storage = rows.find(item => item.storageLocation)?.storageLocation;
  if (reviewing && input) {
    return (
      <ReceiveShell title="Review GRN" fallback="/receive/grn-comparison">
        <Panel>
          <Text style={[styles.description, { color: colors.foreground }]}>{
            invoice.data.supplierName && !isSupplierIdLabel(invoice.data.supplierName)
              ? invoice.data.supplierName
              : (formatOcrSupplierDisplay(invoice.data.supplierName ?? '') || invoice.data.supplierCode || '—')
          }</Text>
          <Text style={[styles.reference, { color: colors.mutedForeground }]}>PO {po.data.poNumber} · Invoice {invoice.data.invoiceNumber}</Text>
          <Text style={[styles.reference, { color: colors.mutedForeground }]}>Lines {input.lines.length}</Text>
          <Text style={[styles.reference, { color: colors.mutedForeground }]}>Accepted {input.lines.reduce((sum, line) => sum + line.acceptedQuantity, 0)}</Text>
          <Text style={[styles.reference, { color: colors.mutedForeground }]}>Damaged {input.lines.reduce((sum, line) => sum + line.damagedQuantity, 0)}</Text>
          <Text style={[styles.reference, { color: colors.mutedForeground }]}>Rejected {input.lines.reduce((sum, line) => sum + line.rejectedQuantity, 0)}</Text>
          <Text style={[styles.reference, { color: colors.mutedForeground }]}>Plant {plant ?? '—'}</Text>
          <Text style={[styles.reference, { color: colors.mutedForeground }]}>Storage Location {storage ?? '—'}</Text>
          <Text style={[styles.reference, { color: colors.mutedForeground }]}>Posting Date {new Date().toISOString().slice(0, 10)}</Text>
        </Panel>
        <PrimaryButton label={isPosting ? 'POSTING GRN…' : 'POST GRN'} onPress={() => void submit()} disabled={isPosting || !input} />
        <View style={{ height: 12 }} />
        <PrimaryButton label="Back to quantities" secondary onPress={() => setReviewing(false)} disabled={isPosting} />
      </ReceiveShell>
    );
  }
  return (
    <ReceiveShell title="Physical quantities" fallback="/receive/grn-comparison">
      <Text style={[styles.copy, { color: colors.mutedForeground }]}>Invoice quantity is reference only. Enter physical received quantity at the store. Accepted stock can default to physical received quantity after you enter it.</Text>
      {rows.map((item) => (
        <Panel key={item.id}>
          <Text style={[styles.description, { color: colors.foreground }]}>{item.description}</Text>
          <Text style={[styles.reference, { color: colors.mutedForeground }]}>PO ordered {formatQuantity(item.orderedQuantity)} · previously received {formatQuantity(item.receivedQuantity)} · open {formatQuantity(item.openQuantity)} {item.uom}</Text>
          <Text style={[styles.reference, { color: colors.mutedForeground }]}>Invoice quantity: {formatQuantity(invoiceQty(item))} {item.uom}</Text>
          {defaultPhysicalReceivedQuantity(invoiceQty(item) ?? null, Number(item.openQuantity)).overDelivery
            ? <Text style={[styles.reference, { color: colors.error }]}>OVER_DELIVERY_REVIEW_REQUIRED — invoice qty exceeds PO open qty. Enter a valid physical quantity.</Text>
            : <Text style={[styles.reference, { color: colors.mutedForeground }]}>Physical quantity is prefills from invoice and remains editable.</Text>}
            <View style={styles.fields}>
            <View style={styles.field}><Text style={[styles.label, { color: colors.mutedForeground }]}>PHYSICALLY RECEIVED</Text><TextInput keyboardType="decimal-pad" value={received[item.id] ?? ''} onChangeText={(value) => {
              setReceived((current) => ({ ...current, [item.id]: value }));
              setAccepted((current) => current[item.id] == null || current[item.id] === '' ? { ...current, [item.id]: value } : current);
            }} style={[styles.input, { borderColor: colors.border, color: colors.foreground }]} /></View>
            <View style={styles.field}><Text style={[styles.label, { color: colors.mutedForeground }]}>ACCEPTED</Text><TextInput keyboardType="decimal-pad" value={accepted[item.id] ?? received[item.id] ?? ''} onChangeText={(value) => setAccepted((current) => ({ ...current, [item.id]: value }))} style={[styles.input, { borderColor: colors.border, color: colors.foreground }]} /></View>
              <View style={styles.field}><Text style={[styles.label, { color: colors.mutedForeground }]}>DAMAGED</Text><TextInput keyboardType="decimal-pad" value={damaged[item.id] ?? '0'} onChangeText={(value) => setDamaged((current) => ({ ...current, [item.id]: value }))} style={[styles.input, { borderColor: colors.border, color: colors.foreground }]} /></View>
              <View style={styles.field}><Text style={[styles.label, { color: colors.mutedForeground }]}>REJECTED</Text><TextInput keyboardType="decimal-pad" value={rejected[item.id] ?? '0'} onChangeText={(value) => setRejected((current) => ({ ...current, [item.id]: value }))} style={[styles.input, { borderColor: colors.border, color: colors.foreground }]} /></View>
          </View>
        </Panel>
      ))}
      <PrimaryButton label="Review GRN" onPress={() => void submit()} disabled={!input} />
    </ReceiveShell>
  );
}

const styles = StyleSheet.create({
  copy: { marginTop: 12, fontFamily: 'Inter_400Regular', fontSize: 14, lineHeight: 21 },
  description: { fontFamily: 'Inter_700Bold', fontSize: 15 },
  reference: { marginTop: 6, fontFamily: 'Inter_400Regular', fontSize: 12 },
  fields: { flexDirection: 'row', gap: 12, marginTop: 16 },
  field: { flex: 1 },
  label: { fontFamily: 'Inter_600SemiBold', fontSize: 9, letterSpacing: 0.8, marginBottom: 7 },
  input: { height: 46, borderWidth: 1, borderRadius: 8, paddingHorizontal: 12, fontFamily: 'Inter_600SemiBold', fontSize: 15 },
});
