import { useRouter } from 'expo-router';
import React, { useEffect, useState } from 'react';
import { StyleSheet, Text, View } from 'react-native';
import type { GoodsReceipt, PurchaseOrder, Supplier } from '@workspace/api-client-react';
import { SilaCard, SilaEmptyState, SilaLoadingState, SilaScreen, SilaSearchBar } from '@/components/sila/ui';
import { useColors } from '@/hooks/useColors';
import { useStoreScope } from '@/providers/StoreScopeProvider';
import { grnService, purchaseOrderService, supplierService } from '@/services/ops';

export default function GlobalSearchScreen() {
  const colors = useColors();
  const router = useRouter();
  const { organization } = useStoreScope();
  const [query, setQuery] = useState('');
  const [debounced, setDebounced] = useState('');
  const [loading, setLoading] = useState(false);
  const [suppliers, setSuppliers] = useState<Supplier[]>([]);
  const [pos, setPos] = useState<PurchaseOrder[]>([]);
  const [grns, setGrns] = useState<GoodsReceipt[]>([]);

  useEffect(() => {
    const handle = setTimeout(() => setDebounced(query.trim()), 300);
    return () => clearTimeout(handle);
  }, [query]);

  useEffect(() => {
    if (debounced.length < 2 || !organization?.id) {
      setSuppliers([]);
      setPos([]);
      setGrns([]);
      return;
    }
    let cancelled = false;
    setLoading(true);
    void Promise.all([
      supplierService.search(organization.id, debounced),
      purchaseOrderService.searchOpen({ organizationId: organization.id, query: debounced }),
      grnService.list(),
    ]).then(([supplierResult, poResult, grnResult]) => {
      if (cancelled) return;
      setLoading(false);
      setSuppliers(supplierResult.status === 'OK' || supplierResult.status === 'EMPTY' ? supplierResult.data : []);
      setPos(poResult.status === 'OK' || poResult.status === 'EMPTY' ? poResult.data : []);
      const allGrns = grnResult.status === 'OK' || grnResult.status === 'EMPTY' ? grnResult.data : [];
      setGrns(allGrns.filter((item) =>
        item.grnNumber.toLowerCase().includes(debounced.toLowerCase()) ||
        item.purchaseOrderNumber.toLowerCase().includes(debounced.toLowerCase()) ||
        item.supplierName.toLowerCase().includes(debounced.toLowerCase())));
    });
    return () => { cancelled = true; };
  }, [debounced, organization?.id]);

  return (
    <SilaScreen title="Search" eyebrow="SILA ME" fallback="/home">
      <SilaSearchBar value={query} onChangeText={setQuery} placeholder="Suppliers, POs, GRNs" autoFocus />
      {loading ? <SilaLoadingState /> : null}
      {!loading && debounced.length >= 2 && suppliers.length === 0 && pos.length === 0 && grns.length === 0 ? (
        <SilaEmptyState title="No matches." description="Materials and invoices will join search when those APIs are available." />
      ) : null}
      {suppliers.length ? <Text style={[styles.group, { color: colors.mutedForeground }]}>SUPPLIERS</Text> : null}
      {suppliers.map((item) => (
        <SilaCard key={item.id} onPress={() => router.push({ pathname: '/suppliers/[id]', params: { id: item.id } })}>
          <Text style={[styles.title, { color: colors.foreground }]}>{item.name}</Text>
          <Text style={[styles.meta, { color: colors.mutedForeground }]}>{item.supplierCode}</Text>
        </SilaCard>
      ))}
      {pos.length ? <Text style={[styles.group, { color: colors.mutedForeground }]}>PURCHASE ORDERS</Text> : null}
      {pos.map((item) => (
        <SilaCard key={item.id} onPress={() => router.push({ pathname: '/receive/po/[id]', params: { id: item.poNumber } })}>
          <Text style={[styles.title, { color: colors.foreground }]}>{item.poNumber}</Text>
          <Text style={[styles.meta, { color: colors.mutedForeground }]}>{item.supplierName}</Text>
        </SilaCard>
      ))}
      {grns.length ? <Text style={[styles.group, { color: colors.mutedForeground }]}>GRNs</Text> : null}
      {grns.map((item) => (
        <SilaCard key={item.id} onPress={() => router.push({ pathname: '/receive/grn-result', params: { grnId: item.id } })}>
          <Text style={[styles.title, { color: colors.foreground }]}>{item.grnNumber}</Text>
          <Text style={[styles.meta, { color: colors.mutedForeground }]}>{item.supplierName}</Text>
        </SilaCard>
      ))}
    </SilaScreen>
  );
}

const styles = StyleSheet.create({
  group: { marginTop: 12, marginBottom: 8, fontFamily: 'Inter_600SemiBold', fontSize: 10, letterSpacing: 1.1 },
  title: { fontFamily: 'Inter_700Bold', fontSize: 14 },
  meta: { marginTop: 4, fontFamily: 'Inter_400Regular', fontSize: 12 },
});
