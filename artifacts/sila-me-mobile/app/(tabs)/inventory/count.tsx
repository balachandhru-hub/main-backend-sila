import { useRouter } from 'expo-router';
import React, { useCallback, useEffect, useState } from 'react';
import { StyleSheet, Text, TextInput, View } from 'react-native';
import { customFetch } from '@workspace/api-client-react';
import { SilaBottomAction, SilaCard, SilaQuantityInput, SilaScreen, SilaStatusChip } from '@/components/sila/ui';
import { useColors } from '@/hooks/useColors';
import { useStoreScope } from '@/providers/StoreScopeProvider';

type CountRow = { id: string; countNumber: string; countType: string; location: string; businessDate: string; materials: number; counted: number; remaining?: number; matched: number; shortage: number; surplus: number; status: string };
type Line = { id: string; materialId: string; materialCode: string; description: string; systemQty?: number | null; systemUom: string; physicalQty?: number | null; varianceQty?: number | null; status: string; bookVisible: boolean; baseUom?: string | null };
type Detail = CountRow & { blindCount: boolean; remaining: number; lines: Line[]; currency?: string | null; shortageValue?: number | null };
type Hit = { id: string; lineId?: string | null; materialCode: string; description: string; baseUom: string; alternateUom?: string | null };

function api<T>(url: string, init?: RequestInit) {
  return customFetch<T>(url, { credentials: 'include', ...init, responseType: 'json' });
}

