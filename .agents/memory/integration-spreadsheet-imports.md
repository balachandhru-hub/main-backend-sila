---
name: Integration spreadsheet imports
description: Rules for PO and supplier spreadsheet updates.
---

Spreadsheet updates must be previewed and normalized before commit, then revalidated and committed in one transaction through the same upsert and execution-history path as API imports.

**Why:** Spreadsheet data is user-supplied and can contain mixed valid and invalid rows; allowing mutation during preview would make partial overwrites possible.

**How to apply:** Keep templates/export columns canonical, reject invalid or duplicate keys before mutation, and preserve the Excel execution trigger when extending the import workflow.

PO imports must resolve an existing active supplier in the organization/entity master; never create a guessed supplier or invent currency, totals, item descriptions, UOMs, or other required values.

**Why:** Supplier identity and financial fields are authoritative master data, and silent defaults make PO matching and GRN decisions unsafe.

**How to apply:** Validate ISO currency and required PO header fields during preview and commit, then reuse the same supplier and PO tables for API imports, spreadsheet imports, and exports.