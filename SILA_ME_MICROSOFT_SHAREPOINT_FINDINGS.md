# Microsoft SharePoint integration findings

## Current defect

The Cloud Add User flow previously accepted a Microsoft destination URL and sent it as a pending storage assignment. It did not authenticate Microsoft, obtain a delegated Graph token, resolve a SharePoint site or document library, browse/select a folder, or perform a read/write test. A URL alone therefore could not prove that the destination was usable.

## Implemented boundary

- Microsoft authorization is started and completed by the API.
- OAuth state is cryptographically random, hashed before persistence, scoped to the initiating user and organization, expires after ten minutes, and is single-use.
- Add User drafts are kept server-side for the OAuth round trip. Password and token-like fields are stripped before persistence.
- Microsoft access tokens are never returned to the browser. Persisted credentials are encrypted with an environment-provided key.
- `CONNECTED` is assigned only after Graph site resolution, library/folder resolution, and a temporary write/delete test succeed.
- User creation rejects a Microsoft assignment unless its organization connection is `CONNECTED`.
- Diagnostic messages use `[MS-SP]` and do not include authorization codes, tokens, or secrets.

## Required environment configuration

The API must be configured with:

- `Microsoft__TenantId`
- `Microsoft__ClientId`
- `Microsoft__ClientSecret`
- `Microsoft__RedirectUri` (must exactly match the registered API callback URL)
- `Microsoft__TokenEncryptionKey` (a stable secret used to encrypt stored credential material)
- Optional: `Microsoft__Scopes`

The Microsoft app registration must grant the delegated Graph scopes required by the configured scope set and allow the redirect URI. Real SharePoint validation remains blocked until these values and Microsoft consent are available.

## Explicitly protected scope

This change does not modify Mobile authentication, scanning, OCR, PDF generation, purchase-order matching, GRN posting, inventory behavior, or Bala’s user record.