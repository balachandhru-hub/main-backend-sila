export const SYSTEM_LABELS: Record<string, string> = {
  SAP_S4HANA: 'SAP S/4HANA',
  SAP_ARIBA: 'SAP Ariba',
  ORACLE: 'Oracle',
  ODOO: 'Odoo',
  CUSTOM: 'Custom API',
  OTHER: 'Other',
};

export const INTERFACE_LABELS: Record<string, string> = {
  REST: 'REST',
  SOAP: 'SOAP',
  ODATA_V2: 'OData V2',
  ODATA_V4: 'OData V4',
};

export const AUTH_LABELS: Record<string, string> = {
  NONE: 'None',
  BASIC: 'Basic',
  API_KEY: 'API Key',
  BEARER_STATIC: 'Static Bearer',
  BEARER_TOKEN: 'Static Bearer',
  TOKEN_API: 'Token API',
  CUSTOM_TOKEN_ENDPOINT: 'Token API',
  OAUTH2_CLIENT_CREDENTIALS: 'OAuth2 Client Credentials',
  CUSTOM_HEADER: 'Custom Header',
};

export const INTERFACES_BY_SYSTEM: Record<string, string[]> = {
  SAP_S4HANA: ['ODATA_V2', 'ODATA_V4', 'REST'],
  SAP_ARIBA: ['REST', 'SOAP'],
  ORACLE: ['REST', 'SOAP'],
  ODOO: ['REST'],
  CUSTOM: ['REST', 'SOAP', 'ODATA_V2', 'ODATA_V4'],
  OTHER: ['REST', 'SOAP', 'ODATA_V2', 'ODATA_V4'],
};

export const AUTH_MATRIX: Record<string, string[]> = {
  'SAP_S4HANA:ODATA_V2': ['BASIC', 'OAUTH2_CLIENT_CREDENTIALS'],
  'SAP_S4HANA:ODATA_V4': ['BASIC', 'OAUTH2_CLIENT_CREDENTIALS'],
  'SAP_S4HANA:REST': ['BASIC', 'OAUTH2_CLIENT_CREDENTIALS', 'BEARER_STATIC', 'API_KEY'],
  'SAP_ARIBA:SOAP': ['BASIC'],
  'SAP_ARIBA:REST': ['TOKEN_API', 'API_KEY', 'BASIC', 'OAUTH2_CLIENT_CREDENTIALS'],
  'ORACLE:REST': ['BASIC', 'BEARER_STATIC', 'OAUTH2_CLIENT_CREDENTIALS', 'API_KEY', 'TOKEN_API'],
  'ORACLE:SOAP': ['BASIC'],
  'ODOO:REST': ['NONE', 'BASIC', 'API_KEY', 'OAUTH2_CLIENT_CREDENTIALS', 'TOKEN_API'],
  'CUSTOM:REST': ['NONE', 'BASIC', 'API_KEY', 'BEARER_STATIC', 'OAUTH2_CLIENT_CREDENTIALS', 'TOKEN_API', 'CUSTOM_HEADER'],
  'CUSTOM:SOAP': ['NONE', 'BASIC', 'API_KEY', 'BEARER_STATIC', 'OAUTH2_CLIENT_CREDENTIALS', 'TOKEN_API', 'CUSTOM_HEADER'],
  'CUSTOM:ODATA_V2': ['NONE', 'BASIC', 'API_KEY', 'BEARER_STATIC', 'OAUTH2_CLIENT_CREDENTIALS', 'TOKEN_API', 'CUSTOM_HEADER'],
  'CUSTOM:ODATA_V4': ['NONE', 'BASIC', 'API_KEY', 'BEARER_STATIC', 'OAUTH2_CLIENT_CREDENTIALS', 'TOKEN_API', 'CUSTOM_HEADER'],
  'OTHER:REST': ['NONE', 'BASIC', 'API_KEY', 'BEARER_STATIC', 'OAUTH2_CLIENT_CREDENTIALS', 'TOKEN_API', 'CUSTOM_HEADER'],
  'OTHER:SOAP': ['NONE', 'BASIC', 'API_KEY', 'BEARER_STATIC', 'OAUTH2_CLIENT_CREDENTIALS', 'TOKEN_API', 'CUSTOM_HEADER'],
  'OTHER:ODATA_V2': ['NONE', 'BASIC', 'API_KEY', 'BEARER_STATIC', 'OAUTH2_CLIENT_CREDENTIALS', 'TOKEN_API', 'CUSTOM_HEADER'],
  'OTHER:ODATA_V4': ['NONE', 'BASIC', 'API_KEY', 'BEARER_STATIC', 'OAUTH2_CLIENT_CREDENTIALS', 'TOKEN_API', 'CUSTOM_HEADER'],
};

export const API_TYPES = [
  'GET_SUPPLIER', 'GET_MATERIAL', 'GET_PO', 'POST_PO', 'UPDATE_PO', 'GET_GRN', 'POST_GRN', 'CANCEL_GRN',
  'GET_INVOICE', 'POST_INVOICE', 'GET_STOCK', 'UPDATE_STOCK', 'GET_COMPANY_CODE', 'GET_PLANT', 'GET_STORAGE_LOCATION',
  'CUSTOM',
];

