/**
 * PROTECTED SILA INVOICE RECEIVING FLOW
 * See docs/PROTECTED_INVOICE_FLOW.md.
 * Scan/Upload → OCR → Supplier Master → Open POs → Invoice Review → Save and Continue → Finalize GRN.
 * Do not modify this contract/behavior from unrelated feature work.
 */
import { useLocalSearchParams, useRouter } from 'expo-router';
import { Feather } from '@expo/vector-icons';
import React, { useEffect, useMemo, useRef, useState } from 'react';
import { ActivityIndicator, Alert, Linking, Modal, Pressable, ScrollView, StyleSheet, Text, TextInput, View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { getGetAccessContextQueryKey, getGetInvoiceBasicExtractionQueryKey, getGetInvoiceQueryKey, getGetPurchaseOrderQueryKey, getListSuppliersQueryKey, getSearchPurchaseOrdersQueryKey, uploadInvoiceDocument, useGetAccessContext, useGetInvoice, useGetInvoiceBasicExtraction, useGetPurchaseOrder, useListSuppliers, useSearchPurchaseOrders, useUpdateInvoice, type PurchaseOrder, type Supplier } from '@workspace/api-client-react';
import { ReceiveShell, HeaderAction, PrimaryButton, StatusPill, formatQuantity } from '@/components/receive-ui';
import { useColors } from '@/hooks/useColors';
import { useScanSession } from '@/providers/ScanSessionProvider';
import { evaluateBasicSufficiency } from '@/providers/scan-session';
import { useAuth } from '@/providers/AuthProvider';
import { useStoreScope } from '@/providers/StoreScopeProvider';
import { recognizeInvoicePages } from '@/services/mobile-ocr';
import { parseMobileAmountExtras } from '@/services/invoice-parser';
import { mobileTenant } from '@/config/mobile-tenant';
import {
  autoSelectPurchaseOrder,
  extractOcrSupplierCandidate,
  findOcrPurchaseOrder,
  findUniqueSupplierMatch,
  formatPurchaseOrderMatch,
  invoiceQuantityForPoItem,
  mergeOpenPurchaseOrders,
  normalizePurchaseOrderNumber,
  ocrSupplierScreenValue,
  persistedSupplierName,
  isSupplierIdLabel,
  isSupplierNameNoise,
  mergeRereadBasicFields,
  invoiceReviewOcrBanner,
  openPoLookupEntityCode,
  rankOpenPurchaseOrders,
  resolveCanonicalCustomerOrganization,
  shouldRenderPurchaseOrderSelector,
  toAuthoritativeSupplier,
  validatePurchaseOrderSelection,
} from '@/services/invoice-review-state';
import {
  performInvoiceReviewSave,
  readInvoicePdfBlob,
  resolveInvoiceOcrScope,
} from '@/services/invoice-review-save';

function formatPoOption(po: PurchaseOrder) {
  const date = po.poDate ? new Date(`${po.poDate}T00:00:00`).toLocaleDateString(undefined, { day: '2-digit', month: 'short', year: 'numeric' }) : 'No date';
  const amount = po.totalAmount != null ? `${po.currency} ${Number(po.totalAmount).toLocaleString()}` : po.currency;
  const openItems = po.items.filter(item => item.goodsReceiptExpected && !item.deletionIndicator && !item.deliveryCompleted && Number(item.openQuantity) > 0).length;
  return `${date} • Company ${po.companyCode ?? '—'} • ${amount} • ${openItems} open items`;
}

export default function InvoiceReview() {
  const colors = useColors(); const router = useRouter(); const { user } = useAuth(); const { invoiceId, duplicate } = useLocalSearchParams<{ invoiceId?: string; duplicate?: string }>();
  const { session, updateBasic, setSaving, setSaved, setOcrStatus, setOcrResult } = useScanSession(); const isScanner = !invoiceId;
  const isDuplicateReview = duplicate === '1';
  const { organization: scopedOrganization, unit: scopedUnit, units: scopedUnits } = useStoreScope();
  const context = useGetAccessContext({ query: { queryKey: getGetAccessContextQueryKey(), enabled: Boolean(user && isScanner), retry: false } });
  const invoice = useGetInvoice(invoiceId ?? '', { query: { queryKey: getGetInvoiceQueryKey(invoiceId ?? ''), enabled: Boolean(invoiceId), retry: false } });
  const basic = useGetInvoiceBasicExtraction(invoiceId ?? '', { query: { queryKey: getGetInvoiceBasicExtractionQueryKey(invoiceId ?? ''), enabled: Boolean(invoiceId), retry: false } });
  const updateInvoice = useUpdateInvoice(); const [fields, setFields] = useState({ ...session.basic, supplierName: ocrSupplierScreenValue(session.basic.supplierName) });
  const [infoOpen, setInfoOpen] = useState(false);
  const [rereading, setRereading] = useState(false);
  const insets = useSafeAreaInsets();
  const [saveMessage, setSaveMessage] = useState<string | null>(null);
  const [duplicateInvoiceId, setDuplicateInvoiceId] = useState<string | null>(null);
  const [supplierQuery, setSupplierQuery] = useState('');
  const [debouncedSupplierQuery, setDebouncedSupplierQuery] = useState('');
  const [poQuery, setPoQuery] = useState('');
  const [selectedSupplierId, setSelectedSupplierId] = useState<string | null>(null);
  const [selectedSupplier, setSelectedSupplier] = useState<Supplier | null>(null);
  const [selectedPo, setSelectedPo] = useState<PurchaseOrder | null>(null);
  const [poPickerOpen, setPoPickerOpen] = useState(false);
  const [ocrPoNumber, setOcrPoNumber] = useState(session.basicExtraction.purchaseOrderNumber);
  const [ocrPoMatched, setOcrPoMatched] = useState(false);
  const seededSupplierSearch = useRef(false);
  const autoMatchedSupplier = useRef(false);
  const autoMatchedPo = useRef(false);
  const saveLock = useRef(false);
  const canonicalOrganization = resolveCanonicalCustomerOrganization(
    context.data?.organizations ?? [],
    mobileTenant.tenantCode,
    mobileTenant.tenantName,
  );
  const organizationId = invoice.data?.organizationId ?? canonicalOrganization?.id ?? scopedOrganization?.id;
  useEffect(() => {
    const handle = setTimeout(() => setDebouncedSupplierQuery(supplierQuery.trim()), 300);
    return () => clearTimeout(handle);
  }, [supplierQuery]);
  const supplierSearch = useListSuppliers(
    { organizationId: organizationId ?? '', query: debouncedSupplierQuery || undefined, status: 'ACTIVE' },
    { query: { queryKey: getListSuppliersQueryKey({ organizationId: organizationId ?? '', query: debouncedSupplierQuery || undefined, status: 'ACTIVE' }), enabled: Boolean(organizationId && debouncedSupplierQuery.length >= 2), retry: false } },
  );
  const selectedSupplierLookup = useListSuppliers(
    { organizationId: organizationId ?? '', query: invoice.data?.supplierCode || undefined, status: 'ACTIVE' },
    { query: { queryKey: getListSuppliersQueryKey({ organizationId: organizationId ?? '', query: invoice.data?.supplierCode || undefined, status: 'ACTIVE' }), enabled: Boolean(organizationId && selectedSupplierId && !selectedSupplier && invoice.data?.supplierCode), retry: false } },
  );
  const poEntityCode = openPoLookupEntityCode(selectedSupplier?.entityCode);
  const ocrPoNormalized = normalizePurchaseOrderNumber(ocrPoNumber);
  const ocrPoLookupNumber = ocrPoNumber.trim() || ocrPoNormalized;
  const openPos = useSearchPurchaseOrders(
    { organizationId, supplierId: selectedSupplierId, openOnly: true },
    { query: { queryKey: getSearchPurchaseOrdersQueryKey({ organizationId, supplierId: selectedSupplierId, openOnly: true }), enabled: Boolean(selectedSupplierId && organizationId), retry: false } },
  );
  const ocrPoLookup = useGetPurchaseOrder(
    ocrPoLookupNumber,
    { organizationId },
    { query: { queryKey: getGetPurchaseOrderQueryKey(ocrPoLookupNumber, { organizationId }), enabled: Boolean(ocrPoLookupNumber && selectedSupplierId && organizationId), retry: false } },
  );
  const eligibleOpenPos = useMemo(
    () => mergeOpenPurchaseOrders(
      openPos.data ?? [],
      ocrPoLookup.data
        && (!selectedSupplierId || !ocrPoLookup.data.supplierId || ocrPoLookup.data.supplierId === selectedSupplierId)
        ? ocrPoLookup.data
        : null,
    ),
    [ocrPoLookup.data, openPos.data, selectedSupplierId],
  );
  useEffect(() => {
    if (isScanner) {
      setFields({ ...session.basicExtraction, supplierName: ocrSupplierScreenValue(session.basicExtraction.supplierName) });
      setOcrPoNumber(session.basicExtraction.purchaseOrderNumber);
    } else if (basic.data || invoice.data) {
      setFields(current => ({
        ...current,
        supplierName: ocrSupplierScreenValue(basic.data?.supplierName ?? invoice.data?.supplierName ?? ''),
        supplierTrn: basic.data?.supplierTrn ?? invoice.data?.supplierTaxNumber ?? '',
        supplierInvoiceNumber: basic.data?.supplierInvoiceNumber ?? invoice.data?.invoiceNumber ?? '',
        invoiceDate: basic.data?.invoiceDate ?? invoice.data?.invoiceDate ?? '',
        purchaseOrderNumber: basic.data?.purchaseOrderNumber ?? invoice.data?.purchaseOrderNumber ?? '',
        invoiceGross: String(basic.data?.invoiceGross ?? invoice.data?.grossAmount ?? ''),
        currency: basic.data?.currency ?? invoice.data?.currency ?? '',
      }));
      setOcrPoNumber(basic.data?.purchaseOrderNumber ?? invoice.data?.purchaseOrderNumber ?? '');
      setSelectedSupplierId(invoice.data?.supplierId ?? null);
      setSelectedSupplier(current => current?.id === invoice.data?.supplierId ? current : null);
    }
  }, [isScanner, session.basicExtraction, basic.data, invoice.data]);
  useEffect(() => {
    if (seededSupplierSearch.current || selectedSupplierId) return;
    const candidate = extractOcrSupplierCandidate(fields.supplierName);
    const seed = candidate.supplierId || ocrSupplierScreenValue(fields.supplierName) || fields.supplierTrn;
    if (seed.trim().length >= 2) {
      seededSupplierSearch.current = true;
      setSupplierQuery(seed);
    }
  }, [fields.supplierName, fields.supplierTrn, selectedSupplierId]);
  useEffect(() => {
    if (selectedSupplier || !selectedSupplierId || !selectedSupplierLookup.data?.length) return;
    const match = selectedSupplierLookup.data.find(item => item.id === selectedSupplierId)
      ?? selectedSupplierLookup.data.find(item => item.supplierCode === invoice.data?.supplierCode);
    if (match) setSelectedSupplier(match);
  }, [invoice.data?.supplierCode, selectedSupplier, selectedSupplierId, selectedSupplierLookup.data]);
  useEffect(() => {
    if (autoMatchedSupplier.current || selectedSupplierId || !supplierSearch.data?.length) return;
    const exact = findUniqueSupplierMatch(fields.supplierName, supplierSearch.data, fields.supplierTrn);
    if (exact) {
      autoMatchedSupplier.current = true;
      setSelectedSupplierId(exact.id);
      setSelectedSupplier(exact);
      setSupplierQuery('');
      if (!fields.supplierTrn.trim() && (exact.trn || exact.taxNumber)) {
        setFields(current => ({ ...current, supplierTrn: exact.trn ?? exact.taxNumber ?? '' }));
      }
    }
  }, [fields.supplierName, fields.supplierTrn, selectedSupplierId, supplierSearch.data]);
  useEffect(() => {
    if (!selectedSupplierId || eligibleOpenPos.length === 0 || !organizationId) return;
    const exact = findOcrPurchaseOrder(ocrPoNumber, eligibleOpenPos, selectedSupplierId, organizationId, poEntityCode);
    if (exact) {
      autoMatchedPo.current = true;
      setSelectedPo(current => current?.id === exact.id ? current : exact);
      setOcrPoMatched(true);
      setFields(current => current.purchaseOrderNumber === exact.poNumber ? current : { ...current, purchaseOrderNumber: exact.poNumber });
      return;
    }
    if (autoMatchedPo.current) return;
    const ranked = rankOpenPurchaseOrders(
      eligibleOpenPos,
      selectedSupplierId,
      organizationId,
      poEntityCode,
      session.ocrLines,
      fields.invoiceGross,
      fields.currency,
      ocrPoNumber,
    );
    const auto = autoSelectPurchaseOrder(ranked);
    if (auto) {
      autoMatchedPo.current = true;
      setSelectedPo(auto);
      setOcrPoMatched(
        ranked.find(item => item.po.id === auto.id)?.band === 'exact'
        || normalizePurchaseOrderNumber(auto.poNumber) === normalizePurchaseOrderNumber(ocrPoNumber),
      );
      setFields(current => ({ ...current, purchaseOrderNumber: auto.poNumber }));
      return;
    }
    setOcrPoMatched(false);
    setSelectedPo(current => current && !eligibleOpenPos.some(item => item.id === current.id) ? null : current);
  }, [eligibleOpenPos, fields.currency, fields.invoiceGross, ocrPoNumber, organizationId, poEntityCode, selectedSupplierId, session.ocrLines]);
  const change = (key: keyof typeof fields, value: string) => {
    setSaveMessage(null);
    setFields(current => ({ ...current, [key]: value }));
    if (isScanner) updateBasic({ [key]: value });
  };
  const chooseSupplier = (supplier: Supplier) => {
    setSaveMessage(null);
    setSelectedSupplierId(supplier.id);
    setSelectedSupplier(supplier);
    setSupplierQuery('');
    autoMatchedPo.current = false;
    setSelectedPo(null);
    if (!fields.supplierTrn.trim() && (supplier.trn || supplier.taxNumber)) change('supplierTrn', supplier.trn ?? supplier.taxNumber ?? '');
  };
  const choosePo = (po: PurchaseOrder) => {
    if (!selectedSupplierId || !organizationId) return;
    const selectionError = validatePurchaseOrderSelection(
      po,
      selectedSupplierId,
      organizationId,
      poEntityCode,
    );
    if (selectionError) {
      setSaveMessage(selectionError);
      setInfoOpen(true);
      return;
    }
    setSelectedPo(po);
    setPoPickerOpen(false);
    autoMatchedPo.current = true;
    setOcrPoMatched(normalizePurchaseOrderNumber(po.poNumber) === normalizePurchaseOrderNumber(ocrPoNumber));
    change('purchaseOrderNumber', po.poNumber);
  };
  const rereadInvoice = async () => {
    if (!isScanner || rereading) return;
    if (!session.pdfUri && !session.pages.length) {
      Alert.alert('Re-read failed', 'The scanned document is not available to re-read.');
      return;
    }
    setRereading(true);
    setOcrStatus('extracting', session.pages.length);
    try {
      const access = context.data ?? (await context.refetch()).data;
      const ocrScope = resolveInvoiceOcrScope(
        access?.organizations ?? [],
        access?.units ?? scopedUnits,
        mobileTenant.tenantCode,
        mobileTenant.tenantName,
        { organization: scopedOrganization ?? canonicalOrganization, unit: scopedUnit },
      );
      const organizationIdForOcr = ocrScope.organizationId
        ?? scopedOrganization?.id
        ?? canonicalOrganization?.id
        ?? access?.organizations[0]?.id;
      if (!session.pdfUri) throw new Error('REREAD_DOCUMENT_MISSING');
      if (!organizationIdForOcr) throw new Error('BACKEND_OCR_SCOPE_UNAVAILABLE');
      const result = await recognizeInvoicePages(session.pages, progress => setOcrStatus('extracting', progress.page), {
        pdfUri: session.pdfUri,
        organizationId: organizationIdForOcr,
        forceAdvanced: true,
        onFallbackStart: requestId => {
          setOcrStatus('extracting', session.pages.length);
          if (__DEV__) console.log('[OCR_TRACE]', { requestId, event: 'FALLBACK_LOADING_STATE', trigger: 'MANUAL_REREAD' });
        },
      });
      if (result.status === 'FAILED') throw new Error(result.error || 'ADVANCED_OCR_FAILED');
      const rereadBasic = mergeRereadBasicFields(result.basic, session.basic, session.manualFields);
      setOcrResult({ ...result, basic: rereadBasic });
      setFields({ ...rereadBasic, supplierName: ocrSupplierScreenValue(rereadBasic.supplierName) });
      setOcrPoNumber(rereadBasic.purchaseOrderNumber || session.basicExtraction.purchaseOrderNumber);
      autoMatchedPo.current = false;
    } catch (error) {
      const message = error instanceof Error ? error.message : String(error);
      const extractedCount = Object.values(session.basicExtraction).filter((value) => String(value).trim()).length;
      setOcrStatus(extractedCount > 0 ? 'partial' : 'failed', undefined, extractedCount > 0 ? undefined : message);
      Alert.alert('Re-read failed', message);
    } finally {
      setRereading(false);
    }
  };
  const viewDocument = () => {
    if (!session.pdfUri) return;
    void Linking.openURL(session.pdfUri).catch(() => Alert.alert('Document unavailable', 'The scanned PDF could not be opened.'));
  };
  const openExistingInvoice = () => {
    if (!duplicateInvoiceId) return;
    router.push({ pathname: '/receive/invoice-review', params: { invoiceId: duplicateInvoiceId, duplicate: '1' } });
  };
  const saveScanner = async () => {
    if (saveLock.current || session.saving) return;
    saveLock.current = true;
    const organization = scopedOrganization ?? canonicalOrganization ?? context.data?.organizations[0] ?? null;
    setSaveMessage(null);
    setDuplicateInvoiceId(null);
    setSaving(true);
    try {
      const outcome = await performInvoiceReviewSave({
        saving: false,
        fields,
        selectedSupplier,
        selectedSupplierId,
        selectedPo,
        noPurchaseOrder: false,
        organization,
        pdfUri: session.pdfUri,
        pageCount: Math.max(session.pages.length, session.pdfUri ? 1 : 0),
        scanSessionId: session.id,
        ocrRequestId: session.ocrRequestId,
        manualFields: session.manualFields,
        invoiceLinesJson: session.ocrLines.length ? JSON.stringify(session.ocrLines) : null,
        idempotencyKey: `mobile-save-${session.id}`,
        upload: (request, options) => uploadInvoiceDocument(request, options),
        readPdf: uri => readInvoicePdfBlob(uri),
      });
      if (outcome.kind === 'ignored' || outcome.kind === 'blocked') {
        setSaving(false);
        saveLock.current = false;
        if (outcome.kind === 'blocked') {
          setSaveMessage(outcome.blockers.join(' · '));
        }
        return;
      }
      if (outcome.kind === 'error') {
        setDuplicateInvoiceId(outcome.duplicateInvoiceId ?? null);
        setSaveMessage(outcome.message);
        return;
      }
      router.replace(outcome.navigation);
      setSaved(outcome.document.invoiceId);
    } catch (error) {
      setSaveMessage(error instanceof Error ? error.message : 'The invoice could not be saved.');
    } finally {
      saveLock.current = false;
      setSaving(false);
    }
  };
  const continueExisting = () => {
    if (!invoice.data || updateInvoice.isPending) return;
    if (!selectedSupplierId && !invoice.data.supplierId) {
      setSaveMessage('Select the supplier from Supplier Master before continuing.');
      return;
    }
    const validation = evaluateBasicSufficiency(fields, { requirePurchaseOrderNumber: true });
    if (validation.missingFields.length > 0) {
      setSaveMessage(`Complete the required fields: ${validation.missingFields.join(', ')}.`);
      return;
    }
    setSaveMessage(null);
     updateInvoice.mutate({ id: invoice.data.id, data: { invoiceNumber: fields.supplierInvoiceNumber.trim(), invoiceDate: fields.invoiceDate || null, supplierName: persistedSupplierName(fields.supplierName, selectedSupplier) || null, supplierTaxNumber: fields.supplierTrn.trim() || null, poNumber: selectedPo?.poNumber ?? (fields.purchaseOrderNumber.trim() || null), noPurchaseOrder: false, supplierId: selectedSupplierId ?? invoice.data.supplierId ?? undefined, currency: fields.currency.trim() || null, grossAmount: fields.invoiceGross ? Number(fields.invoiceGross) : null } }, { onSuccess: () => router.push({ pathname: '/receive/finalize-grn', params: { invoiceId: invoice.data!.id, purchaseOrderId: selectedPo?.id ?? invoice.data!.purchaseOrderId ?? '', documentId: invoice.data!.documentId } }), onError: error => {
      const data = error && typeof error === 'object' && 'data' in error ? (error as { data?: unknown }).data : null;
      const apiError = data && typeof data === 'object' ? data as { code?: string; message?: string; missingFields?: string[] } : null;
      setSaveMessage(apiError?.missingFields?.length ? `Complete the required fields: ${apiError.missingFields.join(', ')}.` : apiError?.message ?? (error instanceof Error ? error.message : 'The invoice could not be saved. Please retry.'));
    } });
  };
  const leave = () => isScanner
    ? router.replace('/receive/document-preview')
    : router.replace('/receive');
  const diagnostics = session.ocrDiagnostics;
  const ocrHelpNote = 'OCR extracts invoice information. Supplier Master identifies the supplier. Eligible open POs are matched automatically when possible. Manual values are kept on Re-read. Save and Continue persists the invoice and opens Finalize GRN.';
  const ocrBanner = invoiceReviewOcrBanner(
    session.ocrStatus,
    isScanner ? session.basicExtraction : fields,
    session.ocrText,
  );
  const ocrStatusCopy = ocrBanner?.text ?? null;
  const ocrStatusTone = ocrBanner?.tone === 'error' ? colors.error : colors.mutedForeground;
  const amountExtras = parseMobileAmountExtras(session.ocrText ?? '');
  const infoSheet = (
      <Modal visible={infoOpen} animationType="slide" transparent onRequestClose={() => setInfoOpen(false)}>
        <View style={styles.infoOverlay}>
          <View style={[styles.infoSheet, { backgroundColor: colors.background, paddingBottom: insets.bottom + 16 }]}>
            <View style={[styles.infoHeader, { borderBottomColor: colors.border }]}>
              <Text style={[styles.infoTitle, { color: colors.foreground }]}>System messages</Text>
              <Pressable testID="ocr-info-close" accessibilityLabel="Close system messages" onPress={() => setInfoOpen(false)} hitSlop={12}>
                <Feather name="x" size={22} color={colors.foreground} />
              </Pressable>
            </View>
            <ScrollView contentContainerStyle={styles.infoBody}>
              <Text style={[styles.section, { color: colors.mutedForeground }]}>INVOICE RECEIVING · OCR THEN SUPPLIER THEN OPEN PO</Text>
              {isScanner && session.pdfUri ? (
                <Text style={[styles.pdfMeta, { color: colors.primary }]}>Original PDF ready · {session.pages.length} page{session.pages.length === 1 ? '' : 's'} · {session.pdfSize ? `${(session.pdfSize / 1024 / 1024).toFixed(2)} MB` : 'size pending'}</Text>
              ) : null}
              {isScanner && session.ocrStatus === 'reading' ? (
                <Text style={[styles.status, { color: colors.primary }]}>Reading page {session.ocrPage || 1} of {session.pages.length}…</Text>
              ) : null}
              {isScanner && session.ocrStatus === 'extracting' ? (
                <Text style={[styles.status, { color: colors.primary }]}>Improving invoice extraction…</Text>
              ) : null}
              {isScanner && ocrStatusCopy ? (
                <Text style={[styles.warning, { color: ocrStatusTone }]}>{ocrStatusCopy}</Text>
              ) : null}
              {saveMessage ? (
                <Text style={[styles.saveMessage, { color: colors.error }]}>{saveMessage}</Text>
              ) : null}
              {isDuplicateReview && !isScanner ? (
                <Text style={[styles.saveMessage, { color: colors.error }]}>The existing invoice from the duplicate warning could not be loaded. Go back to the scan to keep the original document; no new invoice was created.</Text>
              ) : null}
              {isScanner && diagnostics ? (
                <View style={[styles.diagnostics, { borderColor: colors.border }]}>
                  <Text style={[styles.diagnosticsTitle, { color: colors.mutedForeground }]}>OCR DIAGNOSTICS · DEVELOPMENT</Text>
                  <Text style={[styles.diagnosticsText, { color: colors.mutedForeground }]}>Request {diagnostics.requestId}</Text>
                  <Text style={[styles.diagnosticsText, { color: colors.mutedForeground }]}>Fallback {diagnostics.fallbackDecision ?? 'not requested'} · trigger {diagnostics.fallbackTrigger ?? 'native'}</Text>
                  <Text style={[styles.diagnosticsText, { color: colors.mutedForeground }]}>Backend {diagnostics.backendStatus ?? 'not used'} · fields {diagnostics.backendFieldCount ?? '—'} · confidence {diagnostics.backendConfidence ?? '—'}</Text>
                  <Text style={[styles.diagnosticsText, { color: colors.mutedForeground }]}>Lines {diagnostics.backendLineCount ?? '—'} · config v{diagnostics.effectiveConfiguration?.version ?? '—'} · threshold {diagnostics.effectiveConfiguration?.minimumMobileConfidence ?? '—'}</Text>
                  <Text style={[styles.diagnosticsText, { color: colors.mutedForeground }]}>Final {diagnostics.finalStatus ?? '—'} · missing {diagnostics.finalMissingFields?.join(', ') || 'none'}</Text>
                </View>
              ) : null}
              <Text style={[styles.note, { color: colors.mutedForeground }]}>{ocrHelpNote}</Text>
            </ScrollView>
          </View>
        </View>
      </Modal>
  );
  if (!isScanner && invoice.isPending) return <><ReceiveShell title="Review invoice" onInfo={() => setInfoOpen(true)}><ActivityIndicator color={colors.primary} /></ReceiveShell>{infoSheet}</>;
  if (!isScanner && (invoice.isError || !invoice.data)) return <><ReceiveShell title="Review invoice" onInfo={() => setInfoOpen(true)}><Text style={{ color: colors.error }}>Invoice review is unavailable.</Text></ReceiveShell>{infoSheet}</>;
  const data = invoice.data;
   const missingFields = new Set(evaluateBasicSufficiency(fields, { requirePurchaseOrderNumber: true }).missingFields);
   const effectiveSupplierId = selectedSupplierId ?? (!isScanner ? data?.supplierId ?? null : null);
     const selectedSupplierLabel = selectedSupplier
       ? selectedSupplier
       : supplierSearch.data?.find(item => item.id === effectiveSupplierId)
         ?? null;
  const ocrPoReason = ocrPoNumber.trim() && !ocrPoMatched && selectedSupplierId && organizationId
    ? (openPos.isError && !ocrPoLookup.data
      ? 'Unable to load open purchase orders.'
      : findOcrPurchaseOrder(ocrPoNumber, eligibleOpenPos, selectedSupplierId, organizationId, poEntityCode)
        || (selectedPo && normalizePurchaseOrderNumber(selectedPo.poNumber) === normalizePurchaseOrderNumber(ocrPoNumber))
        ? null
        : eligibleOpenPos.length > 0
          ? 'OCR purchase order is not an open PO for this supplier.'
          : null)
    : null;
  const supplierDisplayName = selectedSupplierLabel?.name ?? (data?.supplierName && !isSupplierIdLabel(data.supplierName) ? data.supplierName : 'this supplier');
  const supplierDisplayCode = selectedSupplierLabel?.supplierCode ?? data?.supplierCode ?? '';
  const ocrSupplierValue = ocrSupplierScreenValue(fields.supplierName);
  const ocrSupplierId = extractOcrSupplierCandidate(fields.supplierName).supplierId;
  const selectorPos = eligibleOpenPos.filter((po) => {
    const term = poQuery.trim().toLowerCase();
    if (!term) return true;
    return po.poNumber.toLowerCase().includes(term) || (po.companyCode ?? '').toLowerCase().includes(term);
  });
  const rankedForStatus = selectedSupplierId && organizationId
    ? rankOpenPurchaseOrders(eligibleOpenPos, selectedSupplierId, organizationId, poEntityCode, session.ocrLines, fields.invoiceGross, fields.currency, ocrPoNumber)
    : [];
  const selectedMatch = selectedPo ? rankedForStatus.find(item => item.po.id === selectedPo.id) : undefined;
  const matchingStatus = selectedPo
    ? (ocrPoMatched || selectedMatch?.band === 'exact' || selectedMatch?.band === 'strong' ? 'Matched Automatically' : (selectedMatch ? formatPurchaseOrderMatch(selectedMatch) : 'Selected'))
    : selectedSupplierId
      ? 'Select an eligible open purchase order'
      : 'Resolve the supplier to load open purchase orders';
  const invoiceLines = data?.lines?.length
    ? data.lines.map(line => ({
      id: line.id,
      description: line.description,
      quantity: line.quantity,
      uom: line.uom,
      unitPrice: line.unitPrice,
      lineAmount: line.lineAmount,
      materialCode: line.supplierMaterialCode,
      poItemNumber: line.purchaseOrderItemId ? String(line.lineNumber) : null,
    }))
    : session.ocrLines.map((line, index) => ({
      id: `${line.lineNumber ?? index}`,
      description: line.description,
      quantity: line.quantity,
      uom: line.uom,
      unitPrice: line.unitPrice,
      lineAmount: line.lineAmount,
      materialCode: line.materialCode,
      poItemNumber: line.poItemNumber,
    }));
  const saveFooter = (
    <View style={styles.footerBlock}>
      {saveMessage ? <Text testID="invoice-save-error" style={[styles.saveMessage, { color: colors.error }]}>{saveMessage}</Text> : null}
      {duplicateInvoiceId ? <PrimaryButton testID="open-existing-invoice" label="Open existing invoice" onPress={openExistingInvoice} secondary /> : null}
      <PrimaryButton
        testID="save-and-continue"
        label={isScanner ? (session.saving ? 'Saving...' : 'SAVE AND CONTINUE') : (updateInvoice.isPending ? 'Saving...' : 'SAVE AND CONTINUE')}
        onPress={isScanner ? () => void saveScanner() : continueExisting}
        disabled={session.saving || updateInvoice.isPending || rereading}
      />
    </View>
  );
  return (
    <>
    <ReceiveShell title="Invoice Review" fallback="/receive/document-preview" onBack={leave} onInfo={() => setInfoOpen(true)} footer={saveFooter}>
      {data && <StatusPill label={data.status} tone={data.status === 'READY_FOR_GRN' ? 'success' : 'warning'} />}
      <View style={[styles.sectionCard, { borderColor: colors.border }]}>
        <Text style={[styles.section, { color: colors.mutedForeground, marginBottom: 8 }]}>INVOICE DOCUMENT</Text>
        {session.pdfUri ? <Text style={[styles.pdfMeta, { color: colors.primary, marginBottom: 8 }]}>Original document ready · {session.pages.length || 1} page{(session.pages.length || 1) === 1 ? '' : 's'}</Text> : <Text style={[styles.supplierMeta, { color: colors.error }]}>Scanned document is not available.</Text>}
        {session.ocrStatus === 'extracting' || rereading ? <Text style={[styles.status, { color: colors.primary, marginBottom: 8 }]}>Improving invoice extraction…</Text> : null}
        {ocrStatusCopy ? <Text style={[styles.warning, { color: ocrStatusTone, marginBottom: 8 }]}>{ocrStatusCopy}</Text> : null}
        {isScanner ? (
          <View style={styles.secondaryActions}>
            <HeaderAction testID="view-document" icon="file-text" label="VIEW DOCUMENT" accessibilityLabel="View document" onPress={viewDocument} disabled={!session.pdfUri} />
            <HeaderAction testID="reread-invoice" icon="refresh-cw" label={rereading ? 'RE-READING...' : 'RE-READ INVOICE'} accessibilityLabel={rereading ? 'Re-reading invoice' : 'Re-read invoice'} onPress={() => void rereadInvoice()} disabled={rereading || (!session.pages.length && !session.pdfUri)} loading={rereading} />
          </View>
        ) : null}
      </View>
      <View style={[styles.sectionCard, { borderColor: colors.border }]}>
           <Text style={[styles.section, { color: colors.mutedForeground, marginBottom: 8 }]}>SUPPLIER</Text>
           <Text style={[styles.label, { color: colors.mutedForeground }]}>OCR SUPPLIER</Text>
           <Text testID="ocr-supplier-value" style={[styles.ocrSupplier, { color: colors.mutedForeground }]}>{ocrSupplierValue || 'Not read'}{ocrSupplierId ? ` · ID ${ocrSupplierId}` : ''}</Text>
           <Text style={[styles.label, { color: colors.foreground, marginTop: 10 }]}>SUPPLIER NAME</Text>
           {selectedSupplierLabel || (data?.supplierId && (data.supplierCode || (data.supplierName && !isSupplierNameNoise(data.supplierName)))) ? (
             <View style={styles.selectedSupplier}>
               <View style={{ flex: 1 }}>
                 <Text style={[styles.selectedSupplierName, { color: colors.foreground }]}>{
                   selectedSupplierLabel
                     ? toAuthoritativeSupplier(selectedSupplierLabel).supplierName
                     : (data?.supplierName && !isSupplierIdLabel(data.supplierName) && !isSupplierNameNoise(data.supplierName) ? data.supplierName : (data?.supplierCode ?? 'Not selected'))
                 }</Text>
                 <Text style={[styles.supplierIdText, { color: colors.foreground }]}>Supplier ID {selectedSupplierLabel?.supplierCode ?? data?.supplierCode}</Text>
                 {(selectedSupplierLabel?.legalName) ? <Text style={[styles.supplierMeta, { color: colors.mutedForeground }]}>{selectedSupplierLabel.legalName}</Text> : null}
               </View>
               <Pressable onPress={() => { setSelectedSupplierId(null); setSelectedSupplier(null); autoMatchedSupplier.current = false; seededSupplierSearch.current = false; autoMatchedPo.current = false; setSelectedPo(null); }} hitSlop={10}>
                 <Text style={{ color: colors.primary, fontFamily: 'Inter_600SemiBold' }}>CHANGE SUPPLIER</Text>
               </Pressable>
             </View>
           ) : (
             <>
               <Text style={[styles.supplierMeta, { color: colors.error }]}>Not selected</Text>
               <TextInput
                 value={supplierQuery}
                 onChangeText={setSupplierQuery}
                 placeholder="Search supplier name, ID, TRN, or alias"
                 placeholderTextColor={colors.mutedForeground}
                 autoCapitalize="none"
                 autoCorrect={false}
                 style={[styles.input, { borderColor: colors.border, color: colors.foreground }]}
               />
               {debouncedSupplierQuery.length >= 2 && supplierSearch.isPending && <Text style={[styles.supplierMeta, { color: colors.mutedForeground }]}>Searching suppliers…</Text>}
               {debouncedSupplierQuery.length >= 2 && !supplierSearch.isPending && supplierSearch.data?.map(supplier => (
                 <Pressable key={supplier.id} onPress={() => chooseSupplier(supplier)} style={[styles.supplierResult, { borderColor: colors.border }]}>
                   <Text style={[styles.selectedSupplierName, { color: colors.foreground }]}>{supplier.name}</Text>
                   <Text style={[styles.supplierIdText, { color: colors.foreground }]}>{supplier.supplierCode}</Text>
                   <Text style={[styles.supplierMeta, { color: colors.mutedForeground }]}>{[supplier.trn ?? supplier.taxNumber, supplier.entityCode].filter(Boolean).join(' · ')}</Text>
                 </Pressable>
               ))}
               {debouncedSupplierQuery.length >= 2 && !supplierSearch.isPending && supplierSearch.data?.length === 0 && <Text style={[styles.supplierMeta, { color: colors.mutedForeground }]}>No active supplier matched that search.</Text>}
             </>
           )}
           <Text style={[styles.label, { color: colors.mutedForeground, marginTop: 10 }]}>TRN</Text>
           <TextInput value={fields.supplierTrn} onChangeText={value => change('supplierTrn', value)} placeholder="Optional" placeholderTextColor={colors.mutedForeground} style={[styles.input, { borderColor: colors.border, color: colors.foreground }]} />
      </View>
      <View style={[styles.sectionCard, { borderColor: colors.border }]}>
        <Text style={[styles.section, { color: colors.mutedForeground, marginBottom: 8 }]}>INVOICE</Text>
        <View style={styles.field}>
          <Text style={[styles.label, { color: missingFields.has('supplierInvoiceNumber') ? colors.error : colors.mutedForeground }]}>INVOICE NUMBER{missingFields.has('supplierInvoiceNumber') ? ' · REQUIRED' : ''}</Text>
          <TextInput value={fields.supplierInvoiceNumber} onChangeText={value => change('supplierInvoiceNumber', value)} placeholder="Invoice number" placeholderTextColor={colors.mutedForeground} style={[styles.input, { borderColor: missingFields.has('supplierInvoiceNumber') ? colors.error : colors.border, color: colors.foreground }]} />
        </View>
        <View style={styles.field}>
          <Text style={[styles.label, { color: colors.mutedForeground }]}>INVOICE DATE</Text>
          <TextInput value={fields.invoiceDate} onChangeText={value => change('invoiceDate', value)} placeholder="YYYY-MM-DD" placeholderTextColor={colors.mutedForeground} style={[styles.input, { borderColor: colors.border, color: colors.foreground }]} />
        </View>
        <View style={styles.field}>
          <Text style={[styles.label, { color: colors.mutedForeground }]}>CURRENCY</Text>
          <TextInput value={fields.currency} onChangeText={value => change('currency', value)} placeholder="AED" placeholderTextColor={colors.mutedForeground} style={[styles.input, { borderColor: colors.border, color: colors.foreground }]} />
        </View>
        {amountExtras.netAmount ? <Text style={[styles.supplierMeta, { color: colors.mutedForeground }]}>Net Amount {amountExtras.netAmount}</Text> : null}
        {amountExtras.taxAmount ? <Text style={[styles.supplierMeta, { color: colors.mutedForeground }]}>Tax {amountExtras.taxAmount}</Text> : null}
        <View style={styles.field}>
          <Text style={[styles.label, { color: missingFields.has('invoiceGross') ? colors.error : colors.mutedForeground }]}>GROSS AMOUNT{missingFields.has('invoiceGross') ? ' · REQUIRED' : ''}</Text>
          <TextInput value={fields.invoiceGross} onChangeText={value => change('invoiceGross', value)} placeholder="0.00" placeholderTextColor={colors.mutedForeground} style={[styles.input, { borderColor: missingFields.has('invoiceGross') ? colors.error : colors.border, color: colors.foreground }]} />
        </View>
      </View>
      <View style={[styles.sectionCard, { borderColor: colors.border }]}>
          <Text style={[styles.section, { color: colors.mutedForeground, marginBottom: 8 }]}>PURCHASE ORDER</Text>
          {ocrPoNumber ? <Text style={[styles.ocrSupplier, { color: colors.mutedForeground }]}>OCR PO {ocrPoNumber}{ocrPoMatched ? '  MATCHED' : ''}</Text> : <Text style={[styles.ocrSupplier, { color: colors.mutedForeground }]}>OCR PO not read — matching open POs from invoice lines when possible.</Text>}
          {ocrPoReason ? <Text style={[styles.supplierMeta, { color: colors.error }]}>{ocrPoReason}</Text> : null}
          <Text style={[styles.label, { color: colors.mutedForeground, marginTop: 10 }]}>PO NUMBER</Text>
          {!shouldRenderPurchaseOrderSelector(selectedSupplierId) ? <Text style={[styles.supplierMeta, { color: colors.mutedForeground }]}>Resolve the supplier to load eligible open purchase orders.</Text> : (
            <>
              <Pressable
                testID="po-selector"
                onPress={() => setPoPickerOpen(true)}
                style={[styles.selectedPo, { borderColor: colors.border }]}
              >
                <Text style={[styles.selectedSupplierName, { color: colors.foreground }]}>{selectedPo ? selectedPo.poNumber : 'SELECT PURCHASE ORDER ▼'}</Text>
                {selectedPo ? (
                  <>
                    <Text style={[styles.supplierMeta, { color: colors.mutedForeground }]}>PO Date {selectedPo.poDate ?? '—'} · Company Code {selectedPo.companyCode ?? '—'}</Text>
                    <Text style={[styles.supplierMeta, { color: colors.mutedForeground }]}>Status {selectedPo.status}</Text>
                    <Text style={[styles.supplierMeta, { color: colors.primary }]}>{matchingStatus}</Text>
                    <Text style={{ color: colors.primary, fontFamily: 'Inter_600SemiBold', marginTop: 6 }}>CHANGE PO</Text>
                  </>
                ) : (
                  <Text style={[styles.supplierMeta, { color: colors.mutedForeground }]}>{matchingStatus}</Text>
                )}
              </Pressable>
              {openPos.isPending || openPos.isFetching ? <Text style={[styles.supplierMeta, { color: colors.mutedForeground }]}>Loading open purchase orders...</Text> : null}
              {openPos.isError ? (
                <View>
                  <Text style={[styles.supplierMeta, { color: colors.error }]}>Unable to load open purchase orders.</Text>
                  <Pressable onPress={() => void openPos.refetch()} style={[styles.secondaryButton, { borderColor: colors.border, marginTop: 8 }]}>
                    <Text style={{ color: colors.primary, fontFamily: 'Inter_600SemiBold' }}>RETRY</Text>
                  </Pressable>
                </View>
              ) : null}
              {selectedSupplierId && !openPos.isPending && !openPos.isError && eligibleOpenPos.length === 0 ? (
                <View>
                  <Text style={[styles.supplierMeta, { color: colors.error }]}>No eligible open purchase orders found for {supplierDisplayName}{supplierDisplayCode ? ` (${supplierDisplayCode})` : ''}.</Text>
                  <Pressable onPress={() => void openPos.refetch()} style={[styles.secondaryButton, { borderColor: colors.border, marginTop: 8 }]}>
                    <Text style={{ color: colors.primary, fontFamily: 'Inter_600SemiBold' }}>RETRY</Text>
                  </Pressable>
                </View>
              ) : null}
              <Modal visible={poPickerOpen} animationType="slide" transparent onRequestClose={() => setPoPickerOpen(false)}>
                <View style={styles.infoOverlay}>
                  <View style={[styles.infoSheet, { backgroundColor: colors.background, paddingBottom: insets.bottom + 16 }]}>
                    <View style={[styles.infoHeader, { borderBottomColor: colors.border }]}>
                      <Text style={[styles.infoTitle, { color: colors.foreground }]}>Select purchase order</Text>
                      <Pressable onPress={() => setPoPickerOpen(false)} hitSlop={12}>
                        <Feather name="x" size={22} color={colors.foreground} />
                      </Pressable>
                    </View>
                    <ScrollView contentContainerStyle={styles.infoBody}>
                      <TextInput value={poQuery} onChangeText={setPoQuery} placeholder="Search open PO number" placeholderTextColor={colors.mutedForeground} autoCapitalize="none" style={[styles.input, { borderColor: colors.border, color: colors.foreground, marginBottom: 12 }]} />
                      {openPos.isPending ? <Text style={[styles.supplierMeta, { color: colors.mutedForeground }]}>Loading open purchase orders...</Text> : null}
                      {selectorPos.map((po) => (
                        <Pressable key={po.id} testID={`po-option-${po.poNumber}`} onPress={() => choosePo(po)} style={[styles.supplierResult, { borderColor: selectedPo?.id === po.id ? colors.primary : colors.border, marginBottom: 8 }]}>
                          <Text style={[styles.selectedSupplierName, { color: colors.foreground }]}>{po.poNumber}</Text>
                          <Text style={[styles.supplierMeta, { color: colors.mutedForeground }]}>Company Code {po.companyCode ?? '—'} · {po.status}</Text>
                          <Text style={[styles.supplierMeta, { color: colors.mutedForeground }]}>{formatPoOption(po)}</Text>
                        </Pressable>
                      ))}
                      {!openPos.isPending && selectorPos.length === 0 ? (
                        <Text style={[styles.supplierMeta, { color: colors.error }]}>No eligible open purchase orders found for {supplierDisplayName}{supplierDisplayCode ? ` (${supplierDisplayCode})` : ''}.</Text>
                      ) : null}
                    </ScrollView>
                  </View>
                </View>
              </Modal>
            </>
          )}
      </View>
      {(invoiceLines.length || selectedPo?.items?.length) ? (
        <View style={[styles.sectionCard, { borderColor: colors.border }]}>
          <Text style={[styles.section, { color: colors.mutedForeground, marginBottom: 8 }]}>INVOICE ITEMS</Text>
          {selectedPo?.items?.length ? selectedPo.items.filter(item => item.goodsReceiptExpected !== false && !item.deletionIndicator).map(item => {
            const invoiceQty = invoiceQuantityForPoItem(invoiceLines, item);
            return (
              <View key={item.id} style={styles.lineBlock}>
                <Text style={[styles.line, { color: colors.foreground }]}>PO Item {item.itemNumber ?? item.lineNumber} · {item.materialCode}</Text>
                <Text style={[styles.supplierMeta, { color: colors.mutedForeground }]}>{item.description}</Text>
                <Text style={[styles.supplierMeta, { color: colors.foreground }]}>Ordered {formatQuantity(item.orderedQuantity)} · Received {formatQuantity(item.receivedQuantity)} · Open {formatQuantity(item.openQuantity)} {item.uom}</Text>
                <Text style={[styles.supplierMeta, { color: colors.foreground }]}>Invoice Qty {formatQuantity(invoiceQty)} · {item.unitPrice != null ? `Unit ${item.unitPrice}` : 'Unit —'} {item.uom ?? ''}</Text>
              </View>
            );
          }) : invoiceLines.map(line => (
            <View key={line.id} style={styles.lineBlock}>
              <Text style={[styles.line, { color: colors.foreground }]}>{line.poItemNumber ? `PO Item ${line.poItemNumber} · ` : ''}{line.materialCode ?? line.description}</Text>
              <Text style={[styles.supplierMeta, { color: colors.mutedForeground }]}>{line.description}</Text>
              <Text style={[styles.supplierMeta, { color: colors.foreground }]}>{formatQuantity(line.quantity)} {line.uom ?? ''}{line.unitPrice != null ? ` · Unit ${line.unitPrice}` : ''}{line.lineAmount != null ? ` · ${line.lineAmount}` : ''}</Text>
            </View>
          ))}
        </View>
      ) : null}
    </ReceiveShell>
      {infoSheet}
    </>
  );
}
const styles = StyleSheet.create({
  section: { fontFamily: 'Inter_700Bold', fontSize: 10, letterSpacing: 1.1, marginBottom: 12 },
  pdfMeta: { fontFamily: 'Inter_600SemiBold', fontSize: 12, marginBottom: 14 },
  status: { marginBottom: 12, fontFamily: 'Inter_600SemiBold', fontSize: 12 },
  warning: { marginBottom: 14, fontFamily: 'Inter_400Regular', fontSize: 13, lineHeight: 19 },
  diagnostics: { borderWidth: 1, borderRadius: 8, padding: 10, marginBottom: 14, gap: 3 },
  diagnosticsTitle: { fontFamily: 'Inter_700Bold', fontSize: 10, letterSpacing: .8 },
  diagnosticsText: { fontFamily: 'Inter_400Regular', fontSize: 10, lineHeight: 15 },
  sectionCard: { borderWidth: 1, borderRadius: 8, padding: 12, marginTop: 12, gap: 6, backgroundColor: '#FFFFFF' },
  selectedSupplier: { flexDirection: 'row', alignItems: 'center', gap: 12 },
  selectedSupplierName: { fontFamily: 'Inter_700Bold', fontSize: 15 },
  supplierIdText: { fontFamily: 'Inter_600SemiBold', fontSize: 13, marginTop: 2 },
  ocrSupplier: { fontFamily: 'Inter_400Regular', fontSize: 13, lineHeight: 18 },
  supplierMeta: { fontFamily: 'Inter_400Regular', fontSize: 11, lineHeight: 16 },
  supplierResult: { borderWidth: 1, borderRadius: 8, padding: 10, gap: 3 },
  selectedPo: { borderWidth: 1, borderRadius: 8, padding: 10, marginTop: 4, gap: 3 },
  field: { gap: 6, marginBottom: 12 },
  label: { fontFamily: 'Inter_600SemiBold', fontSize: 10, letterSpacing: .8 },
  input: { minHeight: 44, borderWidth: 1, borderRadius: 8, paddingHorizontal: 12, fontFamily: 'Inter_400Regular', fontSize: 14 },
  footerBlock: { gap: 10 },
  saveMessage: { fontFamily: 'Inter_600SemiBold', fontSize: 12, lineHeight: 18 },
  line: { marginBottom: 2, fontFamily: 'Inter_600SemiBold', fontSize: 13, lineHeight: 19 },
  lineBlock: { marginBottom: 12, gap: 2 },
  secondaryActions: { flexDirection: 'row', gap: 10, flexWrap: 'wrap' },
  secondaryButton: { flex: 1, minHeight: 44, borderWidth: 1, borderRadius: 8, alignItems: 'center', justifyContent: 'center', paddingHorizontal: 8 },
  note: { marginVertical: 8, fontFamily: 'Inter_400Regular', fontSize: 12, lineHeight: 18 },
  infoOverlay: { flex: 1, backgroundColor: 'rgba(8, 18, 32, 0.45)', justifyContent: 'flex-end' },
  infoSheet: { maxHeight: '82%', borderTopLeftRadius: 16, borderTopRightRadius: 16 },
  infoHeader: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', paddingHorizontal: 20, paddingTop: 18, paddingBottom: 12, borderBottomWidth: 1 },
  infoTitle: { fontFamily: 'Inter_700Bold', fontSize: 18 },
  infoBody: { paddingHorizontal: 20, paddingTop: 16, paddingBottom: 24 },
});
