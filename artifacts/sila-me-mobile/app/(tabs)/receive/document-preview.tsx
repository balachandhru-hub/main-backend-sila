import { Feather } from '@expo/vector-icons';
import { useLocalSearchParams, useRouter } from 'expo-router';
import React, { useEffect, useRef, useState } from 'react';
import { ActivityIndicator, Alert, Image, Platform, Pressable, StyleSheet, Text, View } from 'react-native';
import { ReceiveShell, Panel, PrimaryButton } from '@/components/receive-ui';
import { useColors } from '@/hooks/useColors';
import { cropPage, rotatePage, buildScanPdf } from '@/services/mobile-scanner';
import { recognizeInvoicePages } from '@/services/mobile-ocr';
import { useScanSession } from '@/providers/ScanSessionProvider';
import { useAuth } from '@/providers/AuthProvider';
import { getGetAccessContextQueryKey, useGetAccessContext } from '@workspace/api-client-react';

export default function DocumentPreview() {
  const colors = useColors(); const router = useRouter(); const { autoCreate } = useLocalSearchParams<{ autoCreate?: string }>(); const { user } = useAuth(); const { session, removePage, movePage, replacePage, setPdf, setOcrStatus, setOcrResult } = useScanSession();
  const context = useGetAccessContext({ query: { queryKey: getGetAccessContextQueryKey(), enabled: Boolean(user), retry: false } });
  const [working, setWorking] = useState(false);
  const [progressText, setProgressText] = useState('');
  const [pendingDeleteId, setPendingDeleteId] = useState<string | null>(null);
  const autoStarted = useRef(false);
  const manage = async (action: 'rotate' | 'crop', id: string) => {
    const page = session.pages.find(item => item.id === id); if (!page) return;
    setWorking(true); try { replacePage(id, action === 'rotate' ? await rotatePage(page) : await cropPage(page)); } finally { setWorking(false); }
  };
  const handleCreatePdf = async () => {
    if (!session.pages.length) return;
    setWorking(true);
    setProgressText('Creating PDF…');
    try {
      if (session.pages.some(page => !page.processedImageUri)) throw new Error('SCAN_PAGE_SOURCE_MISSING');
      const pdf = await buildScanPdf(session.pages, session.id);
      setPdf(pdf.uri, pdf.size);
      try {
        setOcrStatus('reading', 0);
        setProgressText('Reading invoice…');
        const access = context.data ?? (await context.refetch()).data;
        const result = await recognizeInvoicePages(session.pages, progress => {
          setOcrStatus('reading', progress.page);
          setProgressText(`Reading page ${progress.page} of ${progress.total}`);
        }, {
          pdfUri: pdf.uri,
          organizationId: access?.organizations[0]?.id,
          operatingUnitId: access?.units[0]?.id ?? null,
           onFallbackStart: requestId => {
             setOcrStatus('extracting', session.pages.length);
             setProgressText('Improving invoice extraction…');
             if (__DEV__) console.log('[OCR_TRACE]', { requestId, event: 'FALLBACK_LOADING_STATE' });
           },
        });
        if (__DEV__) console.log('[OCR_TRACE]', { requestId: result.diagnostics?.requestId, event: 'FORM_UPDATE' });
        setOcrResult(result);
      } catch (error) {
        const message = error instanceof Error ? error.message : String(error);
        setOcrStatus('failed', undefined, message);
        console.warn('[OCR] Extraction failed after PDF creation', message);
      }
      router.push('/receive/invoice-review');
    } catch (error) {
      const message = error instanceof Error ? error.message : String(error);
      console.error('[Scanner] PDF creation failed', {
        message,
        stack: error instanceof Error ? error.stack : undefined,
        platform: Platform.OS,
        pageCount: session.pages.length,
        pageUris: session.pages.map(page => ({ id: page.id, processedImageUri: page.processedImageUri })),
      });
      Alert.alert(
        'PDF could not be created',
        message.startsWith('Unable to read scanned Page')
          ? `${message}. Retake or remove this page.`
          : 'Your scanned pages are safe. Please try again.',
        [
          { text: 'BACK TO PAGES', style: 'cancel' },
          { text: 'TRY AGAIN', onPress: () => { void handleCreatePdf(); } },
        ],
      );
    }
    finally { setWorking(false); setProgressText(''); }
  };
  useEffect(() => {
    if (autoCreate === '1' && !autoStarted.current && session.pages.length && !session.pdfUri) {
      autoStarted.current = true;
      void handleCreatePdf();
    }
  }, [autoCreate, session.pages.length, session.pdfUri]);
  const done = () => {
    if (session.pdfUri && !session.pdfDirty) router.push('/receive/invoice-review');
    else void handleCreatePdf();
  };
  const handleDeletePage = (pageId: string) => {
    if (Platform.OS === 'web') {
      if (window.confirm('Delete this page? This page will be removed from the scan.')) removePage(pageId);
      return;
    }
    setPendingDeleteId(pageId);
  };
  const leave = () => session.pages.length ? router.replace('/receive/scan') : router.replace('/receive');
  return (
    <ReceiveShell title="Review pages" fallback="/receive/scan" onBack={leave}>
      <Text style={[styles.copy, { color: colors.mutedForeground }]}>Reorder, rotate, crop, or remove pages before creating one PDF. Maximum 20 pages.</Text>
      <Panel>
        {session.pages.length === 0 ? <Text style={{ color: colors.mutedForeground }}>No pages captured yet.</Text> : session.pages.map((page, index) => (
          <View key={page.id} style={[styles.page, { borderColor: colors.border }]}>
            <Image source={{ uri: page.thumbnailUri ?? page.processedImageUri }} style={styles.thumb} />
            <View style={styles.pageInfo}><Text style={[styles.pageTitle, { color: colors.foreground }]}>Page {index + 1}</Text><Text style={[styles.meta, { color: colors.mutedForeground }]}>{page.width ?? '—'} × {page.height ?? '—'}</Text>
              <View style={styles.actions}>
                <Pressable onPress={() => movePage(page.id, -1)}><Feather name="chevron-left" size={18} color={colors.primary} /></Pressable>
                <Pressable onPress={() => movePage(page.id, 1)}><Feather name="chevron-right" size={18} color={colors.primary} /></Pressable>
                <Pressable onPress={() => router.push({ pathname: '/receive/scan', params: { replacePageId: page.id } })}><Feather name="camera" size={17} color={colors.primary} /></Pressable>
                <Pressable onPress={() => manage('rotate', page.id)}><Feather name="rotate-cw" size={17} color={colors.primary} /></Pressable>
                <Pressable onPress={() => manage('crop', page.id)}><Feather name="crop" size={17} color={colors.primary} /></Pressable>
                <Pressable testID={`delete-page-${page.id}`} accessibilityLabel={`Delete page ${index + 1}`} onPress={() => handleDeletePage(page.id)} disabled={working}><Feather name="trash-2" size={17} color={colors.error} /></Pressable>
              </View>
              {pendingDeleteId === page.id && <View style={[styles.deleteConfirm, { borderColor: colors.border }]}>
                <Text style={[styles.deleteText, { color: colors.foreground }]}>Remove this page?</Text>
                <View style={styles.deleteActions}>
                  <Pressable onPress={() => setPendingDeleteId(null)}><Text style={{ color: colors.mutedForeground, fontFamily: 'Inter_600SemiBold' }}>CANCEL</Text></Pressable>
                  <Pressable onPress={() => { removePage(page.id); setPendingDeleteId(null); }}><Text style={{ color: colors.error, fontFamily: 'Inter_700Bold' }}>DELETE</Text></Pressable>
                </View>
              </View>}
            </View>
          </View>
        ))}
        <Pressable onPress={() => router.push('/receive/scan')} style={[styles.add, { borderColor: colors.border }]}><Feather name="plus" size={18} color={colors.primary} /><Text style={{ color: colors.primary, fontFamily: 'Inter_600SemiBold' }}>Add page</Text></Pressable>
      </Panel>
        <PrimaryButton testID="create-pdf" label={working ? progressText || 'Creating PDF…' : session.pdfUri && !session.pdfDirty ? `PDF ready · ${session.pages.length} page${session.pages.length === 1 ? '' : 's'}` : `Create PDF · ${session.pages.length} page${session.pages.length === 1 ? '' : 's'}`} onPress={done} disabled={!session.pages.length || working} />
       {working && <><ActivityIndicator color={colors.primary} style={{ marginTop: 16 }} /><Text style={[styles.progress, { color: colors.mutedForeground }]}>{progressText}</Text></>}
    </ReceiveShell>
  );
}
const styles = StyleSheet.create({
  copy: { marginTop: 14, fontFamily: 'Inter_400Regular', fontSize: 14, lineHeight: 21 }, page: { flexDirection: 'row', borderBottomWidth: 1, paddingVertical: 12, gap: 12 },
  thumb: { width: 68, height: 88, borderRadius: 6, backgroundColor: '#edf3fb' }, pageInfo: { flex: 1, justifyContent: 'center', gap: 5 }, pageTitle: { fontFamily: 'Inter_600SemiBold', fontSize: 14 }, meta: { fontFamily: 'Inter_400Regular', fontSize: 11 }, actions: { flexDirection: 'row', alignItems: 'center', gap: 12 },
  add: { minHeight: 45, borderWidth: 1, borderRadius: 8, borderStyle: 'dashed', marginTop: 14, flexDirection: 'row', alignItems: 'center', justifyContent: 'center', gap: 8 }, deleteConfirm: { marginTop: 8, paddingTop: 8, borderTopWidth: 1, flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' }, deleteText: { fontFamily: 'Inter_600SemiBold', fontSize: 12 }, deleteActions: { flexDirection: 'row', gap: 16 }, progress: { marginTop: 8, textAlign: 'center', fontFamily: 'Inter_400Regular', fontSize: 13 },
});