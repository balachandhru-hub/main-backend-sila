import { Feather } from '@expo/vector-icons';
import { useRouter } from 'expo-router';
import React, { useMemo, useState } from 'react';
import { ActivityIndicator, Pressable, StyleSheet, Text, View } from 'react-native';
import { getGetGrnsQueryKey, useGetGrns } from '@workspace/api-client-react';
import { SilaCard, SilaEmptyState, SilaErrorState, SilaScreen, SilaSearchBar, SilaStatusChip } from '@/components/sila/ui';
import { useColors } from '@/hooks/useColors';

const ranges = ['Today', '7 Days', '30 Days', 'Custom'] as const;

export default function ReceiveHistory() {
  const colors = useColors();
  const router = useRouter();
  const [query, setQuery] = useState('');
  const [range, setRange] = useState<(typeof ranges)[number]>('30 Days');
  const grns = useGetGrns({ query: { queryKey: getGetGrnsQueryKey(), retry: false } });

  const filtered = useMemo(() => {
    const term = query.trim().toLowerCase();
    const now = Date.now();
    const days = range === 'Today' ? 1 : range === '7 Days' ? 7 : range === '30 Days' ? 30 : 3650;
    return (grns.data ?? []).filter((grn) => {
      const ageDays = (now - new Date(grn.receiptDate).getTime()) / 86_400_000;
      if (ageDays > days) return false;
      if (!term) return true;
      return (
        grn.grnNumber.toLowerCase().includes(term) ||
        grn.supplierName.toLowerCase().includes(term) ||
        grn.purchaseOrderNumber.toLowerCase().includes(term) ||
        (grn.invoiceNumber ?? '').toLowerCase().includes(term) ||
        (grn.erpMaterialDocument ?? '').toLowerCase().includes(term) ||
        grn.status.toLowerCase().includes(term)
      );
    });
  }, [grns.data, query, range]);

  return (
    <SilaScreen title="Receiving history" eyebrow="RECEIVE" fallback="/receive">
      <SilaSearchBar value={query} onChangeText={setQuery} placeholder="Supplier, PO, invoice, GRN, material document" />
      <View style={styles.filters}>
        {ranges.map((item) => (
          <Pressable key={item} onPress={() => setRange(item)} style={[styles.filter, { borderColor: range === item ? colors.primary : colors.border }]}>
            <Text style={{ color: range === item ? colors.primary : colors.mutedForeground, fontFamily: 'Inter_600SemiBold', fontSize: 11 }}>{item}</Text>
          </Pressable>
        ))}
      </View>
      {grns.isPending ? <ActivityIndicator color={colors.primary} /> : null}
      {grns.isError ? <SilaErrorState message="History is unavailable." onRetry={() => void grns.refetch()} /> : null}
      {!grns.isPending && !grns.isError && filtered.length === 0 ? <SilaEmptyState title="No receipts match these filters." /> : null}
      {filtered.map((grn) => (
        <SilaCard key={grn.id} onPress={() => router.push({ pathname: '/receive/grn-result', params: { grnId: grn.id } })}>
          <View style={styles.row}>
            <View style={{ flex: 1 }}>
              <Text style={[styles.number, { color: colors.foreground }]}>{grn.grnNumber}</Text>
              <Text style={[styles.meta, { color: colors.mutedForeground }]}>{grn.supplierName}</Text>
              <Text style={[styles.meta, { color: colors.mutedForeground }]}>PO {grn.purchaseOrderNumber} · Invoice {grn.invoiceNumber ?? '—'} · MD {grn.erpMaterialDocument ?? '—'}</Text>
            </View>
            <Feather name="chevron-right" size={18} color={colors.primary} />
          </View>
          <View style={styles.bottom}>
            <SilaStatusChip label={grn.status} tone={grn.status === 'POSTED' ? 'success' : grn.status === 'FAILED' ? 'failed' : 'warning'} />
            <Text style={[styles.date, { color: colors.mutedForeground }]}>{new Date(grn.postedAt ?? grn.receiptDate).toLocaleDateString()}</Text>
          </View>
        </SilaCard>
      ))}
    </SilaScreen>
  );
}

const styles = StyleSheet.create({
  filters: { flexDirection: 'row', flexWrap: 'wrap', gap: 8, marginBottom: 12 },
  filter: { minHeight: 36, borderWidth: 1, borderRadius: 8, paddingHorizontal: 10, alignItems: 'center', justifyContent: 'center' },
  row: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center' },
  number: { fontFamily: 'Inter_700Bold', fontSize: 16 },
  meta: { marginTop: 4, fontFamily: 'Inter_400Regular', fontSize: 12 },
  bottom: { marginTop: 12, flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center' },
  date: { fontFamily: 'Inter_400Regular', fontSize: 12 },
});
