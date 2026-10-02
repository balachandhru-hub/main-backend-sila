import assert from 'node:assert/strict';
import test from 'node:test';
import { PDFDocument } from 'pdf-lib';
import {
  addPage,
  completeSession,
  createScanSession,
  evaluateBasicSufficiency,
  movePage,
  normalizeSession,
  removePage,
  replacePage,
  setPdf,
  type ScanPage,
} from '../providers/scan-session';
import { parseMobileBasicFields } from '../services/invoice-parser';
import { combinePageOcrText } from '../services/ocr-text';
import { buildScanPdfBytes, detectImageFormat } from '../services/pdf';
import { classifyImageUri, decodeDataUri, isEphemeralBrowserUri } from '../services/image-uri';
import { deleteScanSessionPdf, isScanSessionPdf } from '../services/scan-pdf-cache';
import { getInvoiceSaveRecovery } from '../services/invoice-save-errors';
import {
  isoInvoiceDate,
  invoiceReviewHeaderActions,
  performInvoiceReviewSave,
} from '../services/invoice-review-save';
import { uploadInvoiceDocument } from '@workspace/api-client-react';
import {
  assertForcedAdvancedReady,
  autoSelectPurchaseOrder,
  defaultPhysicalReceivedQuantity,
  extractOcrSupplierCandidate,
  findOcrPurchaseOrder,
  findUniqueSupplierMatch,
  formatOcrSupplierDisplay,
  invoiceQuantityForPoItem,
  invoiceReviewBlockers,
  isInvoiceReviewReady,
  mergeRereadBasicFields,
  openPoLookupEntityCode,
  ocrSupplierScreenValue,
  persistedSupplierName,
  rankOpenPurchaseOrders,
  scorePurchaseOrderMatch,
  shouldRenderPurchaseOrderSelector,
  validatePurchaseOrderSelection,
} from '../services/invoice-review-state';
import type { PurchaseOrder, Supplier } from '@workspace/api-client-react';

function page(id: string, processedImageUri = `${id}.jpg`, overrides: Partial<ScanPage> = {}): ScanPage {
  return {
    id,
    originalImageUri: `${id}-original.jpg`,
    processedImageUri,
    thumbnailUri: `${id}-thumbnail.jpg`,
    width: 1240,
    height: 1754,
    rotation: 0,
    createdAt: '2026-09-14T00:00:00.000Z',
    ...overrides,
  };
}

test('page edits invalidate a previously generated PDF and preserve order', () => {
  let session = createScanSession();
  session = addPage(session, page('one'));
  session = addPage(session, page('two'));
  session = addPage(session, page('three'));
  session = setPdf(session, 'file:///invoice.pdf', 1234);

  assert.equal(session.pdfDirty, false);
  session = movePage(session, 'three', -1);
  assert.deepEqual(session.pages.map(item => item.id), ['one', 'three', 'two']);
  assert.equal(session.pdfUri, undefined);
  assert.equal(session.pdfDirty, true);

  const rotated = replacePage(session, 'one', page('one', 'one-rotated.jpg', {
    rotation: 90,
    width: 1754,
    height: 1240,
    thumbnailUri: 'one-rotated-thumbnail.jpg',
  }));
  assert.equal(rotated.pages[0].rotation, 90);
  assert.equal(rotated.pages[0].processedImageUri, 'one-rotated.jpg');
  assert.equal(rotated.pages[0].thumbnailUri, 'one-rotated-thumbnail.jpg');
  assert.equal(rotated.pdfDirty, true);

  const cropped = replacePage(rotated, 'two', page('two', 'two-cropped.jpg', {
    width: 1140,
    height: 1654,
    thumbnailUri: 'two-cropped-thumbnail.jpg',
  }));
  assert.equal(cropped.pages.find(item => item.id === 'two')?.processedImageUri, 'two-cropped.jpg');
  assert.equal(cropped.pages.find(item => item.id === 'two')?.thumbnailUri, 'two-cropped-thumbnail.jpg');

  const deleted = removePage(cropped, 'three');
  assert.deepEqual(deleted.pages.map(item => item.id), ['one', 'two']);
  assert.equal(deleted.pdfDirty, true);
});

test('completing a save clears pages, OCR fields, and manual edits so the next scan starts clean', () => {
  const dirty = normalizeSession({
    id: 'scan-previous-invoice',
    pages: [page('one'), page('two')],
    pdfUri: 'file:///previous-invoice.pdf',
    pdfSize: 4321,
    pdfDirty: false,
    basic: {
      supplierName: 'Test SBN',
      supplierTrn: '10012013040',
      supplierInvoiceNumber: 'INV-90015',
      invoiceDate: '2026-09-10',
      purchaseOrderNumber: '4500003415',
      invoiceGross: '542.50',
      currency: 'AED',
    },
    manualFields: ['supplierInvoiceNumber'],
    ocrText: 'previous invoice raw text',
    ocrAvailable: true,
    ocrStatus: 'complete',
    ocrDiagnostics: { requestId: 'ocr-previous' },
  });

  const completed = completeSession('invoice-123');

  assert.equal(completed.savedInvoiceId, 'invoice-123');
  assert.deepEqual(completed.pages, []);
  assert.equal(completed.pdfUri, undefined);
  assert.equal(completed.pdfSize, undefined);
  assert.equal(completed.pdfDirty, true);
  assert.deepEqual(completed.manualFields, []);
  assert.equal(completed.ocrText, undefined);
  assert.equal(completed.ocrAvailable, undefined);
  assert.equal(completed.ocrStatus, 'idle');
  assert.equal(completed.ocrDiagnostics, undefined);
  for (const key of Object.keys(completed.basic) as Array<keyof typeof completed.basic>) {
    assert.equal(completed.basic[key], '', `expected ${key} to be cleared`);
    assert.equal(completed.basicExtraction[key], '', `expected ${key} to be cleared`);
  }

  // Confirm the fixture actually had the leaked values that must not survive.
  assert.equal(dirty.basic.supplierName, 'Test SBN');
  assert.equal(dirty.pages.length, 2);
});

