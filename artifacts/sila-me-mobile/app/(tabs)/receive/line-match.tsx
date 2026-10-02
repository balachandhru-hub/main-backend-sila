import { useLocalSearchParams, useRouter } from 'expo-router';
import React from 'react';
import { Text } from 'react-native';
import { ReceiveShell, Panel, PrimaryButton } from '@/components/receive-ui';
import { useColors } from '@/hooks/useColors';
import { useQuery } from '@tanstack/react-query';
import { getInvoice } from '@workspace/api-client-react';
export default function LineMatch() {
  const colors = useColors(); const router = useRouter(); const { invoiceId } = useLocalSearchParams<{ invoiceId: string }>();
  const invoice = useQuery({ queryKey: ['receive-invoice-lines', invoiceId], queryFn: () => getInvoice(invoiceId ?? ''), enabled: Boolean(invoiceId) });
  return <ReceiveShell title="Review invoice lines" fallback="/receive/po-match">
    <Panel>{invoice.data?.lines.map(line => <Text key={line.id} style={{ color: colors.foreground, fontFamily: 'Inter_600SemiBold', marginBottom: 12 }}>{line.description} · {line.quantity ?? '—'} {line.uom ?? ''}</Text>) ?? <Text style={{ color: colors.mutedForeground }}>Loading invoice lines…</Text>}</Panel>
    <PrimaryButton label="Compare PO and invoice" onPress={() => router.push({ pathname: '/receive/grn-comparison', params: { invoiceId } })} disabled={!invoice.data || !invoice.data.purchaseOrderId} />
  </ReceiveShell>;
}