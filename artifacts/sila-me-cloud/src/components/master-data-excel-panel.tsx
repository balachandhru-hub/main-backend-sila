import { useRef, useState } from 'react';
import { Download, FileSpreadsheet, UploadCloud } from 'lucide-react';
import { customFetch } from '@workspace/api-client-react';

export type OperationalMasterKind = 'COMPANY_CODES' | 'PROPERTIES' | 'PLANTS' | 'STORAGE_LOCATIONS';

type PreviewRow = {
  rowNumber: number;
  isValid: boolean;
  action?: string;
  values: Record<string, string | null>;
  errors: string[];
  errorCodes?: string[];
};

type Preview = {
  fileName: string;
  totalRows: number;
  validRows: number;
  invalidRows: number;
  newRows?: number;
  updateRows?: number;
  unchangedRows?: number;
  rows: PreviewRow[];
};

const request = { credentials: 'include' as const };

function noticeText(error: unknown): string {
  return error instanceof Error && error.message ? error.message : 'Preview failed.';
}

function pickValue(values: Record<string, string | null>, keys: string[]): string | undefined {
  const normalized = Object.fromEntries(
    Object.entries(values).map(([key, value]) => [key.replace(/[^a-z0-9]/gi, '').toUpperCase(), value]),
  );
  for (const key of keys) {
    const value = values[key] ?? normalized[key.replace(/[^a-z0-9]/gi, '').toUpperCase()];
    if (value) return value;
  }
  return undefined;
}

function rowKey(row: PreviewRow, recordKind?: OperationalMasterKind): string {
  const values = row.values;
  if (recordKind === 'COMPANY_CODES') return pickValue(values, ['CompanyCode', 'COMPANYCODE']) ?? '—';
  if (recordKind === 'PROPERTIES') return pickValue(values, ['PropertyCode', 'PROPERTYCODE']) ?? '—';
  if (recordKind === 'PLANTS') return pickValue(values, ['Plant', 'PlantCode', 'PLANT']) ?? '—';
  if (recordKind === 'STORAGE_LOCATIONS') {
    const plant = pickValue(values, ['Plant', 'PlantCode', 'PLANT']) ?? '';
    const location = pickValue(values, ['StorageLocation', 'StorageLocationCode', 'STORAGELOCATION']) ?? '';
    return [plant, location].filter(Boolean).join(' / ') || '—';
  }
  return pickValue(values, ['PO_NUMBER', 'SUPPLIER_CODE']) ?? '—';
}

