import { useLocalSearchParams } from 'expo-router';
import React from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { SilaBottomAction, SilaCard, SilaNotConnected, SilaScreen } from '@/components/sila/ui';
import { useColors } from '@/hooks/useColors';
import { capabilityMessage } from '@/services/ops/capabilities';
import { itoService } from '@/services/ops';

export default function TaskDetailScreen() {
  const colors = useColors();
  const { id, type } = useLocalSearchParams<{ id: string; type?: string }>();
  const isIto = type === 'ito' || (id ?? '').startsWith('ito');
  const [message, setMessage] = React.useState<string | null>(null);

  const attempt = async (action: 'approve' | 'reject' | 'sendBack') => {
    const result = await itoService.approve();
    setMessage(result.status === 'NOT_IMPLEMENTED' ? result.message : `Decision ${action} recorded.`);
  };

  if (isIto) {
    return (
      <SilaScreen title="ITO Approval" eyebrow="TASKS" fallback="/tasks">
        <SilaCard>
          <Text style={[styles.title, { color: colors.foreground }]}>{id === 'ito-preview' ? 'ITO Approval' : id}</Text>
          <Text style={[styles.meta, { color: colors.mutedForeground }]}>FROM — source property / store</Text>
          <Text style={[styles.meta, { color: colors.mutedForeground }]}>TO — destination property / store</Text>
          <Text style={[styles.meta, { color: colors.mutedForeground }]}>Materials · Requested By · Requested Date</Text>
        </SilaCard>
        <View style={{ gap: 10, marginTop: 12 }}>
          <SilaBottomAction label="Review" secondary onPress={() => undefined} />
          <SilaBottomAction label="Approve" disabled onPress={() => void attempt('approve')} />
          <SilaBottomAction label="Reject" disabled secondary onPress={() => void attempt('reject')} />
          <SilaBottomAction label="Send back" disabled secondary onPress={() => void attempt('sendBack')} />
          <SilaBottomAction label="Attempt approve (backend required)" secondary onPress={() => void attempt('approve')} />
        </View>
        <SilaNotConnected message={message ?? capabilityMessage('itoApproval')} />
      </SilaScreen>
    );
  }

  return (
    <SilaScreen title="Approval detail" eyebrow="TASKS" fallback="/tasks">
      <SilaCard>
        <Text style={[styles.title, { color: colors.foreground }]}>Request {id}</Text>
        <Text style={[styles.meta, { color: colors.mutedForeground }]}>Request type, requester, store, reason, and line details will appear when approvals are connected.</Text>
      </SilaCard>
      <View style={{ gap: 10, marginTop: 12 }}>
        <SilaBottomAction label="Approve" disabled onPress={() => undefined} />
        <SilaBottomAction label="Reject" disabled secondary onPress={() => undefined} />
        <SilaBottomAction label="Send back" disabled secondary onPress={() => undefined} />
      </View>
      <SilaNotConnected message={capabilityMessage('approvals')} />
    </SilaScreen>
  );
}

const styles = StyleSheet.create({
  title: { fontFamily: 'Inter_700Bold', fontSize: 16 },
  meta: { marginTop: 8, fontFamily: 'Inter_400Regular', fontSize: 13, lineHeight: 19 },
});
