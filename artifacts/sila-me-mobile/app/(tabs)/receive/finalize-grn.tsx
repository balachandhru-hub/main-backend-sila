/**
 * PROTECTED SILA INVOICE RECEIVING FLOW — See docs/PROTECTED_INVOICE_FLOW.md.
 * Finalize GRN: physical quantities. POST GRN only happens here, from saved InvoiceId.
 */
import { useLocalSearchParams, useRouter } from 'expo-router';
import React, { useEffect, useState } from 'react';
import { ActivityIndicator, Alert, Modal, Pressable, ScrollView, StyleSheet, Text, TextInput, View } from 'react-native';
import { getInvoice, type GrnLine } from '@workspace/api-client-react';
import { ReceiveShell, Panel, PrimaryButton, formatQuantity } from '@/components/receive-ui';
import { useColors } from '@/hooks/useColors';
import { formatOcrSupplierDisplay, isSupplierIdLabel } from '@/services/invoice-review-state';
import { erpRequestPayloadText, prepareGrn, postGoodsReceipt, type FinalizeGoodsReceipt } from '@/services/grn-posting';

export default function FinalizeGrn() {
  const colors = useColors();
  const router = useRouter();
  const { invoiceId, purchaseOrderId, grnId } = useLocalSearchParams<{ invoiceId?: string; purchaseOrderId?: string; grnId?: string }>();
  const [grn, setGrn] = useState<FinalizeGoodsReceipt>();
  const [accepted, setAccepted] = useState<Record<string, string>>({});
  const [loading, setLoading] = useState(true);
  const [posting, setPosting] = useState(false);
  const [error, setError] = useState<string>();
  const [payloadOpen, setPayloadOpen] = useState(false);
  const [postingPayload, setPostingPayload] = useState<string>();

  useEffect(() => {
    let cancelled = false;
    async function load() {
      try {
        setLoading(true);
        const invoice = invoiceId ? await getInvoice(invoiceId) : null;
        const prepared = await prepareGrn({
          invoiceId: invoiceId || invoice?.id || '',
          purchaseOrderId: purchaseOrderId || invoice?.purchaseOrderId || undefined,
        });
        if (cancelled) return;
        if (prepared.status === 'POSTED') {
          router.replace({ pathname: '/receive/grn-result', params: { grnId: prepared.id } });
          return;
        }
        setGrn(prepared);
        const next: Record<string, string> = {};
        prepared.lines.forEach((line: GrnLine) => { next[line.purchaseOrderItemId] = String(line.acceptedQuantity); });
        setAccepted(next);
        if (prepared.status === 'FAILED' || prepared.erpPostingStatus === 'FAILED') {
          setError(prepared.failureMessage ?? 'The receipt was not posted.');
          setPostingPayload(erpRequestPayloadText(prepared.erpResponseJson));
        } else {
          setError(undefined);
          setPostingPayload(undefined);
        }
      } catch (cause) {
        const data = cause && typeof cause === 'object' && 'data' in cause ? (cause as { data?: { message?: string } }).data : null;
        if (!cancelled) setError(data?.message ?? 'The saved GRN could not be loaded.');
      } finally {
        if (!cancelled) setLoading(false);
      }
    }
    void load();
    return () => { cancelled = true; };
  }, [invoiceId, purchaseOrderId, grnId, router]);

  const submit = async () => {
    if (!grn) return;
    const lines = grn.lines.map(line => {
      const acceptedQuantity = Number(accepted[line.purchaseOrderItemId] ?? line.acceptedQuantity);
      return {
        purchaseOrderItemId: line.purchaseOrderItemId,
        receivedQuantity: acceptedQuantity,
        acceptedQuantity,
        damagedQuantity: line.damagedQuantity,
        rejectedQuantity: line.rejectedQuantity,
      };
    });
    if (lines.some(line => !Number.isFinite(line.acceptedQuantity) || line.acceptedQuantity < 0)) {
      Alert.alert('Check quantities', 'Accepted GRN quantity must be a number of zero or more.');
      return;
    }
    setPosting(true);
    try {
      const posted = await postGoodsReceipt(grn.id, lines);
      if (posted.status === 'POSTED' || posted.erpPostingStatus === 'POSTED') {
        router.replace({ pathname: '/receive/grn-result', params: { grnId: posted.id } });
        return;
      }
      setGrn(posted);
      setError(posted.failureMessage ?? 'The receipt was not posted.');
      setPostingPayload(erpRequestPayloadText(posted.erpResponseJson));
    } catch (cause) {
      const data = cause && typeof cause === 'object' && 'data' in cause
        ? (cause as { data?: { message?: string; code?: string; erpResponseJson?: string | null } }).data
        : null;
      setError(data?.message ?? 'The receipt could not be posted.');
      setPostingPayload(erpRequestPayloadText(data?.erpResponseJson));
    } finally {
      setPosting(false);
    }
  };

  if (loading) return <ReceiveShell title="Finalize GRN"><ActivityIndicator color={colors.primary} /></ReceiveShell>;
  if (!grn) return <ReceiveShell title="Finalize GRN"><Text style={{ color: colors.error }}>{error ?? 'The saved GRN could not be loaded.'}</Text></ReceiveShell>;

  const supplierName = grn.supplierName && !isSupplierIdLabel(grn.supplierName)
    ? grn.supplierName
    : (formatOcrSupplierDisplay(grn.supplierName ?? '') || '—');
  const canEdit = grn.status !== 'POSTED' && grn.erpPostingStatus !== 'POSTING_UNKNOWN';
  const erpPayload = postingPayload ?? erpRequestPayloadText(grn.erpResponseJson);

  return (
    <ReceiveShell title="Finalize GRN" fallback="/receive">
      <Panel>
        <Text style={[styles.label, { color: colors.mutedForeground }]}>SUPPLIER</Text>
        <Text style={[styles.value, { color: colors.foreground }]}>{supplierName}</Text>
        <Text style={[styles.meta, { color: colors.mutedForeground }]}>Supplier Invoice Number {grn.invoiceNumber ?? '—'}</Text>
        <Text style={[styles.meta, { color: colors.mutedForeground }]}>Purchase Order {grn.purchaseOrderNumber}</Text>
        <Text style={[styles.meta, { color: colors.mutedForeground }]}>Company Code {grn.companyCode ?? '—'}</Text>
        <Text style={[styles.meta, { color: colors.mutedForeground }]}>GRN Number / Receipt Number {grn.erpMaterialDocument ?? grn.grnNumber}</Text>
        <Text style={[styles.meta, { color: colors.mutedForeground }]}>Received Date {new Date(grn.receiptDate).toISOString().slice(0, 10)}</Text>
      </Panel>
      {grn.lines.map(line => (
        <Panel key={line.id}>
          <Text style={[styles.value, { color: colors.foreground }]}>PO Item {line.purchaseOrderLineNumber} · {line.materialCode}</Text>
          <Text style={[styles.meta, { color: colors.mutedForeground }]}>{line.description}</Text>
          <Text style={[styles.meta, { color: colors.foreground }]}>Ordered Qty {formatQuantity((line as GrnLine & { orderedQuantity?: number }).orderedQuantity ?? line.openQuantityBefore)} {line.uom}</Text>
          <Text style={[styles.meta, { color: colors.foreground }]}>Open Qty {formatQuantity(line.openQuantityBefore)} {line.uom}</Text>
          <Text style={[styles.meta, { color: colors.foreground }]}>Invoice Qty {formatQuantity(line.invoiceQuantity)} {line.uom}</Text>
          <Text style={[styles.label, { color: colors.mutedForeground, marginTop: 12 }]}>ACCEPTED GRN QTY</Text>
          <TextInput
            editable={canEdit}
            keyboardType="decimal-pad"
            value={accepted[line.purchaseOrderItemId] ?? String(line.acceptedQuantity)}
            onChangeText={value => setAccepted(current => ({ ...current, [line.purchaseOrderItemId]: value }))}
            style={[styles.input, { borderColor: colors.border, color: colors.foreground }]}
          />
          {line.rejectedQuantity > 0 ? <Text style={[styles.meta, { color: colors.foreground }]}>Rejected Qty {formatQuantity(line.rejectedQuantity)} {line.uom}</Text> : null}
          <Text style={[styles.meta, { color: colors.mutedForeground }]}>UOM {line.uom}</Text>
        </Panel>
      ))}
          {error ? (
        <View style={styles.errorRow}>
          <Text style={{ color: colors.error, flex: 1 }}>{error}</Text>
          <Pressable
            testID="erp-payload-info"
            accessibilityRole="button"
            accessibilityLabel="Show ERP payload"
            onPress={() => setPayloadOpen(true)}
            hitSlop={8}
            style={styles.infoHit}
          >
            <View style={[styles.infoBadge, { borderColor: colors.primary }]}>
              <Text style={[styles.infoBadgeText, { color: colors.primary }]}>i</Text>
            </View>
          </Pressable>
        </View>
      ) : null}
      <Modal visible={payloadOpen} animationType="slide" transparent onRequestClose={() => setPayloadOpen(false)}>
        <View style={styles.payloadOverlay}>
          <View style={[styles.payloadSheet, { backgroundColor: colors.background }]}>
            <View style={[styles.payloadHeader, { borderBottomColor: colors.border }]}>
              <Text style={[styles.payloadTitle, { color: colors.foreground }]}>ERP payload</Text>
              <Pressable accessibilityLabel="Close ERP payload" onPress={() => setPayloadOpen(false)} hitSlop={12}>
                <Text style={{ color: colors.primary, fontFamily: 'Inter_600SemiBold', fontSize: 14 }}>Close</Text>
              </Pressable>
            </View>
            <ScrollView contentContainerStyle={styles.payloadBody}>
              <Text selectable style={[styles.payloadText, { color: colors.foreground }]}>{erpPayload ?? 'No ERP payload was captured for this posting attempt.'}</Text>
            </ScrollView>
          </View>
        </View>
      </Modal>
      <PrimaryButton label={posting ? 'POSTING GRN…' : 'POST GRN'} onPress={() => void submit()} disabled={posting || !canEdit} />
    </ReceiveShell>
  );
}

