# SILA ME Cloud Frontend Baseline

Baseline captured on September 15, 2026 before the Cloud frontend foundation expansion.

## Repository state

- Branch: `main`
- Baseline commit: `acf704e8ea18b94e3e99c04bb42823fdd2f31205`
- Mobile, API, and shared authorization work already present in the repository must remain functional.
- The working tree contained the attached product brief only before this frontend pass.

## Current application

- Cloud artifact: `artifacts/sila-me-cloud`
- Stack: React, Vite, TypeScript, Wouter, TanStack Query, shared Orval-generated API client.
- Entry points: `src/main.tsx`, `src/App.tsx`.
- Authentication: existing cookie-backed Cloud session flow is preserved in `SilaCloudLayout` and `pages/login.tsx`.
- API client: `@workspace/api-client-react`, with generated hooks and request credentials handled by the existing pages/components.
- Design tokens: `src/index.css` plus `@workspace/design-tokens/tokens.css`; the current UI uses a white surface, SILA blue, dark navy text, light borders, and Inter.
- Brand asset: `src/assets/sila-logo.png`, surfaced through `src/components/cloud-mark.tsx`.

## Existing reusable UI

- `src/components/sila-layout.tsx`: authenticated shell, responsive sidebar, operating-unit context, notification/help affordances, user menu, permission-aware administration navigation, and logout.
- `src/components/sila-ui.tsx`: page header, data table wrapper, empty state, query error, status badge, and development API-pending marker.
- `src/components/permission.tsx`: route permission guard and access checks.
- `src/components/error-boundary.tsx`: route-level error boundary.
- Existing shadcn/Radix primitives under `src/components/ui`.

## Existing working pages

- `/dashboard`: live health, invoice, GRN, access-scope, and invoice-exposure signals.
- `/receiving/purchase-orders` and `/receiving/purchase-orders/:poNumber`: live purchase-order list/detail.
- `/receiving/invoices` and `/receiving/invoices/:id`: live invoice list/detail, stored PDF access, and extraction controls.
- `/receiving/goods-receipts` and `/receiving/goods-receipts/:id`: live GRN list/detail.
- `/admin/users` and `/admin/users/:userId`: live user provisioning, access preview, and user detail.
- `/admin/roles`: live role and permission catalog.
- `/admin/organization`: existing organization view.
- `/login`: existing Cloud login and session restoration behavior.
- `/404`: existing not-found page fallback.

## Current gaps to address in this pass

- Navigation and route names need to follow the canonical Cloud product route contract while preserving existing working bookmarks.
- Future Menu Engineering, Inventory, Procurement, Documents, Approvals, Analytics, and remaining Administration pages are currently lightweight API-pending shells.
- Dashboard needs to communicate the broader hospitality operations flow without fabricating unavailable module data.
- Shared shell interactions need stronger expanded/collapsed persistence, responsive navigation, context selectors, and consistent API-pending states.
- A frontend readiness matrix must document which pages are connected to real API data and which are UI-ready pending backend support.

## Protected scope

- Do not modify Mobile or scanner/OCR/PDF implementation.
- Do not replace Cloud cookie authentication or session management.
- Do not create backend Menu Engineering, Forecast, or Analytics APIs.
- Do not invent production data for API-pending modules.