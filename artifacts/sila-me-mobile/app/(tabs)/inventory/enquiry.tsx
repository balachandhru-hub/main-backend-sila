import { useLocalSearchParams, useRouter } from 'expo-router';
import React, { useEffect, useState } from 'react';
import { StyleSheet, Text, TextInput } from 'react-native';
import { customFetch } from '@workspace/api-client-react';
import { SilaBottomAction, SilaCard, SilaScreen } from '@/components/sila/ui';
import { useColors } from '@/hooks/useColors';
import { useStoreScope } from '@/providers/StoreScopeProvider';

const categories = [
  ['BREAKAGE', 'Breakage'], ['SPILLAGE', 'Spillage'], ['UNRECORDED_CONSUMPTION', 'Unrecorded Consumption'],
  ['UNRECORDED_TRANSFER', 'Unrecorded Transfer'], ['COMPLIMENTARY_GUEST_RECOVERY', 'Complimentary / Guest Recovery'],
  ['INCORRECT_PREVIOUS_COUNT', 'Incorrect Previous Count'], ['POS_RECIPE_MAPPING_ISSUE', 'POS / Recipe Mapping Issue'],
  ['UOM_PACK_CONVERSION_ISSUE', 'UOM / Pack Conversion Issue'], ['EXPIRED_OR_SPOILED', 'Expired / Spoiled'],
  ['THEFT_SUSPECTED_LOSS', 'Suspected Loss'], ['OTHER', 'Other'],
] as const;

type Enquiry = {
  id: string; enquiryNumber: string; status: string; managerConfigured: boolean; managerGroup?: string | null;
  materialCode?: string | null; description?: string | null; location?: string | null;
  systemQty?: number | null; physicalQty?: number | null; shortageQty?: number | null; uom?: string | null;
  shortageValue?: number | null; currency?: string | null;
};

function api<T>(url: string, init?: RequestInit) {
  return customFetch<T>(url, { credentials: 'include', ...init, responseType: 'json' });
}

export default function ShortageEnquiryScreen() {
  const colors = useColors();
  const router = useRouter();
  const { id } = useLocalSearchParams<{ id: string }>();
  const { organization } = useStoreScope();
  const [item, setItem] = useState<Enquiry | null>(null);
  const [category, setCategory] = useState<string>('BREAKAGE');
  const [comments, setComments] = useState('');
  const [message, setMessage] = useState<string | null>(null);

  useEffect(() => {
    if (!organization?.id || !id) return;
    void api<Enquiry>(`/api/v1/inventory/stock-counts/enquiries/${id}?organizationId=${organization.id}`).then(setItem).catch(() => setMessage('Enquiry is unavailable.'));
  }, [id, organization?.id]);

  async function submit() {
    if (!organization?.id || !id) return;
    await api(`/api/v1/inventory/stock-counts/enquiries/${id}/respond?organizationId=${organization.id}`, {
      method: 'POST', headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ category, comments, attachmentName: null }),
    });
    setMessage('Justification submitted. Inventory was not changed.');
    router.replace('/tasks' as never);
  }

  return (
    <SilaScreen title="Justification" eyebrow="SHORTAGE ENQUIRY" fallback="/tasks" stickyFooter={<SilaBottomAction label="Submit justification" disabled={!comments.trim()} onPress={() => void submit().catch(() => setMessage('Justification was not submitted.'))} />}>
      {!item?.managerConfigured ? <Text style={[styles.warn, { color: colors.warning }]}>MANAGER NOT CONFIGURED</Text> : null}
      <Text style={[styles.title, { color: colors.foreground }]}>{item?.location}</Text>
      <Text style={[styles.title, { color: colors.foreground }]}>{item?.description}</Text>
      <Text style={[styles.meta, { color: colors.mutedForeground }]}>Material {item?.materialCode} · {item?.enquiryNumber}</Text>
      <SilaCard>
        <Text style={[styles.meta, { color: colors.foreground }]}>System {item?.systemQty ?? '—'} {item?.uom}</Text>
        <Text style={[styles.meta, { color: colors.foreground }]}>Physical {item?.physicalQty ?? '—'} {item?.uom}</Text>
        <Text style={[styles.title, { color: colors.foreground }]}>Shortage {item?.shortageQty ?? '—'} {item?.uom}</Text>
        <Text style={[styles.meta, { color: colors.foreground }]}>{item?.shortageValue ?? '—'} {item?.currency}</Text>
      </SilaCard>
      {categories.map(([key, label]) => (
        <SilaBottomAction key={key} label={label} secondary={category !== key} onPress={() => setCategory(key)} />
      ))}
      <TextInput value={comments} onChangeText={setComments} placeholder="What happened?" multiline style={[styles.input, { color: colors.foreground, borderColor: colors.border }]} />
      {message ? <Text style={[styles.meta, { color: colors.warning }]}>{message}</Text> : null}
    </SilaScreen>
  );
}

const styles = StyleSheet.create({
  title: { fontFamily: 'Inter_700Bold', fontSize: 16, marginTop: 4 },
  meta: { marginTop: 4, fontFamily: 'Inter_400Regular', fontSize: 13 },
  warn: { fontFamily: 'Inter_700Bold', fontSize: 13, marginBottom: 8 },
  input: { borderWidth: 1, borderRadius: 8, minHeight: 88, padding: 12, marginVertical: 8, fontFamily: 'Inter_400Regular' },
});
