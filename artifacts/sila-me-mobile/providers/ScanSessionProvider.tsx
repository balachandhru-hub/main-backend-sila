/**
 * PROTECTED SILA INVOICE RECEIVING FLOW — See docs/PROTECTED_INVOICE_FLOW.md.
 */
import AsyncStorage from '@react-native-async-storage/async-storage';
import React, { createContext, useContext, useEffect, useMemo, useState } from 'react';
import { Platform } from 'react-native';
import {
  addPage,
  completeSession,
  createScanSession,
  movePage,
  normalizeSession,
  removePage,
  replacePage,
  setPdf,
  type BasicFields,
  type OcrDiagnostics,
  type OcrResult,
  type OcrStatus,
  type ScanPage,
  type ScanSession,
  evaluateBasicSufficiency,
  createOcrRequestId,
} from './scan-session';
import { isEphemeralBrowserUri } from '../services/image-uri';
import { deleteScanSessionPdf } from '../services/scan-pdf-cache';
import { formatOcrSupplierDisplay } from '../services/invoice-review-state';
import * as FileSystem from 'expo-file-system/legacy';
export type { BasicFields, ScanPage, ScanSession } from './scan-session';
const ScanContext = createContext<null | {
  session: ScanSession; addPage: (page: ScanPage) => void; replacePage: (id: string, page: ScanPage) => void;
  removePage: (id: string) => void; movePage: (id: string, direction: -1 | 1) => void;
  updateBasic: (basic: Partial<BasicFields>) => void; setPdf: (uri: string, size?: number) => void;
  setOcrText: (text: string) => void;
  setOcrStatus: (status: OcrStatus, page?: number, error?: string) => void;
  setOcrResult: (result: OcrResult) => void;
  setSaving: (saving: boolean) => void; setSaved: (invoiceId: string) => void; reset: () => void;
}>(null);
const STORAGE_KEY = 'sila-me-active-scan-session';
const isBrowserRuntime = Platform.OS === 'web' || typeof window !== 'undefined';

function hasEphemeralBrowserUris(value: ScanSession) {
  return value.pages.some(page => (
    isEphemeralBrowserUri(page.originalImageUri)
    || isEphemeralBrowserUri(page.processedImageUri)
    || isEphemeralBrowserUri(page.thumbnailUri)
  )) || isEphemeralBrowserUri(value.pdfUri);
}

function cleanupSessionPdf(value: ScanSession) {
  void deleteScanSessionPdf(
    value.id,
    value.pdfUri,
    uri => FileSystem.deleteAsync(uri, { idempotent: true }),
  ).catch(error => {
    if (__DEV__) console.warn('[SCAN-PDF-CLEANUP]', error);
  });
}