test('deleting pages by id handles middle, last, and only page states', () => {
  let session = createScanSession();
  session = addPage(session, page('one'));
  session = addPage(session, page('two'));
  session = addPage(session, page('three'));
  session = setPdf(session, 'file:///invoice.pdf', 1234);

  const middle = removePage(session, 'two');
  assert.deepEqual(middle.pages.map(item => item.id), ['one', 'three']);
  assert.equal(middle.pdfUri, undefined);
  assert.equal(middle.pdfDirty, true);

  const last = removePage(middle, 'three');
  assert.deepEqual(last.pages.map(item => item.id), ['one']);
  assert.equal(last.pdfDirty, true);

  const only = removePage(last, 'one');
  assert.deepEqual(only.pages, []);
  assert.equal(only.pdfUri, undefined);
  assert.equal(only.pdfDirty, true);
});

test('restored legacy pages keep source, processed, and thumbnail metadata separate', () => {
  const session = normalizeSession({
    id: 'scan-restored',
    pdfUri: 'file:///old.pdf',
    pages: [{ id: 'legacy', uri: 'file:///processed.jpg', originalImageUri: 'file:///source.jpg' }],
  });

  assert.equal(session.pages[0].originalImageUri, 'file:///source.jpg');
  assert.equal(session.pages[0].processedImageUri, 'file:///processed.jpg');
  assert.equal(session.pages[0].thumbnailUri, 'file:///processed.jpg');
  assert.equal(session.pdfDirty, false);
});

test('one processed image produces one PDF page and reordered pages are read in order', async () => {
  const jpeg = Uint8Array.from(Buffer.from(
    '/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAP//////////////////////////////////////////////////////////////////////////////////////2wBDAf//////////////////////////////////////////////////////////////////////////////////////wAARCAABAAEDASIAAhEBAxEB/8QAFQABAQAAAAAAAAAAAAAAAAAAAAX/xAAUEAEAAAAAAAAAAAAAAAAAAAAA/9oADAMBAAIQAxAAAAH/AP/EABQQAQAAAAAAAAAAAAAAAAAAABD/2gAIAQEAAT8hP//EABQRAQAAAAAAAAAAAAAAAAAAABD/2gAIAQIBAT8hP//EABQRAQAAAAAAAAAAAAAAAAAAABD/2gAIAQMBAT8hP//Z',
    'base64',
  ));
  const readOrder: string[] = [];
  const singlePagePdf = await buildScanPdfBytes(
    [page('single', 'single-processed.jpg')],
    async () => jpeg,
  );
  assert.equal((await PDFDocument.load(singlePagePdf)).getPageCount(), 1);

  const reorderedPdf = await buildScanPdfBytes(
    [
      page('second', 'second-processed.jpg'),
      page('third', 'third-processed.jpg'),
      page('first', 'first-processed.jpg'),
    ],
    async uri => {
      readOrder.push(uri);
      return jpeg;
    },
  );

  assert.deepEqual(readOrder, ['second-processed.jpg', 'third-processed.jpg', 'first-processed.jpg']);
  const pdf = await PDFDocument.load(reorderedPdf);
  assert.equal(pdf.getPageCount(), 3);
});

test('PDF image format detection supports JPEG and PNG and rejects unknown bytes', () => {
  assert.equal(detectImageFormat(Uint8Array.from([0xff, 0xd8, 0xff, 0x00])), 'jpg');
  assert.equal(detectImageFormat(Uint8Array.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a])), 'png');
  assert.throws(() => detectImageFormat(Uint8Array.from([0x00, 0x01])), /UNSUPPORTED_SCAN_IMAGE_FORMAT/);
});

test('image URI handling selects browser and native readers without file probing', () => {
  assert.equal(classifyImageUri('blob:https://preview.example/scan'), 'blob');
  assert.equal(classifyImageUri('https://preview.example/scan.jpg'), 'https');
  assert.equal(classifyImageUri('data:image/jpeg;base64,/9j/'), 'data');
  assert.equal(classifyImageUri('file:///cache/scan.jpg'), 'file');
  assert.equal(classifyImageUri('content://media/external/images/1'), 'content');
  assert.deepEqual(decodeDataUri('data:text/plain;base64,SGk='), Uint8Array.from([72, 105]));
  assert.equal(isEphemeralBrowserUri('blob:https://preview.example/scan'), true);
  assert.equal(isEphemeralBrowserUri('data:image/jpeg;base64,/9j/'), false);
  assert.throws(() => classifyImageUri('ftp://preview.example/scan.jpg'), /UNSUPPORTED_IMAGE_URI_SCHEME/);
});

test('PDF failure does not mutate the source page list', async () => {
  const session = addPage(createScanSession(), page('one', 'missing-processed.jpg'));
  await assert.rejects(
    () => buildScanPdfBytes(session.pages, async () => { throw new Error('SCAN_IMAGE_NOT_FOUND'); }),
    /SCAN_IMAGE_NOT_FOUND/,
  );
  assert.deepEqual(session.pages.map(item => item.id), ['one']);
  assert.equal(session.pdfUri, undefined);
  assert.equal(session.pdfDirty, true);
});

