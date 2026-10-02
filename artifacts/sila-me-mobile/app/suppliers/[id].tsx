import { useLocalSearchParams } from 'expo-router';
import React, { useEffect, useState } from 'react';
import { StyleSheet, Text } from 'react-native';
import { getSupplierMaster, type Supplier } from '@workspace/api-client-react';
import { SilaCard, SilaErrorState, SilaLoadingState, SilaScreen, SilaStatusChip } from '@/components/sila/ui';
import { useColors } from '@/hooks/useColors';
import { useStoreScope } from '@/providers/StoreScopeProvider';

export default function SupplierDetailScreen() {
  const colors = useColors();
  const { id } = useLocalSearchParams<{ id: string }>();
  const { organization } = useStoreScope();
  const [supplier, setSupplier] = useState<Supplier | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!id || !organization?.id) return;
    setLoading(true);
    void getSupplierMaster(id, { organizationId: organization.id })
      .then((data) => { setSupplier(data); setError(null); })
      .catch(() => setError('Supplier details could not be loaded.'))
      .finally(() => setLoading(false));
  }, [id, organization?.id]);

  if (loading) return <SilaScreen title="Supplier" fallback="/suppliers"><SilaLoadingState /></SilaScreen>;
  if (error || !supplier) return <SilaScreen title="Supplier" fallback="/suppliers"><SilaErrorState message={error ?? 'Supplier not found.'} /></SilaScreen>;

  return (
    <SilaScreen title={supplier.name} eyebrow="SUPPLIER" fallback="/suppliers">
      <SilaCard>
        <Text style={[styles.meta, { color: colors.mutedForeground }]}>Supplier ID {supplier.supplierCode}</Text>
        <Text style={[styles.meta, { color: colors.mutedForeground }]}>TRN {supplier.trn ?? supplier.taxNumber ?? '—'}</Text>
        <Text style={[styles.meta, { color: colors.mutedForeground }]}>Address {[supplier.street, supplier.city, supplier.country].filter(Boolean).join(', ') || '—'}</Text>
        <Text style={[styles.meta, { color: colors.mutedForeground }]}>Phone {supplier.phone ?? '—'}</Text>
        <Text style={[styles.meta, { color: colors.mutedForeground }]}>Email {supplier.email ?? '—'}</Text>
        <Text style={[styles.meta, { color: colors.mutedForeground }]}>Currency {supplier.currency ?? '—'}</Text>
        <SilaStatusChip label={supplier.status} tone={supplier.status === 'ACTIVE' ? 'success' : 'pending'} />
      </SilaCard>
    </SilaScreen>
  );
}

const styles = StyleSheet.create({
  meta: { marginBottom: 8, fontFamily: 'Inter_400Regular', fontSize: 13, lineHeight: 19 },
});
