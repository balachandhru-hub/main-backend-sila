/**
 * PROTECTED SILA INVOICE RECEIVING FLOW — See docs/PROTECTED_INVOICE_FLOW.md.
 * OCR merge. Authority: MANUAL > BACKEND MERGED OCR > MOBILE OCR. Re-read must not duplicate documents.
 */
import { createOcrRequestId, evaluateBasicSufficiency, type ScanPage } from '@/providers/scan-session';
import type { BasicFields, OcrDiagnostics, OcrExtractionStatus, OcrResult } from '@/providers/scan-session';
import { extractAdvancedInvoice, extractBasicInvoice } from '@workspace/api-client-react';
import type { AdvancedInvoiceExtractionResponse } from '@workspace/api-client-react';
import Constants from 'expo-constants';
import { Platform } from 'react-native';
import { parseMobileBasicFields } from './invoice-parser';
import { assertForcedAdvancedReady, formatOcrSupplierDisplay } from './invoice-review-state';
import { recognizePageText } from './ocr';
import { combinePageOcrText } from './ocr-text';

export type OcrProgress = {
  page: number;
  total: number;
};

export type OcrResolverOptions = {
  pdfUri?: string;
  organizationId?: string;
  operatingUnitId?: string | null;
  forceAdvanced?: boolean;
  ocrRequestId?: string;
  onFallbackStart?: (requestId: string) => void;
};

function runtimeEnvironment() {
  if (Platform.OS === 'web') return 'Web';
  return Constants.appOwnership === 'expo' ? 'Expo Go' : 'Development Build';
}

function extractionStatus(basic: BasicFields, rawText: string): OcrExtractionStatus {
  if (!rawText.trim()) return 'FAILED';
  const values = Object.values(basic);
  return values.every(value => value.trim()) ? 'SUCCESS' : 'PARTIAL';
}

function uriScheme(uri: string) {
  const match = uri.match(/^([a-z][a-z\d+.-]*):/i);
  return match?.[1]?.toLowerCase() ?? 'local';
}

function asString(value: unknown) {
  return typeof value === 'string' ? value : value === null || value === undefined ? '' : String(value);
}

function asNumber(value: unknown) {
  return typeof value === 'number' && Number.isFinite(value) ? value : undefined;
}

function mergeBasicFields(preferred: BasicFields, fallback?: BasicFields): BasicFields {
  return {
    supplierName: preferred.supplierName.trim() || fallback?.supplierName || '',
    supplierTrn: preferred.supplierTrn.trim() || fallback?.supplierTrn || '',
    supplierInvoiceNumber: preferred.supplierInvoiceNumber.trim() || fallback?.supplierInvoiceNumber || '',
    invoiceDate: preferred.invoiceDate.trim() || fallback?.invoiceDate || '',
    purchaseOrderNumber: preferred.purchaseOrderNumber.trim() || fallback?.purchaseOrderNumber || '',
    invoiceGross: preferred.invoiceGross.trim() || fallback?.invoiceGross || '',
    currency: preferred.currency.trim() || fallback?.currency || '',
  };
}

function basicFromAdvanced(response: AdvancedInvoiceExtractionResponse, fallback?: BasicFields): BasicFields {
  const header = response.header;
  return mergeBasicFields({
    supplierName: formatOcrSupplierDisplay(asString(header.supplierName.value)),
    supplierTrn: asString(header.supplierTrn.value),
    supplierInvoiceNumber: asString(header.invoiceNumber.value),
    invoiceDate: asString(header.invoiceDate.value),
    purchaseOrderNumber: asString(header.purchaseOrderNumber.value),
    invoiceGross: asNumber(header.grossAmount.value)?.toString() ?? '',
    currency: asString(header.currency.value),
  }, fallback);
}

