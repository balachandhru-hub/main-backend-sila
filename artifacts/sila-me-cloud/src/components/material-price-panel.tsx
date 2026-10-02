import { useState } from 'react';
import { useMutation, useQuery } from '@tanstack/react-query';
import { customFetch } from '@workspace/api-client-react';

const request = { credentials: 'include' as const };
async function api<T>(url: string, init?: RequestInit): Promise<T> {
  return customFetch<T>(url, { ...request, ...init, responseType: 'json' });
}

export type MaterialPriceRow = {
  id: string;
  materialCode: string;
  name: string;
  description?: string;
  unitCost?: number | null;
  approvedUnitPrice?: number | null;
  baseUom: string;
  currency?: string | null;
  hasValidUnitPrice: boolean;
  priceStatus?: string;
  priceStatusLabel?: string;
  proposedUnitPrice?: number | null;
  companyCode?: string | null;
  valuationArea?: string | null;
  plant?: string | null;
  priceControl?: string | null;
  priceUom?: string | null;
  priceEffectiveFrom?: string | null;
  pendingChangeRequestId?: string | null;
  hasPendingPriceChange?: boolean;
  packSummary?: string | null;
  conversions?: Array<{ fromUom: string; toUom: string; numerator: number; denominator: number }>;
  convFactor?: number | null;
  convUnit?: string | null;
  convValue?: number | null;
  conversionText?: string | null;
  materialGroup?: string | null;
  category?: string | null;
  materialType?: string | null;
  supplierSummary?: string | null;
  source?: string;
  updatedAt?: string;
  approvalStatus?: string;
  activeStatus?: string;
  alternateUom?: string | null;
  valuationClass?: string | null;
  standardPrice?: number | null;
  movingAveragePrice?: number | null;
  valuations?: Array<{ valuationArea: string; priceControl?: string; standardPrice?: number; movingAveragePrice?: number; currency?: string }>;
  inventoryItem?: boolean;
  inventoryType?: string;
  batchManaged?: boolean;
  expiryManaged?: boolean;
  shelfLifeDays?: number | null;
  serialManaged?: boolean;
};

export function money(amount: number | null | undefined, currency: string) {
  if (amount == null || Number.isNaN(amount)) return '';
  return `${currency} ${amount.toFixed(2)}`;
}

export function priceStatusLabel(status: string | undefined) {
  switch (status) {
    case 'PRICE_APPROVED': return 'Approved';
    case 'PRICE_MISSING': return 'Price Missing';
    case 'PRICE_PENDING_APPROVAL': return 'Pending Approval';
    case 'PRICE_REJECTED': return 'Rejected';
    case 'PRICE_EXPIRED': return 'Invalid';
    default: return status ? 'Invalid' : 'Price Missing';
  }
}

export function canCorrectPrice(status: string) {
  return status === 'PRICE_MISSING' || status === 'PRICE_REJECTED' || status === 'PRICE_INVALID' || status === 'PRICE_EXPIRED';
}

export function unitPriceDisplay(status: string, approved: number | null, proposed: number | null, currency: string, uom?: string) {
  const suffix = uom ? ` / ${uom}` : '';
  if (approved != null) return `${money(approved, currency)}${suffix}`;
  if (status === 'PRICE_PENDING_APPROVAL') return 'PRICE MISSING';
  if (status === 'PRICE_MISSING') return 'PRICE MISSING';
  if (status === 'PRICE_REJECTED') return 'REJECTED';
  if (status === 'PRICE_EXPIRED') return 'INVALID';
  return proposed != null ? `${money(proposed, currency)}${suffix}` : 'INVALID';
}

