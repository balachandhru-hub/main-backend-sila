# SILA ME Cloud frontend readiness

`CONNECTED` means a page consumes an existing generated Cloud hook. `API_PENDING` means a complete, navigable shell with honest empty states and no fabricated production data.

## Shared foundation

| Surface | UI status | API status | Data source | Permission | Notes |
| --- | --- | --- | --- | --- | --- |
| Authenticated shell | Ready | CONNECTED | Cloud session, access context, logout hooks | Authenticated session | Cookie auth is unchanged. Sidebar collapse and group state persist locally; drawer behavior is responsive. |
| Operating-unit context | Ready | CONNECTED | `useGetAccessContext` | Authenticated session | Scope selector is populated from accessible units. |
| Loading / error / empty / 403 / 404 | Ready | CONNECTED | Query state and permission context | Authenticated session | Shared shells expose retry and stable test IDs. |

## Canonical modules and pages

| Canonical page | UI status | API status | Data source | Permission | Notes |
| --- | --- | --- | --- | --- | --- |
| `/dashboard` | Ready | CONNECTED | Health, access context, invoices, GRNs | Authenticated session | Operational command center with live receiving signals and pending chain stages. |
| `/menu-engineering/menu-planning` | Ready | API_PENDING | None | Authenticated session | Filter, tabs and empty state; menu service not connected. |
| `/menu-engineering/recipes` | Ready | API_PENDING | None | Authenticated session | Recipe workspace shell. |
| `/menu-engineering/recipes/:id` | Ready | API_PENDING | None | Authenticated session | Recipe detail shell. |
| `/menu-engineering/ingredients` | Ready | API_PENDING | None | Authenticated session | Ingredient workspace shell. |
| `/menu-engineering/portion-planning` | Ready | API_PENDING | None | Authenticated session | Portion planning shell. |
| `/menu-engineering/demand-forecast` | Ready | API_PENDING | None | Authenticated session | Forecast shell without unsupported values. |
| `/menu-engineering/menu-performance` | Ready | API_PENDING | None | Authenticated session | Performance shell. |
| `/inventory/stock` | Ready | API_PENDING | None | Authenticated session | Stock shell; no fabricated balances. |
| `/inventory/stock/:id` | Ready | API_PENDING | None | Authenticated session | Stock detail shell. |
| `/inventory/count` | Ready | API_PENDING | None | Authenticated session | Count session shell. |
| `/inventory/count/:id` | Ready | API_PENDING | None | Authenticated session | Count detail shell. |
| `/inventory/transfers` | Ready | API_PENDING | None | Authenticated session | Transfer shell. |
| `/inventory/goods-issue` | Ready | API_PENDING | None | Authenticated session | Goods issue shell. |
| `/inventory/damage-waste` | Ready | API_PENDING | None | Authenticated session | Damage and waste shell. |
| `/inventory/batches` | Ready | API_PENDING | None | Authenticated session | Batch and expiry shell. |
| `/purchasing/requirements` | Ready | API_PENDING | None | Authenticated session | Requirements shell. |
| `/purchasing/suggestions` | Ready | API_PENDING | None | Authenticated session | Suggestions shell. |
| `/purchasing/purchase-orders` | Ready | CONNECTED | Purchase-order generated hooks | Authenticated session | Live PO list; legacy receiving alias retained. |
| `/purchasing/purchase-orders/:id` | Ready | CONNECTED | `useGetPurchaseOrder` | Authenticated session | Live PO detail; canonical `id` and legacy `poNumber` supported. |
| `/purchasing/suppliers` | Ready | API_PENDING | None | Authenticated session | Supplier shell. |
| `/receiving/receive` | Ready | API_PENDING | None | Authenticated session | Receiving entry shell pending workflow API. |
| `/receiving/invoices` | Ready | CONNECTED | Invoice/document generated hooks | Authenticated session | Existing invoice list and upload preserved. |
| `/receiving/invoices/:id` | Ready | CONNECTED | Invoice, extraction and matching hooks | Authenticated session | Existing detail, stored PDF and correction behavior preserved. |
| `/receiving/goods-receipts` | Ready | CONNECTED | GRN generated hooks | Authenticated session | Existing list and posting behavior preserved. |
| `/receiving/goods-receipts/:id` | Ready | CONNECTED | GRN generated hooks | Authenticated session | Existing detail and validation behavior preserved. |
| `/receiving/exceptions` | Ready | API_PENDING | None | Authenticated session | Exception triage shell. |
| `/documents/inbox` | Ready | API_PENDING | None | Authenticated session | Inbox shell. |
| `/documents/invoice-capture` | Ready | API_PENDING | None | Authenticated session | Capture shell; existing invoice upload remains on receiving invoices. |
| `/documents/extraction` | Ready | API_PENDING | None | Authenticated session | Extraction shell. |
| `/documents/archive` | Ready | API_PENDING | None | Authenticated session | Archive shell. |
| `/approvals` | Ready | API_PENDING | None | Authenticated session | Approval queue shell. |
| `/analytics` | Ready | API_PENDING | None | Authenticated session | Analytics landing shell. |
| `/analytics/consumption` | Ready | API_PENDING | None | Authenticated session | Consumption analytics shell. |
| `/analytics/food-cost` | Ready | API_PENDING | None | Authenticated session | Food-cost analytics shell. |
| `/analytics/menu-performance` | Ready | API_PENDING | None | Authenticated session | Menu-performance analytics shell. |
| `/analytics/inventory-variance` | Ready | API_PENDING | None | Authenticated session | Inventory variance shell. |
| `/analytics/waste` | Ready | API_PENDING | None | Authenticated session | Waste analytics shell. |
| `/analytics/purchases` | Ready | API_PENDING | None | Authenticated session | Purchasing analytics shell. |
| `/master-data/materials` | Ready | API_PENDING | None | Authenticated session | Materials shell. |
| `/master-data/ingredients` | Ready | API_PENDING | None | Authenticated session | Ingredients shell. |
| `/master-data/recipes` | Ready | API_PENDING | None | Authenticated session | Recipes shell. |
| `/master-data/menu-items` | Ready | API_PENDING | None | Authenticated session | Menu-item shell. |
| `/master-data/suppliers` | Ready | API_PENDING | None | Authenticated session | Suppliers shell. |
| `/master-data/uom` | Ready | API_PENDING | None | Authenticated session | Unit-of-measure shell. |
| `/master-data/categories` | Ready | API_PENDING | None | Authenticated session | Categories shell. |
| `/master-data/locations` | Ready | CONNECTED | `useGetAccessContext` operating units | `location` | Live location view scoped to the authenticated access context; no location mutations are exposed because the current API has no location write contract. |
| `/admin/users` | Ready | CONNECTED | Access user hooks | `user` | Existing provisioning and list behavior preserved. |
| `/admin/users/:userId` | Ready | CONNECTED | Access user detail hooks | `user` | Existing access preview preserved. |
| `/admin/roles` | Ready | CONNECTED | Role and permission hooks | `role` | Existing role catalog preserved. |
| `/admin/organization` | Ready | CONNECTED | Organization hooks | `organization` | Existing organization view preserved. |
| `/admin/properties` | Ready | API_PENDING | None | `organization` | Property structure shell. |
| `/admin/document-extraction` | Ready | CONNECTED | Extraction-agent hooks | `extraction` | Existing configuration surface preserved. |
| `/admin/integrations` | Ready | CONNECTED | Storage-connection hooks | `integration` | Existing integration surface preserved. |
| `/admin/configuration` | Ready | API_PENDING | None | `configuration` | Canonical shell; plural legacy alias retained. |
| `/admin/audit` | Ready | API_PENDING | None | `audit` | Audit shell pending audit endpoint. |

## Compatibility aliases

Existing bookmarks remain reachable for receiving purchase orders, menu-engineering legacy slugs, inventory counts/adjustments/damage, procurement routes, document delivery/extraction routes, plural admin configurations, and `/master-data`.

## Change boundary

No backend, generated API contract, database, authentication implementation, Mobile code, scanner/OCR/PDF behavior, or external repository was changed. Future module pages must consume generated hooks when the corresponding contracts are available.