const styles = StyleSheet.create({
  label: { fontFamily: 'Inter_600SemiBold', fontSize: 10, letterSpacing: 0.8 },
  value: { marginTop: 6, fontFamily: 'Inter_700Bold', fontSize: 16 },
  meta: { marginTop: 6, fontFamily: 'Inter_400Regular', fontSize: 13 },
  input: { height: 46, borderWidth: 1, borderRadius: 8, paddingHorizontal: 12, fontFamily: 'Inter_600SemiBold', fontSize: 15, marginTop: 6 },
  errorRow: { flexDirection: 'row', alignItems: 'flex-start', gap: 8, marginBottom: 12 },
  infoHit: { width: 28, height: 28, alignItems: 'center', justifyContent: 'center' },
  infoBadge: { width: 22, height: 22, borderRadius: 11, borderWidth: 1.5, alignItems: 'center', justifyContent: 'center' },
  infoBadgeText: { fontFamily: 'Inter_700Bold', fontSize: 13, lineHeight: 16, textTransform: 'lowercase' },
  payloadOverlay: { flex: 1, backgroundColor: 'rgba(8, 18, 32, 0.45)', justifyContent: 'flex-end' },
  payloadSheet: { maxHeight: '82%', borderTopLeftRadius: 16, borderTopRightRadius: 16 },
  payloadHeader: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', paddingHorizontal: 20, paddingTop: 18, paddingBottom: 12, borderBottomWidth: 1 },
  payloadTitle: { fontFamily: 'Inter_700Bold', fontSize: 18 },
  payloadBody: { paddingHorizontal: 20, paddingTop: 16, paddingBottom: 32 },
  payloadText: { fontFamily: 'Inter_400Regular', fontSize: 12, lineHeight: 18 },
});
