---
name: API and Expo runtime quirks
description: Non-obvious runtime constraints for this multi-artifact foundation.
---

The API artifact workflow runs from `artifacts/api-server`, so commands that reference the shared .NET project must use paths relative to that directory. Expo SecureStore is native-only in the browser preview; mobile code should keep SecureStore for native builds and use a non-sensitive web fallback solely for Expo web preview.

**Why:** The first API workflow launch failed because the project path was resolved from the artifact directory, and Expo web crashed when it called the native SecureStore implementation.

**How to apply:** Check managed artifact working directories before editing workflow commands, and guard native-only storage APIs with a platform-specific fallback.

The Mobile PDF generator must use `pdf-lib`'s self-contained bundled ESM build; the package source ESM entry can fail in Expo Web with a null `tslib` `__extends` import.

**Why:** Expo Web's Metro runtime evaluated the source `pdf-lib` entry and threw `Cannot destructure property '__extends' from null or undefined`; the bundled ESM build includes its helpers and loads correctly.

**How to apply:** Import the bundled browser ESM subpath through the scanner PDF wrapper, then verify both the Expo web preview and native bundles after changing scanner dependencies.

Expo SDK 57's legacy FileSystem APIs are native-only in the web preview; browser `blob:`, `data:`, `http:`, and `https:` image URIs must be read with browser fetch/decoding, and generated web PDFs must use Blob URLs.

**Why:** Expo Web throws when `getInfoAsync()` is called for browser-backed scan images, even though the same legacy API remains appropriate for native `file://` and `content://` sources.

**How to apply:** Classify the URI before any FileSystem call, use a browser-runtime guard in addition to `Platform.OS`, keep `content://` materialization native-only, and avoid persisting assumptions that a web Blob URL survives a page reload.

The Nix-hosted .NET SDK can run `dotnet` while a globally installed `dotnet-ef` tool cannot locate the runtime unless `DOTNET_ROOT` points to the SDK's `share/dotnet` directory. The Mobile static build script defaults to Metro port 8081 but supports `EXPO_METRO_PORT` when the mockup sandbox owns that port.

**Why:** These are workspace-level tool and workflow constraints rather than application failures.

**How to apply:** Set `DOTNET_ROOT` for standalone EF CLI commands, and use `EXPO_METRO_PORT` for Mobile static builds when port 8081 is occupied.

The API test project may need a normal `dotnet restore` before `dotnet test --no-restore`; a partially populated NuGet cache can omit xUnit analyzer assemblies. Tests and the running API should not apply migrations concurrently against the shared development database.

**Why:** A stale test cache caused a misleading compiler failure, and simultaneous host startup can make one process attempt a migration while another is applying it.

**How to apply:** Restore the test project once when analyzer metadata is missing, and restart or coordinate the API workflow before running migration-backed integration tests.

Cloud Vite production builds require both `PORT` and the artifact `BASE_PATH` to be supplied when run outside the managed workflow.

**Why:** The Vite configuration intentionally fails fast when either preview-routing value is absent, so a plain package build can report an environment error even when the source is valid.

**How to apply:** Use the registered artifact base path and an available port for standalone Cloud build verification; do not weaken the fail-fast configuration.

Native ML Kit OCR must be isolated behind platform-specific or deferred imports; Expo web/Expo Go does not contain the native text-recognition module.

**Why:** A direct top-level ML Kit import crashed the web preview before routing could render, while a deferred native import kept the preview healthy and preserved OCR for development builds.

**How to apply:** Keep the web path as a no-op/reference-only OCR path, and document that real on-device OCR requires an iOS/Android development build rather than Expo Go.

Native scan sessions can contain iOS Photos or asset-library URI schemes that are not directly readable by the PDF byte loader.

**Why:** A restored or picker-provided native image URI can be valid for React Native image rendering while still being rejected by URI classification and FileSystem byte reads.

**How to apply:** On native only, materialize unknown image URI schemes through Expo ImageManipulator into a cached JPEG before reading bytes for PDF generation; keep browser URI handling unchanged.

Multipart uploads built from browser or Expo Go `Blob` values do not reliably carry a useful filename; generated file endpoints must append an explicit filename, and backend validation should use MIME/signature checks rather than extension alone.

**Why:** Basic invoice OCR was rejected before reaching the OCR provider because the Blob arrived with a generic filename such as `blob`, even though it contained a non-empty PDF.

**How to apply:** For PDF uploads, send an explicit `.pdf` multipart filename, preserve `application/pdf`, log only size/scheme/status diagnostics, and reject empty or invalid signatures with a structured document error.

The Mobile Expo Go fallback can use the authenticated API's local OCR path, which renders scanned PDFs with `pdftoppm` and recognizes them with Tesseract; this is intentionally not an external OCR provider.

**Why:** Expo Go does not ship the native ML Kit module, so the backend fallback must remain self-contained and the runtime must keep the local OCR executables available.

**How to apply:** Preserve the Tesseract Nix dependency and verify `pdftoppm` remains available whenever the backend OCR fallback is changed or deployed.

Shared Orval generation cleans the React client output directory before recreating generated files; a running Expo Metro process can cache the brief missing-module state even after generation succeeds.

**Why:** Expo reported `./generated/api` missing after a successful client regeneration, and a clean workflow restart resolved the stale resolver state without source changes.

**How to apply:** Restart Expo after regenerating shared API clients, then confirm Metro bundles without `UnableToResolveError`.

OpenAPI operation-parameter schemas can collide with generated Zod API exports when a new operation is added; the workspace's post-generation export filter must include each intentional collision.

**Why:** Supplier Master route generation produced duplicate parameter exports even though the OpenAPI document and generated client were otherwise valid.

**How to apply:** After adding operation IDs, run the full api-spec codegen/typecheck and extend the export filter rather than hand-editing generated files.

Mobile receiving treats an intentional no-PO choice as an explicit review state, not as a missing OCR value; the server must accept that flag while keeping PO-backed GRN posting blocked.

**Why:** Requiring a PO at upload prevented invoices with no PO from being saved, while silently treating an empty OCR PO as intentional would allow accidental bypasses.

**How to apply:** Keep the no-PO selection visible in review, persist it through upload/update, preserve it during rereads, and finish no-PO review without entering PO-dependent GRN steps.