async function runAdvanced(
  source: Blob,
  pages: ScanPage[],
  options: OcrResolverOptions,
  basic: BasicFields,
  confidence: number,
  requestId: string,
) {
  if (!options.organizationId) throw new Error('BACKEND_OCR_SCOPE_UNAVAILABLE');
  const trigger = options.forceAdvanced ? 'MANUAL_REREAD' : 'AUTO_FALLBACK';
  if (__DEV__) console.log('[OCR_TRACE]', { requestId, event: 'BACKEND_REQUEST_STARTED', trigger, pdf: 'exact-generated-pdf' });
  return extractAdvancedInvoice({
    organizationId: options.organizationId,
    operatingUnitId: options.operatingUnitId ?? null,
    trigger,
    mobileSupplierName: basic.supplierName || null,
    mobileSupplierInvoiceNumber: basic.supplierInvoiceNumber || null,
    mobilePurchaseOrderNumber: basic.purchaseOrderNumber || null,
    mobileInvoiceGross: basic.invoiceGross ? Number(basic.invoiceGross) : null,
    mobileConfidence: confidence,
    ocrRequestId: requestId,
    file: new Blob([source], { type: 'application/pdf' }),
  });
}

function basicConfidence(basic: BasicFields) {
  return Object.values(basic).filter(value => value.trim()).length / 7;
}

function resultFromAdvanced(
  response: AdvancedInvoiceExtractionResponse,
  pages: ScanPage[],
  requestId: string,
  fallback?: BasicFields,
): OcrResult {
  const fromText = parseMobileBasicFields(response.rawText ?? '');
  const basic = basicFromAdvanced(response, mergeBasicFields(fallback ?? {
    supplierName: '', supplierTrn: '', supplierInvoiceNumber: '', invoiceDate: '',
    purchaseOrderNumber: '', invoiceGross: '', currency: '',
  }, fromText));
  const required = response.effectiveConfiguration ?? undefined;
  const evaluation = evaluateBasicSufficiency(basic, required);
  const status: OcrExtractionStatus = response.status === 'FAILED'
    ? 'FAILED'
    : evaluation.status;
  const diagnostics: OcrDiagnostics = {
    requestId,
    backendStatus: response.status,
    backendProvider: response.provider,
    backendConfidence: response.confidence,
    backendFieldCount: Object.values(basic).filter(Boolean).length,
    backendLineCount: response.lines?.length ?? 0,
    finalStatus: status,
    finalMissingFields: evaluation.missingFields,
    effectiveConfiguration: response.effectiveConfiguration ? {
      automaticBackendFallbackEnabled: response.effectiveConfiguration.automaticBackendFallbackEnabled ?? false,
      minimumMobileConfidence: response.effectiveConfiguration.minimumMobileConfidence ?? 0,
      version: response.effectiveConfiguration.version ?? 0,
    } : undefined,
    mergeStatus: 'ADVANCED_FIELDS_APPLIED',
  };
  if (__DEV__) console.log('[OCR_TRACE]', {
    requestId,
    event: 'BACKEND_RESULT_RECEIVED',
    status: response.status,
    provider: response.provider,
    fieldCount: diagnostics.backendFieldCount,
    confidence: response.confidence,
    requiresReview: response.requiresReview,
  });
  return {
    pageTexts: pages.map((page, index) => ({ id: page.id, text: index === 0 ? response.rawText ?? '' : '' })),
    rawText: response.rawText ?? '',
    basic,
    available: status !== 'FAILED',
    status,
    error: response.errorMessage ?? undefined,
    advanced: response,
    diagnostics: {
      ...diagnostics,
      fallbackDecision: 'FALLBACK_EXECUTED',
      fallbackTrigger: response.trigger,
    },
  };
}