export const PAGE_SECTIONS = ['GENERAL', 'API DETAILS', 'AUTHENTICATION', 'TOKEN CONFIGURATION', 'ADVANCED', 'TEST & SAVE'] as const;
export const REMOVED_FEATURES = [
  'Purchase Order screen', 'Supplier screen', 'PO upload', 'Supplier upload', 'PO scheduler', 'Supplier scheduler',
  'Source Data', 'Source Schema', 'Destination Schema', 'Request Mapping', 'Response Mapping', 'Mapping Designer',
  'Mapping Preview', 'Routing configuration', 'Execution configuration', 'Schema discovery',
] as const;
export const STEP1_TEST_MESSAGE = 'Connection testing will be enabled in Step 2.';
export const CONNECTION_NOT_TESTED = 'NOT TESTED';
export const CONNECTION_SUCCESSFUL = 'CONNECTION SUCCESSFUL';
export const CONNECTION_FAILED = 'CONNECTION FAILED';
export const DRAFT_STATUS = 'DRAFT';
export const VALIDATED_STATUS = 'VALIDATED';

export type ConfigForm = {
  name: string;
  description: string;
  systemKind: string;
  processType: string;
  protocol: string;
  baseUrl: string;
  resourcePath: string;
  servicePath: string;
  entitySet: string;
  httpMethod: string;
  authenticationType: string;
  username: string;
  password: string;
  clientId: string;
  clientSecret: string;
  bearerToken: string;
  apiKey: string;
  apiKeyHeader: string;
  apiKeyPlacement: 'HEADER' | 'QUERY';
  customHeaderName: string;
  tokenEndpoint: string;
  tokenHttpMethod: string;
  tokenScope: string;
  tokenHeadersText: string;
  tokenBodyText: string;
  tokenResponsePath: string;
  tokenType: string;
  tokenExpiryPath: string;
  authorizationHeaderName: string;
  contentType: string;
  accept: string;
  timeoutSeconds: number;
  csrfRequired: boolean;
  csrfFetchMethod: string;
  csrfFetchPath: string;
  csrfHeaderName: string;
  csrfHeaderValue: string;
  csrfResponseHeader: string;
  wsdlUrl: string;
  soapOperation: string;
  soapAction: string;
  soapVersion: string;
  secretConfigured: boolean;
};

export function isWriteMethod(method: string) {
  return ['POST', 'PUT', 'PATCH', 'DELETE'].includes(method.toUpperCase());
}

export function isOData(protocol: string) {
  return protocol.startsWith('ODATA');
}

export function interfacesFor(system: string) {
  return INTERFACES_BY_SYSTEM[system] ?? ['REST'];
}

export function authFor(system: string, protocol: string) {
  return AUTH_MATRIX[`${system}:${protocol}`] ?? ['NONE', 'BASIC'];
}

export function toApiAuth(auth: string) {
  if (auth === 'TOKEN_API') return 'CUSTOM_TOKEN_ENDPOINT';
  if (auth === 'BEARER_STATIC') return 'BEARER_TOKEN';
  return auth;
}

export function fromApiAuth(auth: string) {
  if (auth === 'CUSTOM_TOKEN_ENDPOINT') return 'TOKEN_API';
  if (auth === 'BEARER_TOKEN') return 'BEARER_STATIC';
  return auth;
}

export function defaultsFor(system: string, processType: string, protocol: string): Pick<ConfigForm, 'authenticationType' | 'httpMethod' | 'contentType' | 'accept' | 'csrfRequired'> {
  const validProtocol = interfacesFor(system).includes(protocol) ? protocol : interfacesFor(system)[0];
  const method = processType.startsWith('GET_') ? 'GET' : 'POST';
  const soap = validProtocol === 'SOAP';
  return {
    authenticationType: authFor(system, validProtocol)[0],
    httpMethod: method,
    contentType: soap ? 'text/xml' : 'application/json',
    accept: soap ? 'text/xml' : 'application/json',
    csrfRequired: system === 'SAP_S4HANA' && isOData(validProtocol) && isWriteMethod(method),
  };
}

export function applySelection(form: ConfigForm, patch: Partial<ConfigForm>): ConfigForm {
  const next = { ...form, ...patch };
  const protocol = interfacesFor(next.systemKind).includes(next.protocol) ? next.protocol : interfacesFor(next.systemKind)[0];
  const defaults = defaultsFor(next.systemKind, next.processType, protocol);
  const authenticationType = patch.authenticationType && authFor(next.systemKind, protocol).includes(patch.authenticationType)
    ? patch.authenticationType
    : (patch.systemKind || patch.protocol
      ? defaults.authenticationType
      : (authFor(next.systemKind, protocol).includes(next.authenticationType) ? next.authenticationType : defaults.authenticationType));
  const csrfRequired = next.systemKind === 'SAP_S4HANA' && isOData(protocol) && isWriteMethod(next.httpMethod || defaults.httpMethod)
    ? (patch.csrfRequired ?? defaults.csrfRequired)
    : false;
  return {
    ...next,
    protocol,
    authenticationType,
    httpMethod: patch.httpMethod ?? (patch.processType || patch.systemKind ? defaults.httpMethod : next.httpMethod),
    contentType: patch.contentType ?? ((patch.protocol || patch.systemKind) ? defaults.contentType : next.contentType),
    accept: patch.accept ?? ((patch.protocol || patch.systemKind) ? defaults.accept : next.accept),
    csrfRequired,
  };
}

