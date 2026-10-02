import { useRouter } from 'expo-router';
import React from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { SilaCard, SilaOfflineBanner, SilaScreen, SilaStatusChip } from '@/components/sila/ui';
import { useColors } from '@/hooks/useColors';

const actions = [
  { title: 'Scan Invoice', description: 'Capture invoice pages, review OCR, match supplier and PO, then save.', href: '/receive/scan', testID: 'receive-scan' },
  { title: 'Receive Against PO', description: 'Select supplier and open PO when no invoice scan is needed first.', href: '/receive/against-po', testID: 'receive-against-po' },
  { title: 'View Open PO', description: 'Search eligible open purchase orders for receiving.', href: '/receive/open-po', testID: 'receive-open-po' },
  { title: 'Pending GRN', description: 'Draft, posting, failed, and unknown goods receipts.', href: '/receive/pending', testID: 'receive-pending' },
  { title: 'Receiving History', description: 'Posted and historical GRN documents.', href: '/receive/history', testID: 'receive-history' },
] as const;

export default function ReceiveLanding() {
  const colors = useColors();
  const router = useRouter();
  return (
    <SilaScreen title="Receive" eyebrow="RECEIVING" showBack={false}>
      <SilaOfflineBanner />
      <Text style={[styles.copy, { color: colors.mutedForeground }]}>
        Invoice scan, PO receiving, and GRN posting for the selected store.
      </Text>
      {actions.map((action) => (
        <SilaCard key={action.href} onPress={() => router.push(action.href as never)}>
          <View style={styles.row} testID={action.testID}>
            <View style={{ flex: 1 }}>
              <Text style={[styles.title, { color: colors.foreground }]}>{action.title}</Text>
              <Text style={[styles.meta, { color: colors.mutedForeground }]}>{action.description}</Text>
            </View>
            <SilaStatusChip label="OPEN" tone="neutral" />
          </View>
        </SilaCard>
      ))}
    </SilaScreen>
  );
}

const styles = StyleSheet.create({
  copy: { marginBottom: 14, fontFamily: 'Inter_400Regular', fontSize: 14, lineHeight: 21 },
  row: { flexDirection: 'row', gap: 12, alignItems: 'flex-start' },
  title: { fontFamily: 'Inter_700Bold', fontSize: 15 },
  meta: { marginTop: 6, fontFamily: 'Inter_400Regular', fontSize: 12, lineHeight: 18 },
});
