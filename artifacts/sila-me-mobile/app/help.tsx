import React from 'react';
import { StyleSheet, Text } from 'react-native';
import { SilaCard, SilaScreen } from '@/components/sila/ui';
import { useColors } from '@/hooks/useColors';

export default function HelpScreen() {
  const colors = useColors();
  return (
    <SilaScreen title="Help" eyebrow="MORE" fallback="/more">
      <SilaCard>
        <Text style={[styles.title, { color: colors.foreground }]}>Receiving</Text>
        <Text style={[styles.meta, { color: colors.mutedForeground }]}>Scan invoice, confirm supplier from master, select open PO, save, then post GRN from physical quantities.</Text>
      </SilaCard>
      <SilaCard>
        <Text style={[styles.title, { color: colors.foreground }]}>Inventory drafts</Text>
        <Text style={[styles.meta, { color: colors.mutedForeground }]}>Count, transfer, issue, and damage screens can hold drafts. They do not post stock until backend transactions are connected.</Text>
      </SilaCard>
    </SilaScreen>
  );
}

const styles = StyleSheet.create({
  title: { fontFamily: 'Inter_700Bold', fontSize: 15 },
  meta: { marginTop: 6, fontFamily: 'Inter_400Regular', fontSize: 13, lineHeight: 19 },
});
