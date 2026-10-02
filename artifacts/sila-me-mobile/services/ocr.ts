import { Platform } from 'react-native';

export type OcrPageResult = {
  text: string;
  available: boolean;
};

export async function recognizePageText(uri: string): Promise<OcrPageResult> {
  if (Platform.OS === 'web') return { text: '', available: false };
  const native = await import('./ocr.native');
  return native.recognizePageText(uri);
}