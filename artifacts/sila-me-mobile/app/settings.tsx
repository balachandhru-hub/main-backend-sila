import React from 'react';
import { StyleSheet, Text } from 'react-native';
import { SilaCard, SilaScreen } from '@/components/sila/ui';
import { useColors } from '@/hooks/useColors';
import { useStoreScope } from '@/providers/StoreScopeProvider';

export default function SettingsScreen() {
  const colors = useColors();
  const { isOnline, organization, unit } = useStoreScope();
  return (
    <SilaScreen title="Settings" eyebrow="MORE" fallback="/more">
      <SilaCard>
        <Text style={[styles.title, { color: colors.foreground }]}>Connectivity</Text>
        <Text style={[styles.meta, { color: colors.mutedForeground }]}>{isOnline ? 'ONLINE' : 'OFFLINE'}</Text>
      </SilaCard>
      <SilaCard>
        <Text style={[styles.title, { color: colors.foreground }]}>Working context</Text>
        <Text style={[styles.meta, { color: colors.mutedForeground }]}>{organization?.name ?? '—'} · {unit?.name ?? '—'}</Text>
      </SilaCard>
    </SilaScreen>
  );
}

const styles = StyleSheet.create({
  title: { fontFamily: 'Inter_700Bold', fontSize: 15 },
  meta: { marginTop: 6, fontFamily: 'Inter_400Regular', fontSize: 13 },
});
