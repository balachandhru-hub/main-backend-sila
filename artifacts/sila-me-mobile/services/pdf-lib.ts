// Use pdf-lib's browser-ready bundled ESM build. The package's source ESM
// entry imports tslib dynamically in a way that Expo Web can evaluate as null.
// The bundled build contains its helpers and works consistently in Expo Web
// and native Metro bundles.
// @ts-expect-error pdf-lib does not publish declarations for this bundled subpath.
import { PDFDocument } from 'pdf-lib/dist/pdf-lib.esm.js';

export { PDFDocument };