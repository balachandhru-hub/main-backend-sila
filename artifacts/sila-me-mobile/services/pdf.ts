import type { ScanPage } from '@/providers/scan-session';
import { PDFDocument } from './pdf-lib';

export type ReadImageBytesContext = { pageId: string; pageNumber: number };
export type ReadImageBytes = (uri: string, context?: ReadImageBytesContext) => Promise<Uint8Array>;

export function detectImageFormat(bytes: Uint8Array): 'jpg' | 'png' {
  if (bytes.length >= 3 && bytes[0] === 0xff && bytes[1] === 0xd8 && bytes[2] === 0xff) return 'jpg';
  if (
    bytes.length >= 8
    && bytes[0] === 0x89
    && bytes[1] === 0x50
    && bytes[2] === 0x4e
    && bytes[3] === 0x47
    && bytes[4] === 0x0d
    && bytes[5] === 0x0a
    && bytes[6] === 0x1a
    && bytes[7] === 0x0a
  ) return 'png';
  throw new Error('UNSUPPORTED_SCAN_IMAGE_FORMAT');
}

function imageSize(page: ScanPage) {
  return { width: page.width || 1240, height: page.height || 1754 };
}

export async function buildScanPdfBytes(pages: ScanPage[], readImageBytes: ReadImageBytes) {
  const pdf = await PDFDocument.create();
  const a4 = { portrait: [595.28, 841.89] as const, landscape: [841.89, 595.28] as const };

  for (const [index, page] of pages.entries()) {
    let bytes: Uint8Array;
    try {
      bytes = await readImageBytes(page.processedImageUri, { pageId: page.id, pageNumber: index + 1 });
    } catch (error) {
      const reason = error instanceof Error ? error.message : String(error);
      throw new Error(`Unable to read scanned Page ${index + 1}: ${reason}`);
    }
    const image = detectImageFormat(bytes) === 'png'
      ? await pdf.embedPng(bytes)
      : await pdf.embedJpg(bytes);
    const source = imageSize(page);
    const pageSize = (source.width >= source.height ? [...a4.landscape] : [...a4.portrait]) as [number, number];
    const pdfPage = pdf.addPage(pageSize);
    const scale = Math.min(pageSize[0] / source.width, pageSize[1] / source.height);
    const width = source.width * scale;
    const height = source.height * scale;
    pdfPage.drawImage(image, {
      x: (pageSize[0] - width) / 2,
      y: (pageSize[1] - height) / 2,
      width,
      height,
    });
  }

  return pdf.save({ useObjectStreams: true });
}