export async function recognizeInvoicePages(
  pages: ScanPage[],
  onProgress?: (progress: OcrProgress) => void,
  options?: OcrResolverOptions,
): Promise<OcrResult> {
  const requestId = options?.ocrRequestId ?? createOcrRequestId();
  const trace = (event: string, details?: Record<string, unknown>) => {
    if (__DEV__) console.log('[OCR_TRACE]', { requestId, event, ...details });
  };
  trace('MOBILE_START', { pageCount: pages.length, environment: runtimeEnvironment() });
  if (__DEV__) {
    console.log('[BASIC-OCR] DOCUMENT', { pageCount: pages.length, pdfUri: options?.pdfUri ? uriScheme(options.pdfUri) : 'not-created' });
  }
  if (options?.forceAdvanced) {
    assertForcedAdvancedReady(options);
    options.onFallbackStart?.(requestId);
    trace('FALLBACK_TRIGGERED', { trigger: 'MANUAL_REREAD', reason: 'FORCED_REREAD' });
    const source = await (await fetch(options.pdfUri!)).blob();
    if (source.size <= 0) throw new Error('EMPTY_PDF');
    const emptyBasic: BasicFields = {
      supplierName: '', supplierTrn: '', supplierInvoiceNumber: '', invoiceDate: '',
      purchaseOrderNumber: '', invoiceGross: '', currency: '',
    };
    const advanced = await runAdvanced(source, pages, options, emptyBasic, 0, requestId);
    trace('BACKEND_API_RESPONSE', { status: advanced.status });
    if (advanced.status === 'SKIPPED' || advanced.status === 'FAILED') {
      throw new Error(advanced.errorMessage || (advanced.status === 'SKIPPED' ? 'ADVANCED_OCR_SKIPPED' : 'ADVANCED_OCR_FAILED'));
    }
    return resultFromAdvanced(advanced, pages, requestId, parseMobileBasicFields(advanced.rawText ?? ''));
  }
  const pageTexts: Array<{ id: string; text: string }> = [];
  let available = true;
  let error: string | undefined;

  for (const [index, page] of pages.entries()) {
    const pageNumber = index + 1;
    onProgress?.({ page: pageNumber, total: pages.length });
    if (__DEV__) {
      console.log('[OCR] START PAGE', pageNumber);
      console.log('[OCR] IMAGE URI:', page.processedImageUri.split(':')[0] || 'local');
    }
    try {
      const result = await recognizePageText(page.processedImageUri);
      const text = typeof result?.text === 'string' ? result.text : '';
      const pageAvailable = result?.available === true;
      pageTexts.push({ id: page.id, text });
      available = available && pageAvailable;
      if (__DEV__) {
        console.log('[OCR] TEXT LENGTH:', text.length);
        console.log('[OCR] COMPLETE PAGE', pageNumber);
      }
    } catch (caught) {
      available = false;
      error = caught instanceof Error ? caught.message : String(caught);
      pageTexts.push({ id: page.id, text: '' });
      if (__DEV__) console.warn('[OCR] PAGE FAILED', pageNumber, error);
    }
  }

  const rawText = combinePageOcrText(pageTexts);
  const nativeBasic = parseMobileBasicFields(rawText);
  const mobileEvaluation = evaluateBasicSufficiency(nativeBasic);
  trace('MOBILE_RESULT', {
    available,
    status: extractionStatus(nativeBasic, rawText),
    fieldCount: Object.values(nativeBasic).filter(Boolean).length,
    missingFields: mobileEvaluation.missingFields,
  });
  const shouldUseAdvanced = Boolean(options?.pdfUri && options.organizationId && rawText.trim());
  if (shouldUseAdvanced && options?.pdfUri && options.organizationId) {
    try {
      options.onFallbackStart?.(requestId);
      trace('FALLBACK_TRIGGERED', { trigger: 'AUTO_FALLBACK', reason: mobileEvaluation.missingFields.length ? 'MISSING_REQUIRED_FIELDS' : 'SERVER_POLICY_CHECK' });
      const source = await (await fetch(options.pdfUri)).blob();
      const advanced = await runAdvanced(source, pages, options, nativeBasic, basicConfidence(nativeBasic), requestId);
      trace('BACKEND_API_RESPONSE', { status: advanced.status });
      if (advanced.status === 'SKIPPED') {
        const skippedDiagnostics: OcrDiagnostics = {
          requestId,
          mobileStatus: extractionStatus(nativeBasic, rawText),
          mobileFields: Object.keys(nativeBasic).filter(key => Boolean(nativeBasic[key as keyof BasicFields])),
          fallbackDecision: 'SKIPPED',
          fallbackTrigger: advanced.trigger,
          finalStatus: extractionStatus(nativeBasic, rawText),
          finalMissingFields: mobileEvaluation.missingFields,
          effectiveConfiguration: advanced.effectiveConfiguration ? {
             automaticBackendFallbackEnabled: advanced.effectiveConfiguration.automaticBackendFallbackEnabled ?? false,
             minimumMobileConfidence: advanced.effectiveConfiguration.minimumMobileConfidence ?? 0,
             version: advanced.effectiveConfiguration.version ?? 0,
          } : undefined,
          mergeStatus: 'MOBILE_RESULT_RETAINED',
        };
        trace('MERGE_COMPLETE', { source: 'MOBILE_RESULT_RETAINED', finalStatus: skippedDiagnostics.finalStatus });
        return { pageTexts, rawText, basic: nativeBasic, available, status: extractionStatus(nativeBasic, rawText), error, advanced, diagnostics: skippedDiagnostics };
      }
      if (advanced.status === 'FAILED') {
        throw new Error(advanced.errorMessage || 'ADVANCED_OCR_FAILED');
      }
      const advancedResult = resultFromAdvanced(advanced, pages, requestId, nativeBasic);
      trace('FINAL_EVALUATION', { status: advancedResult.status, missingFields: advancedResult.diagnostics?.finalMissingFields });
      return advancedResult;
    } catch (caught) {
      trace('BACKEND_API_FAILURE', { message: caught instanceof Error ? caught.message : String(caught) });
      if (__DEV__) console.warn('[ADVANCED-OCR] FALLBACK_FAILED', caught instanceof Error ? caught.message : String(caught));
    }
  }
  if (available && rawText.trim()) {
    const status = extractionStatus(nativeBasic, rawText);
     if (__DEV__) {
      console.log('[BASIC-OCR] PROVIDER_SELECTED', 'NATIVE_ML_KIT');
      console.log('[BASIC-OCR] RAW_TEXT_LENGTH', rawText.length);
      console.log('[BASIC-OCR] PARSER_RESULT', extractionStatus(nativeBasic, rawText));
      console.log('[BASIC-OCR] RESPONSE_RECEIVED');
      console.log('[BASIC-OCR] FORM_POPULATED');
    }
    return {
      pageTexts,
      rawText,
      basic: nativeBasic,
      available: true,
      status: extractionStatus(nativeBasic, rawText),
       error,
       diagnostics: { requestId, mobileStatus: status, finalStatus: status, mergeStatus: 'MOBILE_RESULT_RETAINED' },
    };
  }

  if (!options?.pdfUri || !options.organizationId) {
    const status = extractionStatus(nativeBasic, rawText);
    if (__DEV__) {
      console.warn('[BASIC-OCR] FAILED_STAGE', 'PROVIDER_SELECTION');
      console.warn('[BASIC-OCR] ERROR', error ?? 'BACKEND_OCR_SCOPE_UNAVAILABLE');
    }
    return { pageTexts, rawText, basic: nativeBasic, available: false, status, error: error ?? 'BACKEND_OCR_SCOPE_UNAVAILABLE' };
  }

  let failedStage = 'UPLOAD_STARTED';
  try {
     if (__DEV__) console.log('[BASIC-OCR] PROVIDER_SELECTED', 'BACKEND_BASIC_OCR');
    failedStage = 'PDF_READ';
    const source = await (await fetch(options.pdfUri)).blob();
    if (source.size <= 0) throw new Error('EMPTY_PDF');
    if (__DEV__) {
      console.log('[BASIC-OCR] PDF_SIZE_BYTES', source.size);
      console.log('[BASIC-OCR] UPLOAD_STARTED');
    }
    failedStage = 'BACKEND_REQUEST';
    const response = await extractBasicInvoice({
      organizationId: options.organizationId,
      operatingUnitId: options.operatingUnitId ?? null,
      pageCount: pages.length,
       ocrRequestId: requestId,
      file: new Blob([source], { type: 'application/pdf' }),
    });
    if (__DEV__) console.log('[BASIC-OCR] RESPONSE_RECEIVED');
    const backendText = response.rawText ?? '';
    const backendPageTexts = pages.map((page, index) => ({ id: page.id, text: index === 0 ? backendText : '' }));
    const basic = mergeBasicFields({
      supplierName: formatOcrSupplierDisplay(response.supplierName ?? ''),
      supplierTrn: response.supplierTrn ?? '',
      supplierInvoiceNumber: response.supplierInvoiceNumber ?? '',
      invoiceDate: response.invoiceDate ?? '',
      purchaseOrderNumber: response.purchaseOrderNumber ?? '',
      invoiceGross: response.invoiceGross === null || response.invoiceGross === undefined ? '' : String(response.invoiceGross),
      currency: response.currency ?? '',
    }, parseMobileBasicFields(backendText));
    const status: OcrExtractionStatus = response.status === 'SUCCESS' || response.status === 'PARTIAL' || response.status === 'FAILED'
      ? response.status
      : extractionStatus(basic, backendText);
    if (__DEV__) {
      console.log('[BASIC-OCR] RAW_TEXT_LENGTH', backendText.length);
      console.log('[BASIC-OCR] PARSER_RESULT', status);
      console.log('[BASIC-OCR] FORM_POPULATED');
    }
    const basicResult: OcrResult = {
      pageTexts: backendPageTexts,
      rawText: backendText,
      basic,
      available: response.available,
      status,
      error: status === 'FAILED' ? 'BACKEND_OCR_RETURNED_NO_TEXT' : undefined,
       diagnostics: { requestId, mobileStatus: status, finalStatus: status, mergeStatus: 'BACKEND_BASIC_APPLIED' },
    };
     if (options.organizationId) {
      try {
         options.onFallbackStart?.(requestId);
         trace('FALLBACK_TRIGGERED', { trigger: options.forceAdvanced ? 'MANUAL_REREAD' : 'AUTO_FALLBACK', reason: options.forceAdvanced ? 'FORCED_REREAD' : 'BACKEND_BASIC_RESULT' });
         const advanced = await runAdvanced(source, pages, options, basic, response.confidence ?? basicConfidence(basic), requestId);
         trace('BACKEND_API_RESPONSE', { status: advanced.status });
         if (advanced.status !== 'SKIPPED') return resultFromAdvanced(advanced, pages, requestId);
         const skippedDiagnostics: OcrDiagnostics = {
           ...basicResult.diagnostics,
           requestId,
           fallbackDecision: 'SKIPPED',
           fallbackTrigger: advanced.trigger,
           effectiveConfiguration: advanced.effectiveConfiguration ? {
             automaticBackendFallbackEnabled: advanced.effectiveConfiguration.automaticBackendFallbackEnabled ?? false,
             minimumMobileConfidence: advanced.effectiveConfiguration.minimumMobileConfidence ?? 0,
             version: advanced.effectiveConfiguration.version ?? 0,
           } : undefined,
         };
         return { ...basicResult, advanced, diagnostics: skippedDiagnostics };
      } catch (caught) {
         trace('BACKEND_API_FAILURE', { message: caught instanceof Error ? caught.message : String(caught) });
        if (__DEV__) console.warn('[ADVANCED-OCR] FALLBACK_FAILED', caught instanceof Error ? caught.message : String(caught));
      }
    }
    return basicResult;
  } catch (caught) {
    const backendError = caught instanceof Error ? caught.message : String(caught);
    if (__DEV__) {
      console.warn('[BASIC-OCR] FAILED_STAGE', failedStage);
      console.warn('[BASIC-OCR] ERROR', backendError);
    }
    return {
      pageTexts,
      rawText,
      basic: nativeBasic,
      available: false,
      status: 'FAILED',
       error: error ? `${error}; ${backendError}` : backendError,
       diagnostics: { requestId, fallbackDecision: 'FAILED', finalStatus: 'FAILED' },
    };
  }
}