export function showCsrf(form: Pick<ConfigForm, 'systemKind' | 'protocol' | 'httpMethod'>) {
  return form.systemKind === 'SAP_S4HANA' && isOData(form.protocol) && isWriteMethod(form.httpMethod);
}

export function showTokenApi(form: Pick<ConfigForm, 'authenticationType'>) {
  return ['TOKEN_API', 'CUSTOM_TOKEN_ENDPOINT'].includes(form.authenticationType);
}

export function showOAuth(form: Pick<ConfigForm, 'authenticationType'>) {
  return form.authenticationType === 'OAUTH2_CLIENT_CREDENTIALS';
}

export function visibleApiFields(protocol: string): string[] {
  if (protocol === 'SOAP') return ['baseUrl', 'wsdlUrl', 'soapOperation', 'soapAction', 'soapVersion', 'contentType'];
  if (isOData(protocol)) return ['baseUrl', 'servicePath', 'entitySet', 'httpMethod', 'contentType', 'accept', 'timeoutSeconds'];
  return ['baseUrl', 'resourcePath', 'httpMethod', 'contentType', 'accept', 'timeoutSeconds'];
}

export function validateConfigForm(form: ConfigForm, editingWithSecret = false): string[] {
  const errors: string[] = [];
  if (!form.name.trim()) errors.push('Configuration Name is required.');
  if (!form.systemKind) errors.push('System is required.');
  if (!form.processType) errors.push('API Type is required.');
  if (!form.protocol) errors.push('Interface Type is required.');
  const fields = visibleApiFields(form.protocol);
  if (fields.includes('baseUrl') && !form.baseUrl.trim()) errors.push(form.protocol === 'SOAP' ? 'Endpoint URL is required.' : 'Base URL is required.');
  if (fields.includes('resourcePath') && !form.resourcePath.trim()) errors.push('Resource Path is required.');
  if (fields.includes('servicePath') && !form.servicePath.trim()) errors.push('Service Path is required.');
  if (fields.includes('entitySet') && !form.entitySet.trim()) errors.push('Entity Set is required.');
  if (form.authenticationType === 'BASIC') {
    if (!form.username.trim()) errors.push('Username is required.');
    if (!form.password.trim() && !editingWithSecret) errors.push('Password is required.');
  }
  if (form.authenticationType === 'API_KEY') {
    if (!form.apiKeyHeader.trim()) errors.push('Key Name is required.');
    if (!form.apiKey.trim() && !editingWithSecret) errors.push('Key Value is required.');
  }
  if (['BEARER_STATIC', 'BEARER_TOKEN'].includes(form.authenticationType) && !form.bearerToken.trim() && !editingWithSecret) {
    errors.push('Bearer Token is required.');
  }
  if (showOAuth(form)) {
    if (!form.tokenEndpoint.trim()) errors.push('Token URL is required.');
    if (!form.clientId.trim()) errors.push('Client ID is required.');
    if (!form.clientSecret.trim() && !editingWithSecret) errors.push('Client Secret is required.');
  }
  if (showTokenApi(form)) {
    if (!form.tokenEndpoint.trim()) errors.push('Token URL is required.');
    if (!form.tokenResponsePath.trim()) errors.push('Access Token Response Path is required.');
  }
  if (showCsrf(form) && form.csrfRequired && !form.csrfFetchPath.trim()) errors.push('CSRF Token Fetch Path is required.');
  return errors;
}

export function testConnectionResult() {
  return { success: false, fake: false, message: STEP1_TEST_MESSAGE };
}

export type ConnectionCheck = { name: string; success: boolean; code?: string | null };
export type ConnectionTestResult = {
  success: boolean;
  testId?: string | null;
  status: string;
  checks?: ConnectionCheck[];
  testedAt?: string;
  expiresAt?: string | null;
  errorCode?: string | null;
  message?: string | null;
  requestJson?: string | null;
};

export function parsePairs(text: string): Record<string, string> | null {
  const result: Record<string, string> = {};
  for (const line of text.split('\n')) {
    const trimmed = line.trim();
    if (!trimmed) continue;
    const index = trimmed.indexOf('=') >= 0 ? trimmed.indexOf('=') : trimmed.indexOf(':');
    if (index <= 0) continue;
    result[trimmed.slice(0, index).trim()] = trimmed.slice(index + 1).trim();
  }
  return Object.keys(result).length ? result : null;
}

export function formatPairs(value: Record<string, string> | null | undefined) {
  if (!value) return '';
  return Object.entries(value).map(([key, item]) => `${key}=${item}`).join('\n');
}
