import { useLocalSearchParams, useRouter } from 'expo-router';
import React, { useEffect, useState } from 'react';
import { StyleSheet, Text, View } from 'react-native';
import type { PurchaseOrder } from '@workspace/api-client-react';
import { SilaBottomAction, SilaCard, SilaEmptyState, SilaErrorState, SilaLoadingState, SilaScreen, SilaStatusChip } from '@/components/sila/ui';
import { useColors } from '@/hooks/useColors';
import { useStoreScope } from '@/providers/StoreScopeProvider';
import { purchaseOrderService } from '@/services/ops';

export default function PoDetailScreen() {
  const colors = useColors();
  const router = useRouter();
  const { id } = useLocalSearchParams<{ id: string }>();
  const { organization } = useStoreScope();
  const [po, setPo] = useState<PurchaseOrder | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!id) return;
    setLoading(true);
    void purchaseOrderService.getByNumber(id, organization?.id).then((result) => {
      setLoading(false);
      if (result.status === 'OK') setPo(result.data);
      else if (result.status === 'ERROR') setError(result.message);
    });
  }, [id, organization?.id]);

  if (loading) return <SilaScreen title="Purchase order" fallback="/receive/open-po"><SilaLoadingState /></SilaScreen>;
  if (error || !po) return <SilaScreen title="Purchase order" fallback="/receive/open-po"><SilaErrorState message={error ?? 'Purchase order not found.'} /></SilaScreen>;

  const eligible = po.items.filter((item) => item.goodsReceiptExpected && !item.deletionIndicator && !item.deliveryCompleted && Number(item.openQuantity) > 0);
  const serviceOnly = po.items.length > 0 && po.items.every((item) => !item.goodsReceiptExpected);

  return (
    <SilaScreen
      title={po.poNumber}
      eyebrow="PURCHASE ORDER"
      fallback="/receive/open-po"
      stickyFooter={serviceOnly ? undefined : <SilaBottomAction label="Receive goods" onPress={() => router.push('/receive/against-po')} disabled={eligible.length === 0} />}
    >
      <SilaCard>
        <Text style={[styles.title, { color: colors.foreground }]}>{po.supplierName}</Text>
        <Text style={[styles.meta, { color: colors.mutedForeground }]}>Supplier ID linked · Company {po.companyCode ?? '—'}</Text>
        <Text style={[styles.meta, { color: colors.mutedForeground }]}>Type {po.purchaseOrderType ?? '—'} · Date {po.poDate ?? '—'} · {po.currency}</Text>
        <View style={{ marginTop: 10 }}><SilaStatusChip label={po.status} tone="neutral" /></View>
      </SilaCard>
      {serviceOnly ? (
        <SilaEmptyState title="SERVICE PO" description="Goods receipt is not available for this PO." />
      ) : (
        po.items.map((item) => (
          <SilaCard key={item.id}>
            <Text style={[styles.title, { color: colors.foreground }]}>{item.itemNumber ?? item.lineNumber} · {item.materialCode}</Text>
            <Text style={[styles.meta, { color: colors.mutedForeground }]}>{item.description}</Text>
            <Text style={[styles.meta, { color: colors.mutedForeground }]}>Ordered {item.orderedQuantity} · Received {item.receivedQuantity} · Open {item.openQuantity} {item.uom}</Text>
            <Text style={[styles.meta, { color: colors.mutedForeground }]}>GR expected {item.goodsReceiptExpected ? 'Yes' : 'No'}</Text>
          </SilaCard>
        ))
      )}
    </SilaScreen>
  );
}

const styles = StyleSheet.create({
  title: { fontFamily: 'Inter_700Bold', fontSize: 15 },
  meta: { marginTop: 4, fontFamily: 'Inter_400Regular', fontSize: 12, lineHeight: 18 },
});