test('scan PDF cleanup only removes the affected session file', async () => {
  const deleted: string[] = [];
  const remove = async (uri: string) => { deleted.push(uri); };

  assert.equal(isScanSessionPdf('file:///cache/invoice-scan-one.pdf', 'scan-one'), true);
  assert.equal(isScanSessionPdf('file:///cache/invoice-scan-one--pdf-123-abc.pdf', 'scan-one'), true);
  assert.equal(isScanSessionPdf('file:///cache/invoice-scan-one-extra--pdf-123-abc.pdf', 'scan-one'), false);
  assert.equal(isScanSessionPdf('file:///cache/invoice-scan-two.pdf', 'scan-one'), false);
  assert.equal(await deleteScanSessionPdf(
    'scan-one',
    'file:///cache/invoice-scan-one.pdf',
    remove,
  ), true);
  assert.equal(await deleteScanSessionPdf(
    'scan-one',
    'file:///cache/invoice-scan-two.pdf',
    remove,
  ), false);
  assert.equal(await deleteScanSessionPdf(
    'scan-one',
    'file:///cache/unrelated.pdf',
    remove,
  ), false);
  assert.deepEqual(deleted, ['file:///cache/invoice-scan-one.pdf']);
});

test('cleanup failure rejects before an old cached PDF can become active', async () => {
  let activePdfUri: string | undefined;

  await assert.rejects(
    async () => {
      await deleteScanSessionPdf(
        'scan-one',
        'file:///cache/invoice-scan-one.pdf',
        async () => { throw new Error('CACHE_DELETE_FAILED'); },
      );
      activePdfUri = 'file:///cache/invoice-scan-one.pdf';
    },
    /CACHE_DELETE_FAILED/,
  );

  assert.equal(activePdfUri, undefined);
});

test('a delayed old-PDF cleanup cannot delete a newly generated session PDF', async () => {
  const files = new Set([
    'file:///cache/invoice-scan-one--pdf-old.pdf',
  ]);
  let releaseDelete: (() => void) | undefined;
  const deleteStarted = new Promise<void>(resolve => {
    releaseDelete = resolve;
  });
  const oldDelete = deleteScanSessionPdf(
    'scan-one',
    'file:///cache/invoice-scan-one--pdf-old.pdf',
    async uri => {
      await deleteStarted;
      files.delete(uri);
    },
  );

  const newPdfUri = 'file:///cache/invoice-scan-one--pdf-new.pdf';
  files.add(newPdfUri);
  releaseDelete?.();
  await oldDelete;

  assert.deepEqual([...files], [newPdfUri]);
});

test('deleting after PDF creation rebuilds the PDF from the remaining pages', async () => {
  const jpeg = Uint8Array.from(Buffer.from(
    '/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAP//////////////////////////////////////////////////////////////////////////////////////2wBDAf//////////////////////////////////////////////////////////////////////////////////////wAARCAABAAEDASIAAhEBAxEB/8QAFQABAQAAAAAAAAAAAAAAAAAAAAX/xAAUEAEAAAAAAAAAAAAAAAAAAAAA/9oADAMBAAIQAxAAAAH/AP/EABQQAQAAAAAAAAAAAAAAAAAAABD/2gAIAQEAAT8hP//EABQRAQAAAAAAAAAAAAAAAAAAABD/2gAIAQIBAT8hP//EABQRAQAAAAAAAAAAAAAAAAAAABD/2gAIAQMBAT8hP//Z',
    'base64',
  ));
  let session = createScanSession();
  session = addPage(session, page('a', 'a-processed.jpg'));
  session = addPage(session, page('b', 'b-processed.jpg'));
  session = addPage(session, page('c', 'c-processed.jpg'));
  session = setPdf(session, 'file:///three-pages.pdf', 1234);
  session = removePage(session, 'b');

  const readOrder: string[] = [];
  const bytes = await buildScanPdfBytes(session.pages, async uri => {
    readOrder.push(uri);
    return jpeg;
  });

  assert.deepEqual(readOrder, ['a-processed.jpg', 'c-processed.jpg']);
  assert.equal((await PDFDocument.load(bytes)).getPageCount(), 2);
  assert.equal(session.pdfUri, undefined);
  assert.equal(session.pdfDirty, true);
});

test('OCR parser extracts representative invoice fields', () => {
  const result = parseMobileBasicFields(`
    Gulf Kitchen Foods LLC
    TRN: 100123456700003
    Invoice No: TAX-7788
    Invoice Date: 14/09/2026
    Purchase Order Number: 4500001001
    Currency: AED
    Grand Total: AED 1,047.50
  `);

  assert.deepEqual(result, {
    supplierName: 'Gulf Kitchen Foods LLC',
    supplierTrn: '100123456700003',
    supplierInvoiceNumber: 'TAX-7788',
    invoiceDate: '2026-09-14',
    purchaseOrderNumber: '4500001001',
    invoiceGross: '1047.50',
    currency: 'AED',
  });
  assert.equal(parseMobileBasicFields(`
    Supplier Invoice Number: TEST-INV-17092026-001
    Invoice Date: 17/09/2026
  `).supplierInvoiceNumber, 'TEST-INV-17092026-001');
  assert.equal(parseMobileBasicFields(`
    Invoice No: TEST-INV-17092026-001
  `).supplierInvoiceNumber, 'TEST-INV-17092026-001');
});

