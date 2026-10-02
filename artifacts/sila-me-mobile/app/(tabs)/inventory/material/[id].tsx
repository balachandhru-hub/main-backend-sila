import { useLocalSearchParams, useRouter } from 'expo-router';
import React from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { SilaBottomAction, SilaCard, SilaNotConnected, SilaScreen } from '@/components/sila/ui';
import { useColors } from '@/hooks/useColors';
import { capabilityMessage } from '@/services/ops/capabilities';

export default function StockDetailScreen() {
  const colors = useColors();
  const router = useRouter();
  const { id } = useLocalSearchParams<{ id?: string }>();
  return (
    <SilaScreen title="Stock detail" eyebrow="INVENTORY" fallback="/inventory/search">
      <SilaCard>
        <Text style={[styles.title, { color: colors.foreground }]}>Material {id ?? '—'}</Text>
        <Text style={[styles.meta, { color: colors.mutedForeground }]}>Available qty, batch, and location load from inventory APIs when connected.</Text>
      </SilaCard>
      <SilaNotConnected message={capabilityMessage('inventoryLookup')} />
      <View style={{ gap: 10, marginTop: 12 }}>
        <SilaBottomAction label="Transfer" secondary onPress={() => router.push('/inventory/transfer')} />
        <SilaBottomAction label="Issue" secondary onPress={() => router.push('/inventory/issue')} />
        <SilaBottomAction label="Count" secondary onPress={() => router.push('/inventory/count')} />
      </View>
    </SilaScreen>
  );
}

const styles = StyleSheet.create({
  title: { fontFamily: 'Inter_700Bold', fontSize: 16 },
  meta: { marginTop: 8, fontFamily: 'Inter_400Regular', fontSize: 13, lineHeight: 19 },
});
