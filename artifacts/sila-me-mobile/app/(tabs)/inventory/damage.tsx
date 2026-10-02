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

const types = ['Damaged', 'Expired', 'Rejected', 'Spoiled', 'Broken', 'Missing', 'Other'] as const;

export default function DamageReportScreen() {
  const colors = useColors();
  const router = useRouter();
  const guard = useDiscardGuard();
  const [type, setType] = useState<string | null>(null);
  const [material, setMaterial] = useState('');
  const [qty, setQty] = useState('');
  const [notes, setNotes] = useState('');
  const [review, setReview] = useState(false);

  if (review) {
    return (
      <SilaScreen title="Review stock issue" eyebrow="DAMAGE / REJECTION" onBack={() => setReview(false)} stickyFooter={<SilaBottomAction label="Submit report" disabled onPress={() => undefined} />}>
        {guard.dialog}
        <SilaCard>
          <Text style={[styles.title, { color: colors.foreground }]}>{type ?? 'Type not selected'}</Text>
          <Text style={[styles.meta, { color: colors.mutedForeground }]}>{material || 'No material'} · Qty {qty || '—'}</Text>
          <Text style={[styles.meta, { color: colors.mutedForeground }]}>{notes || 'No notes'}</Text>
        </SilaCard>
        <SilaNotConnected message={capabilityMessage('damageReport')} />
      </SilaScreen>
    );
  }

  return (
    <SilaScreen
      title="Report stock issue"
      eyebrow="INVENTORY"
      fallback="/inventory"
      onBack={() => guard.requestLeave(() => router.replace('/inventory'))}
      stickyFooter={<SilaBottomAction label="Review" onPress={() => setReview(true)} />}
    >
      {guard.dialog}
      <View style={{ gap: 8, marginBottom: 12 }}>
        {types.map((item) => (
          <SilaBottomAction key={item} label={item} secondary={type !== item} onPress={() => { setType(item); guard.setDirty(true); }} />
        ))}
      </View>
      <SilaSearchBar value={material} onChangeText={(value) => { setMaterial(value); guard.setDirty(true); }} placeholder="Select material" />
      <SilaQuantityInput label="QUANTITY" value={qty} onChangeText={(value) => { setQty(value); guard.setDirty(true); }} />
      <SilaSearchBar value={notes} onChangeText={(value) => { setNotes(value); guard.setDirty(true); }} placeholder="Notes" />
      <SilaNotConnected message={capabilityMessage('damageReport')} />
    </SilaScreen>
  );
}

const styles = StyleSheet.create({
  title: { fontFamily: 'Inter_700Bold', fontSize: 15 },
  meta: { marginTop: 8, fontFamily: 'Inter_400Regular', fontSize: 12, lineHeight: 18 },
});