export default function StockCountScreen() {
  const colors = useColors();
  const router = useRouter();
  const { organization } = useStoreScope();
  const organizationId = organization?.id;
  const [rows, setRows] = useState<CountRow[]>([]);
  const [detail, setDetail] = useState<Detail | null>(null);
  const [mode, setMode] = useState<'home' | 'scan' | 'search' | 'list' | 'count'>('home');
  const [barcode, setBarcode] = useState('');
  const [query, setQuery] = useState('');
  const [hits, setHits] = useState<Hit[]>([]);
  const [line, setLine] = useState<Line | null>(null);
  const [fullQty, setFullQty] = useState('');
  const [openQty, setOpenQty] = useState('');
  const [openUom, setOpenUom] = useState('ML');
  const [message, setMessage] = useState<string | null>(null);
  const [method, setMethod] = useState('MANUAL');

  const load = useCallback(async () => {
    if (!organizationId) return;
    const data = await api<CountRow[]>(`/api/v1/inventory/stock-counts?organizationId=${organizationId}`);
    setRows(data.filter((item) => item.status !== 'CANCELLED' && item.status !== 'COMPLETED'));
  }, [organizationId]);
  useEffect(() => { void load().catch(() => setMessage('Stock counts are unavailable.')); }, [load]);

  async function openSession(id: string) {
    const data = await api<Detail>(`/api/v1/inventory/stock-counts/${id}?organizationId=${organizationId}&take=0`);
    setDetail(data);
    setHits([]);
    setMode('scan');
    setMessage(null);
  }

  async function choose(hit: Hit) {
    if (!detail || !hit.lineId) { setMessage('That material is not on this count.'); return; }
    const fresh = await api<Detail>(`/api/v1/inventory/stock-counts/${detail.id}?organizationId=${organizationId}&line=${encodeURIComponent(hit.materialCode)}&take=1`);
    const match = fresh.lines.find((item) => item.id === hit.lineId) ?? fresh.lines[0];
    if (!match) { setMessage('That material is not on this count.'); return; }
    setLine(match);
    setOpenUom(hit.alternateUom || 'ML');
    setFullQty('');
    setOpenQty('');
    setMode('count');
  }

  async function scan() {
    if (!detail || !barcode.trim()) return;
    const result = await api<{ code: string; message?: string | null; candidates: Hit[] }>(`/api/v1/inventory/stock-counts/${detail.id}/identify-barcode?organizationId=${organizationId}`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ barcode }) });
    setMethod('BARCODE');
    if (result.code !== 'IDENTIFIED') { setMessage(result.message || 'BARCODE NOT MAPPED'); setMode('search'); return; }
    setMessage(null);
    await choose(result.candidates[0]);
  }

  async function photo() {
    if (!detail) return;
    const result = await api<{ message?: string | null }>(`/api/v1/inventory/stock-counts/${detail.id}/identify-photo?organizationId=${organizationId}`, { method: 'POST' });
    setMethod('PHOTO');
    setMessage(result.message || 'Confirm the material.');
    setMode('search');
  }

  async function search() {
    if (!detail || query.trim().length < 1) return;
    const located = await api<Hit[]>(`/api/v1/inventory/stock-counts/materials?organizationId=${organizationId}&sessionId=${detail.id}&query=${encodeURIComponent(query)}`);
    setHits(located);
    setMethod('SEARCH');
    if (located.length === 0) setMessage('No material matched.');
  }

  async function save() {
    if (!detail || !line) return;
    const saved = await api<Line>(`/api/v1/inventory/stock-counts/${detail.id}/lines/${line.id}?organizationId=${organizationId}`, {
      method: 'POST', headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ method, fullQty: fullQty === '' ? null : Number(fullQty), fullUom: line.systemUom, openQty: openQty === '' ? null : Number(openQty), openUom }),
    });
    setMessage(saved.bookVisible && saved.varianceQty != null ? `Saved. Variance ${saved.varianceQty} ${saved.systemUom}. Inventory was not changed.` : 'Saved. Inventory was not changed.');
    setBarcode('');
    await openSession(detail.id);
    setMode('scan');
  }

  if (!detail || mode === 'home') {
    return (
      <SilaScreen title="Stock Count" eyebrow="INVENTORY" fallback="/inventory">
        {message ? <Text style={[styles.meta, { color: colors.warning }]}>{message}</Text> : null}
        {rows.length === 0 ? <SilaCard><Text style={[styles.title, { color: colors.foreground }]}>No open counts</Text></SilaCard> : rows.map((item) => (
          <SilaCard key={item.id} onPress={() => void openSession(item.id).catch(() => setMessage('Could not open the count.'))}>
            <Text style={[styles.title, { color: colors.foreground }]}>{item.countNumber}</Text>
            <Text style={[styles.meta, { color: colors.mutedForeground }]}>{item.location} · {item.businessDate.slice(0, 10)} · {item.materials} materials</Text>
            <Text style={[styles.meta, { color: colors.foreground }]}>Counted {item.counted} · Matched {item.matched} · Shortage {item.shortage} · Surplus {item.surplus}</Text>
          </SilaCard>
        ))}
      </SilaScreen>
    );
  }

  if (mode === 'count' && line) {
    return (
      <SilaScreen title="Physical count" eyebrow="STOCK COUNT" onBack={() => setMode('scan')} stickyFooter={<SilaBottomAction label="Save and scan next" onPress={() => void save().catch(() => setMessage('Count was not saved.'))} />}>
        <Text style={[styles.title, { color: colors.foreground }]}>{line.description}</Text>
        <Text style={[styles.meta, { color: colors.mutedForeground }]}>{line.materialCode} · {detail.location} · {line.systemUom}</Text>
        {line.systemQty == null ? <Text style={[styles.meta, { color: colors.mutedForeground }]}>Blind count. Enter what is physically there.</Text> : <Text style={[styles.meta, { color: colors.foreground }]}>System {line.systemQty} {line.systemUom}</Text>}
        <SilaQuantityInput label={`FULL ${line.systemUom}`} value={fullQty} onChangeText={setFullQty} />
        <SilaQuantityInput label="OPEN QUANTITY" value={openQty} onChangeText={setOpenQty} />
        <TextInput value={openUom} onChangeText={setOpenUom} placeholder="Open unit" style={[styles.input, { color: colors.foreground, borderColor: colors.border }]} />
        {message ? <Text style={[styles.meta, { color: colors.warning }]}>{message}</Text> : null}
      </SilaScreen>
    );
  }

  return (
    <SilaScreen title={detail.location} eyebrow={detail.countNumber} onBack={() => { setDetail(null); setMode('home'); }} stickyFooter={<SilaBottomAction label="Continue count" onPress={() => setMode('scan')} />}>
      <Text style={[styles.title, { color: colors.foreground }]}>{detail.materials} materials</Text>
      <View style={styles.row}>
        <SilaStatusChip label={`Counted ${detail.counted}`} tone="info" />
        <SilaStatusChip label={`Left ${detail.remaining}`} tone="pending" />
        <SilaStatusChip label={`Match ${detail.matched}`} tone="success" />
        <SilaStatusChip label={`Short ${detail.shortage}`} tone="failed" />
        <SilaStatusChip label={`Plus ${detail.surplus}`} tone="warning" />
      </View>
      {message ? <Text style={[styles.meta, { color: colors.warning }]}>{message}</Text> : null}
      {mode === 'scan' || mode === 'search' ? (
        <>
          <TextInput value={barcode} onChangeText={setBarcode} placeholder="Scan barcode" autoFocus onSubmitEditing={() => void scan()} style={[styles.input, { color: colors.foreground, borderColor: colors.border }]} />
          <SilaBottomAction label="Scan barcode" onPress={() => void scan().catch(() => setMessage('Barcode lookup failed.'))} />
          <SilaBottomAction label="Take photo" secondary onPress={() => void photo().catch(() => setMessage('Photo was not recorded.'))} />
          <TextInput value={query} onChangeText={setQuery} placeholder="Search material, id, or group" onSubmitEditing={() => void search()} style={[styles.input, { color: colors.foreground, borderColor: colors.border }]} />
          <SilaBottomAction label="Search material" secondary onPress={() => void search().catch(() => setMessage('Search failed.'))} />
          {hits.map((item) => (
            <SilaCard key={item.id} onPress={() => void choose(item)}>
              <Text style={[styles.title, { color: colors.foreground }]}>{item.description}</Text>
              <Text style={[styles.meta, { color: colors.mutedForeground }]}>{item.materialCode}</Text>
            </SilaCard>
          ))}
        </>
      ) : null}
      <SilaBottomAction label="Count list" secondary onPress={() => void api<Detail>(`/api/v1/inventory/stock-counts/${detail.id}?organizationId=${organizationId}&take=40`).then((data) => { setDetail(data); setMode('list'); })} />
      {mode === 'list' ? detail.lines.map((item) => (
        <SilaCard key={item.id} onPress={() => { setLine(item); setMethod('MANUAL'); setMode('count'); }}>
          <Text style={[styles.title, { color: colors.foreground }]}>{item.description}</Text>
          <Text style={[styles.meta, { color: colors.mutedForeground }]}>{item.materialCode} · {item.status}{item.physicalQty != null ? ` · ${item.physicalQty}` : ''}</Text>
        </SilaCard>
      )) : null}
      <SilaCard onPress={() => router.push('/tasks' as never)}><Text style={[styles.title, { color: colors.foreground }]}>Shortage tasks</Text></SilaCard>
    </SilaScreen>
  );
}

const styles = StyleSheet.create({
  title: { fontFamily: 'Inter_700Bold', fontSize: 16 },
  meta: { marginTop: 4, fontFamily: 'Inter_400Regular', fontSize: 13, lineHeight: 18 },
  row: { flexDirection: 'row', flexWrap: 'wrap', gap: 8, marginVertical: 12 },
  input: { borderWidth: 1, borderRadius: 8, minHeight: 44, paddingHorizontal: 12, marginVertical: 8, fontFamily: 'Inter_400Regular' },
});
