import * as FileSystem from 'expo-file-system/legacy';
import * as ImageManipulator from 'expo-image-manipulator';
import { Platform } from 'react-native';
import type { BasicFields, ScanPage } from '@/providers/ScanSessionProvider';
import { recognizePageText } from './ocr';
import { buildScanPdfBytes } from './pdf';
import { PDFDocument } from './pdf-lib';
import { classifyImageUri, decodeDataUri, type ImageUriKind } from './image-uri';
export { parseMobileBasicFields } from './invoice-parser';

const JPEG_QUALITY = 0.92;
const THUMBNAIL_QUALITY = 0.68;

export async function runOnDeviceOcr(uri: string) {
  return recognizePageText(uri);
}

export function normalizeLocalFileUri(uri: string) {
  const value = uri.trim();
  if (value.startsWith('file://') || value.startsWith('content://')) return value;
  if (value.startsWith('/')) return `file://${value}`;
  return value;
}

function uniqueId(prefix: string) {
  return `${prefix}-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
}

export async function prepareScanPage(originalImageUri: string, id = uniqueId('page')): Promise<ScanPage> {
  // The processed image is a single high-quality image pass. It is never replaced
  // by the smaller preview thumbnail.
  const processed = await ImageManipulator.manipulateAsync(
    originalImageUri,
    [],
    { compress: JPEG_QUALITY, format: ImageManipulator.SaveFormat.JPEG }
  );
  const thumbnail = await ImageManipulator.manipulateAsync(
    processed.uri,
    [{ resize: { width: Math.min(480, processed.width) } }],
    { compress: THUMBNAIL_QUALITY, format: ImageManipulator.SaveFormat.JPEG }
  );
  return {
    id,
    originalImageUri,
    processedImageUri: processed.uri,
    thumbnailUri: thumbnail.uri,
    width: processed.width,
    height: processed.height,
    rotation: 0,
    createdAt: new Date().toISOString(),
  };
}

async function readFetchBytes(uri: string, kind: ImageUriKind) {
  const response = await fetch(uri);
  if (kind !== 'blob' && !response.ok) throw new Error(`IMAGE_FETCH_FAILED:${response.status}`);
  return new Uint8Array(await response.arrayBuffer());
}

function base64ToBytes(base64: string) {
  const binary = atob(base64);
  const bytes = new Uint8Array(binary.length);
  for (let index = 0; index < binary.length; index += 1) bytes[index] = binary.charCodeAt(index);
  return bytes;
}

function isWebRuntime() {
  return Platform.OS === 'web';
}

export async function readImageBytes(uri: string, context?: { pageId?: string; pageNumber?: number }) {
  const normalizedUri = normalizeLocalFileUri(uri);
  let kind: ImageUriKind;
  try {
    kind = classifyImageUri(normalizedUri);
  } catch (error) {
    if (isWebRuntime()) throw error;
    // Older iOS pickers and restored sessions can contain Photos/asset-library
    // URIs. ImageManipulator resolves those native references and writes a
    // regular cached JPEG that the PDF reader can consume.
    try {
      const materialized = await ImageManipulator.manipulateAsync(
        normalizedUri,
        [],
        { compress: JPEG_QUALITY, format: ImageManipulator.SaveFormat.JPEG },
      );
      if (materialized.uri === normalizedUri) throw new Error('IMAGE_URI_MATERIALIZATION_FAILED');
      return readImageBytes(materialized.uri, context);
    } catch {
      throw error;
    }
  }
  if (__DEV__) {
    console.log('[PDF-DEBUG] image', {
      pageId: context?.pageId,
      pageNumber: context?.pageNumber,
      processedImageUri: normalizedUri,
      uriPrefix: normalizedUri.substring(0, 40),
      uriKind: kind,
      platform: Platform.OS,
    });
  }
  if (kind === 'blob' || kind === 'http' || kind === 'https') return readFetchBytes(normalizedUri, kind);
  if (kind === 'data') return decodeDataUri(normalizedUri);
  if (isWebRuntime()) throw new Error(`UNSUPPORTED_IMAGE_URI_SCHEME:${kind}:web`);

  let readUri = normalizedUri;
  let temporaryUri: string | undefined;
  try {
    if (kind === 'content') {
      const directory = FileSystem.cacheDirectory ?? FileSystem.documentDirectory;
      if (!directory) throw new Error('LOCAL_SCAN_DIRECTORY_UNAVAILABLE');
      temporaryUri = `${directory}scan-source-${Date.now()}-${Math.random().toString(36).slice(2, 8)}.img`;
      await FileSystem.copyAsync({ from: normalizedUri, to: temporaryUri });
      readUri = temporaryUri;
    }

    try {
      const { File } = await import('expo-file-system');
      const file = new File(readUri);
      if (!file.exists) throw new Error(`SCAN_IMAGE_NOT_FOUND:${uri}`);
      return await file.bytes();
    } catch (modernError) {
      if (typeof FileSystem.readAsStringAsync !== 'function') throw modernError;
      const base64 = await FileSystem.readAsStringAsync(readUri, { encoding: 'base64' });
      return base64ToBytes(base64);
    }
  } finally {
    if (temporaryUri) await FileSystem.deleteAsync(temporaryUri, { idempotent: true }).catch(() => undefined);
  }
}

export async function buildScanPdf(pages: ScanPage[], scanSessionId: string) {
  const output = await buildScanPdfBytes(pages, readImageBytes);
  const document = await PDFDocument.load(output);
  if (document.getPageCount() !== pages.length) throw new Error('PDF_PAGE_COUNT_VERIFICATION_FAILED');
  if (isWebRuntime()) {
    const uri = URL.createObjectURL(new Blob([output], { type: 'application/pdf' }));
    return { uri, size: output.byteLength };
  }
  const directory = FileSystem.cacheDirectory ?? FileSystem.documentDirectory;
  if (!directory) throw new Error('LOCAL_PDF_DIRECTORY_UNAVAILABLE');
  // Each generation gets a unique path so a delayed cleanup from an earlier
  // session state can never delete the newly generated PDF.
  const uri = `${directory}invoice-${scanSessionId}--${uniqueId('pdf')}.pdf`;
  try {
    const { File } = await import('expo-file-system');
    const file = new File(uri);
    file.write(output);
    if (!file.exists || !file.size) throw new Error('PDF_WRITE_VERIFICATION_FAILED');
    return { uri, size: file.size };
  } catch (modernError) {
    if (typeof FileSystem.writeAsStringAsync !== 'function') throw modernError;
    let binary = '';
    const chunkSize = 0x8000;
    for (let offset = 0; offset < output.length; offset += chunkSize) {
      binary += String.fromCharCode(...output.subarray(offset, Math.min(offset + chunkSize, output.length)));
    }
    const base64 = btoa(binary);
    await FileSystem.writeAsStringAsync(uri, base64, { encoding: 'base64' });
    const info = await FileSystem.getInfoAsync(uri);
    if (!info.exists || info.isDirectory || !info.size) throw new Error('PDF_WRITE_VERIFICATION_FAILED');
    return { uri, size: info.size };
  }
}

async function refreshThumbnail(page: ScanPage, processedImageUri: string, width: number) {
  const thumbnail = await ImageManipulator.manipulateAsync(
    processedImageUri,
    [{ resize: { width: Math.min(480, width) } }],
    { compress: THUMBNAIL_QUALITY, format: ImageManipulator.SaveFormat.JPEG }
  );
  return thumbnail.uri;
}

export async function rotatePage(page: ScanPage, degrees = 90) {
  const result = await ImageManipulator.manipulateAsync(
    page.processedImageUri,
    [{ rotate: degrees }],
    { compress: JPEG_QUALITY, format: ImageManipulator.SaveFormat.JPEG }
  );
  const rotation = ((page.rotation + degrees + 360) % 360) as ScanPage['rotation'];
  return {
    ...page,
    processedImageUri: result.uri,
    thumbnailUri: await refreshThumbnail(page, result.uri, result.width),
    width: result.width,
    height: result.height,
    rotation,
  };
}

export async function cropPage(page: ScanPage) {
  const width = page.width || 1000;
  const height = page.height || 1400;
  const result = await ImageManipulator.manipulateAsync(
    page.processedImageUri,
    [{ crop: { originX: width * .04, originY: height * .04, width: width * .92, height: height * .92 } }],
    { compress: JPEG_QUALITY, format: ImageManipulator.SaveFormat.JPEG }
  );
  return {
    ...page,
    processedImageUri: result.uri,
    thumbnailUri: await refreshThumbnail(page, result.uri, result.width),
    width: result.width,
    height: result.height,
  };
}