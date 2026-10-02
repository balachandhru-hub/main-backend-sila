import { useEffect, useMemo, useState } from 'react';
import { Activity, CheckCircle2, Info, KeyRound, Plus, RefreshCw, Save, ShieldCheck, TestTube2 } from 'lucide-react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { customFetch, type IntegrationConfiguration } from '@workspace/api-client-react';
import { getGetAccessContextQueryKey, useGetAccessContext } from '@workspace/api-client-react';
import { SilaPageHeader, StatusBadge } from '@/components/sila-ui';
import {
  API_TYPES, AUTH_LABELS,   CONNECTION_FAILED, CONNECTION_NOT_TESTED, CONNECTION_SUCCESSFUL, DRAFT_STATUS, INTERFACE_LABELS, SYSTEM_LABELS, VALIDATED_STATUS,
  applySelection, authFor, formatPairs, fromApiAuth, interfacesFor, parsePairs, showCsrf, showOAuth, showTokenApi,
  toApiAuth, validateConfigForm, visibleApiFields, type ConfigForm, type ConnectionTestResult,
} from '@/lib/integration-designer';

const request = { credentials: 'include' as const };
const emptyForm = (): ConfigForm => applySelection({
  name: '', description: '', systemKind: 'SAP_S4HANA', processType: 'POST_GRN', protocol: 'ODATA_V2',
  baseUrl: '', resourcePath: '', servicePath: '', entitySet: '', httpMethod: 'POST', authenticationType: 'BASIC',
  username: '', password: '', clientId: '', clientSecret: '', bearerToken: '', apiKey: '', apiKeyHeader: 'APIKey',
  apiKeyPlacement: 'HEADER', customHeaderName: 'X-API-Key', tokenEndpoint: '', tokenHttpMethod: 'POST', tokenScope: '',
  tokenHeadersText: '', tokenBodyText: '', tokenResponsePath: 'access_token', tokenType: 'Bearer', tokenExpiryPath: '',
  authorizationHeaderName: 'Authorization', contentType: 'application/json', accept: 'application/json', timeoutSeconds: 30,
  csrfRequired: true, csrfFetchMethod: 'GET', csrfFetchPath: '', csrfHeaderName: 'X-CSRF-Token', csrfHeaderValue: 'Fetch',
  csrfResponseHeader: 'X-CSRF-Token', wsdlUrl: '', soapOperation: '', soapAction: '', soapVersion: '1.1', secretConfigured: false,
}, {});

function asForm(item: IntegrationConfiguration): ConfigForm {
  const extra = item as IntegrationConfiguration & Record<string, unknown>;
  const designer = (extra.designer ?? {}) as Record<string, unknown>;
  return applySelection(emptyForm(), {
    name: item.name,
    description: String(extra.description ?? ''),
    systemKind: String(extra.systemKind ?? 'CUSTOM'),
    processType: item.processType,
    protocol: item.protocol,
    baseUrl: item.baseUrl,
    resourcePath: item.resourcePath ?? '',
    servicePath: String(extra.servicePath ?? ''),
    entitySet: String(extra.entitySet ?? ''),
    httpMethod: String(extra.httpMethod ?? 'GET'),
    authenticationType: fromApiAuth(item.authenticationType),
    username: item.username ?? '',
    timeoutSeconds: item.timeoutSeconds,
    contentType: String(designer.contentType ?? (item.protocol === 'SOAP' ? 'text/xml' : 'application/json')),
    accept: String(designer.accept ?? ''),
    csrfRequired: Boolean(designer.csrfRequired),
    csrfFetchMethod: String(designer.csrfFetchMethod ?? 'GET'),
    csrfFetchPath: String(designer.csrfFetchPath ?? ''),
    csrfHeaderName: String(designer.csrfHeaderName ?? 'X-CSRF-Token'),
    csrfHeaderValue: String(designer.csrfHeaderValue ?? 'Fetch'),
    csrfResponseHeader: String(designer.csrfResponseHeader ?? 'X-CSRF-Token'),
    wsdlUrl: String(designer.wsdlUrl ?? ''),
    soapOperation: String(designer.soapOperation ?? ''),
    soapAction: String(designer.soapAction ?? ''),
    soapVersion: String(designer.soapVersion ?? '1.1'),
    tokenEndpoint: String(extra.tokenEndpoint ?? ''),
    tokenHttpMethod: String(designer.tokenHttpMethod ?? 'POST'),
    tokenScope: String(extra.tokenScope ?? ''),
    tokenHeadersText: formatPairs((extra.tokenHeaders as Record<string, string> | null | undefined) ?? null),
    tokenBodyText: formatPairs((extra.tokenBody as Record<string, string> | null | undefined) ?? null),
    tokenResponsePath: String(designer.tokenResponsePath ?? 'access_token'),
    tokenType: String(designer.tokenType ?? 'Bearer'),
    tokenExpiryPath: String(designer.tokenExpiryPath ?? ''),
    authorizationHeaderName: String(designer.authorizationHeaderName ?? 'Authorization'),
    apiKeyHeader: String(designer.apiKeyHeader ?? 'APIKey'),
    apiKeyPlacement: designer.apiKeyPlacement === 'QUERY' ? 'QUERY' : 'HEADER',
    customHeaderName: String(designer.customHeaderName ?? 'X-API-Key'),
    secretConfigured: item.credentialStatus === 'Configured',
  });
}

