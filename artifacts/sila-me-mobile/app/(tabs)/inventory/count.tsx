import { useRouter } from 'expo-router';
import React, { useState } from 'react';
import { StyleSheet, Text, View } from 'react-native';
import {
  SilaBottomAction,
  SilaCard,
  SilaEmptyState,
  SilaNotConnected,
  SilaQuantityInput,
  SilaScreen,
  SilaSearchBar,
  SilaStatusChip,
  useDiscardGuard,
} from '@/components/sila/ui';
import { useColors } from '@/hooks/useColors';
import { capabilityMessage } from '@/services/ops/capabilities';

const reasons = ['Physical Difference', 'Damage', 'Unrecorded Consumption', 'Receiving Difference', 'Transfer Difference', 'Other'] as const;

export default function StockCountScreen() {
  const colors = useColors();
  const router = useRouter();
  const guard = useDiscardGuard();
  const [step, setStep] = useState<'landing' | 'entry' | 'review'>('landing');
  const [location, setLocation] = useState('');
  const [material, setMaterial] = useState('');
  const [systemQty] = useState('');
  const [counted, setCounted] = useState('');
  const [reason, setReason] = useState<string | null>(null);
  const variance = counted === '' || systemQty === '' ? null : Number(counted) - Number(systemQty || 0);

  if (step === 'landing') {
    return (
      <SilaScreen title="Stock count" eyebrow="INVENTORY" fallback="/inventory" onBack={() => guard.requestLeave(() => router.replace('/inventory'))}>
        {guard.dialog}
        <SilaCard><Text style={[styles.title, { color: colors.foreground }]}>Assigned Counts</Text><SilaEmptyState title="No assigned counts." /></SilaCard>
        <SilaCard><Text style={[styles.title, { color: colors.foreground }]}>In Progress</Text><SilaEmptyState title="No counts in progress." /></SilaCard>
        <SilaCard><Text style={[styles.title, { color: colors.foreground }]}>Completed</Text><SilaEmptyState title="No completed counts." /></SilaCard>
        <View style={{ marginTop: 12 }}>
          <SilaBottomAction label="Start stock count" onPress={() => { setStep('entry'); guard.setDirty(true); }} />
        </View>
      </SilaScreen>
    );
  }

  if (step === 'entry') {
    return (
      <SilaScreen
        title="Count entry"
        eyebrow="STOCK COUNT"
        onBack={() => setStep('landing')}
        stickyFooter={<SilaBottomAction label="Review count" onPress={() => setStep('review')} />}
      >
        {guard.dialog}
        <SilaSearchBar value={location} onChangeText={setLocation} placeholder="Select location" />
        <SilaSearchBar value={material} onChangeText={setMaterial} placeholder="Select or scan material" />
        <SilaCard>
          <Text style={[styles.meta, { color: colors.mutedForeground }]}>System quantity stays blank until inventory lookup is connected.</Text>
          <View style={styles.row}>
            <SilaQuantityInput label="SYSTEM QTY" value={systemQty} onChangeText={() => undefined} />
            <SilaQuantityInput label="COUNTED QTY" value={counted} onChangeText={(value) => { setCounted(value); guard.setDirty(true); }} />
          </View>
          <Text style={[styles.title, { color: variance && variance !== 0 ? colors.warning : colors.foreground, marginTop: 12 }]}>
            Variance {variance == null ? '—' : variance}
          </Text>
          {variance != null && variance !== 0 ? (
            <View style={{ marginTop: 12, gap: 8 }}>
              <Text style={[styles.meta, { color: colors.mutedForeground }]}>Reason required</Text>
              {reasons.map((item) => (
                <SilaBottomAction key={item} label={item} secondary={reason !== item} onPress={() => setReason(item)} />
              ))}
            </View>
          ) : null}
        </SilaCard>
      </SilaScreen>
    );
  }

  return (
    <SilaScreen title="Review count" eyebrow="STOCK COUNT" onBack={() => setStep('entry')} stickyFooter={<SilaBottomAction label="Submit count" disabled onPress={() => undefined} />}>
      {guard.dialog}
      <SilaCard>
        <Text style={[styles.title, { color: colors.foreground }]}>Location {location || '—'}</Text>
        <Text style={[styles.meta, { color: colors.mutedForeground }]}>Material {material || '—'}</Text>
        <Text style={[styles.meta, { color: colors.mutedForeground }]}>Counted {counted || '—'} · Variance {variance ?? '—'} · Reason {reason ?? '—'}</Text>
      </SilaCard>
      <SilaStatusChip label="DRAFT ONLY" tone="pending" />
      <SilaNotConnected message={capabilityMessage('stockCount')} />
    </SilaScreen>
  );
}

const styles = StyleSheet.create({
  title: { fontFamily: 'Inter_700Bold', fontSize: 15 },
  meta: { marginTop: 6, fontFamily: 'Inter_400Regular', fontSize: 12, lineHeight: 18 },
  row: { flexDirection: 'row', gap: 10, marginTop: 12 },
});