export function MaterialPricePanel({ organizationId, materialId, onClose, onSubmitted }: {
  organizationId: string; materialId: string; onClose: () => void; onSubmitted: (material: MaterialPriceRow) => void;
}) {
  const detail = useQuery({
    queryKey: ['recipe-material', organizationId, materialId],
    queryFn: () => api<MaterialPriceRow>(`/api/v1/recipe-management/materials/${materialId}?organizationId=${organizationId}`),
    retry: false,
  });
  const uoms = useQuery({
    queryKey: ['recipe-uoms', organizationId],
    queryFn: () => api<Array<{ code: string; name: string }>>(`/api/v1/recipe-management/uoms?organizationId=${organizationId}&status=ACTIVE`),
    enabled: Boolean(organizationId), retry: false,
  });
  const [unitPrice, setUnitPrice] = useState('');
  const [priceUom, setPriceUom] = useState('');
  const [effectiveFrom, setEffectiveFrom] = useState('');
  const [comment, setComment] = useState('');
  const [error, setError] = useState('');
  const row = detail.data;
  const approved = row?.approvedUnitPrice ?? (row?.hasValidUnitPrice ? row.unitCost ?? null : null);
  const selectedUom = priceUom || row?.priceUom || row?.baseUom || '';
  const submit = useMutation({
    mutationFn: () => api<MaterialPriceRow>(`/api/v1/recipe-management/materials/${materialId}/price-change?organizationId=${organizationId}`, {
      method: 'POST', headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        unitPrice: Number(unitPrice), currency: row?.currency || 'AED', priceUom: selectedUom,
        companyCode: row?.companyCode, valuationArea: row?.valuationArea, plant: row?.plant,
        priceControl: row?.priceControl, comment,
        effectiveFrom: effectiveFrom ? new Date(effectiveFrom).toISOString() : null,
      }),
    }),
    onSuccess: (material) => onSubmitted(material),
    onError: () => setError('Could not submit the Material price change for approval.'),
  });
  const uomOptions = Array.from(new Set([
    row?.baseUom, row?.priceUom, ...(row?.conversions?.flatMap((item) => [item.fromUom, item.toUom]) ?? []),
    ...(uoms.data?.map((item) => item.code) ?? []),
  ].filter(Boolean) as string[]));
  return (
    <div className="sila-drawer-backdrop" data-testid="material-price-panel" onClick={onClose}>
      <aside className="sila-drawer" onClick={(event) => event.stopPropagation()}>
        <h3>Update Unit Price</h3>
        <p className="sila-muted-copy">This submits a Material CHANGE for approval. It does not overwrite the currently approved price.</p>
        {detail.isPending && <p>Loading Material Master…</p>}
        {row && (
          <>
            <h4>Material</h4>
            <div className="sila-form-grid" style={{ gridTemplateColumns: '1fr' }}>
              <label className="sila-form-field"><span>Material ID</span><input className="sila-input" readOnly value={row.materialCode} /></label>
              <label className="sila-form-field"><span>Material Description</span><input className="sila-input" readOnly value={row.name || row.description || ''} /></label>
              <label className="sila-form-field"><span>Base UOM</span><input className="sila-input" readOnly value={row.baseUom} /></label>
            </div>
            <h4>Valuation context</h4>
            <div className="sila-form-grid" style={{ gridTemplateColumns: '1fr' }}>
              <label className="sila-form-field"><span>Company Code / Valuation Area</span><input className="sila-input" readOnly value={[row.companyCode, row.valuationArea].filter(Boolean).join(' / ') || '—'} /></label>
              <label className="sila-form-field"><span>Plant</span><input className="sila-input" readOnly value={row.plant || row.valuationArea || '—'} /></label>
              <label className="sila-form-field"><span>Current Approved Unit Price</span><input className="sila-input" readOnly value={approved == null ? 'NONE' : money(approved, row.currency || 'AED')} /></label>
              <label className="sila-form-field"><span>Current Price UOM</span><input className="sila-input" readOnly value={row.priceUom || row.baseUom} /></label>
              <label className="sila-form-field"><span>Currency</span><input className="sila-input" readOnly value={row.currency || 'AED'} /></label>
            </div>
            {row.hasPendingPriceChange && (
              <p className="sila-recipe-cost-note">A price change is already pending approval. Proposed: {row.proposedUnitPrice == null ? '—' : money(row.proposedUnitPrice, row.currency || 'AED')}</p>
            )}
            <h4>New price</h4>
            <div className="sila-form-grid" style={{ gridTemplateColumns: '1fr' }}>
              <label className="sila-form-field"><span>New Unit Price *</span><input className="sila-input" data-testid="input-new-unit-price" inputMode="decimal" value={unitPrice} onChange={(event) => setUnitPrice(event.target.value)} /></label>
              <label className="sila-form-field"><span>Price UOM</span>
                <select className="sila-select" value={selectedUom} onChange={(event) => setPriceUom(event.target.value)}>
                  {uomOptions.map((code) => <option key={code} value={code}>{code}</option>)}
                </select>
              </label>
              <label className="sila-form-field"><span>Effective From</span><input className="sila-input" type="date" value={effectiveFrom} onChange={(event) => setEffectiveFrom(event.target.value)} /></label>
              <label className="sila-form-field"><span>Reason / Comment *</span><input className="sila-input" data-testid="input-price-reason" value={comment} onChange={(event) => setComment(event.target.value)} /></label>
            </div>
          </>
        )}
        <div className="sila-integration-actions" style={{ marginTop: 16 }}>
          <button className="sila-button" type="button" onClick={onClose}>Cancel</button>
          <button className="sila-button sila-button--primary" type="button" data-testid="button-submit-material-price"
            disabled={submit.isPending || Boolean(row?.hasPendingPriceChange) || unitPrice.trim() === '' || Number.isNaN(Number(unitPrice)) || comment.trim() === ''}
            onClick={() => { setError(''); submit.mutate(); }}>
            {submit.isPending ? 'Submitting…' : 'Submit for Approval'}
          </button>
        </div>
        {error && <p className="sila-recipe-cost-note">{error}</p>}
      </aside>
    </div>
  );
}
