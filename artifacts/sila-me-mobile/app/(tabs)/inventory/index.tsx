import { useRouter } from 'expo-router';
import React, { useEffect, useState } from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { SilaCard, SilaOfflineBanner, SilaScreen, SilaSummaryCard } from '@/components/sila/ui';
import { useColors } from '@/hooks/useColors';
import { INVENTORY_ACTIONS } from '@/services/ops/nav';
import { liveStockService, type MobileHome } from '@/services/ops/live-stock-ito-service';
import { useStoreScope } from '@/providers/StoreScopeProvider';

export default function InventoryLanding() {
  const colors = useColors();
  const router = useRouter();
  const { organization } = useStoreScope();
  const [home, setHome] = useState<MobileHome | null>(null);
  useEffect(() => {
    if (!organization?.id) return;
    void liveStockService.home(organization.id).then((result) => {
      if (result.status === 'OK') setHome(result.data);
    });
  }, [organization?.id]);
  return (
    <SilaScreen title="Inventory" eyebrow="STORE STOCK" showBack={false}>
      <SilaOfflineBanner />
      <Text style={[styles.copy, { color: colors.mutedForeground }]}>
        Search or scan a material, then move stock from nearby locations. Company code, plant, and GL are derived from Location Master.
      </Text>
      <Text style={[styles.section, { color: colors.mutedForeground }]}>MY LOCATION</Text>
      <Text style={[styles.title, { color: colors.foreground, marginBottom: 12 }]}>{home?.myLocationName ?? (home ? 'No operational location assigned' : '…')}</Text>
      <View style={styles.summary}>
        <SilaSummaryCard label="Available Items" value={home ? String(home.availableItems) : '—'} />
        <SilaSummaryCard label="Low Stock" value={home ? String(home.lowStock) : '—'} />
        <SilaSummaryCard label="Incoming" value={home ? String(home.incoming) : '—'} />
        <SilaSummaryCard label="My Actions" value={home ? String(home.myActions) : '—'} />
      </View>
      <Text style={[styles.section, { color: colors.mutedForeground }]}>TASKS</Text>
      <View style={styles.summary}>
        <SilaSummaryCard label="Incoming Transfer" value={home ? String(home.incomingTransfers) : '—'} />
        <SilaSummaryCard label="Awaiting Receipt" value={home ? String(home.awaitingReceipt) : '—'} />
        <SilaSummaryCard label="Approval" value={home ? String(home.approvals) : '—'} />
        <SilaSummaryCard label="Critical Alert" value={home ? String(home.criticalAlerts) : '—'} />
      </View>
      {INVENTORY_ACTIONS.map((action) => (
        <SilaCard key={action.href} onPress={() => router.push(action.href as never)}>
          <Text style={[styles.title, { color: colors.foreground }]}>{action.title}</Text>
        </SilaCard>
      ))}
      <SilaCard onPress={() => router.push('/inventory/quick-transfer?mode=record' as never)}>
        <Text style={[styles.title, { color: colors.foreground }]}>Record Quick Transfer</Text>
      </SilaCard>
      <SilaCard onPress={() => router.push('/inventory/alerts' as never)}>
        <Text style={[styles.title, { color: colors.foreground }]}>Alerts / Actions</Text>
      </SilaCard>
    </SilaScreen>
  );
}

const styles = StyleSheet.create({
  copy: { marginBottom: 14, fontFamily: 'Inter_400Regular', fontSize: 14, lineHeight: 21 },
  section: { fontFamily: 'Inter_600SemiBold', fontSize: 10, letterSpacing: 1.1, marginBottom: 10 },
  summary: { flexDirection: 'row', flexWrap: 'wrap', gap: 10, marginBottom: 14 },
  title: { fontFamily: 'Inter_700Bold', fontSize: 15 },
});