function toDraftPayload(form: ConfigForm, connectionTestId?: string | null) {
  return {
    name: form.name, description: form.description, systemKind: form.systemKind, processType: form.processType,
    protocol: form.protocol, baseUrl: form.baseUrl, resourcePath: form.resourcePath, servicePath: form.servicePath,
    entitySet: form.entitySet, httpMethod: form.httpMethod, authenticationType: toApiAuth(form.authenticationType),
    username: form.username || null, password: form.password || null, clientId: form.clientId || null,
    clientSecret: form.clientSecret || null, bearerToken: form.bearerToken || form.apiKey || null, apiKey: form.apiKey || null,
    tokenEndpoint: form.tokenEndpoint || null, tokenScope: form.tokenScope || null,
    tokenHeaders: parsePairs(form.tokenHeadersText), tokenBody: parsePairs(form.tokenBodyText),
    timeoutSeconds: form.timeoutSeconds, entityCode: 'ALL',
    designer: {
      contentType: form.contentType, accept: form.accept, csrfRequired: form.csrfRequired, csrfFetchMethod: form.csrfFetchMethod,
      csrfFetchPath: form.csrfFetchPath || (form.csrfRequired && form.servicePath
        ? (form.servicePath.endsWith('/') ? form.servicePath : `${form.servicePath.replace(/\/?$/, '')}/`)
        : form.csrfFetchPath), csrfHeaderName: form.csrfHeaderName, csrfHeaderValue: form.csrfHeaderValue,
      csrfResponseHeader: form.csrfResponseHeader, cookieHandling: 'AUTOMATIC', wsdlUrl: form.wsdlUrl,
      soapOperation: form.soapOperation, soapAction: form.soapAction, soapVersion: form.soapVersion,
      tokenHttpMethod: form.tokenHttpMethod, tokenResponsePath: form.tokenResponsePath, tokenType: form.tokenType,
      tokenExpiryPath: form.tokenExpiryPath, apiKeyHeader: form.apiKeyHeader, apiKeyPlacement: form.apiKeyPlacement,
      customHeaderName: form.customHeaderName, authorizationHeaderName: form.authorizationHeaderName,
    },
    testId: connectionTestId || undefined,
  };
}

async function api<T>(url: string, init?: RequestInit): Promise<T> {
  return customFetch<T>(url, { ...request, ...init, responseType: 'json' });
}

function apiErrorText(error: unknown) {
  if (error && typeof error === 'object' && 'data' in error) {
    const data = (error as { data?: Record<string, unknown> | null }).data;
    const code = typeof data?.code === 'string' ? data.code : '';
    const message = typeof data?.message === 'string' ? data.message : error instanceof Error ? error.message : '';
    const errors = data?.errors && typeof data.errors === 'object'
      ? Object.entries(data.errors as Record<string, unknown>).map(([key, value]) => `${key}: ${Array.isArray(value) ? value.join(', ') : String(value)}`).join('; ')
      : '';
    return [code, message, errors].filter(Boolean).join(' — ') || CONNECTION_FAILED;
  }
  return error instanceof Error ? error.message : CONNECTION_FAILED;
}

function testFailureText(result: ConnectionTestResult) {
  return [result.errorCode, result.message].filter(Boolean).join(' — ') || CONNECTION_FAILED;
}

function formatRequestJson(value: string) {
  try {
    return JSON.stringify(JSON.parse(value), null, 2);
  } catch {
    return value;
  }
}

function connectionLabel(status: string) {
  return ['VALIDATED', 'ACTIVE', 'TESTED'].includes(status) ? CONNECTION_SUCCESSFUL : CONNECTION_NOT_TESTED;
}

