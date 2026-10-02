# Protected Invoice Receiving Flow

**Current restore version:** `invoice-receiving-v2`  
**Previous freeze:** `invoice-receiving-v1`  
**Machine-readable manifest:** `protected-flows/invoice-receiving.json`

Restore OCR + Invoice Review + Finalize GRN + GRN posting:

```bash
git fetch --tags
git checkout invoice-receiving-v2
```

`invoice-receiving-v2` is the known-good checkpoint after: Invoice Review freeze, no operating-unit requirement (company code/property), and remaining qty from posted GRNs.

These files and contracts must not be modified by unrelated feature work.
Changes require an explicit Invoice Receiving/OCR requirement.

OCR internals may improve later. The Mobile Invoice Review contract (DTO field names and meaning) must not change arbitrarily.

---

## Protected business flow

Scan/Upload → OCR → Supplier Resolution → PO Matching → Invoice Review → Save and Continue → Finalize GRN

Downstream (not this module): Finalize GRN → POST GRN → Integration Route Resolver (Company Code) → Ariba/S4/etc.

Downstream work must consume the saved `InvoiceId` / `GoodsReceiptId`. It must not rewrite Invoice Review or OCR.

---

## Frozen invariants

1. No PO / NO-PO buttons on Invoice Review.
2. OCR returns all available invoice header and line fields.
3. Supplier ID, when available, resolves against Supplier Master. Never create a supplier from OCR.
4. Resolved Supplier determines eligible Open POs (`openOnly=true` + that supplier).
5. A valid OCR PO number automatically selects that PO when it is in the eligible set.
6. If PO number is unavailable, supplier + invoice lines identify the best eligible Open PO.
7. If there is no confident PO match, the user selects from that Supplier’s eligible Open POs only.
8. Manual / user-confirmed values have authority over OCR values (MANUAL > BACKEND MERGED OCR > MOBILE OCR).
9. View Document opens the current source document (same scan session / stored document).
10. Re-Read Invoice reprocesses the same document and must not create duplicate invoice or document records.
11. Save and Continue persists the authoritative reviewed invoice and lines once.
12. Save and Continue navigates **only** to Finalize GRN.
13. Save and Continue does **not** post the GRN.
14. Finalize GRN is responsible for physical received-quantity confirmation.
15. POST GRN and external integration happen after Finalize GRN and must not modify Invoice Review / OCR logic.
16. Finalize GRN / prepare GRN is scoped by organization and PO company code (and property routing). An operating unit is not required.

---

## Module boundary (identify, do not duplicate)

| Step | Owner | Must not live in |
| --- | --- | --- |
| Scan / Upload | Mobile `scan.tsx` + `POST /api/v1/documents/invoices` | Ariba, routing, inventory |
| OCR (mobile + backend) | `mobile-ocr.ts`, `invoice-parser.ts`, `AdvancedInvoiceOcrService`, `InvoiceOcrFieldReader` | `AribaPostGrnAdapter`, Integration Routing |
| Invoice extraction result | `BasicOcrResponse`, `AdvancedInvoiceExtractionResponse`, scan-session merge | GRN post adapters |
| Supplier resolution | `invoice-review-state.ts` `findUniqueSupplierMatch` + Supplier Master `GET /api/v1/suppliers` | Ariba SOAP |
| PO matching | `invoice-review-state.ts` `autoSelectPurchaseOrder` + `GET /api/v1/purchase-orders/search` | Integration Route Resolver |
| Invoice Review UI | `invoice-review.tsx` | Cloud `/five` (this screen is Mobile-only) |
| View Document | Invoice Review + `GET /api/v1/documents/{id}/content` | POST GRN |
| Re-Read Invoice | `forceAdvanced` on the same document; `POST /api/v1/invoices/advanced-extract` | New document upload |
| Save Invoice | `invoice-review-save.ts` → `UploadInvoiceAsync` | `PostGrnAsync` |
| Save and Continue navigation | `invoiceSaveNavigation` → `/receive/finalize-grn` | Ariba / routing |

`OperationalService` is a shared backend class. Treat **Upload / OCR / GetInvoice / PO search** as the protected Invoice Receiving surface. Treat **PrepareGrn / PostGrn** as downstream GRN posting. Do not fold OCR/review into posting, and do not fold posting into review.

---

## Backend services

- `apps/api/Services/OperationalServices.cs` — `UploadInvoiceAsync`, `ExtractBasicInvoiceAsync`, `RunAdvancedInvoiceExtractionFileAsync`, `RunAdvancedInvoiceExtractionAsync`, invoice getters. Downstream: `PrepareGrnAsync`, `PostGrnAsync` (consume `InvoiceId`; do not change OCR).
- `apps/api/Services/AdvancedInvoiceOcrService.cs` — backend Advanced OCR. Class declaration must remain `public sealed class AdvancedInvoiceOcrService(`.
- `apps/api/Services/InvoiceOcrFieldReader.cs` — scored header/line field extraction.
- `apps/api/Services/InvoiceExtractionPipeline.cs` — extraction provider pipeline.
- `apps/api/Controllers/OperationsController.cs` — Invoice Review HTTP surface listed below.