async function downloadWorkbook(url: string, fallbackName: string) {
  const blob = await customFetch<Blob>(url, { ...request, method: 'GET', responseType: 'blob' });
  const objectUrl = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = objectUrl;
  link.download = fallbackName;
  document.body.appendChild(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(objectUrl);
}

export function MasterDataExcelPanel({
  organizationId,
  entityCode,
  kind,
  recordKind,
  onImported,
}: {
  organizationId?: string;
  entityCode?: string;
  kind?: 'SUPPLIERS' | 'PURCHASE_ORDERS';
  recordKind?: OperationalMasterKind;
  onImported: () => void;
}) {
  const fileRef = useRef<HTMLInputElement>(null);
  const [preview, setPreview] = useState<Preview>();
  const [busy, setBusy] = useState<'preview' | 'commit' | 'download' | null>(null);
  const [notice, setNotice] = useState<string>();
  const entity = entityCode || 'ALL';
  const params = recordKind
    ? `organizationId=${organizationId}&kind=${recordKind}`
    : `organizationId=${organizationId}&entityCode=${encodeURIComponent(entity)}&kind=${kind}`;
  const templateUrl = recordKind ? `/api/v1/master-data/records/import/template?${params}` : `/api/v1/master-data/import/template?${params}`;
  const exportUrl = recordKind ? `/api/v1/master-data/records/export?${params}` : `/api/v1/master-data/export?${params}`;
  const previewUrl = recordKind ? `/api/v1/master-data/records/import/preview?${params}` : `/api/v1/master-data/import/preview?${params}`;
  const commitUrl = recordKind ? `/api/v1/master-data/records/import?${params}` : `/api/v1/master-data/import?organizationId=${organizationId}&entityCode=${encodeURIComponent(entity)}`;
  const fileStem = (recordKind ?? kind ?? 'master-data').toLowerCase();

  async function download(url: string, name: string) {
    if (!organizationId) return;
    setBusy('download');
    setNotice(undefined);
    try {
      await downloadWorkbook(url, name);
    } catch (error) {
      setNotice(noticeText(error));
    } finally {
      setBusy(null);
    }
  }

  async function previewFile(file: File) {
    if (!organizationId) return;
    setBusy('preview');
    setNotice(undefined);
    try {
      const body = new FormData();
      body.append('file', file);
      const result = await customFetch<Preview>(previewUrl, {
        ...request, method: 'POST', body, responseType: 'json',
      });
      setPreview(result);
      if (result.totalRows === 0) setNotice('No data rows were found in the spreadsheet.');
    } catch (error) {
      setNotice(noticeText(error));
    } finally {
      setBusy(null);
    }
  }

  async function confirm() {
    if (!organizationId || !preview) return;
    setBusy('commit');
    try {
      await customFetch(commitUrl, {
        ...request, method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ kind: recordKind ?? kind, rows: preview.rows.filter(row => row.isValid).map(row => row.values) }),
        responseType: 'json',
      });
      setPreview(undefined);
      setNotice('Import confirmed. Master data was updated.');
      onImported();
    } catch (error) {
      setNotice(error instanceof Error ? error.message : 'Confirm import failed. No records were changed.');
    } finally {
      setBusy(null);
    }
  }

  return (
    <section className="sila-card" style={{ padding: 16, marginBottom: 18 }}>
      <div className="sila-data-actions">
        <input ref={fileRef} hidden type="file" accept=".xlsx" onChange={event => { const file = event.target.files?.[0]; if (file) void previewFile(file); event.target.value = ''; }} />
        <button className="sila-button" type="button" disabled={!organizationId || busy === 'download'} onClick={() => void download(templateUrl, `${fileStem}-template.xlsx`)}><FileSpreadsheet size={14} /> Download template</button>
        <button className="sila-button" type="button" disabled={!organizationId || busy === 'download'} onClick={() => void download(exportUrl, `${fileStem}-export.xlsx`)}><Download size={14} /> Download Excel</button>
        <button className="sila-button sila-button--primary" type="button" disabled={!organizationId || busy === 'preview'} onClick={() => fileRef.current?.click()}><UploadCloud size={14} /> {busy === 'preview' ? 'Reading…' : 'Upload Excel'}</button>
      </div>
      <p className="sila-muted-copy">Organization and entity are taken from the filters above. File selection only previews; Confirm import writes to PostgreSQL.</p>
      {notice && <p className="sila-muted-copy">{notice}</p>}
      {preview && (
        <div className="sila-import-preview">
          <div className="sila-import-preview__header">
            <div>
              <strong>{preview.fileName}</strong>
              <small>{preview.validRows} valid · {preview.invalidRows} invalid · {preview.newRows ?? 0} new · {preview.updateRows ?? 0} updates · {preview.unchangedRows ?? 0} unchanged</small>
            </div>
            <div className="sila-integration-actions">
              <button className="sila-button" type="button" onClick={() => setPreview(undefined)}>Discard</button>
              <button className="sila-button sila-button--primary" type="button" disabled={preview.validRows === 0 || preview.invalidRows > 0 || busy === 'commit'} onClick={() => void confirm()}>{busy === 'commit' ? 'Saving…' : 'Confirm import'}</button>
            </div>
          </div>
          <div className="sila-table-wrap">
            <table className="sila-table">
              <thead><tr><th>Row</th><th>Action</th><th>Key</th><th>Codes</th><th>Message</th></tr></thead>
              <tbody>
                {preview.rows.slice(0, 50).map(row => (
                  <tr key={row.rowNumber}>
                    <td>{row.rowNumber}</td>
                    <td>{row.isValid ? row.action ?? 'READY' : 'ERROR'}</td>
                    <td>{rowKey(row, recordKind)}</td>
                    <td>{row.errorCodes?.join(', ') || '—'}</td>
                    <td>{row.errors.join(' ') || 'Ready'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </section>
  );
}