test('OCR parser extracts the required invoice fixture fields', () => {
  assert.deepEqual(parseMobileBasicFields(`
    ABC FOOD TRADING LLC
    TRN: 100123456700003
    TAX INVOICE
    Invoice No: INV-1001
    Invoice Date: 15/09/2026
    PO Number: 4500001001
    Subtotal: AED 950.00
    VAT 5%: AED 47.50
    Grand Total: AED 997.50
  `), {
    supplierName: 'ABC FOOD TRADING LLC',
    supplierTrn: '100123456700003',
    supplierInvoiceNumber: 'INV-1001',
    invoiceDate: '2026-09-15',
    purchaseOrderNumber: '4500001001',
    invoiceGross: '997.50',
    currency: 'AED',
  });
});

test('OCR parser normalizes alternate invoice labels, date separators, and comma-decimal totals', () => {
  assert.deepEqual(parseMobileBasicFields(`
    Desert Harvest General Trading
    VAT Registration No: 100 987 654 300 002
    Invoice Ref = DH-2026-0915
    Date = 15.09.26
    Your PO: PO-77881
    Total Payable: AED 1.047,50
  `), {
    supplierName: 'Desert Harvest General Trading',
    supplierTrn: '100987654300002',
    supplierInvoiceNumber: 'DH-2026-0915',
    invoiceDate: '2026-09-15',
    purchaseOrderNumber: 'PO-77881',
    invoiceGross: '1047.50',
    currency: 'AED',
  });
});

test('OCR parser normalizes ISO dates and invoice IDs with grouped whole totals', () => {
  assert.deepEqual(parseMobileBasicFields(`
    Northern Supplies Company
    Tax Registration: 100-111-222-300-004
    Invoice ID: NS/8842
    Invoice Date: 2026-09-05
    Customer PO: 550019
    Invoice Total: USD 12,500
  `), {
    supplierName: 'Northern Supplies Company',
    supplierTrn: '100111222300004',
    supplierInvoiceNumber: 'NS/8842',
    invoiceDate: '2026-09-05',
    purchaseOrderNumber: '550019',
    invoiceGross: '12500',
    currency: 'USD',
  });
});

test('OCR parser leaves missing optional fields explicitly blank', () => {
  assert.deepEqual(parseMobileBasicFields(`
    Corner Market LLC
    Inv No. # CM-44
    Invoice Date: 7-Sep-2026
    Amount Due: 275.00
  `), {
    supplierName: 'Corner Market LLC',
    supplierTrn: '',
    supplierInvoiceNumber: 'CM-44',
    invoiceDate: '2026-09-07',
    purchaseOrderNumber: '',
    invoiceGross: '275.00',
    currency: '',
  });
});

test('OCR parser keeps supplier codes without ID labels and prefers a real name', () => {
  assert.equal(parseMobileBasicFields(`
    TAX INVOICE
    Supplier
    ID: 1003430
    Invoice Number: INV-100
    Grand Total: AED 10.00
  `).supplierName, '1003430');
  assert.equal(parseMobileBasicFields(`
    TAX INVOICE
    Supplier: ABC Food Trading LLC
    Supplier ID: 1003430
    Invoice Number: INV-100
    Grand Total: AED 10.00
  `).supplierName, 'ABC Food Trading LLC');
});

test('OCR parser reads banner invoice numbers, limited supplier names, and Total amounts', () => {
  assert.deepEqual(parseMobileBasicFields(`
    TAX INVOICE
    TESTINVOICE123
    Supplier
    ABRACADABRA LIFE LIMITED
    Supplier ID: 1003754
    Invoice Date 21/09/2026
    PO Number 4500002849
    Currency AED
    Description PO Item Quantity Unit Price (AED) Amount (AED)
    test003 (Material: M) 10 1.00 2,000.00 2,000.00
    Subtotal AED 2,000.00
    Tax AED 0.00
    Total AED 2,000.00
  `), {
    supplierName: 'ABRACADABRA LIFE LIMITED',
    supplierTrn: '',
    supplierInvoiceNumber: 'TESTINVOICE123',
    invoiceDate: '2026-09-21',
    purchaseOrderNumber: '4500002849',
    invoiceGross: '2000.00',
    currency: 'AED',
  });
  assert.equal(parseMobileBasicFields(`
    TAX INVOICE
    TESTINVOICE123
    Supplier
    Invoice
    Date 21/09/2026
    ABRACADABRA LIFE LIMITED
    Supplier ID: 1003754
    Invoice Date 21/09/2026
    PO Number 4500002849
    Currency AED
    Total AED 2,000.00
  `).supplierName, 'ABRACADABRA LIFE LIMITED');
  assert.equal(formatOcrSupplierDisplay('Invoice'), '');
  assert.equal(formatOcrSupplierDisplay('TAX INVOICE'), '');
});

test('OCR parser prefers the supplier and leaves an absent PO blank', () => {
  const result = parseMobileBasicFields(`
    ABC Food Trading LLC
    Supplier: ABC Food Trading LLC
    TRN: 100123456700003
    Bill To: FIVE Hotels
    Invoice Number: INV-2
    Grand Total: USD 42.00
  `);

  assert.equal(result.supplierName, 'ABC Food Trading LLC');
  assert.equal(result.purchaseOrderNumber, '');
  assert.equal(result.invoiceGross, '42.00');
  assert.equal(result.currency, 'USD');
});

