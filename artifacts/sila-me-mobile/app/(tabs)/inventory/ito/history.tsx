import { useRouter } from 'expo-router';
import React, { useState } from 'react';
import { StyleSheet, Text } from 'react-native';
import { SilaEmptyState, SilaNotConnected, SilaScreen, SilaSearchBar } from '@/components/sila/ui';
import { useColors } from '@/hooks/useColors';
import { capabilityMessage } from '@/services/ops/capabilities';

export default function ItoHistoryScreen() {
  const colors = useColors();
  const router = useRouter();
  const [query, setQuery] = useState('');

  return (
    <SilaScreen title="Internal Transfer History" eyebrow="MORE" fallback="/more" onBack={() => router.replace('/more')}>
      <SilaSearchBar value={query} onChangeText={setQuery} placeholder="Search ITO history…" />
      <Text style={[styles.copy, { color: colors.mutedForeground }]}>
        Completed, cancelled, and failed transfer orders will appear here once connected.
      </Text>
      <SilaNotConnected message={capabilityMessage('internalTransferOrder')} />
      <SilaEmptyState title="No transfer history." description="History loads from the ITO service when available." />
    </SilaScreen>
  );
}

const styles = StyleSheet.create({
  copy: { marginBottom: 12, fontFamily: 'Inter_400Regular', fontSize: 13, lineHeight: 19 },
});
