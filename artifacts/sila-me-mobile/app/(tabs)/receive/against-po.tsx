import { useRouter } from 'expo-router';
import React, { useEffect, useState } from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { getGetAccessContextQueryKey, useGetAccessContext } from '@workspace/api-client-react';
import type { PurchaseOrder, Supplier } from '@workspace/api-client-react';
import {
  SilaBottomAction,
  SilaCard,
  SilaEmptyState,
  SilaErrorState,
  SilaLoadingState,
  SilaQuantityInput,
  SilaScreen,
  SilaSearchBar,
  SilaStatusChip,
  useDiscardGuard,
} from '@/components/sila/ui';
import { useColors } from '@/hooks/useColors';
import { purchaseOrderService, supplierService } from '@/services/ops';
import { useStoreScope } from '@/providers/StoreScopeProvider';

function formatPo(po: PurchaseOrder) {
  const openItems = po.items.filter((item) => item.goodsReceiptExpected && !item.deletionIndicator && !item.deliveryCompleted && Number(item.openQuantity) > 0).length;
  const date = po.poDate ?? '—';
  const amount = po.totalAmount != null ? `${po.currency} ${Number(po.totalAmount).toLocaleString()}` : po.currency;
  return `${date} · ${amount} · ${openItems} open items`;
}

