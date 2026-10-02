import assert from 'node:assert/strict';
import test from 'node:test';
import { readFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import {
  API_TYPES, AUTH_MATRIX,   CONNECTION_NOT_TESTED, DRAFT_STATUS, INTERFACES_BY_SYSTEM, PAGE_SECTIONS, REMOVED_FEATURES, STEP1_TEST_MESSAGE,
  applySelection, authFor, fromApiAuth, interfacesFor, showCsrf, showTokenApi,
  toApiAuth, validateConfigForm, visibleApiFields, type ConfigForm,
} from '../lib/integration-designer';

const page = readFileSync(join(dirname(fileURLToPath(import.meta.url)), '../pages/integrations.tsx'), 'utf8');
const empty = (): ConfigForm => applySelection({
  name: '', description: '', systemKind: 'SAP_S4HANA', processType: 'POST_GRN', protocol: 'ODATA_V2',
  baseUrl: '', resourcePath: '', servicePath: '', entitySet: '', httpMethod: 'POST', authenticationType: 'BASIC',
  username: '', password: '', clientId: '', clientSecret: '', bearerToken: '', apiKey: '', apiKeyHeader: 'APIKey',
  apiKeyPlacement: 'HEADER', customHeaderName: 'X-API-Key', tokenEndpoint: '', tokenHttpMethod: 'POST', tokenScope: '',
  tokenHeadersText: '', tokenBodyText: '', tokenResponsePath: 'access_token', tokenType: 'Bearer', tokenExpiryPath: '',
  authorizationHeaderName: 'Authorization', contentType: 'application/json', accept: 'application/json', timeoutSeconds: 30,
  csrfRequired: true, csrfFetchMethod: 'GET', csrfFetchPath: '', csrfHeaderName: 'X-CSRF-Token', csrfHeaderValue: 'Fetch',
  csrfResponseHeader: 'X-CSRF-Token', wsdlUrl: '', soapOperation: '', soapAction: '', soapVersion: '1.1', secretConfigured: false,
}, {});

test('only one integration configuration UI remains on the page', () => {
  assert.equal((page.match(/export default function IntegrationsPage/g) ?? []).length, 1);
  assert.ok(page.includes('Add API Configuration'));
  assert.ok(page.includes('Save Configuration'));
  assert.ok(PAGE_SECTIONS.includes('GENERAL'));
});

test('PO, supplier, mapping, and schema screens are absent', () => {
  for (const feature of REMOVED_FEATURES) assert.ok(!page.includes(feature));
  assert.ok(!page.includes('data-update'));
  assert.ok(!page.includes('Purchase Orders'));
  assert.ok(!page.includes('Source data'));
  assert.ok(!page.includes('Destination schema'));
  assert.ok(!page.includes('Request mapping'));
  assert.ok(!page.includes('Mapping Designer'));
  assert.ok(!page.includes('designer/test'));
  assert.ok(!page.includes('designer/preview'));
});

test('SAP S4 OData shows business API and CSRF for writes, not SOAP', () => {
  const form = applySelection(empty(), { systemKind: 'SAP_S4HANA', protocol: 'ODATA_V2', processType: 'POST_GRN', httpMethod: 'POST' });
  assert.deepEqual(interfacesFor('SAP_S4HANA'), ['ODATA_V2', 'ODATA_V4', 'REST']);
  assert.ok(!interfacesFor('SAP_S4HANA').includes('SOAP'));
  assert.deepEqual([...visibleApiFields(form.protocol)], ['baseUrl', 'servicePath', 'entitySet', 'httpMethod', 'contentType', 'accept', 'timeoutSeconds']);
  assert.equal(showCsrf(form), true);
  assert.equal(form.csrfRequired, true);
  assert.equal(form.authenticationType, 'BASIC');
});

test('SAP Ariba REST shows Token API and hides SOAP/OData fields', () => {
  const form = applySelection(empty(), { systemKind: 'SAP_ARIBA', protocol: 'REST' });
  assert.deepEqual(interfacesFor('SAP_ARIBA'), ['REST', 'SOAP']);
  assert.ok(!interfacesFor('SAP_ARIBA').includes('ODATA_V2'));
  assert.ok(authFor('SAP_ARIBA', 'REST').includes('TOKEN_API'));
  assert.ok(AUTH_MATRIX['SAP_ARIBA:REST'].includes('TOKEN_API'));
  assert.equal(showTokenApi({ authenticationType: form.authenticationType }), true);
  assert.deepEqual([...visibleApiFields(form.protocol)], ['baseUrl', 'resourcePath', 'httpMethod', 'contentType', 'accept', 'timeoutSeconds']);
  assert.equal(showCsrf(form), false);
});

test('SAP Ariba SOAP shows SOAP fields and Basic auth only', () => {
  const form = applySelection(empty(), { systemKind: 'SAP_ARIBA', protocol: 'SOAP' });
  assert.deepEqual(authFor('SAP_ARIBA', 'SOAP'), ['BASIC']);
  assert.deepEqual([...visibleApiFields(form.protocol)], ['baseUrl', 'wsdlUrl', 'soapOperation', 'soapAction', 'soapVersion', 'contentType']);
  assert.equal(showCsrf(form), false);
  assert.equal(showTokenApi(form), false);
});

test('Oracle, Odoo, and Custom keep protocol matrices', () => {
  assert.deepEqual(INTERFACES_BY_SYSTEM.ORACLE, ['REST', 'SOAP']);
  assert.deepEqual(INTERFACES_BY_SYSTEM.ODOO, ['REST']);
  assert.ok(interfacesFor('CUSTOM').includes('ODATA_V4'));
  assert.ok(authFor('CUSTOM', 'REST').includes('API_KEY'));
  assert.ok(authFor('ORACLE', 'REST').includes('OAUTH2_CLIENT_CREDENTIALS'));
});

test('changing system clears an invalid interface type', () => {
  const form = applySelection(empty(), { systemKind: 'SAP_S4HANA', protocol: 'ODATA_V2' });
  const next = applySelection(form, { systemKind: 'SAP_ARIBA' });
  assert.equal(next.protocol, 'REST');
  assert.ok(interfacesFor(next.systemKind).includes(next.protocol));
});

test('required field validation covers S4, token API, and SOAP', () => {
  assert.ok(validateConfigForm(empty()).includes('Configuration Name is required.'));
  const s4 = applySelection(empty(), { name: 'FIVE S4', systemKind: 'SAP_S4HANA', protocol: 'ODATA_V2', httpMethod: 'POST', csrfRequired: true });
  assert.ok(validateConfigForm(s4).some(item => item.includes('Base URL')));
  assert.ok(validateConfigForm(s4).some(item => item.includes('Service Path')));
  assert.ok(validateConfigForm(s4).some(item => item.includes('Entity Set')));
  assert.ok(validateConfigForm(s4).some(item => item.includes('Username')));
  const ariba = applySelection(empty(), { name: 'Ariba', systemKind: 'SAP_ARIBA', protocol: 'REST', baseUrl: 'https://api.ariba.com', resourcePath: '/orders', authenticationType: 'TOKEN_API' });
  assert.ok(validateConfigForm(ariba).some(item => item.includes('Token URL')));
  const soap = applySelection(empty(), { name: 'Ariba SOAP', systemKind: 'SAP_ARIBA', protocol: 'SOAP' });
  assert.ok(validateConfigForm(soap).some(item => item.includes('Endpoint URL')));
});

test('validated save requires a successful test and draft save stays available', () => {
  assert.equal(DRAFT_STATUS, 'DRAFT');
  assert.equal(CONNECTION_NOT_TESTED, 'NOT TESTED');
  assert.ok(page.includes('Save Configuration'));
  assert.ok(page.includes('Save Draft'));
  assert.ok(page.includes('test-connection'));
  assert.ok(page.includes('designer/draft'));
  assert.ok(page.includes('disabled={save.isPending || !testId}'));
  assert.ok(page.includes('CONNECTION_SUCCESSFUL'));
  assert.ok(API_TYPES.includes('POST_GRN'));
  assert.ok(!API_TYPES.includes('GET_WBS'));
});

test('Test Connection is wired to the backend and does not fake success', () => {
  assert.ok(page.includes('/api/v1/integrations/test-connection'));
  assert.ok(page.includes('csrf-request-info'));
  assert.ok(page.includes('Show request'));
  assert.ok(page.includes('useState<{ id: string; name: string } | null>(null)'));
  assert.ok(!page.includes('testConnectionResult()'));
  assert.ok(!page.includes(STEP1_TEST_MESSAGE));
  assert.equal(toApiAuth('TOKEN_API'), 'CUSTOM_TOKEN_ENDPOINT');
  assert.equal(fromApiAuth('BEARER_TOKEN'), 'BEARER_STATIC');
});
