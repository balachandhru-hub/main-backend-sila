import { useRouter } from 'expo-router';
import React, { useEffect, useMemo, useState } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { customFetch, getGetGrnsQueryKey, useGetGrns } from '@workspace/api-client-react';
import { SilaCard, SilaEmptyState, SilaLoadingState, SilaNotConnected, SilaScreen, SilaStatusChip } from '@/components/sila/ui';
import { useColors } from '@/hooks/useColors';
import { useStoreScope } from '@/providers/StoreScopeProvider';
import { capabilityMessage } from '@/services/ops/capabilities';
import { TASK_TABS } from '@/services/ops/nav';

type StockTask = { kind: string; sessionId: string; enquiryId?: string | null; number: string; location: string; title: string; status: string };

const tabs = TASK_TABS;

export default function TasksLanding() {
  const colors = useColors();
  const router = useRouter();
  const { organization } = useStoreScope();
  const [stockTasks, setStockTasks] = useState<StockTask[]>([]);
  const [tab, setTab] = useState<(typeof tabs)[number]>('EXCEPTIONS');
  useEffect(() => {
    if (!organization?.id) return;
    void customFetch<StockTask[]>(`/api/v1/inventory/stock-counts/tasks?organizationId=${organization.id}`, { credentials: 'include', responseType: 'json' })
      .then(setStockTasks)
      .catch(() => setStockTasks([]));
  }, [organization?.id]);
  const groups = ['STOCK_COUNT', 'SHORTAGE_ENQUIRY', 'RECOUNT_REQUIRED', 'INFORMATION_REQUESTED'] as const;
  const grns = useGetGrns({ query: { queryKey: getGetGrnsQueryKey(), retry: false, enabled: tab === 'EXCEPTIONS' } });
  const exceptions = useMemo(
    () => (grns.data ?? []).filter((item) => item.status === 'FAILED' || item.status === 'UNKNOWN'),
    [grns.data],
  );

  return (
    <SilaScreen title="Tasks" eyebrow="OPERATIONS" showBack={false}>
      {groups.map((kind) => {
        const items = stockTasks.filter((item) => item.kind === kind);
        return (
          <SilaCard key={kind}>
            <Text style={[styles.title, { color: colors.foreground }]}>{kind.replaceAll('_', ' ')} · {items.length}</Text>
            {items.map((item) => (
              <Text key={`${item.kind}-${item.enquiryId ?? item.sessionId}-${item.title}`} style={[styles.meta, { color: colors.primary }]} onPress={() => router.push(item.enquiryId ? { pathname: '/inventory/enquiry', params: { id: item.enquiryId } } : '/inventory/count')}>
                {item.location} · {item.title}
              </Text>
            ))}
          </SilaCard>
        );
      })}
      <View style={styles.tabs}>
        {tabs.map((item) => (
          <Pressable key={item} onPress={() => setTab(item)} style={[styles.tab, { borderColor: tab === item ? colors.primary : colors.border, backgroundColor: tab === item ? `${colors.primary}12` : colors.card }]}>
            <Text style={{ color: tab === item ? colors.primary : colors.mutedForeground, fontFamily: 'Inter_600SemiBold', fontSize: 10 }}>{item}</Text>
          </Pressable>
        ))}
      </View>
      {tab === 'ITO APPROVALS' ? (
        <>
          <Text style={[styles.copy, { color: colors.mutedForeground }]}>
            Supports ITO Source Approval, Destination Approval, Dispatch, and Receipt once connected.
          </Text>
          <SilaNotConnected message={capabilityMessage('itoApproval')} />
          <SilaEmptyState title="No ITO approval tasks." description="Approval cards appear when the ITO approval service is connected. Actions never simulate success." />
          <SilaCard onPress={() => router.push({ pathname: '/tasks/[id]', params: { id: 'ito-preview', type: 'ito' } })}>
            <Text style={[styles.title, { color: colors.foreground }]}>ITO Approvals</Text>
            <Text style={[styles.meta, { color: colors.mutedForeground }]}>FROM · TO · Materials · Requested By · Review</Text>
          </SilaCard>
        </>
      ) : null}
      {tab === 'APPROVALS' || tab === 'MY TASKS' || tab === 'COMPLETED' ? (
        <>
          <SilaNotConnected message={capabilityMessage(tab === 'APPROVALS' ? 'approvals' : 'itoApproval')} />
          <SilaEmptyState
            title={tab === 'COMPLETED' ? 'No completed tasks.' : 'No tasks available.'}
            description="Stock count variance, goods issue, damage, GRN exception, and invoice/OCR exception tasks appear when connected."
          />
        </>
      ) : null}
      {tab === 'EXCEPTIONS' ? (
        <>
          {grns.isPending ? <SilaLoadingState /> : null}
          {!grns.isPending && exceptions.length === 0 ? <SilaEmptyState title="No exceptions right now." /> : null}
          {exceptions.map((item) => (
            <SilaCard key={item.id} onPress={() => router.push({ pathname: '/receive/grn-result', params: { grnId: item.id } })}>
              <View style={styles.row}>
                <View style={{ flex: 1 }}>
                  <Text style={[styles.title, { color: colors.foreground }]}>GRN Failed / Unknown</Text>
                  <Text style={[styles.meta, { color: colors.mutedForeground }]}>{item.grnNumber} · {item.supplierName}</Text>
                </View>
                <SilaStatusChip label={item.status} tone="failed" />
              </View>
            </SilaCard>
          ))}
        </>
      ) : null}
    </SilaScreen>
  );
}

const styles = StyleSheet.create({
  tabs: { flexDirection: 'row', flexWrap: 'wrap', gap: 8, marginBottom: 14 },
  tab: { minHeight: 36, borderWidth: 1, borderRadius: 8, paddingHorizontal: 10, alignItems: 'center', justifyContent: 'center' },
  row: { flexDirection: 'row', gap: 10 },
  title: { fontFamily: 'Inter_700Bold', fontSize: 14 },
  meta: { marginTop: 4, fontFamily: 'Inter_400Regular', fontSize: 12 },
  copy: { marginBottom: 10, fontFamily: 'Inter_400Regular', fontSize: 12, lineHeight: 18 },
});