export default function ReceiveAgainstPo() {
  const colors = useColors();
  const router = useRouter();
  const { organization } = useStoreScope();
  const access = useGetAccessContext({ query: { queryKey: getGetAccessContextQueryKey(), retry: false } });
  const organizationId = organization?.id ?? access.data?.organizations[0]?.id;
  const [step, setStep] = useState<'supplier' | 'po' | 'items' | 'review'>('supplier');
  const [supplierQuery, setSupplierQuery] = useState('');
  const [debounced, setDebounced] = useState('');
  const [suppliers, setSuppliers] = useState<Supplier[]>([]);
  const [supplierLoading, setSupplierLoading] = useState(false);
  const [supplierError, setSupplierError] = useState<string | null>(null);
  const [supplier, setSupplier] = useState<Supplier | null>(null);
  const [pos, setPos] = useState<PurchaseOrder[]>([]);
  const [poLoading, setPoLoading] = useState(false);
  const [po, setPo] = useState<PurchaseOrder | null>(null);
  const [received, setReceived] = useState<Record<string, string>>({});
  const [accepted, setAccepted] = useState<Record<string, string>>({});
  const [damaged, setDamaged] = useState<Record<string, string>>({});
  const [rejected, setRejected] = useState<Record<string, string>>({});
  const [batch, setBatch] = useState<Record<string, string>>({});
  const [expiry, setExpiry] = useState<Record<string, string>>({});
  const [deliveryNote, setDeliveryNote] = useState('');
  const guard = useDiscardGuard();

  useEffect(() => {
    const handle = setTimeout(() => setDebounced(supplierQuery.trim()), 300);
    return () => clearTimeout(handle);
  }, [supplierQuery]);

  useEffect(() => {
    if (!organizationId || debounced.length < 2) {
      setSuppliers([]);
      return;
    }
    let cancelled = false;
    setSupplierLoading(true);
    void supplierService.search(organizationId, debounced).then((result) => {
      if (cancelled) return;
      setSupplierLoading(false);
      if (result.status === 'OK' || result.status === 'EMPTY') {
        setSuppliers(result.data);
        setSupplierError(null);
      } else if (result.status === 'ERROR') setSupplierError(result.message);
    });
    return () => { cancelled = true; };
  }, [debounced, organizationId]);

  useEffect(() => {
    if (!supplier) return;
    setPoLoading(true);
    void purchaseOrderService.searchOpen({ organizationId, supplierId: supplier.id }).then((result) => {
      setPoLoading(false);
      if (result.status === 'OK' || result.status === 'EMPTY') setPos(result.data);
    });
  }, [organizationId, supplier]);

  const eligible = (po?.items ?? []).filter((item) => item.goodsReceiptExpected && !item.deletionIndicator && !item.deliveryCompleted && Number(item.openQuantity) > 0);

  if (step === 'supplier') {
    return (
      <SilaScreen title="Select supplier" eyebrow="RECEIVE AGAINST PO" fallback="/receive" onBack={() => guard.requestLeave(() => router.replace('/receive'))}>
        {guard.dialog}
        <SilaSearchBar value={supplierQuery} onChangeText={(value) => { setSupplierQuery(value); guard.setDirty(true); }} placeholder="Search supplier name, ID, or TRN" />
        {supplierLoading ? <SilaLoadingState label="Searching suppliers…" /> : null}
        {supplierError ? <SilaErrorState message={supplierError} /> : null}
        {!supplierLoading && debounced.length >= 2 && suppliers.length === 0 ? <SilaEmptyState title="No suppliers matched." /> : null}
        {suppliers.map((item) => (
          <SilaCard key={item.id} onPress={() => { setSupplier(item); setStep('po'); }}>
            <Text style={[styles.strong, { color: colors.foreground }]}>{item.name}</Text>
            <Text style={[styles.meta, { color: colors.mutedForeground }]}>{item.supplierCode}{item.trn || item.taxNumber ? ` · TRN ${item.trn ?? item.taxNumber}` : ''}</Text>
          </SilaCard>
        ))}
      </SilaScreen>
    );
  }

  if (step === 'po') {
    return (
      <SilaScreen title="Select open PO" eyebrow="RECEIVE AGAINST PO" onBack={() => setStep('supplier')}>
        {guard.dialog}
        <Text style={[styles.meta, { color: colors.mutedForeground, marginBottom: 12 }]}>{supplier?.name} · {supplier?.supplierCode}</Text>
        {poLoading ? <SilaLoadingState /> : null}
        {!poLoading && pos.length === 0 ? <SilaEmptyState title="No open purchase orders were found for this supplier." /> : null}
        {pos.map((item) => (
          <SilaCard key={item.id} onPress={() => { setPo(item); setStep('items'); }}>
            <Text style={[styles.strong, { color: colors.foreground }]}>{item.poNumber}</Text>
            <Text style={[styles.meta, { color: colors.mutedForeground }]}>{formatPo(item)}</Text>
          </SilaCard>
        ))}
      </SilaScreen>
    );
  }

  if (step === 'items' && po) {
    return (
      <SilaScreen
        title="Receive quantities"
        eyebrow={po.poNumber}
        onBack={() => setStep('po')}
        stickyFooter={<SilaBottomAction label="Review" onPress={() => setStep('review')} disabled={eligible.length === 0} />}
      >
        {guard.dialog}
        {eligible.map((item) => (
          <SilaCard key={item.id}>
            <Text style={[styles.strong, { color: colors.foreground }]}>{item.itemNumber ?? item.lineNumber} · {item.materialCode}</Text>
            <Text style={[styles.meta, { color: colors.mutedForeground }]}>{item.description}</Text>
            <Text style={[styles.meta, { color: colors.mutedForeground }]}>Ordered {item.orderedQuantity} · Previously received {item.receivedQuantity} · Open {item.openQuantity} {item.uom}</Text>
            <View style={styles.qtyRow}>
              <SilaQuantityInput label="PHYSICAL RECEIVED" value={received[item.id] ?? ''} onChangeText={(value) => { setReceived((current) => ({ ...current, [item.id]: value })); guard.setDirty(true); }} />
            </View>
            <View style={styles.qtyRow}>
              <SilaQuantityInput label="ACCEPTED" value={accepted[item.id] ?? ''} onChangeText={(value) => { setAccepted((current) => ({ ...current, [item.id]: value })); guard.setDirty(true); }} />
              <SilaQuantityInput label="DAMAGED" value={damaged[item.id] ?? ''} onChangeText={(value) => { setDamaged((current) => ({ ...current, [item.id]: value })); guard.setDirty(true); }} />
            </View>
            <View style={styles.qtyRow}>
              <SilaQuantityInput label="REJECTED" value={rejected[item.id] ?? ''} onChangeText={(value) => { setRejected((current) => ({ ...current, [item.id]: value })); guard.setDirty(true); }} />
            </View>
            <SilaSearchBar value={batch[item.id] ?? ''} onChangeText={(value) => { setBatch((current) => ({ ...current, [item.id]: value })); guard.setDirty(true); }} placeholder="Batch (optional)" />
            <SilaSearchBar value={expiry[item.id] ?? ''} onChangeText={(value) => { setExpiry((current) => ({ ...current, [item.id]: value })); guard.setDirty(true); }} placeholder="Expiry date (optional)" />
          </SilaCard>
        ))}
        <SilaSearchBar value={deliveryNote} onChangeText={(value) => { setDeliveryNote(value); guard.setDirty(true); }} placeholder="Delivery note (optional)" />
        {eligible.length === 0 ? <SilaEmptyState title="No GR-eligible open items on this PO." /> : null}
      </SilaScreen>
    );
  }

  return (
    <SilaScreen
      title="Review GRN"
      eyebrow={po?.poNumber}
      onBack={() => setStep('items')}
      stickyFooter={<SilaBottomAction label="Submit receiving" disabled onPress={() => undefined} />}
    >
      {guard.dialog}
      <SilaCard>
        <Text style={[styles.strong, { color: colors.foreground }]}>{supplier?.name}</Text>
        <Text style={[styles.meta, { color: colors.mutedForeground }]}>PO {po?.poNumber}</Text>
        <Text style={[styles.meta, { color: colors.mutedForeground }]}>Lines {eligible.length}</Text>
        <Text style={[styles.meta, { color: colors.mutedForeground }]}>Delivery note {deliveryNote || '—'}</Text>
      </SilaCard>
      {eligible.map((item) => (
        <SilaCard key={item.id}>
          <Text style={[styles.strong, { color: colors.foreground }]}>{item.materialCode}</Text>
          <Text style={[styles.meta, { color: colors.mutedForeground }]}>
            Received {received[item.id] || '—'} · Accepted {accepted[item.id] || '—'} · Damaged {damaged[item.id] || '—'} · Rejected {rejected[item.id] || '—'}
          </Text>
          <Text style={[styles.meta, { color: colors.mutedForeground }]}>Batch {batch[item.id] || '—'} · Expiry {expiry[item.id] || '—'}</Text>
        </SilaCard>
      ))}
      <View style={{ marginTop: 8 }}>
        <SilaStatusChip label="NOT CONNECTED" tone="pending" />
        <Text style={[styles.meta, { color: colors.mutedForeground, marginTop: 10 }]}>
          Direct PO receiving without an invoice uses the existing GRN APIs through the invoice flow today. Invoice-linked GRN posting remains connected from Scan Invoice.
        </Text>
      </View>
    </SilaScreen>
  );
}

const styles = StyleSheet.create({
  strong: { fontFamily: 'Inter_700Bold', fontSize: 15 },
  meta: { marginTop: 4, fontFamily: 'Inter_400Regular', fontSize: 12, lineHeight: 18 },
  qtyRow: { flexDirection: 'row', gap: 10, marginTop: 12 },
});
