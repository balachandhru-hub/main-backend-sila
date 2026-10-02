import { useLocalSearchParams, useRouter } from 'expo-router';
import React, { useState } from 'react';
import { Feather } from '@expo/vector-icons';
import { ActivityIndicator, Modal, Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import { getGetGrnQueryKey, useGetGrn } from '@workspace/api-client-react';
import { ReceiveShell, Panel, PrimaryButton, StatusPill, formatQuantity } from '@/components/receive-ui';
import { useColors } from '@/hooks/useColors';
import { formatOcrSupplierDisplay, isSupplierIdLabel } from '@/services/invoice-review-state';
import { erpRequestPayloadText } from '@/services/grn-posting';

export default function GrnResult() {
  const colors = useColors();
  const router = useRouter();
  const [payloadOpen, setPayloadOpen] = useState(false);
  const { grnId } = useLocalSearchParams<{ grnId: string }>();
  const grn = useGetGrn(grnId ?? '', { query: { queryKey: getGetGrnQueryKey(grnId ?? ''), enabled: Boolean(grnId), retry: false } });
  if (grn.isPending) return <ReceiveShell title="GRN result"><ActivityIndicator color={colors.primary} /></ReceiveShell>;
  if (grn.isError || !grn.data) return <ReceiveShell title="GRN result"><Text style={{ color: colors.error }}>Receipt details are unavailable.</Text></ReceiveShell>;
  const posted = grn.data.status === 'POSTED';
  const failed = grn.data.status === 'FAILED';
  const unknown = grn.data.status === 'UNKNOWN';
  const erpPayload = erpRequestPayloadText(grn.data.erpResponseJson);
  return (
    <ReceiveShell title={posted ? 'GRN posted successfully' : failed ? 'GRN posting failed' : 'GRN result'}>
      <Panel>
        <View style={styles.success}><View style={[styles.icon, { backgroundColor: `${(posted ? colors.success : colors.error)}20` }]}><Feather name={posted ? 'check' : 'alert-circle'} size={24} color={posted ? colors.success : colors.error} /></View><StatusPill label={grn.data.status} tone={posted ? 'success' : 'warning'} /></View>
        <Text style={[styles.number, { color: colors.foreground }]}>{posted ? 'GRN POSTED SUCCESSFULLY' : failed ? 'GRN posting failed' : 'GRN result'}</Text>
        <Text style={[styles.copy, { color: colors.mutedForeground }]}>Purchase Order: {grn.data.purchaseOrderNumber}</Text>
        <Text style={[styles.copy, { color: colors.mutedForeground }]}>Supplier: {
          grn.data.supplierName && !isSupplierIdLabel(grn.data.supplierName)
            ? grn.data.supplierName
            : (formatOcrSupplierDisplay(grn.data.supplierName ?? '') || '—')
        }</Text>
        <Text style={[styles.copy, { color: colors.mutedForeground }]}>Supplier Invoice: {grn.data.invoiceNumber ?? '—'}</Text>
        <Text style={[styles.copy, { color: colors.mutedForeground }]}>ERP Receipt Number: {grn.data.erpMaterialDocument ?? grn.data.grnNumber}</Text>
        <Text style={[styles.copy, { color: colors.mutedForeground }]}>Ariba Reference: {(grn.data as { externalReference?: string | null }).externalReference ?? '—'}</Text>
        <Text style={[styles.copy, { color: colors.mutedForeground }]}>Status: {grn.data.erpPostingStatus ?? grn.data.status}</Text>
        <Text style={[styles.copy, { color: colors.mutedForeground }]}>Posted Date: {grn.data.postedAt ?? grn.data.receiptDate}</Text>
            {failed ? (
          <View style={styles.errorRow}>
            <Text style={[styles.copy, { color: colors.error, flex: 1 }]}>{grn.data.failureMessage ?? 'ERP returned a confirmed failure.'}</Text>
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
        {unknown ? <Text style={[styles.copy, { color: colors.error }]}>ERP outcome is unknown. Reconcile before posting again.</Text> : null}
        {grn.data.lines.map((line) => <Text key={line.id} style={[styles.line, { color: colors.foreground }]}>{line.description}: accepted {formatQuantity(line.acceptedQuantity)} {line.uom}</Text>)}
      </Panel>
      {failed ? <PrimaryButton label="Retry" onPress={() => router.replace({ pathname: '/receive/finalize-grn', params: { invoiceId: grn.data.invoiceId ?? '', grnId: grn.data.id } })} /> : null}
      <PrimaryButton label="View GRN" onPress={() => router.replace({ pathname: '/receive/history' })} />
      <View style={{ height: 12 }} />
      <PrimaryButton label="Done" secondary onPress={() => router.replace('/receive')} />
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
    </ReceiveShell>
  );
}

const styles = StyleSheet.create({
  success: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' },
  icon: { width: 48, height: 48, borderRadius: 24, alignItems: 'center', justifyContent: 'center' },
  number: { marginTop: 20, fontFamily: 'Inter_700Bold', fontSize: 22 },
  copy: { marginTop: 8, fontFamily: 'Inter_400Regular', fontSize: 13, lineHeight: 19 },
  line: { marginTop: 16, fontFamily: 'Inter_600SemiBold', fontSize: 13 },
  errorRow: { flexDirection: 'row', alignItems: 'flex-start', gap: 8 },
  infoHit: { width: 28, height: 28, alignItems: 'center', justifyContent: 'center', marginTop: 8 },
  infoBadge: { width: 22, height: 22, borderRadius: 11, borderWidth: 1.5, alignItems: 'center', justifyContent: 'center' },
  infoBadgeText: { fontFamily: 'Inter_700Bold', fontSize: 13, lineHeight: 16, textTransform: 'lowercase' },
  payloadOverlay: { flex: 1, backgroundColor: 'rgba(8, 18, 32, 0.45)', justifyContent: 'flex-end' },
  payloadSheet: { maxHeight: '82%', borderTopLeftRadius: 16, borderTopRightRadius: 16 },
  payloadHeader: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', paddingHorizontal: 20, paddingTop: 18, paddingBottom: 12, borderBottomWidth: 1 },
  payloadTitle: { fontFamily: 'Inter_700Bold', fontSize: 18 },
  payloadBody: { paddingHorizontal: 20, paddingTop: 16, paddingBottom: 32 },
  payloadText: { fontFamily: 'Inter_400Regular', fontSize: 12, lineHeight: 18 },
});
