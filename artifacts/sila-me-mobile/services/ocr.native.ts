import type { OcrPageResult } from './ocr';

export async function recognizePageText(uri: string): Promise<OcrPageResult> {
  try {
    const { recognizeText } = await import('@infinitered/react-native-mlkit-text-recognition');
    return { text: (await recognizeText(uri)).text ?? '', available: true };
  } catch {
    if (__DEV__) {
      console.warn('[OCR] Native ML Kit is unavailable in this installed build; continuing without on-device OCR.');
    }
    return { text: '', available: false };
  }
}