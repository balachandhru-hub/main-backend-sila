const fs = require("node:fs");
const path = require("node:path");

const root = path.resolve(__dirname, "..", "api-zod", "src");
const typesIndex = path.join(root, "generated", "types", "index.ts");
const conflictingFiles = new Set([
  "activateApiIntegrationParams",
  "deactivateApiIntegrationParams",
  "discoverIntegrationSchemaParams",
  "getApiIntegrationParams",
  "getIntegrationDataUpdateParams",
  "getIntegrationSchemaParams",
  "listIntegrationMappingsParams",
  "pullApiIntegrationParams",
  "saveIntegrationMappingsParams",
  "testApiIntegrationParams",
  "updateApiIntegrationParams",
  "getSupplierMasterParams",
  "getPurchaseOrderParams",
  "updateSupplierMasterParams",
  "commitIntegrationImportParams",
  "downloadIntegrationImportCorrectionReportParams",
  "exportIntegrationDataParams",
  "getIntegrationImportTemplateParams",
  "previewIntegrationImportParams",
  "previewIntegrationImportBody",
  "uploadOrganizationLogoBody",
]);

const filtered = fs.readFileSync(typesIndex, "utf8")
  .split(/\r?\n/)
  .filter(line => {
    const match = line.match(/^export \* from ['"]\.\/([^'"]+)['"];?$/);
    return !match || !conflictingFiles.has(match[1]);
  })
  .join("\n");
fs.writeFileSync(typesIndex, filtered);

// Keep every generated schema publicly reachable while avoiding the duplicate
// operation-parameter names that TypeScript sees in generated/api.ts.
fs.writeFileSync(
  path.join(root, "index.ts"),
  'export * from "./generated/api";\nexport * from "./generated/types";\n',
);