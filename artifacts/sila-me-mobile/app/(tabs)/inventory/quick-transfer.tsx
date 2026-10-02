import { useLocalSearchParams, useRouter } from 'expo-router';
import React, { useEffect, useState } from 'react';
import { Pressable, StyleSheet, Text, TextInput, View } from 'react-native';
import { SilaBottomAction, SilaScreen, SilaSearchBar } from '@/components/sila/ui';
import { useColors } from '@/hooks/useColors';
import { itoService, liveStockService, type InventoryLocationOption, type LiveStockMaterial } from '@/services/ops/live-stock-ito-service';
import { useStoreScope } from '@/providers/StoreScopeProvider';

export default function QuickTransferScreen() {
  const colors = useColors();
  const router = useRouter();
  const params = useLocalSearchParams<{ mode?: string }>();
  const record = params.mode === 'record';
  const { organization } = useStoreScope();
  const [locations, setLocations] = useState<InventoryLocationOption[]>([]);
  const [fromId, setFromId] = useState('');
  const [toId, setToId] = useState('');
  const [query, setQuery] = useState('');
  const [material, setMaterial] = useState<LiveStockMaterial | null>(null);
  const [qty, setQty] = useState('1');
  const [message, setMessage] = useState<string | null>(null);
  useEffect(() => {
    if (!organization?.id) return;
    void liveStockService.locations(organization.id).then((result) => {
      if (result.status === 'OK' || result.status === 'EMPTY') setLocations(result.data.filter((item) => item.transferEnabled));
    });
    void liveStockService.home(organization.id).then((home) => {
      if (home.status === 'OK') setToId(home.data.myLocationId ?? '');
    });
  }, [organization?.id]);
  useEffect(() => {
    if (query.trim().length < 2 || !organization?.id) return;
    const handle = setTimeout(() => {
      void liveStockService.search(query, organization.id).then((result) => {
        if ((result.status === 'OK' || result.status === 'EMPTY') && result.data[0]) setMaterial(result.data[0]);
      });
    }, 300);
    return () => clearTimeout(handle);
  }, [query, organization?.id]);
  return (
    <SilaScreen title={record ? 'Record Quick Transfer' : 'Quick Transfer'} eyebrow="INVENTORY" fallback="/inventory"
      stickyFooter={<SilaBottomAction label={record ? 'Submit already collected' : 'Request'} onPress={() => {
        if (!organization?.id || !material) { setMessage('Search a material first.'); return; }
        void itoService.submit(organization.id, {
          mode: 'QUICK', fromInventoryLocationId: fromId, toInventoryLocationId: toId, reason: record ? 'Already collected' : 'Guest Service',
          alreadyCollected: record, lines: [{ materialId: material.id, quantity: Number(qty) || 1 }],
        }).then((result) => {
          if (result.status === 'OK') router.replace(`/inventory/ito/${result.data.id}` as never);
          else setMessage(result.message);
        });
      }} />}>
      <Text style={{ color: colors.mutedForeground, marginBottom: 8 }}>From location</Text>
      {locations.map((item) => (
        <Pressable key={item.id} onPress={() => setFromId(item.id)} style={[styles.chip, { borderColor: fromId === item.id ? colors.primary : colors.border }]}><Text>{item.locationName}</Text></Pressable>
      ))}
      <Text style={{ color: colors.mutedForeground, marginVertical: 8 }}>To location (defaults to mine)</Text>
      {locations.map((item) => (
        <Pressable key={`to-${item.id}`} onPress={() => setToId(item.id)} style={[styles.chip, { borderColor: toId === item.id ? colors.primary : colors.border }]}><Text>{item.locationName}</Text></Pressable>
      ))}
      <SilaSearchBar value={query} onChangeText={setQuery} placeholder="Search / scan material" />
      {material ? <Text style={{ fontFamily: 'Inter_700Bold' }}>{material.materialCode} · {material.description}</Text> : null}
      <TextInput value={qty} onChangeText={setQty} keyboardType="numeric" style={[styles.input, { borderColor: colors.border, color: colors.foreground }]} />
      {message ? <Text>{message}</Text> : null}
      <View style={{ height: 8 }} />
    </SilaScreen>
  );
}

const styles = StyleSheet.create({
  chip: { borderWidth: 1, borderRadius: 8, padding: 10, marginBottom: 6 },
  input: { borderWidth: 1, borderRadius: 8, minHeight: 44, paddingHorizontal: 10, marginVertical: 10 },
});
