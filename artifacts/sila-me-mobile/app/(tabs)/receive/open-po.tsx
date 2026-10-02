import { useRouter } from 'expo-router';
import React, { useEffect, useMemo, useState } from 'react';
import { StyleSheet, Text, View } from 'react-native';
import type { PurchaseOrder } from '@workspace/api-client-react';
import { SilaCard, SilaEmptyState, SilaErrorState, SilaLoadingState, SilaScreen, SilaSearchBar, SilaStatusChip } from '@/components/sila/ui';
import { useColors } from '@/hooks/useColors';
import { useStoreScope } from '@/providers/StoreScopeProvider';
import { purchaseOrderService } from '@/services/ops';

export default function OpenPoScreen() {
  const colors = useColors();
  const router = useRouter();
  const { organization } = useStoreScope();
  const [query, setQuery] = useState('');
  const [debounced, setDebounced] = useState('');
  const [rows, setRows] = useState<PurchaseOrder[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const handle = setTimeout(() => setDebounced(query.trim()), 300);
    return () => clearTimeout(handle);
  }, [query]);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    void purchaseOrderService.searchOpen({ organizationId: organization?.id, query: debounced || undefined }).then((result) => {
      if (cancelled) return;
      setLoading(false);
      if (result.status === 'OK' || result.status === 'EMPTY') {
        setRows(result.data);
        setError(null);
      } else if (result.status === 'ERROR') setError(result.message);
    });
    return () => { cancelled = true; };
  }, [debounced, organization?.id]);

  const filtered = useMemo(() => rows, [rows]);

  return (
    <SilaScreen title="Open purchase orders" eyebrow="RECEIVE" fallback="/receive">
      <SilaSearchBar value={query} onChangeText={setQuery} placeholder="Search PO number or supplier" />
      {loading ? <SilaLoadingState /> : null}
      {error ? <SilaErrorState message={error} /> : null}
      {!loading && !error && filtered.length === 0 ? <SilaEmptyState title="No open purchase orders found." /> : null}
      {filtered.map((po) => {
        const openItems = po.items.filter((item) => item.goodsReceiptExpected && !item.deletionIndicator && !item.deliveryCompleted && Number(item.openQuantity) > 0).length;
        return (
          <SilaCard key={po.id} onPress={() => router.push({ pathname: '/receive/po/[id]', params: { id: po.poNumber } })}>
            <View style={styles.row}>
              <View style={{ flex: 1 }}>
                <Text style={[styles.title, { color: colors.foreground }]}>{po.poNumber}</Text>
                <Text style={[styles.meta, { color: colors.mutedForeground }]}>{po.supplierName}</Text>
                <Text style={[styles.meta, { color: colors.mutedForeground }]}>{po.poDate ?? '—'} · {po.currency} {po.totalAmount != null ? Number(po.totalAmount).toLocaleString() : '—'} · {openItems} open items</Text>
              </View>
              <SilaStatusChip label={po.status} tone={po.status === 'OPEN' ? 'success' : 'pending'} />
            </View>
          </SilaCard>
        );
      })}
    </SilaScreen>
  );
}

const styles = StyleSheet.create({
  row: { flexDirection: 'row', gap: 10 },
  title: { fontFamily: 'Inter_700Bold', fontSize: 15 },
  meta: { marginTop: 4, fontFamily: 'Inter_400Regular', fontSize: 12 },
});
