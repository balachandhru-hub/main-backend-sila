import { useRouter } from 'expo-router';
import React, { useMemo } from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { getGetGrnsQueryKey, useGetGrns } from '@workspace/api-client-react';
import { SilaCard, SilaEmptyState, SilaErrorState, SilaLoadingState, SilaScreen, SilaStatusChip } from '@/components/sila/ui';
import { useColors } from '@/hooks/useColors';

export default function PendingGrnScreen() {
  const colors = useColors();
  const router = useRouter();
  const grns = useGetGrns({ query: { queryKey: getGetGrnsQueryKey(), retry: false } });
  const pending = useMemo(
    () => (grns.data ?? []).filter((item) => !['POSTED'].includes(item.status)),
    [grns.data],
  );

  return (
    <SilaScreen title="Pending GRN" eyebrow="RECEIVE" fallback="/receive">
      {grns.isPending ? <SilaLoadingState /> : null}
      {grns.isError ? <SilaErrorState message="Pending GRNs could not be loaded." onRetry={() => void grns.refetch()} /> : null}
      {!grns.isPending && !grns.isError && pending.length === 0 ? <SilaEmptyState title="No pending goods receipts." /> : null}
      {pending.map((grn) => (
        <SilaCard key={grn.id} onPress={() => router.push({ pathname: '/receive/grn-result', params: { grnId: grn.id } })}>
          <View style={styles.row}>
            <View style={{ flex: 1 }}>
              <Text style={[styles.title, { color: colors.foreground }]}>{grn.grnNumber}</Text>
              <Text style={[styles.meta, { color: colors.mutedForeground }]}>{grn.supplierName}</Text>
              <Text style={[styles.meta, { color: colors.mutedForeground }]}>PO {grn.purchaseOrderNumber} · Invoice {grn.invoiceNumber ?? '—'} · {new Date(grn.createdAt).toLocaleDateString()}</Text>
            </View>
            <SilaStatusChip label={grn.status} tone={grn.status === 'FAILED' || grn.status === 'UNKNOWN' ? 'failed' : 'pending'} />
          </View>
        </SilaCard>
      ))}
    </SilaScreen>
  );
}

const styles = StyleSheet.create({
  row: { flexDirection: 'row', gap: 10 },
  title: { fontFamily: 'Inter_700Bold', fontSize: 15 },
  meta: { marginTop: 4, fontFamily: 'Inter_400Regular', fontSize: 12 },
});
