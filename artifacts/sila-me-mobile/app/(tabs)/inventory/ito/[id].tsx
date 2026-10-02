import { useLocalSearchParams } from 'expo-router';
import React, { useEffect, useState } from 'react';
import { Pressable, StyleSheet, Text, TextInput, View } from 'react-native';
import { SilaScreen, SilaStatusChip } from '@/components/sila/ui';
import { useColors } from '@/hooks/useColors';
import { itoService, type ItoDetailApi } from '@/services/ops/live-stock-ito-service';
import { useStoreScope } from '@/providers/StoreScopeProvider';

export default function ItoDetailScreen() {
  const colors = useColors();
  const { id } = useLocalSearchParams<{ id: string }>();
  const { organization } = useStoreScope();
  const [detail, setDetail] = useState<ItoDetailApi | null>(null);
  const [qty, setQty] = useState('');
  const [message, setMessage] = useState<string | null>(null);
  const reload = () => {
    if (!id || !organization?.id) return;
    void itoService.get(id, organization.id).then((result) => {
      if (result.status === 'OK') setDetail(result.data);
    });
  };
  useEffect(reload, [id, organization?.id]);
  const act = (path: 'approve' | 'reject' | 'dispatch' | 'receive' | 'discrepancy' | 'handover' | 'dispute-already-collected', confirm = false) => {
    if (!id || !organization?.id) return;
    void itoService.act(organization.id, id, path, Number(qty) || undefined, undefined, confirm).then((result) => {
      if (result.status === 'OK') setDetail(result.data);
      else setMessage(result.message);
    });
  };
  const line = detail?.lines[0];
  return (
    <SilaScreen title={detail?.itoNumber ?? 'ITO'} eyebrow="TRANSFER" fallback="/inventory/ito">
      {detail ? (
        <>
          <SilaStatusChip label={`${detail.mode} ${detail.status}`} />
          <Text style={{ marginTop: 8 }}>{detail.fromLocation} → {detail.toLocation}</Text>
          <Text style={{ color: colors.mutedForeground }}>{detail.transferRelationship}</Text>
          {line ? (
            <>
              <Text style={styles.section}>Material</Text>
              <Text style={{ fontFamily: 'Inter_700Bold' }}>{line.materialCode} · {line.description}</Text>
              <Text>Requested {line.requestedQty} · Available {line.sourceAvailable ?? '—'} · After {line.sourceAfter ?? '—'}</Text>
              <Text>Dispatched {line.dispatchedQty} · Received {line.receivedQty}</Text>
            </>
          ) : null}
          {detail.approvals.map((row) => <Text key={row.id}>{row.side} {row.status} · after {row.stockAfter ?? '—'}</Text>)}
          <TextInput value={qty} onChangeText={setQty} placeholder="Change qty" keyboardType="numeric" style={[styles.input, { borderColor: colors.border, color: colors.foreground }]} />
          <View style={styles.row}>
            {detail.allowedActions.includes('APPROVE') ? <Pressable style={styles.btn} onPress={() => act('approve')}><Text>Approve</Text></Pressable> : null}
            {detail.allowedActions.includes('APPROVE') ? <Pressable style={styles.btn} onPress={() => act('approve')}><Text>Change qty</Text></Pressable> : null}
            {detail.allowedActions.includes('REJECT') ? <Pressable style={styles.btn} onPress={() => act('reject')}><Text>Reject</Text></Pressable> : null}
            {detail.allowedActions.includes('DISPATCH') ? <Pressable style={styles.btn} onPress={() => act('handover')}><Text>Hand over</Text></Pressable> : null}
            {detail.allowedActions.includes('CONFIRM_ALREADY_COLLECTED') ? <Pressable style={styles.btn} onPress={() => act('dispatch', true)}><Text>Confirm collected</Text></Pressable> : null}
            {detail.allowedActions.includes('DISPUTE_ALREADY_COLLECTED') ? <Pressable style={styles.btn} onPress={() => act('dispute-already-collected')}><Text>Dispute collected</Text></Pressable> : null}
            {detail.allowedActions.includes('RECEIVE') ? <Pressable style={styles.btn} onPress={() => act('receive')}><Text>Received</Text></Pressable> : null}
            {detail.allowedActions.includes('REPORT_DISCREPANCY') ? <Pressable style={styles.btn} onPress={() => act('discrepancy')}><Text>Report difference</Text></Pressable> : null}
          </View>
          {detail.events.map((row) => <Text key={row.id} style={{ color: colors.mutedForeground }}>{row.action}</Text>)}
        </>
      ) : null}
      {message ? <Text>{message}</Text> : null}
    </SilaScreen>
  );
}

const styles = StyleSheet.create({
  section: { marginTop: 16, fontFamily: 'Inter_600SemiBold', fontSize: 10, letterSpacing: 1 },
  input: { borderWidth: 1, borderRadius: 8, minHeight: 44, paddingHorizontal: 10, marginVertical: 12 },
  row: { flexDirection: 'row', flexWrap: 'wrap', gap: 8 },
  btn: { borderWidth: 1, borderRadius: 8, paddingHorizontal: 12, minHeight: 40, justifyContent: 'center' },
});