Downstream (must not import or duplicate Invoice Review):

- `apps/api/Services/AribaPostGrnAdapter.cs` — posts from saved `GoodsReceipt` + PO `SourceLastChangedAt`.
- `apps/api/Services/IntegrationRouteResolver.cs` — routes by organization + Company Code + process type.
- `apps/api/Services/IntegrationRouteService.cs` — route administration only.

---

## Frontend / Mobile files

- `artifacts/sila-me-mobile/app/(tabs)/receive/scan.tsx`
- `artifacts/sila-me-mobile/app/(tabs)/receive/invoice-review.tsx`
- `artifacts/sila-me-mobile/app/(tabs)/receive/finalize-grn.tsx` (boundary: quantities + POST GRN only)
- `artifacts/sila-me-mobile/services/invoice-review-state.ts`
- `artifacts/sila-me-mobile/services/invoice-review-save.ts`
- `artifacts/sila-me-mobile/services/invoice-parser.ts`
- `artifacts/sila-me-mobile/services/mobile-ocr.ts`
- `artifacts/sila-me-mobile/services/invoice-receiving-contract.ts` — frozen field contract for agents
- `artifacts/sila-me-mobile/services/invoice-save-errors.ts`
- `artifacts/sila-me-mobile/providers/scan-session.ts`
- `artifacts/sila-me-mobile/providers/ScanSessionProvider.tsx`
- `artifacts/sila-me-mobile/components/receive-ui.tsx` — `ReceiveShell` layout used by Invoice Review (do not restore a sticky header that collapses RN-web)
- `artifacts/sila-me-mobile/services/grn-posting.ts` — downstream `prepareGrn` / `postGoodsReceipt` from saved ids

Scanner helpers used by Scan (do not rewrite Invoice Review when touching them): `document-scanner.ts`, `mobile-scanner.ts`, `ocr.ts` / `ocr.web.ts` / `ocr.native.ts`.

Cloud `artifacts/sila-me-cloud` does **not** host Invoice Review. Do not port this screen into `/five`.

---

## API endpoints (Invoice Review contract)

| Method | Path | Role |
| --- | --- | --- |
| POST | `/api/v1/documents/invoices` | Save reviewed invoice (FormData). Returns `Document` with non-empty `invoiceId`. |
| POST | `/api/v1/documents/basic-extract` | Basic OCR (no persist as the review save) |
| POST | `/api/v1/invoices/advanced-extract` | Advanced OCR on the current file (Re-read / fallback) |
| POST | `/api/v1/documents/{id}/advanced-extract` | Advanced OCR on stored document |
| GET | `/api/v1/documents/{id}` | Document metadata + `invoiceId` |
| GET | `/api/v1/documents/{id}/content` | View Document |
| GET | `/api/v1/invoices/{id}` | Authoritative invoice after save |
| GET | `/api/v1/invoices/{id}/basic-extraction` | Stored basic extraction |
| GET | `/api/v1/suppliers` | Supplier Master for ID/TRN/name resolve |
| GET | `/api/v1/purchase-orders/search?openOnly=true&supplierId=` | Eligible Open POs for resolved supplier |
| GET | `/api/v1/purchase-orders/{poNumber}` | Exact/contains PO lookup |

Downstream (not Invoice Review):

| Method | Path | Role |
| --- | --- | --- |
| POST | `/api/v1/grns/prepare` | Build GRN from saved `invoiceId` |
| POST | `/api/v1/goods-receipts/{id}/post` | POST GRN then integration |

---

## DTO / contracts

Backend (`apps/api/DTOs/`):

- `DocumentResponse` — `id`, `invoiceId`, `saveStatus`, `nextStep`, `message`
- `InvoiceResponse` / `InvoiceLineResponse` — document, supplier, invoice, PO, amounts, lines, `goodsReceiptId`
- `BasicOcrResponse` / `BasicInvoiceExtractionResponse`
- `AdvancedInvoiceHeaderResponse` / `AdvancedInvoiceLineResponse` / `AdvancedInvoiceExtractionResponse` / `AdvancedInvoiceValidationResponse`
- `SupplierResponse`, purchase-order search DTOs

Mobile generated mirrors (do not silently rename fields):

- `@workspace/api-client-react` `Document`, `Invoice`, `InvoiceLine`, `BasicOcrResponse`, `AdvancedInvoiceExtractionResponse`, `Supplier`, `PurchaseOrder`
- `artifacts/sila-me-mobile/services/invoice-receiving-contract.ts` documents the frozen Invoice Review field set

Save FormData (authoritative reviewed payload): organization/unit, sourceChannel `MOBILE_SCANNER`, scanSessionId, supplierId/name/TRN, supplierInvoiceNumber, invoiceDate (ISO DateOnly), purchaseOrderNumber, `noPurchaseOrder=false`, amounts, currency, invoiceLinesJson, Idempotency-Key `mobile-save-{session.id}`.

---

## Future Cursor tasks

Respect `docs/PROTECTED_INVOICE_FLOW.md`. Do not modify protected files.

Ariba posting starts from the saved `GoodsReceiptId`. Integration routing starts from Company Code. Neither needs to modify Invoice Review or OCR.
