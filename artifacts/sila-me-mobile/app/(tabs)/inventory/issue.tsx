import { useRouter } from 'expo-router';
import React, { useState } from 'react';
import { StyleSheet, Text, View } from 'react-native';
import {
  SilaBottomAction,
  SilaCard,
  SilaNotConnected,
  SilaQuantityInput,
  SilaScreen,
  SilaSearchBar,
  useDiscardGuard,
} from '@/components/sila/ui';
import { useColors } from '@/hooks/useColors';
import { capabilityMessage } from '@/services/ops/capabilities';

const issueTypes = ['Kitchen Consumption', 'Engineering', 'Housekeeping', 'F&B Outlet', 'Internal Consumption', 'Other'] as const;

export default function GoodsIssueScreen() {
  const colors = useColors();
  const router = useRouter();
  const guard = useDiscardGuard();
  const [issueType, setIssueType] = useState<string | null>(null);
  const [material, setMaterial] = useState('');
  const [qty, setQty] = useState('');
  const [review, setReview] = useState(false);

  if (review) {
    return (
      <SilaScreen title="Issue review" eyebrow="GOODS ISSUE" onBack={() => setReview(false)} stickyFooter={<SilaBottomAction label="Submit issue" disabled onPress={() => undefined} />}>
        {guard.dialog}
        <SilaCard>
          <Text style={[styles.title, { color: colors.foreground }]}>{issueType ?? 'Issue type not selected'}</Text>
          <Text style={[styles.meta, { color: colors.mutedForeground }]}>{material || 'No material'} · Qty {qty || '—'}</Text>
        </SilaCard>
        <SilaNotConnected message={capabilityMessage('goodsIssue')} />
      </SilaScreen>
    );
  }

  return (
    <SilaScreen
      title="Goods issue"
      eyebrow="INVENTORY"
      fallback="/inventory"
      onBack={() => guard.requestLeave(() => router.replace('/inventory'))}
      stickyFooter={<SilaBottomAction label="Review issue" onPress={() => setReview(true)} />}
    >
      {guard.dialog}
      <Text style={[styles.meta, { color: colors.mutedForeground }]}>Select issue type</Text>
      <View style={{ gap: 8, marginVertical: 10 }}>
        {issueTypes.map((item) => (
          <SilaBottomAction key={item} label={item} secondary={issueType !== item} onPress={() => { setIssueType(item); guard.setDirty(true); }} />
        ))}
      </View>
      <SilaSearchBar value={material} onChangeText={(value) => { setMaterial(value); guard.setDirty(true); }} placeholder="Add material" />
      <SilaQuantityInput label="ISSUE QTY" value={qty} onChangeText={(value) => { setQty(value); guard.setDirty(true); }} />
      <SilaNotConnected message={capabilityMessage('goodsIssue')} />
    </SilaScreen>
  );
}

const styles = StyleSheet.create({
  title: { fontFamily: 'Inter_700Bold', fontSize: 15 },
  meta: { marginTop: 8, fontFamily: 'Inter_400Regular', fontSize: 12, lineHeight: 18 },
});
