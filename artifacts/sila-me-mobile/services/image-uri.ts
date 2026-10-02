export type ImageUriKind = 'blob' | 'http' | 'https' | 'data' | 'file' | 'content';

export function isEphemeralBrowserUri(uri?: string) {
  return Boolean(uri?.startsWith('blob:'));
}

export function classifyImageUri(uri: string): ImageUriKind {
  const value = uri.trim();
  if (!value) throw new Error('EMPTY_IMAGE_URI');
  if (value.startsWith('blob:')) return 'blob';
  if (value.startsWith('http://')) return 'http';
  if (value.startsWith('https://')) return 'https';
  if (value.startsWith('data:')) return 'data';
  if (value.startsWith('content://')) return 'content';
  if (value.startsWith('file://') || value.startsWith('/')) return 'file';
  throw new Error(`UNSUPPORTED_IMAGE_URI_SCHEME:${value.slice(0, 30)}`);
}

function base64ToBytes(value: string) {
  const binary = atob(value.replace(/-/g, '+').replace(/_/g, '/'));
  const bytes = new Uint8Array(binary.length);
  for (let index = 0; index < binary.length; index += 1) bytes[index] = binary.charCodeAt(index);
  return bytes;
}

export function decodeDataUri(uri: string) {
  const comma = uri.indexOf(',');
  if (comma < 0) throw new Error('INVALID_DATA_IMAGE_URI');
  const metadata = uri.slice(5, comma);
  const payload = uri.slice(comma + 1);
  if (/;base64/i.test(metadata)) return base64ToBytes(payload);
  return new TextEncoder().encode(decodeURIComponent(payload));
}