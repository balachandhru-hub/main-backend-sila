import React from 'react';
import { StyleSheet, Text } from 'react-native';
import { SilaCard, SilaScreen } from '@/components/sila/ui';
import { CustomerBrandImage } from '@/components/branding';
import { useColors } from '@/hooks/useColors';
import { MOBILE_CAPABILITIES } from '@/services/ops/capabilities';

export default function AboutScreen() {
  const colors = useColors();
  return (
    <SilaScreen title="About SILA ME" eyebrow="MORE" fallback="/more">
      <CustomerBrandImage style={styles.logo} />
      <SilaCard>
        <Text style={[styles.title, { color: colors.foreground }]}>SILA ME Mobile</Text>
        <Text style={[styles.meta, { color: colors.mutedForeground }]}>Store operations interface for receiving, inventory drafts, tasks, and documents.</Text>
      </SilaCard>
      <SilaCard>
        <Text style={[styles.title, { color: colors.foreground }]}>Capability status</Text>
        {Object.values(MOBILE_CAPABILITIES).map((item) => (
          <Text key={item.key} style={[styles.meta, { color: colors.mutedForeground }]}>{item.label}: {item.status}</Text>
        ))}
      </SilaCard>
    </SilaScreen>
  );
}

const styles = StyleSheet.create({
  logo: { width: 160, height: 40, marginBottom: 16 },
  title: { fontFamily: 'Inter_700Bold', fontSize: 15 },
  meta: { marginTop: 6, fontFamily: 'Inter_400Regular', fontSize: 12, lineHeight: 18 },
});
