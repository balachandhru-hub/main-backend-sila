import { useLocalSearchParams } from 'expo-router';
import React from 'react';
import { StyleSheet, Text } from 'react-native';
import { SilaBottomAction, SilaCard, SilaNotConnected, SilaScreen } from '@/components/sila/ui';
import { useColors } from '@/hooks/useColors';
import { capabilityMessage } from '@/services/ops/capabilities';

export default function DocumentDetailScreen() {
  const colors = useColors();
  const { id } = useLocalSearchParams<{ id: string }>();
  return (
    <SilaScreen title="Document" eyebrow="DOCUMENTS" fallback="/documents">
      <SilaCard>
        <Text style={[styles.title, { color: colors.foreground }]}>Document {id}</Text>
        <Text style={[styles.meta, { color: colors.mutedForeground }]}>File name, type, uploader, related invoice/PO/GRN, and SharePoint status load when the documents API is connected.</Text>
      </SilaCard>
      <SilaBottomAction label="View document" disabled onPress={() => undefined} />
      <SilaNotConnected message={capabilityMessage('documents')} />
    </SilaScreen>
  );
}

const styles = StyleSheet.create({
  title: { fontFamily: 'Inter_700Bold', fontSize: 16 },
  meta: { marginTop: 8, fontFamily: 'Inter_400Regular', fontSize: 13, lineHeight: 19 },
});
