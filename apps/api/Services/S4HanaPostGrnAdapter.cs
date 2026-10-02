using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using SilaMe.Api.DTOs;
using SilaMe.Api.Models;

namespace SilaMe.Api.Services;

// Downstream of Invoice Review. Consumes saved GoodsReceiptId. Do not import or duplicate OCR/review.
// See docs/PROTECTED_INVOICE_FLOW.md.
public sealed class S4HanaPostGrnAdapter(
    IHttpMessageHandlerFactory handlerFactory,
    IntegrationDesignerService designer,
    IntegrationAuthenticationResolver authentication,
    CsrfSessionProvider csrf)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public static bool AppliesTo(ApiIntegrationConfiguration configuration)
    {
        if (configuration.SystemKind != IntegrationSystemKind.SAP_S4HANA) return false;
        if (configuration.Protocol is IntegrationProtocol.ODATA_V2 or IntegrationProtocol.ODATA_V4) return true;
        if (CsrfRequired(configuration)) return true;
        var haystack = string.Concat(configuration.BaseUrl, configuration.ServicePath, configuration.ResourcePath, configuration.EntitySet);
        return haystack.Contains("/sap/opu/odata", StringComparison.OrdinalIgnoreCase)
            || haystack.Contains("API_MATERIAL_DOCUMENT_SRV", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<ErpGoodsReceiptResult> PostAsync(
        ApiIntegrationConfiguration configuration,
        GoodsReceipt goodsReceipt,
        Guid? routeId,
        CancellationToken cancellationToken)
    {
        var draft = designer.RuntimeDraft(configuration);
        if (string.IsNullOrWhiteSpace(draft.EntitySet))
            draft.EntitySet = "A_MaterialDocumentHeader";
        var csrfFetchUrl = IntegrationODataUrl.CsrfFetchUrl(draft);
        var postUrl = IntegrationODataUrl.Combine(
            IntegrationODataUrl.ServiceRoot(draft.BaseUrl, draft.ServicePath, draft.EntitySet).TrimEnd('/'),
            draft.EntitySet);
        var lines = goodsReceipt.Lines.Where(item => item.AcceptedQuantity > 0)
            .OrderBy(item => item.PurchaseOrderItem.LineNumber)
            .ToList();
        var payload = BuildPayload(goodsReceipt, lines, draft);
        var capture = Capture(postUrl, payload, sent: false, csrfFetchUrl: csrfFetchUrl);

        if (lines.Count == 0)
            return Fail("GRN_NO_ACCEPTED_LINES", "At least one GRN line with an accepted quantity is required.", capture, routeId, configuration);

        using var client = CreateSessionClient(draft.TimeoutSeconds);
        var authContext = new IntegrationAuthContext();
        var handler = authentication.Resolve(draft.AuthenticationType);
        async Task ApplyAuthAsync(HttpRequestMessage request) =>
            await handler.AuthenticateAsync(client, request, draft, authContext, cancellationToken);

        CsrfSession session;
        try
        {
            session = await csrf.FetchAsync(client, draft, ApplyAuthAsync, cancellationToken);
        }
        catch (IntegrationException exception)
        {
            return Fail(exception.Code, exception.Message, exception.Detail ?? capture, routeId, configuration, exception.Status);
        }
        catch (CryptographicException)
        {
            return Fail("INTEGRATION_CREDENTIALS_INVALID", "Stored API credentials could not be decrypted. Re-enter the password on the integration configuration.", capture, routeId, configuration);
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, postUrl);
        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
        request.Headers.Accept.Clear();
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.TryAddWithoutValidation(draft.Designer.CsrfHeaderName ?? "X-CSRF-Token", session.Token);
        if (draft.Protocol == IntegrationProtocol.ODATA_V2)
        {
            request.Headers.TryAddWithoutValidation("DataServiceVersion", "2.0");
            request.Headers.TryAddWithoutValidation("MaxDataServiceVersion", "2.0");
        }
        try { await ApplyAuthAsync(request); }
        catch (IntegrationException exception)
        {
            return Fail(exception.Code, exception.Message, exception.Detail ?? capture, routeId, configuration, exception.Status);
        }

        HttpResponseMessage response;
        string body;
        try
        {
            response = await client.SendAsync(request, cancellationToken);
            body = await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new ErpGoodsReceiptResult(true, false, true, null, goodsReceipt.ErpMaterialDocument, null,
                Capture(postUrl, payload, sent: true, csrfFetchUrl: csrfFetchUrl), "ERP_POST_UNKNOWN", "The ERP response timed out.",
                null, routeId, configuration.Id, "SAP_S4HANA");
        }
        catch (HttpRequestException)
        {
            return new ErpGoodsReceiptResult(true, false, true, null, goodsReceipt.ErpMaterialDocument, null,
                Capture(postUrl, payload, sent: true, csrfFetchUrl: csrfFetchUrl), "ERP_POST_UNKNOWN", "The ERP could not be reached after the request was sent.",
                null, routeId, configuration.Id, "SAP_S4HANA");
        }

        var captured = Capture(postUrl, payload, sent: true, csrfFetchUrl: csrfFetchUrl, status: (int)response.StatusCode, response: body, headers: RequestHeaders(request));
        if (!response.IsSuccessStatusCode)
        {
            var sap = SapError(body);
            return new ErpGoodsReceiptResult(true, false, false, (int)response.StatusCode, goodsReceipt.ErpMaterialDocument, null, captured,
                "ERP_POST_FAILED", sap is null ? "The ERP rejected the goods receipt." : $"ERP REJECTED: {sap}",
                null, routeId, configuration.Id, "SAP_S4HANA");
        }

        var document = ReadJson(body, "MaterialDocument", "materialDocument", "MaterialDocumentNumber") ?? goodsReceipt.ErpMaterialDocument;
        var year = ReadJson(body, "MaterialDocumentYear", "DocumentYear", "documentYear");
        return new ErpGoodsReceiptResult(true, true, false, (int)response.StatusCode, document, year, captured, null, null, null, routeId, configuration.Id, "SAP_S4HANA");
    }

    private HttpClient CreateSessionClient(int timeoutSeconds)
    {
        var inner = handlerFactory.CreateHandler("api-integrations");
        var session = new CookieSessionHandler(inner);
        return new HttpClient(session, disposeHandler: true)
        {
            Timeout = TimeSpan.FromSeconds(Math.Clamp(timeoutSeconds <= 0 ? 30 : timeoutSeconds, 5, 300)),
        };
    }

    private static string BuildPayload(GoodsReceipt goodsReceipt, IReadOnlyList<GoodsReceiptLine> lines, IntegrationDesignerDraft draft)
    {
        var receiptDate = DateTime.SpecifyKind(goodsReceipt.ReceiptDate.Date, DateTimeKind.Utc);
        var date = $"/Date({new DateTimeOffset(receiptDate).ToUnixTimeMilliseconds()})/";
        var items = lines.Select(line =>
        {
            var poItem = line.PurchaseOrderItem;
            return new Dictionary<string, object?>
            {
                ["Material"] = line.MaterialCode,
                ["Plant"] = FirstNonEmpty(poItem.Plant, draft.Plant),
                ["GoodsMovementType"] = "101",
                ["PurchaseOrder"] = goodsReceipt.PurchaseOrder.PoNumber,
                ["PurchaseOrderItem"] = PurchaseOrderItemNumber(poItem),
                ["GoodsMovementRefDocType"] = "B",
                ["EntryUnit"] = line.Uom,
                ["QuantityInEntryUnit"] = line.AcceptedQuantity.ToString(CultureInfo.InvariantCulture),
            };
        }).ToList();
        return JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["DocumentDate"] = date,
            ["PostingDate"] = date,
            ["GoodsMovementCode"] = "01",
            ["MaterialDocumentHeaderText"] = goodsReceipt.GrnNumber,
            ["to_MaterialDocumentItem"] = items,
        }, JsonOptions);
    }

    private static string PurchaseOrderItemNumber(PurchaseOrderItem item)
    {
        var raw = item.ItemNumber ?? item.LineNumber.ToString(CultureInfo.InvariantCulture);
        return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number)
            ? number.ToString("00000", CultureInfo.InvariantCulture)
            : raw;
    }

    private static bool CsrfRequired(ApiIntegrationConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration.DesignerJson)) return false;
        try
        {
            return JsonSerializer.Deserialize<IntegrationDesignerDocument>(configuration.DesignerJson, JsonOptions)?.CsrfRequired == true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string Capture(
        string url,
        string requestBody,
        bool sent,
        string? csrfFetchUrl = null,
        int? status = null,
        string? response = null,
        IReadOnlyDictionary<string, string>? headers = null)
    {
        var safe = response is { Length: > 20000 } ? response[..20000] : response;
        return JsonSerializer.Serialize(new
        {
            url,
            method = "POST",
            sent,
            csrfFetchUrl,
            headers,
            requestBody,
            status,
            response = safe,
        }, JsonOptions);
    }

    private static Dictionary<string, string> RequestHeaders(HttpRequestMessage request)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in request.Headers)
        {
            headers[header.Key] = header.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase)
                ? "••••••••"
                : string.Join(", ", header.Value);
        }
        return headers;
    }

    private static string? SapError(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;
        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            if (root.TryGetProperty("error", out var error))
            {
                if (error.TryGetProperty("message", out var message))
                {
                    if (message.ValueKind == JsonValueKind.Object && message.TryGetProperty("value", out var value))
                        return value.GetString();
                    if (message.ValueKind == JsonValueKind.String) return message.GetString();
                }
                if (error.TryGetProperty("code", out var code) && code.ValueKind == JsonValueKind.String)
                    return code.GetString();
            }
        }
        catch (JsonException)
        {
            if (body.Contains("CSRF token validation failed", StringComparison.OrdinalIgnoreCase))
                return "CSRF token validation failed";
        }
        return null;
    }

    private static string? ReadJson(string body, params string[] names)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            if (root.TryGetProperty("d", out var wrapped)) root = wrapped;
            foreach (var name in names)
                if (root.TryGetProperty(name, out var value)) return value.ToString();
        }
        catch (JsonException)
        {
        }
        return null;
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(item => !string.IsNullOrWhiteSpace(item))?.Trim();

    private static ErpGoodsReceiptResult Fail(
        string code,
        string message,
        string? requestJson,
        Guid? routeId,
        ApiIntegrationConfiguration configuration,
        int? status = null) =>
        new(true, false, false, status, null, null, requestJson, code, message, null, routeId, configuration.Id, "SAP_S4HANA");
}