function statusBadge(status: string) {
  return ['ACTIVE', 'VALIDATED', 'TESTED'].includes(status) ? status : DRAFT_STATUS;
}

export default function IntegrationsPage() {
  const context = useGetAccessContext({ request, query: { queryKey: getGetAccessContextQueryKey(), retry: false } });
  const organizationId = context.data?.organizations[0]?.id;
  const queryClient = useQueryClient();
  const [selectedId, setSelectedId] = useState<string>();
  const [editing, setEditing] = useState(false);
  const [form, setForm] = useState<ConfigForm>(emptyForm());
  const [advanced, setAdvanced] = useState(false);
  const [errors, setErrors] = useState<string[]>([]);
  const [notice, setNotice] = useState<{ tone: 'good' | 'bad' | 'warn'; text: string }>();
  const [testId, setTestId] = useState<string>();
  const [testResult, setTestResult] = useState<ConnectionTestResult>();
  const [payloadOpen, setPayloadOpen] = useState(false);
  const [pendingDelete, setPendingDelete] = useState<{ id: string; name: string } | null>(null);

  const configs = useQuery({
    queryKey: ['api-integrations', organizationId],
    queryFn: () => api<IntegrationConfiguration[]>(`/api/v1/integrations?organizationId=${organizationId}`),
    enabled: Boolean(organizationId),
  });
  const selected = useMemo(() => configs.data?.find(item => item.id === selectedId), [configs.data, selectedId]);

  useEffect(() => {
    if (selected && editing) setForm(asForm(selected));
  }, [selected, editing]);

  const patch = (change: Partial<ConfigForm>) => {
    setForm(current => applySelection(current, change));
    setErrors([]);
    setTestId(undefined);
    setTestResult(undefined);
  };

  const save = useMutation({
    mutationFn: () => api<IntegrationConfiguration>(`/api/v1/integrations/designer?organizationId=${organizationId}${selectedId ? `&id=${selectedId}` : ''}`, {
      method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(toDraftPayload(form, testId)),
    }),
    onSuccess: item => {
      setSelectedId(item.id);
      setEditing(false);
      setTestId(undefined);
      setNotice({ tone: 'good', text: `Saved as ${VALIDATED_STATUS}. Connection: ${CONNECTION_SUCCESSFUL}. Secrets remain masked.` });
      queryClient.invalidateQueries({ queryKey: ['api-integrations', organizationId] });
    },
    onError: error => setNotice({ tone: 'bad', text: apiErrorText(error) }),
  });
  const saveDraft = useMutation({
    mutationFn: () => api<IntegrationConfiguration>(`/api/v1/integrations/designer/draft?organizationId=${organizationId}${selectedId ? `&id=${selectedId}` : ''}`, {
      method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(toDraftPayload(form)),
    }),
    onSuccess: item => {
      setSelectedId(item.id);
      setNotice({ tone: 'good', text: `Saved as ${DRAFT_STATUS}. Connection: ${CONNECTION_NOT_TESTED}. Test connection before Save Configuration.` });
      queryClient.invalidateQueries({ queryKey: ['api-integrations', organizationId] });
    },
    onError: error => setNotice({ tone: 'bad', text: apiErrorText(error) }),
  });
  const test = useMutation({
    mutationFn: () => api<ConnectionTestResult>(`/api/v1/integrations/test-connection?organizationId=${organizationId}${selectedId ? `&configurationId=${selectedId}` : ''}`, {
      method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(toDraftPayload(form)),
    }),
    onSuccess: result => {
      setTestResult(result);
      setTestId(result.success && result.testId ? result.testId : undefined);
      setNotice({
        tone: result.success ? 'good' : 'bad',
        text: result.success ? CONNECTION_SUCCESSFUL : testFailureText(result),
      });
    },
    onError: error => {
      const message = apiErrorText(error);
      setTestId(undefined);
      setTestResult({ success: false, status: 'CONNECTION_FAILED', errorCode: undefined, message });
      setNotice({ tone: 'bad', text: message });
    },
  });
  const disable = useMutation({
    mutationFn: () => api<boolean>(`/api/v1/integrations/${selectedId}/designer/disable?organizationId=${organizationId}`, { method: 'POST' }),
    onSuccess: () => { setNotice({ tone: 'good', text: 'Configuration disabled.' }); queryClient.invalidateQueries({ queryKey: ['api-integrations', organizationId] }); },
  });
  const remove = useMutation({
    mutationFn: (id: string) => api<{ deleted: boolean }>(`/api/v1/integrations/${id}?organizationId=${organizationId}`, { method: 'DELETE' }),
    onSuccess: (_result, id) => {
      setPendingDelete(null);
      if (selectedId === id) { setSelectedId(undefined); setEditing(false); }
      setNotice({ tone: 'good', text: 'API configuration deleted.' });
      queryClient.setQueryData<IntegrationConfiguration[]>(['api-integrations', organizationId], current => (current ?? []).filter(item => item.id !== id));
      queryClient.invalidateQueries({ queryKey: ['api-integrations', organizationId] });
    },
    onError: error => {
      setPendingDelete(null);
      const data = error && typeof error === 'object' && 'data' in error
        ? (error as { data?: { message?: string } | null }).data
        : null;
      setNotice({ tone: 'bad', text: typeof data?.message === 'string' && data.message ? data.message : apiErrorText(error) });
    },
  });

  const startCreate = () => { setSelectedId(undefined); setEditing(true); setForm(emptyForm()); setNotice(undefined); setErrors([]); setTestId(undefined); setTestResult(undefined); };
  const startEdit = (id: string) => { setSelectedId(id); setEditing(true); setNotice(undefined); setErrors([]); setTestId(undefined); setTestResult(undefined); };
  const onSave = () => {
    const nextErrors = validateConfigForm(form, Boolean(selected && form.secretConfigured));
    setErrors(nextErrors);
    if (nextErrors.length) { setNotice({ tone: 'bad', text: nextErrors[0] }); return; }
    if (!testId) { setNotice({ tone: 'bad', text: 'Test connection successfully before save.' }); return; }
    save.mutate();
  };
  const onSaveDraft = () => {
    const nextErrors = validateConfigForm(form, Boolean(selected && form.secretConfigured));
    setErrors(nextErrors);
    if (nextErrors.length) { setNotice({ tone: 'bad', text: nextErrors[0] }); return; }
    saveDraft.mutate();
  };
  const onTest = () => {
    const nextErrors = validateConfigForm(form, Boolean(selected && form.secretConfigured));
    setErrors(nextErrors);
    if (nextErrors.length) { setNotice({ tone: 'bad', text: nextErrors[0] }); return; }
    test.mutate();
  };

  const protocols = interfacesFor(form.systemKind);
  const auths = authFor(form.systemKind, form.protocol);
  const fields = visibleApiFields(form.protocol);
  const odata = fields.includes('servicePath');
  const rest = fields.includes('resourcePath');
  const soap = form.protocol === 'SOAP';

  if (!organizationId) return <><SilaPageHeader title="Integrations" description="Choose an accessible organization to manage API configurations." /><div className="sila-card" style={{ padding: 22 }}>No organization scope is available for this account.</div></>;

  return <>
    <SilaPageHeader
      eyebrow="Administration / Integrations"
      title="Integrations"
      description="Configure ERP and API connections for the current customer environment. Test connection first, then save a validated configuration."
      actions={<button className="sila-button sila-button--primary" type="button" onClick={startCreate}><Plus size={14} /> Add API Configuration</button>}
    />
    {notice && <div role="status" className={`sila-integration-notice sila-integration-notice--${notice.tone}`}>{notice.tone === 'good' ? <CheckCircle2 size={15} /> : <Activity size={15} />}<span>{notice.text}</span>{notice.tone === 'bad' && testResult?.requestJson ? <button type="button" data-testid="csrf-request-info" className="sila-button sila-button--quiet" aria-label="Show request" onClick={() => setPayloadOpen(true)}><Info size={15} /></button> : null}</div>}
    <div className="sila-integration-layout">
      <aside className="sila-card sila-integration-list">
        <div className="sila-integration-list__header"><div><span className="sila-section-kicker">Saved configurations</span><strong>API connections</strong></div><RefreshCw size={15} className={configs.isFetching ? 'sila-spin' : ''} /></div>
        {configs.isLoading ? <p className="sila-muted-copy">Loading configurations…</p> : configs.data?.length ? configs.data.map(item => {
          const extra = item as IntegrationConfiguration & { systemKind?: string; validationStatus?: string };
          return <div key={item.id} className={`sila-integration-list__item ${selectedId === item.id ? 'is-selected' : ''}`}>
            <button type="button" className="sila-integration-list__select" onClick={() => { setSelectedId(item.id); setEditing(false); }}>
              <strong>{item.name}</strong>
              <small>{SYSTEM_LABELS[extra.systemKind ?? 'CUSTOM'] ?? extra.systemKind} · {item.processType} · {INTERFACE_LABELS[item.protocol] ?? item.protocol}<br />Connection: {connectionLabel(String(item.status))}</small>
            </button>
            <div className="sila-integration-list__meta">
              <StatusBadge value={statusBadge(String(item.status))} />
              <div className="sila-integration-list__actions">
                <button className="sila-button sila-button--quiet" type="button" onClick={() => startEdit(item.id)}>Edit</button>
                <button className="sila-button sila-button--quiet" type="button" onClick={() => setPendingDelete({ id: item.id, name: item.name })}>Delete</button>
                <button className="sila-button sila-button--quiet" type="button" onClick={() => startEdit(item.id)}>Test</button>
                {String(item.status) === 'DISABLED' || String(item.status) === 'INACTIVE'
                  ? <button className="sila-button sila-button--quiet" type="button" onClick={() => setNotice({ tone: 'warn', text: 'Enable requires a VALIDATED configuration from a successful connection test.' })}>Enable</button>
                  : <button className="sila-button sila-button--quiet" type="button" onClick={() => { setSelectedId(item.id); disable.mutate(); }}>Disable</button>}
              </div>
            </div>
          </div>;
        }) : <p className="sila-muted-copy">No saved configurations yet.</p>}
      </aside>
      <section className="sila-card sila-integration-editor">
        {!editing && !selected && <p className="sila-muted-copy">Select a saved configuration or add a new API configuration.</p>}
        {!editing && selected && <div className="sila-integration-form">
          <div className="sila-integration-editor__top"><div><span className="sila-section-kicker">{SYSTEM_LABELS[String((selected as IntegrationConfiguration & { systemKind?: string }).systemKind ?? 'CUSTOM')]} · {INTERFACE_LABELS[selected.protocol] ?? selected.protocol}</span><h2>{selected.name}</h2></div><StatusBadge value={statusBadge(String(selected.status))} /></div>
          <p className="sila-muted-copy">Connection: {connectionLabel(String(selected.status))}. Status: {selected.status}.</p>
          <div className="sila-integration-actions">
            <button className="sila-button" type="button" onClick={() => startEdit(selected.id)}>Edit</button>
            <button className="sila-button" type="button" onClick={() => setPendingDelete({ id: selected.id, name: selected.name })}>Delete</button>
            <button className="sila-button" type="button" onClick={onTest}>Test Connection</button>
          </div>
        </div>}
        {editing && <form className="sila-integration-form" onSubmit={event => { event.preventDefault(); onSave(); }}>
          <div className="sila-integration-editor__top"><div><span className="sila-section-kicker">API configuration</span><h2>{selectedId ? 'Edit API Configuration' : 'Add API Configuration'}</h2></div></div>

          <div className="sila-integration-section">
            <div className="sila-integration-section__title"><strong>General</strong></div>
            <div className="sila-form-grid">
              <label className="sila-form-field sila-form-field--wide"><span>Configuration name *</span><input className="sila-input" value={form.name} onChange={event => patch({ name: event.target.value })} /></label>
              <label className="sila-form-field"><span>System *</span><select className="sila-select" value={form.systemKind} onChange={event => patch({ systemKind: event.target.value })}>{Object.entries(SYSTEM_LABELS).map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select></label>
              <label className="sila-form-field"><span>API type *</span><select className="sila-select" value={form.processType} onChange={event => patch({ processType: event.target.value })}>{API_TYPES.map(value => <option key={value} value={value}>{value}</option>)}</select></label>
              <label className="sila-form-field"><span>Interface type *</span><select className="sila-select" value={form.protocol} onChange={event => patch({ protocol: event.target.value })}>{protocols.map(value => <option key={value} value={value}>{INTERFACE_LABELS[value] ?? value}</option>)}</select></label>
              <label className="sila-form-field sila-form-field--wide"><span>Description</span><input className="sila-input" value={form.description} onChange={event => patch({ description: event.target.value })} /></label>
              <label className="sila-form-field"><span>Environment</span><input className="sila-input" value="Current customer environment" disabled /></label>
            </div>
          </div>

          <div className="sila-integration-section">
            <div className="sila-integration-section__title"><strong>Business API</strong></div>
            <div className="sila-form-grid">
              <label className="sila-form-field sila-form-field--wide"><span>{soap ? 'Endpoint URL *' : 'Base URL *'}</span><input className="sila-input" value={form.baseUrl} onChange={event => patch({ baseUrl: event.target.value })} placeholder={soap ? 'https://service.example/soap' : 'https://my419951-api.s4hana.cloud.sap'} /></label>
              {odata && <><label className="sila-form-field sila-form-field--wide"><span>Service path *</span><input className="sila-input" value={form.servicePath} onChange={event => patch({ servicePath: event.target.value })} placeholder="/sap/opu/odata/sap/API_MATERIAL_DOCUMENT_SRV" /></label><label className="sila-form-field"><span>Entity set *</span><input className="sila-input" value={form.entitySet} onChange={event => patch({ entitySet: event.target.value })} placeholder="A_MaterialDocumentHeader" /></label></>}
              {rest && <label className="sila-form-field sila-form-field--wide"><span>Resource path *</span><input className="sila-input" value={form.resourcePath} onChange={event => patch({ resourcePath: event.target.value })} /></label>}
              {soap && <><label className="sila-form-field sila-form-field--wide"><span>WSDL URL</span><input className="sila-input" value={form.wsdlUrl} onChange={event => patch({ wsdlUrl: event.target.value })} /></label><label className="sila-form-field"><span>SOAP operation</span><input className="sila-input" value={form.soapOperation} onChange={event => patch({ soapOperation: event.target.value })} /></label><label className="sila-form-field"><span>SOAP action</span><input className="sila-input" value={form.soapAction} onChange={event => patch({ soapAction: event.target.value })} /></label><label className="sila-form-field"><span>SOAP version</span><select className="sila-select" value={form.soapVersion} onChange={event => patch({ soapVersion: event.target.value })}><option>1.1</option><option>1.2</option></select></label></>}
              {!soap && <label className="sila-form-field"><span>HTTP method *</span><select className="sila-select" value={form.httpMethod} onChange={event => patch({ httpMethod: event.target.value })}>{['GET', 'POST', 'PUT', 'PATCH', 'DELETE'].map(value => <option key={value}>{value}</option>)}</select></label>}
              <label className="sila-form-field"><span>Content type</span><input className="sila-input" value={form.contentType} onChange={event => patch({ contentType: event.target.value })} /></label>
              {!soap && <label className="sila-form-field"><span>Accept</span><input className="sila-input" value={form.accept} onChange={event => patch({ accept: event.target.value })} /></label>}
            </div>
          </div>

          <div className="sila-integration-section">
            <div className="sila-integration-section__title"><KeyRound size={15} /><strong>Authentication</strong></div>
            <div className="sila-form-grid">
              <label className="sila-form-field"><span>Authentication type</span><select className="sila-select" value={form.authenticationType} onChange={event => patch({ authenticationType: event.target.value })}>{auths.map(value => <option key={value} value={value}>{AUTH_LABELS[value] ?? value}</option>)}</select></label>
              {form.authenticationType === 'BASIC' && <><label className="sila-form-field"><span>Username *</span><input className="sila-input" autoComplete="off" value={form.username} onChange={event => patch({ username: event.target.value })} /></label><label className="sila-form-field"><span>Password *</span><input className="sila-input" type="password" autoComplete="new-password" value={form.password} placeholder={form.secretConfigured ? 'Configured ••••••••' : ''} onChange={event => patch({ password: event.target.value })} /></label></>}
              {form.authenticationType === 'API_KEY' && <><label className="sila-form-field"><span>Key name *</span><input className="sila-input" value={form.apiKeyHeader} onChange={event => patch({ apiKeyHeader: event.target.value })} /></label><label className="sila-form-field"><span>Key value *</span><input className="sila-input" type="password" value={form.apiKey} placeholder={form.secretConfigured ? 'Configured ••••••••' : ''} onChange={event => patch({ apiKey: event.target.value })} /></label><label className="sila-form-field"><span>Placement</span><select className="sila-select" value={form.apiKeyPlacement} onChange={event => patch({ apiKeyPlacement: event.target.value as 'HEADER' | 'QUERY' })}><option>HEADER</option><option>QUERY</option></select></label></>}
              {['BEARER_STATIC', 'BEARER_TOKEN'].includes(form.authenticationType) && <label className="sila-form-field sila-form-field--wide"><span>Bearer token *</span><input className="sila-input" type="password" value={form.bearerToken} placeholder={form.secretConfigured ? 'Configured ••••••••' : ''} onChange={event => patch({ bearerToken: event.target.value })} /></label>}
              {form.authenticationType === 'CUSTOM_HEADER' && <label className="sila-form-field"><span>Custom header name</span><input className="sila-input" value={form.customHeaderName} onChange={event => patch({ customHeaderName: event.target.value })} /></label>}
              {showOAuth(form) && <><label className="sila-form-field sila-form-field--wide"><span>Token URL *</span><input className="sila-input" value={form.tokenEndpoint} onChange={event => patch({ tokenEndpoint: event.target.value })} /></label><label className="sila-form-field"><span>Client ID *</span><input className="sila-input" value={form.clientId} onChange={event => patch({ clientId: event.target.value })} /></label><label className="sila-form-field"><span>Client secret *</span><input className="sila-input" type="password" value={form.clientSecret} placeholder={form.secretConfigured ? 'Configured ••••••••' : ''} onChange={event => patch({ clientSecret: event.target.value })} /></label><label className="sila-form-field"><span>Scope</span><input className="sila-input" value={form.tokenScope} onChange={event => patch({ tokenScope: event.target.value })} /></label><label className="sila-form-field"><span>Token method</span><select className="sila-select" value={form.tokenHttpMethod} onChange={event => patch({ tokenHttpMethod: event.target.value })}><option>POST</option><option>GET</option></select></label></>}
            </div>
            <p className="sila-integration-credential-note"><ShieldCheck size={14} /> Passwords, API keys, and tokens are never shown after save.</p>
          </div>

          {showTokenApi(form) && <div className="sila-integration-section sila-token-panel">
            <div className="sila-integration-section__title"><strong>Token API configuration</strong><span>Separate from the business API</span></div>
            <div className="sila-form-grid">
              <label className="sila-form-field sila-form-field--wide"><span>Token URL *</span><input className="sila-input" value={form.tokenEndpoint} onChange={event => patch({ tokenEndpoint: event.target.value })} /></label>
              <label className="sila-form-field"><span>HTTP method *</span><select className="sila-select" value={form.tokenHttpMethod} onChange={event => patch({ tokenHttpMethod: event.target.value })}><option>POST</option><option>GET</option></select></label>
              <label className="sila-form-field"><span>Credential / API key</span><input className="sila-input" type="password" value={form.apiKey} placeholder={form.secretConfigured ? 'Configured ••••••••' : ''} onChange={event => patch({ apiKey: event.target.value })} /></label>
              <label className="sila-form-field sila-form-field--wide"><span>Token headers</span><textarea className="sila-input" rows={3} value={form.tokenHeadersText} placeholder="Key=Value" onChange={event => patch({ tokenHeadersText: event.target.value })} /></label>
              <label className="sila-form-field sila-form-field--wide"><span>Token request parameters / body</span><textarea className="sila-input" rows={3} value={form.tokenBodyText} placeholder="grant_type=client_credentials" onChange={event => patch({ tokenBodyText: event.target.value })} /></label>
              <label className="sila-form-field"><span>Access token response path *</span><input className="sila-input" value={form.tokenResponsePath} onChange={event => patch({ tokenResponsePath: event.target.value })} /></label>
              <label className="sila-form-field"><span>Token type</span><input className="sila-input" value={form.tokenType} onChange={event => patch({ tokenType: event.target.value })} /></label>
              <label className="sila-form-field"><span>Token expiry response path</span><input className="sila-input" value={form.tokenExpiryPath} onChange={event => patch({ tokenExpiryPath: event.target.value })} /></label>
              <label className="sila-form-field"><span>Authorization header name</span><input className="sila-input" value={form.authorizationHeaderName} onChange={event => patch({ authorizationHeaderName: event.target.value })} /></label>
            </div>
          </div>}

          {showCsrf(form) && <div className="sila-integration-section">
            <div className="sila-integration-section__title"><strong>CSRF token</strong></div>
            <div className="sila-form-grid">
              <label className="sila-form-field"><span>CSRF token required</span><select className="sila-select" value={form.csrfRequired ? 'YES' : 'NO'} onChange={event => patch({ csrfRequired: event.target.value === 'YES' })}><option>YES</option><option>NO</option></select></label>
              {form.csrfRequired && <><label className="sila-form-field"><span>Token fetch method</span><input className="sila-input" value={form.csrfFetchMethod} onChange={event => patch({ csrfFetchMethod: event.target.value })} /></label><label className="sila-form-field sila-form-field--wide"><span>Token fetch path *</span><input className="sila-input" value={form.csrfFetchPath} onChange={event => patch({ csrfFetchPath: event.target.value })} placeholder="/sap/opu/odata/sap/API_MATERIAL_DOCUMENT_SRV/" /></label><label className="sila-form-field"><span>Request header name</span><input className="sila-input" value={form.csrfHeaderName} onChange={event => patch({ csrfHeaderName: event.target.value })} /></label><label className="sila-form-field"><span>Request header value</span><input className="sila-input" value={form.csrfHeaderValue} onChange={event => patch({ csrfHeaderValue: event.target.value })} /></label><label className="sila-form-field"><span>Response token header</span><input className="sila-input" value={form.csrfResponseHeader} onChange={event => patch({ csrfResponseHeader: event.target.value })} /></label><label className="sila-form-field"><span>Cookie handling</span><input className="sila-input" disabled value="Automatic" /></label></>}
            </div>
          </div>}

          <div className="sila-integration-section">
            <button className="sila-button sila-button--quiet" type="button" onClick={() => setAdvanced(value => !value)}>{advanced ? 'Hide' : 'Show'} advanced</button>
            {advanced && <div className="sila-form-grid"><label className="sila-form-field"><span>Timeout (seconds)</span><input className="sila-input" type="number" value={form.timeoutSeconds} onChange={event => patch({ timeoutSeconds: Number(event.target.value) })} /></label></div>}
          </div>

          <div className="sila-integration-section">
            <div className="sila-integration-section__title"><TestTube2 size={15} /><strong>Test &amp; save</strong></div>
            <p className="sila-muted-copy">Connection status: {test.isPending ? 'TESTING...' : testResult?.success ? CONNECTION_SUCCESSFUL : testResult ? CONNECTION_FAILED : CONNECTION_NOT_TESTED}. Save Configuration is enabled only after a successful test. Save Draft stores the configuration without testing.</p>
            {testResult && !testResult.success && <p className="sila-muted-copy" role="status" style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
              <strong>{testFailureText(testResult)}</strong>
              {testResult.requestJson ? <button type="button" data-testid="csrf-request-info" className="sila-button sila-button--quiet" aria-label="Show request" onClick={() => setPayloadOpen(true)}><Info size={15} /></button> : null}
            </p>}
            {testResult?.checks?.length ? <ul className="sila-muted-copy">{testResult.checks.map(check => <li key={check.name}>{check.name}: {check.success ? 'PASS' : 'FAIL'}{check.code ? ` (${check.code})` : ''}</li>)}</ul> : null}
            {errors.length > 0 && <ul className="sila-muted-copy">{errors.map(error => <li key={error}>{error}</li>)}</ul>}
            <div className="sila-integration-actions">
              <button className="sila-button" type="button" onClick={onTest} disabled={test.isPending}>{test.isPending ? 'TESTING...' : 'Test Connection'}</button>
              <button className="sila-button" type="button" onClick={onSaveDraft} disabled={saveDraft.isPending}>{saveDraft.isPending ? 'SAVING DRAFT...' : 'Save Draft'}</button>
              <button className="sila-button sila-button--primary" type="submit" disabled={save.isPending || !testId}><Save size={14} /> Save Configuration</button>
            </div>
          </div>
        </form>}
      </section>
    </div>
    {pendingDelete ? (
      <div style={{ position: 'fixed', inset: 0, background: 'rgba(38,58,77,.35)', display: 'grid', placeItems: 'center', zIndex: 50 }}>
        <div style={{ background: '#fff', border: '1px solid #c5d8e8', padding: 24, width: 'min(480px, 94vw)' }}>
          <h2 style={{ marginTop: 0 }}>Delete API configuration</h2>
          <p>Are you sure you want to delete this API configuration?</p>
          <p className="sila-muted-copy">{pendingDelete.name}</p>
          <div style={{ display: 'flex', gap: 8, justifyContent: 'flex-end' }}>
            <button type="button" className="sila-button sila-button--quiet" onClick={() => setPendingDelete(null)}>Cancel</button>
            <button type="button" className="sila-button" disabled={remove.isPending} onClick={() => remove.mutate(pendingDelete.id)}>{remove.isPending ? 'Deleting…' : 'Delete'}</button>
          </div>
        </div>
      </div>
    ) : null}
    {payloadOpen && testResult?.requestJson ? (
      <div style={{ position: 'fixed', inset: 0, background: 'rgba(38,58,77,.35)', display: 'grid', placeItems: 'center', zIndex: 50 }}>
        <div style={{ background: '#fff', border: '1px solid #c5d8e8', padding: 24, width: 'min(720px, 94vw)', maxHeight: '82vh', overflow: 'auto' }}>
          <h2 style={{ marginTop: 0 }}>Request</h2>
          <pre style={{ whiteSpace: 'pre-wrap', wordBreak: 'break-word', fontSize: 12, lineHeight: 1.45 }}>{formatRequestJson(testResult.requestJson)}</pre>
          <div style={{ display: 'flex', justifyContent: 'flex-end' }}>
            <button type="button" className="sila-button sila-button--quiet" onClick={() => setPayloadOpen(false)}>Close</button>
          </div>
        </div>
      </div>
    ) : null}
  </>;
}
