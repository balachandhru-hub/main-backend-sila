import { useRouter } from 'expo-router';
import React, { useEffect, useState } from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { SilaCard, SilaEmptyState, SilaScreen, SilaSearchBar } from '@/components/sila/ui';
import { useColors } from '@/hooks/useColors';
import { liveStockService, type LiveStockMaterial } from '@/services/ops';
import { useStoreScope } from '@/providers/StoreScopeProvider';

export default function LiveStockHome() {
  const colors = useColors();
  const router = useRouter();
  const { organization } = useStoreScope();
  const [query, setQuery] = useState('');
  const [debounced, setDebounced] = useState('');
  const [results, setResults] = useState<LiveStockMaterial[]>([]);
  const [message, setMessage] = useState<string | null>(null);
  useEffect(() => {
    const handle = setTimeout(() => setDebounced(query.trim()), 300);
    return () => clearTimeout(handle);
  }, [query]);
  useEffect(() => {
    if (debounced.length < 2 || !organization?.id) {
      setResults([]);
      return;
    }
    let cancelled = false;
    void liveStockService.search(debounced, organization.id).then((result) => {
      if (cancelled) return;
      if (result.status === 'OK' || result.status === 'EMPTY') {
        setResults(result.data);
        setMessage(null);
      } else {
        setMessage(result.status === 'NOT_IMPLEMENTED' ? result.message : result.message);
        setResults([]);
      }
    });
    return () => { cancelled = true; };
  }, [debounced, organization?.id]);
  return (
    <SilaScreen title="Live Inventory" eyebrow="INVENTORY" fallback="/inventory">
      <SilaSearchBar value={query} onChangeText={setQuery} placeholder="Search or scan material ID, name, barcode…" />
      {message ? <Text style={{ color: colors.mutedForeground, marginBottom: 8 }}>{message}</Text> : null}
      {debounced.length >= 2 && results.length === 0 ? <SilaEmptyState title="No materials" description="Server-side search found no matches." /> : null}
      {results.map((item) => (
        <SilaCard key={item.id} onPress={() => router.push(`/inventory/live-stock/${item.id}` as never)}>
          <Text style={[styles.title, { color: colors.foreground }]}>{item.materialCode}</Text>
          <Text style={{ color: colors.mutedForeground }}>{item.description}</Text>
        </SilaCard>
      ))}
      <View style={{ height: 8 }} />
    </SilaScreen>
  );
}

const styles = StyleSheet.create({
  title: { fontFamily: 'Inter_700Bold', fontSize: 15 },
});
