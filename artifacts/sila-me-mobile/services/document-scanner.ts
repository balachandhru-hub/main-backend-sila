export type NativeScanResult = { status: 'success' | 'cancel'; scannedImages: string[] };

export async function scanNativeDocuments(_maxNumDocuments: number): Promise<NativeScanResult | null> {
  return null;
}