test('multi-page OCR keeps page boundaries and finds totals on the final page', () => {
  const rawText = combinePageOcrText([
    { id: 'one', text: 'Supplier: ABC Food Trading LLC\nInvoice Number: INV-3' },
    { id: 'two', text: 'Line item: flour' },
    { id: 'three', text: 'Subtotal: AED 950.00\nVAT: AED 47.50\nGrand Total: AED 997.50' },
  ]);

  assert.match(rawText, /--- PAGE 1 ---/);
  assert.match(rawText, /--- PAGE 2 ---/);
  assert.match(rawText, /--- PAGE 3 ---/);
  assert.equal(parseMobileBasicFields(rawText).invoiceGross, '997.50');
});

test('insufficient mobile extraction stays partial until the merged result has required fields', () => {
  const insufficient = {
    supplierName: 'Supplier',
    supplierTrn: '',
    supplierInvoiceNumber: '',
    invoiceDate: '',
    purchaseOrderNumber: '',
    invoiceGross: '',
    currency: '',
  };
  const first = evaluateBasicSufficiency(insufficient);
  assert.equal(first.status, 'PARTIAL');
  assert.deepEqual(first.missingFields, ['supplierInvoiceNumber', 'purchaseOrderNumber', 'invoiceGross']);

  const merged = { ...insufficient, supplierInvoiceNumber: 'INV-100', purchaseOrderNumber: 'PO-200', invoiceGross: '1250.00' };
  const final = evaluateBasicSufficiency(merged, { requireCurrency: true });
  assert.equal(final.status, 'PARTIAL');
  assert.deepEqual(final.missingFields, ['currency']);
  assert.equal(evaluateBasicSufficiency({ ...merged, currency: 'AED' }).status, 'SUCCESS');
});

test('restored scan session cannot keep labelled OCR supplier ids', () => {
  const restored = normalizeSession({
    pages: [page('one')],
    basicExtraction: {
      supplierName: 'ID: 1003430',
      supplierTrn: '',
      supplierInvoiceNumber: 'INV-1',
      invoiceDate: '',
      purchaseOrderNumber: '4500003415',
      invoiceGross: '10.00',
      currency: 'AED',
    },
  });
  assert.equal(restored.basic.supplierName, '1003430');
  assert.equal(restored.basicExtraction.supplierName, '1003430');
  assert.doesNotMatch(ocrSupplierScreenValue(restored.basic.supplierName), /id\s*:/i);
  assert.equal(mergeRereadBasicFields({ supplierName: 'ID: 1003430' }, { supplierName: 'ID: 1003430' }, []).supplierName, '1003430');
  assert.equal(shouldRenderPurchaseOrderSelector('supplier-uuid'), true);
  assert.equal(shouldRenderPurchaseOrderSelector(null), false);
});

test('Supplier Master match is authoritative and never uses PO metadata', () => {
  const supplier = {
    id: 'supplier-internal-1',
    supplierCode: '1003430',
    name: 'Test SBN',
    legalName: 'Test SBN',
    entityCode: '1050',
    isBlocked: false,
    isDeleted: false,
    status: 'ACTIVE',
    aliases: [],
    updatedAt: '2026-09-17T00:00:00Z',
  } satisfies Supplier;

  const match = findUniqueSupplierMatch('  test-sbn ', [supplier]);
  assert.equal(match?.name, 'Test SBN');
  assert.equal(match?.supplierCode, '1003430');
  assert.notEqual(match?.name, 'Company Code: 1050 Currency: Not provided');

  const byId = findUniqueSupplierMatch('Supplier ID: 1003430', [supplier]);
  assert.equal(byId?.name, 'Test SBN');
  assert.equal(byId?.supplierCode, '1003430');
  assert.equal(extractOcrSupplierCandidate('Supplier ID: 1003430').supplierId, '1003430');
  const byBareId = findUniqueSupplierMatch('ID: 1003430', [supplier]);
  assert.equal(byBareId?.name, 'Test SBN');
  assert.equal(byBareId?.supplierCode, '1003430');
  assert.equal(extractOcrSupplierCandidate('ID: 1003430').supplierId, '1003430');
  assert.equal(extractOcrSupplierCandidate('ID: 1003430').supplierName, '');
  assert.doesNotMatch(ocrSupplierScreenValue('ID: 1003430'), /id\s*:/i);
  assert.equal(ocrSupplierScreenValue('ID: 1003430'), '1003430');
  assert.equal(ocrSupplierScreenValue('Supplier ID: 1003430'), '1003430');
  assert.equal(ocrSupplierScreenValue('Vendor ID: 1003430'), '1003430');
  assert.equal(ocrSupplierScreenValue('Test SBN'), 'Test SBN');
  assert.equal(formatOcrSupplierDisplay('ID:1003430'), '1003430');
  assert.doesNotMatch(formatOcrSupplierDisplay('ID: 1003430'), /id\s*:/i);
  assert.equal(formatOcrSupplierDisplay('Supplier ID: 1003430'), '1003430');
  assert.equal(persistedSupplierName('ID: 1003430', supplier), 'Test SBN');
  assert.equal(persistedSupplierName('ID: 1003430', null), '1003430');
  assert.equal(findUniqueSupplierMatch('unknown vendor', [supplier]), null);
  assert.equal(formatOcrSupplierDisplay('Vendor ID: 1003430'), '1003430');
  assert.doesNotMatch(formatOcrSupplierDisplay(parseMobileBasicFields(`
    TAX INVOICE
    Supplier
    ID: 1003430
    Invoice Number: INV-100
    Grand Total: AED 10.00
  `).supplierName), /id\s*:/i);
});

