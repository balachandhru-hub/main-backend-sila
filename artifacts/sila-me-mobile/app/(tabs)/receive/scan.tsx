/**
 * PROTECTED SILA INVOICE RECEIVING FLOW — See docs/PROTECTED_INVOICE_FLOW.md.
 * Scan/Upload entry. Do not modify this contract/behavior from unrelated feature work.
 */
import { Feather } from '@expo/vector-icons';
import { CameraView, useCameraPermissions } from 'expo-camera';
import Constants, { ExecutionEnvironment } from 'expo-constants';
import * as ImagePicker from 'expo-image-picker';
import { useLocalSearchParams, useRouter } from 'expo-router';
import React, { useEffect, useRef, useState } from 'react';
import { ActivityIndicator, Alert, Image, Platform, Pressable, StyleSheet, Text, View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { ReceiveShell } from '@/components/receive-ui';
import { useColors } from '@/hooks/useColors';
import { cropPage, prepareScanPage, rotatePage } from '@/services/mobile-scanner';
import { scanNativeDocuments } from '@/services/document-scanner';
import { type ScanPage, useScanSession } from '@/providers/ScanSessionProvider';

const MAX_SCAN_PAGES = 20;
const isExpoGo = Platform.OS !== 'web' && Constants.executionEnvironment === ExecutionEnvironment.StoreClient;

export default function ReceiveScan() {
  const colors = useColors();
  const insets = useSafeAreaInsets();
  const router = useRouter();
  const { replacePageId } = useLocalSearchParams<{ replacePageId?: string }>();
  const { addPage, replacePage, session } = useScanSession();
  const camera = useRef<CameraView>(null);
  const [permission, requestPermission] = useCameraPermissions();
  const [flash, setFlash] = useState<'off' | 'on'>('off');
  const [ready, setReady] = useState(false);
  const [working, setWorking] = useState(false);
  const [nativeAttempted, setNativeAttempted] = useState(Platform.OS === 'web' || isExpoGo);
  const [nativeError, setNativeError] = useState('');
  const [fallbackEnabled, setFallbackEnabled] = useState(Platform.OS === 'web' || isExpoGo);
  const [pendingPage, setPendingPage] = useState<ScanPage | null>(null);

  const commitImages = async (images: string[], autoCreate: boolean) => {
    setWorking(true);
    try {
      const available = replacePageId ? 1 : MAX_SCAN_PAGES - session.pages.length;
      for (const [index, uri] of images.slice(0, available).entries()) {
        const page = await prepareScanPage(uri, replacePageId && index === 0 ? replacePageId : undefined);
        if (replacePageId && index === 0) replacePage(replacePageId, page);
        else addPage(page);
      }
      if (images.length) {
        router.replace({
          pathname: '/receive/document-preview',
          params: autoCreate ? { autoCreate: '1' } : {},
        });
      }
    } catch {
      Alert.alert('Could not prepare scan', 'The captured image could not be processed. Try again.');
    } finally {
      setWorking(false);
    }
  };

  const startNativeScan = async () => {
    setWorking(true);
    setNativeError('');
    try {
      const result = await scanNativeDocuments(replacePageId ? 1 : MAX_SCAN_PAGES - session.pages.length);
      setNativeAttempted(true);
      if (result?.status === 'success' && result.scannedImages.length) {
        await commitImages(result.scannedImages, true);
      } else {
        router.replace(session.pages.length ? '/receive/document-preview' : '/receive');
      }
    } catch {
      setNativeError('This installed build does not contain the native document scanner.');
      setNativeAttempted(true);
    } finally {
      setWorking(false);
    }
  };

  useEffect(() => {
    if (Platform.OS !== 'web' && !isExpoGo) void startNativeScan();
  }, []);

  const gallery = async () => {
    const result = await ImagePicker.launchImageLibraryAsync({
      mediaTypes: ['images'],
      allowsMultipleSelection: true,
      quality: 1,
    });
    if (!result.canceled) await commitImages(result.assets.map(asset => asset.uri), false);
  };

  const capture = async () => {
    if (!ready || !camera.current || session.pages.length >= MAX_SCAN_PAGES) return;
    setWorking(true);
    try {
      const photo = await camera.current.takePictureAsync({ quality: 1, skipProcessing: false });
      if (photo?.uri) setPendingPage(await prepareScanPage(photo.uri, replacePageId));
    } catch {
      Alert.alert('Capture failed', 'The invoice page could not be captured. Try again.');
    } finally {
      setWorking(false);
    }
  };

  const editPending = async (action: 'crop' | 'rotate') => {
    if (!pendingPage) return;
    setWorking(true);
    try {
      setPendingPage(action === 'crop' ? await cropPage(pendingPage) : await rotatePage(pendingPage));
    } finally {
      setWorking(false);
    }
  };

  const keepPending = () => {
    if (!pendingPage) return;
    if (replacePageId) replacePage(replacePageId, pendingPage);
    else addPage(pendingPage);
    setPendingPage(null);
    router.replace('/receive/document-preview');
  };

  const leaveScanner = () => {
    const destination = session.pages.length ? '/receive/document-preview' : '/receive';
    if (!session.pages.length) {
      router.replace(destination);
      return;
    }
    Alert.alert(
      'Leave scanner?',
      'Your captured pages will stay saved in this scan session.',
      [
        { text: 'Stay here', style: 'cancel' },
        { text: 'Leave scanner', onPress: () => router.replace(destination) },
      ],
    );
  };

  if (Platform.OS !== 'web' && !nativeAttempted) {
    return (
      <ReceiveShell title="Document scanner" fallback="/receive">
        <ActivityIndicator color={colors.primary} />
        <Text style={[styles.copy, { color: colors.mutedForeground }]}>Opening the iPhone-style document scanner…</Text>
      </ReceiveShell>
    );
  }

  if (Platform.OS !== 'web' && nativeError && !fallbackEnabled) {
    return (
      <ReceiveShell title="Native scanner required" fallback="/receive">
        <Text style={[styles.heading, { color: colors.foreground }]}>Use the SILA development build</Text>
        <Text style={[styles.copy, { color: colors.mutedForeground }]}>{nativeError} The iPhone-style scanner needs a development build; Expo Go cannot load it.</Text>
        <Pressable onPress={startNativeScan} style={[styles.permission, { backgroundColor: colors.primary }]}><Text style={{ color: colors.primaryForeground, fontFamily: 'Inter_600SemiBold' }}>Try native scanner again</Text></Pressable>
        <Pressable onPress={() => setFallbackEnabled(true)} style={[styles.permission, { borderColor: colors.border, borderWidth: 1 }]}><Text style={{ color: colors.foreground, fontFamily: 'Inter_600SemiBold' }}>Use basic camera instead</Text></Pressable>
        <Pressable onPress={gallery} style={[styles.permission, { borderColor: colors.border, borderWidth: 1 }]}><Text style={{ color: colors.foreground, fontFamily: 'Inter_600SemiBold' }}>Import from gallery</Text></Pressable>
      </ReceiveShell>
    );
  }

  if (fallbackEnabled && !permission?.granted) {
    return (
      <ReceiveShell title="Camera access" fallback="/receive">
        {isExpoGo && <Text style={[styles.copy, { color: colors.mutedForeground }]}>Expo Go does not include the native document scanner. Use the basic camera or import invoice pages from your gallery.</Text>}
        <Text style={[styles.copy, { color: colors.mutedForeground }]}>Allow camera access for the basic scanner, or import invoice pages from your gallery.</Text>
        <Pressable onPress={requestPermission} style={[styles.permission, { backgroundColor: colors.primary }]}><Text style={{ color: colors.primaryForeground, fontFamily: 'Inter_600SemiBold' }}>Allow camera</Text></Pressable>
        <Pressable onPress={gallery} style={[styles.permission, { borderColor: colors.border, borderWidth: 1 }]}><Text style={{ color: colors.foreground, fontFamily: 'Inter_600SemiBold' }}>Import from gallery</Text></Pressable>
      </ReceiveShell>
    );
  }

  if (pendingPage) {
    return (
      <View style={styles.captureReview}>
        <View style={[styles.reviewHeader, { paddingTop: insets.top + 10 }]}>
          <Text style={styles.reviewTitle}>Page {replacePageId ? '' : session.pages.length + 1} preview</Text>
          <Text style={styles.reviewMeta}>{pendingPage.width} × {pendingPage.height}</Text>
        </View>
        <Image source={{ uri: pendingPage.processedImageUri }} resizeMode="contain" style={styles.fullPreview} />
        <View style={[styles.reviewControls, { paddingBottom: insets.bottom + 14 }]}>
          <View style={styles.editRow}>
            <Pressable onPress={() => setPendingPage(null)} style={styles.reviewAction}><Feather name="refresh-cw" size={21} color="#fff" /><Text style={styles.actionText}>Retake</Text></Pressable>
            <Pressable onPress={() => editPending('crop')} disabled={working} style={styles.reviewAction}><Feather name="crop" size={21} color="#fff" /><Text style={styles.actionText}>Crop</Text></Pressable>
            <Pressable onPress={() => editPending('rotate')} disabled={working} style={styles.reviewAction}><Feather name="rotate-cw" size={21} color="#fff" /><Text style={styles.actionText}>Rotate</Text></Pressable>
          </View>
          <Pressable onPress={keepPending} disabled={working} style={[styles.keepButton, { backgroundColor: colors.primary }]}>
            {working ? <ActivityIndicator color={colors.primaryForeground} /> : <Text style={[styles.keepText, { color: colors.primaryForeground }]}>Keep scan</Text>}
          </Pressable>
        </View>
      </View>
    );
  }

  return (
    <View style={styles.cameraPage}>
      <CameraView ref={camera} style={StyleSheet.absoluteFill} facing="back" flash={flash} onCameraReady={() => setReady(true)} />
      <View style={[styles.overlay, { paddingTop: insets.top + 12, paddingBottom: insets.bottom + 10 }]}>
        <View style={styles.topbar}>
          <Pressable onPress={leaveScanner} style={styles.circle}><Feather name="x" size={22} color="#fff" /></Pressable>
          <View style={styles.titleBlock}><Text style={styles.title}>Basic document camera</Text><Text style={styles.counter}>{session.pages.length}/{MAX_SCAN_PAGES} pages</Text></View>
          <Pressable onPress={() => setFlash(value => value === 'on' ? 'off' : 'on')} style={styles.circle}><Feather name={flash === 'on' ? 'zap' : 'zap-off'} size={20} color="#fff" /></Pressable>
        </View>
        <View style={styles.frame}>
          <View style={[styles.corner, styles.topLeft]} /><View style={[styles.corner, styles.topRight]} />
          <View style={[styles.corner, styles.bottomLeft]} /><View style={[styles.corner, styles.bottomRight]} />
        </View>
        <View style={styles.bottom}>
          <Pressable onPress={gallery} style={styles.action}><Feather name="image" size={22} color="#fff" /><Text style={styles.actionText}>Gallery</Text></Pressable>
          <Pressable onPress={capture} disabled={!ready || working || session.pages.length >= MAX_SCAN_PAGES} style={[styles.shutter, { borderColor: colors.primary }]}><View style={[styles.shutterInner, { backgroundColor: colors.primary }]} /></Pressable>
          <Pressable onPress={() => router.replace({ pathname: '/receive/document-preview', params: { autoCreate: '1' } })} disabled={!session.pages.length} style={[styles.action, !session.pages.length && styles.disabled]}><Feather name="check" size={22} color="#fff" /><Text style={styles.actionText}>Done</Text></Pressable>
        </View>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  cameraPage: { flex: 1, backgroundColor: '#071b36' },
  overlay: { flex: 1, justifyContent: 'space-between', paddingHorizontal: 24 },
  topbar: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' },
  circle: { width: 42, height: 42, borderRadius: 21, backgroundColor: '#081a33cc', alignItems: 'center', justifyContent: 'center' },
  titleBlock: { alignItems: 'center', gap: 2 },
  title: { color: '#fff', fontFamily: 'Inter_600SemiBold', fontSize: 14 },
  counter: { color: '#ffffffbb', fontFamily: 'Inter_400Regular', fontSize: 11 },
  frame: { alignSelf: 'center', width: '88%', aspectRatio: .71, position: 'relative' },
  corner: { position: 'absolute', width: 38, height: 38, borderColor: '#fff' },
  topLeft: { left: 0, top: 0, borderLeftWidth: 3, borderTopWidth: 3 },
  topRight: { right: 0, top: 0, borderRightWidth: 3, borderTopWidth: 3 },
  bottomLeft: { left: 0, bottom: 0, borderLeftWidth: 3, borderBottomWidth: 3 },
  bottomRight: { right: 0, bottom: 0, borderRightWidth: 3, borderBottomWidth: 3 },
  bottom: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' },
  action: { minWidth: 70, alignItems: 'center', gap: 6 },
  actionText: { color: '#fff', fontFamily: 'Inter_600SemiBold', fontSize: 12 },
  shutter: { width: 76, height: 76, borderRadius: 38, borderWidth: 5, padding: 7 },
  shutterInner: { flex: 1, borderRadius: 32 },
  disabled: { opacity: .35 },
  heading: { fontFamily: 'Inter_700Bold', fontSize: 22, marginTop: 12 },
  copy: { marginTop: 16, fontFamily: 'Inter_400Regular', fontSize: 14, lineHeight: 21 },
  permission: { minHeight: 48, marginTop: 18, borderRadius: 10, alignItems: 'center', justifyContent: 'center' },
  captureReview: { flex: 1, backgroundColor: '#050b14' },
  reviewHeader: { paddingHorizontal: 20, paddingBottom: 12, flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' },
  reviewTitle: { color: '#fff', fontFamily: 'Inter_600SemiBold', fontSize: 16 },
  reviewMeta: { color: '#ffffff99', fontFamily: 'Inter_400Regular', fontSize: 11 },
  fullPreview: { flex: 1, width: '100%' },
  reviewControls: { paddingHorizontal: 20, paddingTop: 14, gap: 16 },
  editRow: { flexDirection: 'row', justifyContent: 'space-around' },
  reviewAction: { alignItems: 'center', gap: 6, minWidth: 72 },
  keepButton: { minHeight: 52, borderRadius: 12, alignItems: 'center', justifyContent: 'center' },
  keepText: { fontFamily: 'Inter_700Bold', fontSize: 15 },
});