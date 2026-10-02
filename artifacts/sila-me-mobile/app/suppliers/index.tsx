import { useRouter } from 'expo-router';
import React, { useEffect, useState } from 'react';
import { StyleSheet, Text } from 'react-native';
import type { Supplier } from '@workspace/api-client-react';
import { SilaCard, SilaEmptyState, SilaErrorState, SilaLoadingState, SilaScreen, SilaSearchBar, SilaStatusChip } from '@/components/sila/ui';
import { useColors } from '@/hooks/useColors';
import { useStoreScope } from '@/providers/StoreScopeProvider';
import { supplierService } from '@/services/ops';

export default function SuppliersScreen() {
  const colors = useColors();
  const router = useRouter();
  const { organization } = useStoreScope();
  const [query, setQuery] = useState('');
  const [debounced, setDebounced] = useState('');
  const [rows, setRows] = useState<Supplier[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const handle = setTimeout(() => setDebounced(query.trim()), 300);
    return () => clearTimeout(handle);
  }, [query]);

  useEffect(() => {
    if (!organization?.id || debounced.length < 2) {
      setRows([]);
      return;
    }
    let cancelled = false;
    setLoading(true);
    void supplierService.search(organization.id, debounced).then((result) => {
      if (cancelled) return;
      setLoading(false);
      if (result.status === 'OK' || result.status === 'EMPTY') {
        setRows(result.data);
        setError(null);
      } else if (result.status === 'ERROR') setError(result.message);
    });
    return () => { cancelled = true; };
  }, [debounced, organization?.id]);

  return (
    <SilaScreen title="Suppliers" eyebrow="MORE" fallback="/more">
      <SilaSearchBar value={query} onChangeText={setQuery} placeholder="Supplier name, ID, or TRN" />
      {loading ? <SilaLoadingState /> : null}
      {error ? <SilaErrorState message={error} /> : null}
      {!loading && debounced.length >= 2 && rows.length === 0 ? <SilaEmptyState title="No suppliers matched." /> : null}
      {rows.map((item) => (
        <SilaCard key={item.id} onPress={() => router.push({ pathname: '/suppliers/[id]', params: { id: item.id } })}>
          <Text style={[styles.title, { color: colors.foreground }]}>{item.name}</Text>
          <Text style={[styles.meta, { color: colors.mutedForeground }]}>{item.supplierCode}{item.trn || item.taxNumber ? ` · TRN ${item.trn ?? item.taxNumber}` : ''}{item.city ? ` · ${item.city}` : ''}</Text>
          <SilaStatusChip label={item.status} tone={item.status === 'ACTIVE' ? 'success' : 'pending'} />
        </SilaCard>
      ))}
    </SilaScreen>
  );
}

const styles = StyleSheet.create({
  title: { fontFamily: 'Inter_700Bold', fontSize: 15 },
  meta: { marginTop: 4, marginBottom: 8, fontFamily: 'Inter_400Regular', fontSize: 12 },
});
