import { useLocalSearchParams, useRouter } from 'expo-router';
import React, { useEffect, useState } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { SilaCard, SilaEmptyState, SilaScreen, SilaStatusChip } from '@/components/sila/ui';
import { useColors } from '@/hooks/useColors';
import { inventoryActionService, itoService, liveStockService, type LiveDecision } from '@/services/ops/live-stock-ito-service';
import { useStoreScope } from '@/providers/StoreScopeProvider';

export default function LiveStockMaterialDetail() {
  const colors = useColors();
  const router = useRouter();
  const { materialId } = useLocalSearchParams<{ materialId: string }>();
  const { organization, hasPermission } = useStoreScope();
  const [live, setLive] = useState<LiveDecision | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [myLocationId, setMyLocationId] = useState<string | null>(null);
  useEffect(() => {
    if (!materialId || !organization?.id) return;
    void liveStockService.home(organization.id).then((home) => {
      const locationId = home.status === 'OK' ? home.data.myLocationId ?? null : null;
      setMyLocationId(locationId);
      return liveStockService.live(materialId, organization.id, 1, locationId ?? undefined);
    }).then((result) => {
      if (result.status === 'OK') setLive(result.data);
      else setMessage(result.status === 'NOT_IMPLEMENTED' ? result.message : result.message);
    });
  }, [materialId, organization?.id]);
  const nearby = (live?.balances ?? []).filter((row) => row.unitId !== myLocationId && (row.availableQty ?? 0) > 0);
  const current = (live?.balances ?? []).find((row) => row.unitId === myLocationId);
  const canQuick = hasPermission('CREATE_INTERNAL_TRANSFER') || hasPermission('CREATE_STOCK_TRANSFER');
  return (
    <SilaScreen title={live?.material.materialCode ?? 'Material'} eyebrow="LIVE INVENTORY" fallback="/inventory/live-stock">
      {message ? <Text style={{ color: colors.mutedForeground }}>{message}</Text> : null}
      {live ? (
        <>
          <Text style={[styles.title, { color: colors.foreground }]}>{live.material.description}</Text>
          <Text style={{ color: colors.mutedForeground, marginBottom: 12 }}>{live.material.materialCode} · {live.material.uom}</Text>
          <View style={styles.row}>
            <SilaStatusChip label={`On hand ${current?.onHandQty ?? 0}`} />
            <SilaStatusChip label={`Available ${current?.availableQty ?? 0}`} />
            <SilaStatusChip label={`Incoming ${current?.inTransitQty ?? 0}`} />
          </View>
          <Text style={[styles.section, { color: colors.mutedForeground }]}>AVAILABLE NEARBY</Text>
          {nearby.length === 0 ? <SilaEmptyState title="No nearby stock" description="No transferable internal quantity is currently recorded." /> : nearby.map((row) => (
            <SilaCard key={row.unitId}>
              <Text style={[styles.title, { color: colors.foreground }]}>{row.unitName}</Text>
              <Text style={{ color: colors.mutedForeground }}>{row.locationType} · Available {row.availableQty} · Transferable {row.transferableQty ?? row.availableQty}</Text>
              <View style={styles.row}>
                {canQuick ? <Pressable testID="get-1" style={[styles.action, { borderColor: colors.primary }]} onPress={() => {
                  if (!myLocationId || !organization?.id) return;
                  void itoService.quickGetOne(organization.id, materialId, row.unitId, myLocationId).then((result) => {
                    if (result.status === 'OK') router.push(`/inventory/ito/${result.data.id}` as never);
                    else setMessage(result.message);
                  });
                }}><Text style={{ color: colors.primary, fontFamily: 'Inter_700Bold' }}>GET 1</Text></Pressable> : null}
                <Pressable style={[styles.action, { borderColor: colors.border }]} onPress={() => router.push(`/inventory/ito/new?from=${row.unitId}&to=${myLocationId ?? ''}&material=${materialId}` as never)}>
                  <Text style={{ color: colors.foreground, fontFamily: 'Inter_700Bold' }}>REQUEST</Text>
                </Pressable>
              </View>
            </SilaCard>
          ))}
          <Text style={{ color: colors.mutedForeground, marginBottom: 8 }}>{live.recommendationReason}</Text>
          <View style={styles.row}>
            <Pressable style={[styles.action, { borderColor: colors.border }]} onPress={() => organization?.id && inventoryActionService.createPr(organization.id, materialId, Math.max(live.shortage, 1), myLocationId ?? undefined)}><Text>Create PR</Text></Pressable>
            <Pressable style={[styles.action, { borderColor: colors.border }]} onPress={() => organization?.id && myLocationId && inventoryActionService.requestCount(organization.id, myLocationId, 'Requested from mobile live inventory')}><Text>Count</Text></Pressable>
          </View>
        </>
      ) : null}
    </SilaScreen>
  );
}

const styles = StyleSheet.create({
  title: { fontFamily: 'Inter_700Bold', fontSize: 18 },
  section: { fontFamily: 'Inter_600SemiBold', fontSize: 10, letterSpacing: 1.1, marginTop: 16, marginBottom: 8 },
  row: { flexDirection: 'row', flexWrap: 'wrap', gap: 8, marginBottom: 12 },
  action: { borderWidth: 1, borderRadius: 8, paddingHorizontal: 12, minHeight: 40, justifyContent: 'center' },
});
