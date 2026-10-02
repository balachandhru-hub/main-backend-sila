export type DeleteCachedPdf = (uri: string) => Promise<void>;

export function isScanSessionPdf(uri: string | undefined, scanSessionId: string) {
  if (!uri || !scanSessionId) return false;
  const fileName = uri.split(/[?#]/, 1)[0].split('/').pop();
  return fileName === `invoice-${scanSessionId}.pdf`
    || Boolean(fileName?.startsWith(`invoice-${scanSessionId}--`) && fileName.endsWith('.pdf'));
}

export async function deleteScanSessionPdf(
  scanSessionId: string,
  uri: string | undefined,
  deleteCachedPdf: DeleteCachedPdf,
) {
  if (!isScanSessionPdf(uri, scanSessionId)) return false;
  await deleteCachedPdf(uri!);
  return true;
}