test('supplier open PO lookup preserves all choices and preselects only OCR match', () => {
  const po = (id: string, poNumber: string): PurchaseOrder => ({
    id,
    poNumber,
    entityCode: '1050',
    companyCode: '1050',
    currency: 'AED',
    status: 'OPEN',
    organizationId: 'org-1',
    supplierId: 'supplier-internal-1',
    supplierName: 'Test SBN',
    items: [{
      id: `${id}-10`,
      lineNumber: 10,
      materialCode: 'MAT-1',
      description: 'Material',
      orderedQuantity: 10,
      receivedQuantity: 0,
      openQuantity: 10,
      uom: 'EA',
      status: 'OPEN',
      goodsReceiptExpected: true,
      deliveryCompleted: false,
      deletionIndicator: false,
    }],
  });
  const choices = [po('po-1', '4500003415'), po('po-2', '4500003420'), po('po-3', '4500003440')];

  assert.equal(choices.length, 3);
  assert.equal(findOcrPurchaseOrder('4500003415', choices, 'supplier-internal-1', 'org-1', '1050')?.id, 'po-1');
  assert.equal(findOcrPurchaseOrder('PO 4500-003415', choices, 'supplier-internal-1', 'org-1', '1050')?.id, 'po-1');
  assert.equal(findOcrPurchaseOrder('missing', choices, 'supplier-internal-1', 'org-1', '1050'), null);

  const invoiceLines = [
    { description: 'GI Red /Socket 2"x1"', quantity: 5, unitPrice: 300, lineAmount: 1500, poItemNumber: '10' },
    { description: 'GI Red /Socket 2"x1"', quantity: 5, unitPrice: 300, lineAmount: 1500, poItemNumber: '20' },
  ];
  const matching = po('po-1', '4500003415');
  matching.items = [
    { ...matching.items[0]!, id: 'po-1-10', lineNumber: 10, itemNumber: '10', description: 'GI Red /Socket 2"x1"', unitPrice: 300, openQuantity: 2000, orderedQuantity: 2000 },
    { ...matching.items[0]!, id: 'po-1-20', lineNumber: 20, itemNumber: '20', description: 'GI Red /Socket 2"x1"', unitPrice: 300, openQuantity: 2000, orderedQuantity: 2000 },
  ];
  const other = po('po-2', '4500003420');
  other.items = [{ ...other.items[0]!, description: 'Unrelated packing material', unitPrice: 9, openQuantity: 50 }];
  const ranked = rankOpenPurchaseOrders([matching, other, po('po-3', '4500003440')], 'supplier-internal-1', 'org-1', '1050', invoiceLines, '24350.00', 'AED');
  assert.equal(autoSelectPurchaseOrder(ranked)?.id, 'po-1');
  const rankedDefaultEntity = rankOpenPurchaseOrders([matching], 'supplier-internal-1', 'org-1', 'DEFAULT', invoiceLines, '24350.00', 'AED', '4500003415');
  assert.equal(autoSelectPurchaseOrder(rankedDefaultEntity)?.poNumber, '4500003415');
  assert.equal(openPoLookupEntityCode('DEFAULT'), undefined);
  assert.equal(openPoLookupEntityCode('1050'), '1050');
  assert.equal(validatePurchaseOrderSelection(matching, 'supplier-internal-1', 'org-1', 'DEFAULT'), null);
  assert.ok((ranked[0]?.matchedLineCount ?? 0) >= 1);
  const scored = scorePurchaseOrderMatch(matching, invoiceLines, '24350.00', 'AED');
  assert.ok(scored.score > scorePurchaseOrderMatch(other, invoiceLines, '24350.00', 'AED').score);

  const ambiguous = rankOpenPurchaseOrders([matching, { ...matching, id: 'po-dup', poNumber: '4500003499' }], 'supplier-internal-1', 'org-1', '1050', invoiceLines, '24350.00', 'AED');
  assert.equal(autoSelectPurchaseOrder(ambiguous.filter(item => item.band !== 'exact')), null);

  assert.equal(defaultPhysicalReceivedQuantity(5, 10).value, '5');
  assert.equal(defaultPhysicalReceivedQuantity(5, 10).overDelivery, false);
  assert.equal(defaultPhysicalReceivedQuantity(12, 10).overDelivery, true);
  assert.equal(defaultPhysicalReceivedQuantity(12, 10).value, '');
});

test('PO validation denies supplier mismatch, closed PO, and no GR eligible lines', () => {
  const base: PurchaseOrder = {
    id: 'po-1',
    poNumber: '4500003415',
    entityCode: '1050',
    currency: 'AED',
    status: 'OPEN',
    organizationId: 'org-1',
    supplierId: 'other-supplier',
    supplierName: 'Other Supplier',
    items: [{
      id: 'line-10',
      lineNumber: 10,
      materialCode: 'MAT-1',
      description: 'Material',
      orderedQuantity: 10,
      receivedQuantity: 0,
      openQuantity: 10,
      uom: 'EA',
      status: 'OPEN',
      goodsReceiptExpected: true,
    }],
  };

  assert.equal(validatePurchaseOrderSelection(base, 'supplier-internal-1', 'org-1', '1050'), 'PO_SUPPLIER_MISMATCH');
  assert.equal(validatePurchaseOrderSelection({ ...base, supplierId: 'supplier-internal-1', status: 'CLOSED' }, 'supplier-internal-1', 'org-1', '1050'), 'PO_NOT_OPEN');
  assert.equal(validatePurchaseOrderSelection({
    ...base,
    supplierId: 'supplier-internal-1',
    items: [{ ...base.items[0]!, goodsReceiptExpected: false }],
  }, 'supplier-internal-1', 'org-1', '1050'), 'PO_NOT_GR_ELIGIBLE');
});

