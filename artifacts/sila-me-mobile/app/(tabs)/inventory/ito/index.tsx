import { useRouter } from 'expo-router';
import React, { useEffect, useState } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { SilaBottomAction, SilaCard, SilaEmptyState, SilaScreen, SilaSearchBar, SilaStatusChip } from '@/components/sila/ui';
import { useColors } from '@/hooks/useColors';
import { ITO_LANDING_TABS } from '@/services/ops/nav';
import { itoService, type ItoListItem } from '@/services/ops/live-stock-ito-service';
import { useStoreScope } from '@/providers/StoreScopeProvider';

const tabStatus: Record<(typeof ITO_LANDING_TABS)[number], string | undefined> = {
  'MY REQUESTS': undefined,
  'REQUIRES MY APPROVAL': 'PENDING_APPROVAL',
  'IN TRANSIT': 'IN_TRANSIT',
  'AWAITING RECEIPT': 'DISPATCHED',
  'COMPLETED': 'COMPLETED',
};

export default function ItoLanding() {
  const colors = useColors();
  const router = useRouter();
  const { organization } = useStoreScope();
  const [tab, setTab] = useState<(typeof ITO_LANDING_TABS)[number]>('MY REQUESTS');
  const [query, setQuery] = useState('');
  const [rows, setRows] = useState<ItoListItem[]>([]);
  useEffect(() => {
    if (!organization?.id) return;
    void itoService.list(organization.id, tabStatus[tab]).then((result) => {
      if (result.status === 'OK' || result.status === 'EMPTY') setRows(result.data);
    });
  }, [organization?.id, tab]);
  const filtered = rows.filter((item) => !query || `${item.itoNumber} ${item.fromLocation} ${item.toLocation}`.toLowerCase().includes(query.toLowerCase()));
  return (
    <SilaScreen title="Transfers" eyebrow="INVENTORY" fallback="/inventory" stickyFooter={<SilaBottomAction testID="raise-ito" label="+ Raise ITO" onPress={() => router.push('/inventory/ito/new')} />}>
      <View style={styles.tabs}>
        {ITO_LANDING_TABS.map((item) => (
          <Pressable key={item} onPress={() => setTab(item)} style={[styles.tab, { borderColor: tab === item ? colors.primary : colors.border }]}>
            <Text style={{ color: tab === item ? colors.primary : colors.mutedForeground, fontFamily: 'Inter_600SemiBold', fontSize: 10 }}>{item}</Text>
          </Pressable>
        ))}
      </View>
      <SilaSearchBar value={query} onChangeText={setQuery} placeholder="ITO number, from, to…" />
      {filtered.length === 0 ? <SilaEmptyState title="No transfers" description="Raise a STANDARD or QUICK ITO from Live Inventory or + Raise ITO." /> : filtered.map((item) => (
        <SilaCard key={item.id} onPress={() => router.push(`/inventory/ito/${item.id}` as never)}>
          <Text style={{ fontFamily: 'Inter_700Bold', color: colors.foreground }}>{item.itoNumber}</Text>
          <Text style={{ color: colors.mutedForeground }}>{item.fromLocation} → {item.toLocation}</Text>
          <SilaStatusChip label={`${item.mode} ${item.status}`} />
        </SilaCard>
      ))}
    </SilaScreen>
  );
}

const styles = StyleSheet.create({
  tabs: { flexDirection: 'row', flexWrap: 'wrap', gap: 8, marginBottom: 12 },
  tab: { minHeight: 36, borderWidth: 1, borderRadius: 8, paddingHorizontal: 10, alignItems: 'center', justifyContent: 'center' },
});
