import type { NativeScanResult } from './document-scanner';

export async function scanNativeDocuments(maxNumDocuments: number): Promise<NativeScanResult> {
  const { default: DocumentScanner, ResponseType } = await import('react-native-document-scanner-plugin');
  const result = await DocumentScanner.scanDocument({
    maxNumDocuments,
    croppedImageQuality: 100,
    responseType: ResponseType.ImageFilePath,
  });
  return {
    status: result.status === 'success' ? 'success' : 'cancel',
    scannedImages: result.scannedImages ?? [],
  };
}