test('Invoice Review header actions sit in the top-right with compact labels', () => {
  const wide = invoiceReviewHeaderActions(900, false);
  assert.equal(wide.view.label, 'VIEW DOCUMENT');
  assert.equal(wide.reread.label, 'RE-READ INVOICE');
  const compact = invoiceReviewHeaderActions(390, true);
  assert.equal(compact.view.label, 'View');
  assert.equal(compact.reread.label, 'Re-reading…');
});

test('Save and Continue is enabled only with authoritative supplier and PO', () => {
  const fields = {
    supplierName: 'Test SBN',
    supplierTrn: '',
    supplierInvoiceNumber: 'TEST-INV-17092026-001',
    invoiceDate: '2026-09-17',
    purchaseOrderNumber: '4500003415',
    invoiceGross: '24350.00',
    currency: '',
  };
  const selectedPo = {
    id: 'po-1',
    poNumber: '4500003415',
    currency: 'AED',
    status: 'OPEN',
    organizationId: 'org-1',
    supplierId: 'supplier-internal-1',
    supplierName: 'Test SBN',
    items: [],
  } satisfies PurchaseOrder;

  assert.equal(isInvoiceReviewReady(fields, null, selectedPo, false), false);
  assert.equal(isInvoiceReviewReady(fields, 'supplier-internal-1', null, false), false);
  assert.equal(isInvoiceReviewReady(fields, 'supplier-internal-1', selectedPo, false), true);
  assert.deepEqual(invoiceReviewBlockers(fields, null, selectedPo, false), ['AUTHORITATIVE_SUPPLIER_REQUIRED']);
  assert.deepEqual(invoiceReviewBlockers(fields, 'supplier-internal-1', null, false), ['PURCHASE_ORDER_REQUIRED']);
});

test('re-read preserves confirmed fields and requires Advanced OCR scope', () => {
  const extracted = {
    supplierName: 'ID: 1003430',
    supplierTrn: '',
    supplierInvoiceNumber: 'NEW-INV',
    invoiceDate: '2026-09-18',
    purchaseOrderNumber: '4500003415',
    invoiceGross: '10.00',
    currency: 'AED',
  };
  const confirmed = {
    ...extracted,
    supplierInvoiceNumber: 'USER-INV',
    purchaseOrderNumber: '4500003415',
  };
  const merged = mergeRereadBasicFields(extracted, confirmed, ['supplierInvoiceNumber', 'purchaseOrderNumber']);
  assert.equal(merged.supplierName, '1003430');
  assert.equal(merged.supplierInvoiceNumber, 'USER-INV');
  assert.equal(merged.purchaseOrderNumber, '4500003415');
  assert.throws(() => assertForcedAdvancedReady({ forceAdvanced: true }), /REREAD_DOCUMENT_MISSING/);
  assert.throws(() => assertForcedAdvancedReady({ forceAdvanced: true, pdfUri: 'file://invoice.pdf' }), /BACKEND_OCR_SCOPE_UNAVAILABLE/);
  assert.doesNotThrow(() => assertForcedAdvancedReady({ forceAdvanced: true, pdfUri: 'file://invoice.pdf', organizationId: 'org-1' }));
});

test('GRN physical qty prefills from invoice qty by PO item number and stays separate', () => {
  const qty = invoiceQuantityForPoItem(
    [{ lineNumber: 10, quantity: 5 }, { lineNumber: 20, quantity: 5 }],
    { id: 'po-item-10', lineNumber: 10, itemNumber: '10' },
  );
  assert.equal(qty, 5);
  const physical = defaultPhysicalReceivedQuantity(qty, 2000);
  assert.equal(physical.value, '5');
  const editedPhysical = '4';
  assert.equal(qty, 5);
  assert.notEqual(editedPhysical, String(qty));
});

