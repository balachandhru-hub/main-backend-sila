import { useRouter } from 'expo-router';
import React, { useEffect, useState } from 'react';
import { StyleSheet, Text } from 'react-native';
import { SilaCard, SilaEmptyState, SilaScreen } from '@/components/sila/ui';
import { useColors } from '@/hooks/useColors';
import { inventoryActionService } from '@/services/ops/live-stock-ito-service';
import { useStoreScope } from '@/providers/StoreScopeProvider';

export default function InventoryAlertsScreen() {
  const colors = useColors();
  const router = useRouter();
  const { organization, hasPermission } = useStoreScope();
  const [rows, setRows] = useState<Array<{ id: string; kind: string; severity: string; title?: string | null; recommendedAction?: string | null }>>([]);
  useEffect(() => {
    if (!organization?.id) return;
    void inventoryActionService.alerts(organization.id).then(setRows).catch(() => setRows([]));
  }, [organization?.id]);
  const visible = rows.filter((item) => item.kind !== 'INVENTORY_VARIANCE' || hasPermission('APPROVE_INVENTORY_COUNT') || hasPermission('CREATE_INVENTORY_COUNT'));
  return (
    <SilaScreen title="Inventory actions" eyebrow="ALERTS" fallback="/inventory">
      {visible.length === 0 ? <SilaEmptyState title="No open actions" description="Stockout, discrepancy, and manager-review tasks appear here." /> : visible.map((item) => (
        <SilaCard key={item.id} onPress={() => {
          if (item.recommendedAction === 'REVIEW_TRANSFER') router.push('/inventory/ito' as never);
          else if (item.recommendedAction === 'QUICK_TRANSFER') router.push('/inventory/quick-transfer' as never);
          else router.push('/inventory/live-stock' as never);
        }}>
          <Text style={[styles.title, { color: colors.foreground }]}>{item.kind.replaceAll('_', ' ')}</Text>
          <Text style={{ color: colors.mutedForeground }}>{item.severity} · {item.title}</Text>
          <Text style={{ color: colors.mutedForeground }}>{item.recommendedAction === 'REQUEST_PHYSICAL_INVENTORY' && !hasPermission('APPROVE_INVENTORY_COUNT') ? 'Manager review' : item.recommendedAction}</Text>
        </SilaCard>
      ))}
    </SilaScreen>
  );
}

const styles = StyleSheet.create({
  title: { fontFamily: 'Inter_700Bold', fontSize: 15 },
});