export function ScanSessionProvider({ children }: { children: React.ReactNode }) {
  const [session, setSession] = useState<ScanSession>(createScanSession);
  const [hydrated, setHydrated] = useState(false);
  useEffect(() => {
    AsyncStorage.getItem(STORAGE_KEY)
      .then(async value => {
        if (!value) return;
        const restored = normalizeSession(JSON.parse(value));
        if (isBrowserRuntime && hasEphemeralBrowserUris(restored)) {
          await AsyncStorage.removeItem(STORAGE_KEY);
          return;
        }
        setSession(restored);
      })
      .finally(() => setHydrated(true));
  }, []);
  useEffect(() => {
    if (!hydrated) return;
    if (session.pages.length && !(isBrowserRuntime && hasEphemeralBrowserUris(session))) {
      void AsyncStorage.setItem(STORAGE_KEY, JSON.stringify(session));
    }
    else void AsyncStorage.removeItem(STORAGE_KEY);
  }, [hydrated, session]);
  const value = useMemo(() => ({
    session,
    addPage: (page: ScanPage) => {
      setSession(current => addPage(current, page));
      cleanupSessionPdf(session);
    },
    replacePage: (id: string, page: ScanPage) => {
      setSession(current => replacePage(current, id, page));
      cleanupSessionPdf(session);
    },
    removePage: (id: string) => {
      setSession(current => removePage(current, id));
      cleanupSessionPdf(session);
    },
    movePage: (id: string, direction: -1 | 1) => {
      const index = session.pages.findIndex(page => page.id === id);
      const canMove = index >= 0 && index + direction >= 0 && index + direction < session.pages.length;
      setSession(current => movePage(current, id, direction));
      if (canMove) cleanupSessionPdf(session);
    },
      updateBasic: (basic: Partial<BasicFields>) => setSession(current => {
        const nextBasic = {
          ...current.basic,
          ...basic,
          supplierName: formatOcrSupplierDisplay(basic.supplierName ?? current.basic.supplierName),
        };
        return {
          ...current,
          basic: nextBasic,
          basicExtraction: { ...current.basicExtraction, ...nextBasic },
          manualFields: Array.from(new Set([...current.manualFields, ...Object.keys(basic)])),
        };
      }),
    setPdf: (pdfUri: string, pdfSize?: number) => setSession(current => setPdf(current, pdfUri, pdfSize)),
    setOcrText: (ocrText: string) => setSession(current => ({ ...current, ocrText })),
     setOcrStatus: (ocrStatus: OcrStatus, ocrPage?: number, ocrError?: string) => setSession(current => ({ ...current, ocrStatus, ocrPage, ocrError })),
      setOcrResult: (result: OcrResult) => setSession(current => {
        const normalizedBasic = {
          ...result.basic,
          supplierName: formatOcrSupplierDisplay(result.basic.supplierName),
        };
        const evaluation = evaluateBasicSufficiency(normalizedBasic, result.advanced?.effectiveConfiguration);
        const finalStatus = result.status === 'FAILED' ? 'FAILED' : evaluation.status;
        const diagnostics: OcrDiagnostics = {
          ...result.diagnostics,
          requestId: result.diagnostics?.requestId ?? current.ocrRequestId ?? createOcrRequestId(),
          finalStatus,
          finalMissingFields: evaluation.missingFields,
          mergeStatus: 'FINAL_MERGED_RESULT_EVALUATED',
        };
        if (__DEV__) console.log('[OCR_TRACE]', {
          requestId: diagnostics.requestId ?? current.ocrRequestId,
          event: 'FINAL_MERGED_RESULT_EVALUATED',
          status: finalStatus,
          missingFields: evaluation.missingFields,
        });
        return {
          ...current,
          pages: current.pages.map(page => ({
            ...page,
            ocrText: result.pageTexts.find(item => item.id === page.id)?.text ?? '',
          })),
          ocrText: result.rawText,
          basic: { ...normalizedBasic },
          basicExtraction: { ...normalizedBasic },
          ocrLines: result.advanced?.lines?.length
            ? result.advanced.lines.map(line => ({
            lineNumber: line.lineNumber,
            description: String(line.description?.value ?? ''),
            quantity: typeof line.quantity?.value === 'number' ? line.quantity.value : Number(line.quantity?.value) || null,
            uom: line.uom?.value == null ? null : String(line.uom.value),
            unitPrice: typeof line.unitPrice?.value === 'number' ? line.unitPrice.value : Number(line.unitPrice?.value) || null,
            lineAmount: typeof line.grossAmount?.value === 'number'
              ? line.grossAmount.value
              : Number(line.grossAmount?.value ?? line.netAmount?.value) || null,
            netAmount: typeof line.netAmount?.value === 'number' ? line.netAmount.value : Number(line.netAmount?.value) || null,
            taxAmount: typeof line.taxAmount?.value === 'number' ? line.taxAmount.value : Number(line.taxAmount?.value) || null,
            materialCode: line.supplierItemCode?.value == null ? null : String(line.supplierItemCode.value),
            poItemNumber: line.poItemNumber?.value == null ? null : String(line.poItemNumber.value),
          }))
            : current.ocrLines,
          manualFields: current.manualFields,
          ocrAvailable: result.available,
          ocrError: result.error,
          ocrPage: undefined,
          ocrDiagnostics: diagnostics,
          ocrRequestId: diagnostics.requestId,
          ocrStatus: finalStatus === 'SUCCESS' ? 'complete' : finalStatus === 'PARTIAL' ? 'partial' : 'unavailable',
        };
      }),
    setSaving: (saving: boolean) => setSession(current => ({ ...current, saving })),
     setSaved: (savedInvoiceId: string) => {
       setSession(() => completeSession(savedInvoiceId));
       cleanupSessionPdf(session);
       void AsyncStorage.removeItem(STORAGE_KEY);
     },
     reset: () => {
       setSession(createScanSession());
       cleanupSessionPdf(session);
       void AsyncStorage.removeItem(STORAGE_KEY);
     },
  }), [session]);
  return <ScanContext.Provider value={value}>{children}</ScanContext.Provider>;
}
export function useScanSession() { const value = useContext(ScanContext); if (!value) throw new Error('ScanSessionProvider is missing'); return value; }