test('native invoice upload preserves structured recoverable errors from a Blob request', async () => {
  const originalFetch = globalThis.fetch;
  const requests: RequestInit[] = [];
  const responses = [
    {
      code: 'INVOICE_REVIEW_FIELDS_REQUIRED',
      message: 'Complete the invoice review before saving.',
      missingFields: ['purchaseOrderNumber'],
    },
    {
      code: 'DUPLICATE_INVOICE',
      message: 'A matching invoice already exists.',
      existingInvoiceId: 'existing-invoice-1',
      duplicateType: 'PROBABLE',
    },
  ];

  try {
    globalThis.fetch = async (_input, init) => {
      requests.push(init ?? {});
      const payload = responses[requests.length - 1];
      return {
        ok: false,
        status: 400,
        statusText: 'Bad Request',
        url: 'https://api.example.test/api/v1/documents/invoices',
        headers: new Headers({ 'content-type': 'application/json' }),
        body: undefined,
        text: async () => JSON.stringify(payload),
      } as unknown as Response;
    };

    const upload = (overrides: Record<string, unknown> = {}) => uploadInvoiceDocument({
      organizationId: 'organization-1',
      operatingUnitId: 'unit-1',
      sourceChannel: 'MOBILE_SCANNER',
      pageCount: 1,
      scanSessionId: 'scan-native-1',
      supplierName: 'Supplier',
      supplierInvoiceNumber: 'INV-100',
      invoiceGross: 125,
      currency: 'AED',
      deferFullExtraction: true,
      file: new Blob(['%PDF-native-invoice'], { type: 'application/pdf' }),
      ...overrides,
    });

    const captureRejection = async (promise: Promise<unknown>) => {
      try {
        await promise;
      } catch (error) {
        return error;
      }
      assert.fail('Expected invoice upload to reject.');
    };

    const missingError = await captureRejection(upload());
    assert.deepEqual((missingError as { data?: { missingFields?: string[] } }).data?.missingFields, ['purchaseOrderNumber']);
    const missingRecovery = getInvoiceSaveRecovery(missingError);
    assert.equal(missingRecovery?.kind, 'missing-fields');
    assert.deepEqual(missingRecovery?.error.missingFields, ['purchaseOrderNumber']);
    assert.equal(missingRecovery?.stayOnScan, true);

    const missingBody = requests[0].body as FormData;
    const missingFile = missingBody.get('file');
    assert.ok(missingFile instanceof Blob);
    assert.equal((missingFile as Blob).type, 'application/pdf');
    assert.equal(missingBody.get('purchaseOrderNumber'), null);

    const duplicateError = await captureRejection(upload({ purchaseOrderNumber: 'PO-100' }));
    assert.equal((duplicateError as { data?: { existingInvoiceId?: string } }).data?.existingInvoiceId, 'existing-invoice-1');
    assert.equal((duplicateError as { data?: { duplicateType?: string } }).data?.duplicateType, 'PROBABLE');
    const duplicateRecovery = getInvoiceSaveRecovery(duplicateError);
    assert.equal(duplicateRecovery?.kind, 'duplicate');
    assert.equal(duplicateRecovery?.error.existingInvoiceId, 'existing-invoice-1');
    assert.equal(duplicateRecovery?.error.duplicateType, 'PROBABLE');
    assert.equal(duplicateRecovery?.stayOnScan, true);
    assert.equal(requests.length, 2);
  } finally {
    globalThis.fetch = originalFetch;
  }
});

test('invoice save sends ISO dates and rejects a missing InvoiceId', async () => {
  assert.equal(isoInvoiceDate('17/09/2026'), '2026-09-17');
  assert.equal(isoInvoiceDate('2026-09-17'), '2026-09-17');
  assert.equal(isoInvoiceDate(''), null);

  const fields = {
    supplierName: 'Test SBN',
    supplierTrn: '',
    supplierInvoiceNumber: 'TEST-INV-17092026-001',
    invoiceDate: '17/09/2026',
    purchaseOrderNumber: '4500003415',
    invoiceGross: '10.00',
    currency: 'AED',
  };
  const supplier = {
    id: 'supplier-internal-1',
    supplierCode: '1003430',
    name: 'Test SBN',
  } as Supplier;
  const selectedPo = {
    id: 'po-1',
    poNumber: '4500003415',
    currency: 'AED',
    status: 'OPEN',
    organizationId: 'org-1',
    supplierId: 'supplier-internal-1',
    supplierName: 'Test SBN',
    items: [],
  } satisfies PurchaseOrder;

  const emptyId = await performInvoiceReviewSave({
    saving: false,
    fields,
    selectedSupplier: supplier,
    selectedSupplierId: supplier.id,
    selectedPo,
    noPurchaseOrder: false,
    organization: { id: 'org-1', code: 'FIVE', name: 'FIVE' },
    operatingUnitId: 'unit-1',
    pdfUri: 'blob:invoice',
    pageCount: 1,
    scanSessionId: 'scan-1',
    ocrRequestId: null,
    manualFields: [],
    invoiceLinesJson: null,
    idempotencyKey: 'mobile-save-scan-1',
    upload: async (request) => {
      assert.equal(request.invoiceDate, '2026-09-17');
      return {
        id: 'doc-1',
        filename: 'invoice.pdf',
        contentType: 'application/pdf',
        fileSizeBytes: 12,
        sourceChannel: 'MOBILE_SCANNER',
        status: 'UPLOADED',
        createdAt: '2026-09-20T00:00:00.000Z',
        invoiceId: '00000000-0000-0000-0000-000000000000',
        saveStatus: 'SAVED',
        nextStep: 'GRN_COMPARISON',
      };
    },
    readPdf: async () => new Blob(['%PDF'], { type: 'application/pdf' }),
  });
  assert.equal(emptyId.kind, 'error');

  const saved = await performInvoiceReviewSave({
    saving: false,
    fields,
    selectedSupplier: supplier,
    selectedSupplierId: supplier.id,
    selectedPo,
    noPurchaseOrder: false,
    organization: { id: 'org-1', code: 'FIVE', name: 'FIVE' },
    operatingUnitId: 'unit-1',
    pdfUri: 'blob:invoice',
    pageCount: 1,
    scanSessionId: 'scan-1',
    ocrRequestId: null,
    manualFields: [],
    invoiceLinesJson: null,
    idempotencyKey: 'mobile-save-scan-1',
    upload: async () => ({
      id: 'doc-2',
      filename: 'invoice.pdf',
      contentType: 'application/pdf',
      fileSizeBytes: 12,
      sourceChannel: 'MOBILE_SCANNER',
      status: 'UPLOADED',
      createdAt: '2026-09-20T00:00:00.000Z',
      invoiceId: 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee',
      saveStatus: 'SAVED',
      nextStep: 'GRN_COMPARISON',
    }),
    readPdf: async () => new Blob(['%PDF'], { type: 'application/pdf' }),
  });
  assert.equal(saved.kind, 'saved');
  if (saved.kind === 'saved') {
    assert.equal(saved.navigation.pathname, '/receive/finalize-grn');
    assert.equal(saved.navigation.params.invoiceId, 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee');
  }
});