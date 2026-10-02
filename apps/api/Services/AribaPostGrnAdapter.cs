using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using SilaMe.Api.DTOs;
using SilaMe.Api.Models;

namespace SilaMe.Api.Services;

// Downstream of Invoice Review. Consumes saved GoodsReceiptId. Do not import or duplicate OCR/review.
// See docs/PROTECTED_INVOICE_FLOW.md.
public sealed class AribaPostGrnAdapter(IHttpClientFactory httpClientFactory, ProtectedIntegrationCredentialStore credentials)
{
    private static readonly XNamespace Soap = "http://schemas.xmlsoap.org/soap/envelope/";
    private static readonly XNamespace Urn = "urn:Ariba:Buyer:vsap";
    public const string DefaultSoapVariant = "vrealm_200086";
    public const string DefaultSoapPartition = "prealm_200086";

    public async Task<ErpGoodsReceiptResult> PostAsync(
        ResolvedIntegrationRoute route,
        GoodsReceipt goodsReceipt,
        CancellationToken cancellationToken)
    {
        var configuration = route.Configuration;
        var po = goodsReceipt.PurchaseOrder;
        var receivableId = ReceivableId(po);
        var (partition, variant) = ReadRealm(configuration);
        var receiptNumber = goodsReceipt.ErpMaterialDocument?.Trim() ?? string.Empty;
        var lines = goodsReceipt.Lines.Where(item => item.AcceptedQuantity > 0)
            .OrderBy(item => item.PurchaseOrderItem.LineNumber)
            .ToList();

        var lineItems = new List<XElement>();
        string? lineError = null;
        foreach (var line in lines)
        {
            if (!TryReceivableLineItemId(line.PurchaseOrderItem, out var receivableLineItemId))
            {
                lineError = $"Purchase order item {line.PurchaseOrderItem.ItemNumber ?? line.PurchaseOrderItem.LineNumber.ToString(CultureInfo.InvariantCulture)} is not compatible with Ariba ReceivableLineItemId conversion.";
                break;
            }
            lineItems.Add(new XElement(Urn + "item",
                new XElement(Urn + "AmountAccepted", "0"),
                new XElement(Urn + "AmountCurrency"),
                new XElement(Urn + "AmountRejected", "0"),
                new XElement(Urn + "Comment", goodsReceipt.Invoice?.InvoiceNumber ?? string.Empty),
                new XElement(Urn + "ASNReference", goodsReceipt.AsnReference ?? string.Empty),
                new XElement(Urn + "IsReceivingByCount", "true"),
                new XElement(Urn + "NumberAccepted", FormatQuantity(line.AcceptedQuantity)),
                new XElement(Urn + "NumberRejected"),
                new XElement(Urn + "ReceivableLineItemId", receivableLineItemId.ToString(CultureInfo.InvariantCulture)),
                new XElement(Urn + "ReceivedDate", DateTime.UtcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture)),
                new XElement(Urn + "ReceivingType", "1")));
        }

        var designer = ParseDesigner(configuration.DesignerJson);
        var url = BuildUrl(configuration);
        var soapAction = designer.SoapAction ?? "ExternalReceiptImport";
        var xml = BuildSoap(partition ?? string.Empty, variant ?? string.Empty, receiptNumber, receivableId, lineItems);
        var requestJson = CaptureRequest(url, soapAction, xml, sent: false);

        if (string.IsNullOrWhiteSpace(receivableId))
            return Fail("ARIBA_RECEIVABLE_ID_MISSING", "Purchase Order SourceLastChangedAt is required for Ariba receipt posting.", requestJson);
        if (string.IsNullOrWhiteSpace(partition) || string.IsNullOrWhiteSpace(variant))
            return Fail("ARIBA_REALM_MISSING", "Ariba partition and variant must be set on the API configuration.", requestJson);
        if (string.IsNullOrWhiteSpace(receiptNumber))
            return Fail("ERP_RECEIPT_NUMBER_MISSING", "A SILA ERP receipt number is required before Ariba posting.", requestJson);
        if (lines.Count == 0)
            return Fail("GRN_NO_ACCEPTED_LINES", "At least one GRN line with an accepted quantity is required.", requestJson);
        if (lineError is not null)
            return Fail("ARIBA_LINE_ID_INVALID", lineError, requestJson);

        var client = httpClientFactory.CreateClient("api-integrations");
        client.Timeout = TimeSpan.FromSeconds(Math.Clamp(configuration.TimeoutSeconds, 5, 300));
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Content = new StringContent(xml, Encoding.UTF8, "text/xml");
        request.Headers.TryAddWithoutValidation("SOAPAction", soapAction);
        try { AddAuthentication(configuration, request); }
        catch (System.Security.Cryptography.CryptographicException)
        {
            return Fail("INTEGRATION_CREDENTIALS_INVALID", "Stored API credentials could not be decrypted. Re-enter the password on the integration configuration.", requestJson);
        }
        catch (IntegrationException exception)
        {
            return Fail(exception.Code, exception.Message, requestJson);
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
            return new ErpGoodsReceiptResult(true, false, true, null, receiptNumber, null, CaptureRequest(url, soapAction, xml, sent: true), "ERP_POST_UNKNOWN", "The Ariba response timed out.", null, route.RouteId, configuration.Id, "SAP_ARIBA");
        }
        catch (HttpRequestException)
        {
            return new ErpGoodsReceiptResult(true, false, true, null, receiptNumber, null, CaptureRequest(url, soapAction, xml, sent: true), "ERP_POST_UNKNOWN", "Ariba could not be reached after the request was sent.", null, route.RouteId, configuration.Id, "SAP_ARIBA");
        }

        var safe = body.Length > 20000 ? body[..20000] : body;
        var captured = CaptureRequest(url, soapAction, xml, sent: true, safe);
        var parsed = ParseResponse(body);
        if (!response.IsSuccessStatusCode || parsed.Fault)
        {
            return new ErpGoodsReceiptResult(true, false, false, (int)response.StatusCode, receiptNumber, null, captured,
                parsed.ErrorCode ?? "ERP_POST_FAILED", parsed.Message ?? "Ariba rejected the goods receipt.",
                parsed.UniqueName, route.RouteId, configuration.Id, "SAP_ARIBA");
        }
        if (parsed.FailedStatus)
        {
            return new ErpGoodsReceiptResult(true, false, false, (int)response.StatusCode, receiptNumber, null, captured,
                "ERP_POST_FAILED", parsed.Message ?? "Ariba rejected the goods receipt.",
                parsed.UniqueName, route.RouteId, configuration.Id, "SAP_ARIBA");
        }

        return new ErpGoodsReceiptResult(true, true, false, (int)response.StatusCode, receiptNumber, null, captured, null, parsed.Message,
            parsed.UniqueName ?? parsed.ErpReceiptNumber, route.RouteId, configuration.Id, "SAP_ARIBA");
    }

    private static bool TryReceivableLineItemId(PurchaseOrderItem item, out int receivableLineItemId)
    {
        receivableLineItemId = 0;
        var lineNumber = item.LineNumber;
        if (lineNumber > 0 && lineNumber % 10 == 0)
        {
            receivableLineItemId = lineNumber / 10;
            return receivableLineItemId > 0;
        }
        if (int.TryParse(item.ItemNumber, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) && parsed > 0 && parsed % 10 == 0)
        {
            receivableLineItemId = parsed / 10;
            return receivableLineItemId > 0;
        }
        return false;
    }

    private static (string? Partition, string? Variant) ReadRealm(ApiIntegrationConfiguration configuration)
    {
        var designer = ParseDesigner(configuration.DesignerJson);
        var partition = FirstNonEmpty(designer.SoapPartition, Value(designer.QueryParameters, "partition"), Value(designer.PathParameters, "partition"), JsonValue(configuration.TokenBodyJson, "partition"), DefaultSoapPartition);
        var variant = FirstNonEmpty(designer.SoapVariant, Value(designer.QueryParameters, "variant"), Value(designer.PathParameters, "variant"), JsonValue(configuration.TokenBodyJson, "variant"), DefaultSoapVariant);
        if (!string.IsNullOrWhiteSpace(partition) && !string.IsNullOrWhiteSpace(variant)) return (partition, variant);
        if (string.IsNullOrWhiteSpace(designer.SamplePayload)) return (partition, variant);
        try
        {
            var document = XDocument.Parse(designer.SamplePayload);
            partition = FirstNonEmpty(partition, ElementValue(document, "partition"), AttributeValue(document, "partition"));
            variant = FirstNonEmpty(variant, ElementValue(document, "variant"), AttributeValue(document, "variant"));
        }
        catch (System.Xml.XmlException)
        {
            // Sample payload is optional configuration, not a posting input.
        }
        return (partition, variant);
    }

    private static ParsedSoap ParseResponse(string body)
    {
        try
        {
            var document = XDocument.Parse(body);
            var fault = document.Descendants().Any(item => item.Name.LocalName is "Fault" or "fault");
            var status = ElementValue(document, "StatusString");
            var message = FirstNonEmpty(ElementValue(document, "Message"), ElementValue(document, "faultstring"), ElementValue(document, "ErrorMessage"));
            var uniqueName = FirstNonEmpty(ElementValue(document, "UniqueName"), ElementValue(document, "uniqueName"));
            var erpReceipt = ElementValue(document, "ERPReceiptNumber");
            var failed = !string.IsNullOrWhiteSpace(status) &&
                (status.Contains("FAIL", StringComparison.OrdinalIgnoreCase) || status.Contains("ERROR", StringComparison.OrdinalIgnoreCase) || status.Contains("REJECT", StringComparison.OrdinalIgnoreCase));
            return new ParsedSoap(fault, failed, uniqueName, erpReceipt, message, fault ? "ARIBA_SOAP_FAULT" : null);
        }
        catch (System.Xml.XmlException)
        {
            return new ParsedSoap(false, false, null, null, null, null);
        }
    }

    private void AddAuthentication(ApiIntegrationConfiguration config, HttpRequestMessage request)
    {
        switch (config.AuthenticationType)
        {
            case IntegrationAuthenticationType.BASIC:
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{config.Username}:{credentials.Unprotect(config.ProtectedPassword)}")));
                break;
            case IntegrationAuthenticationType.BEARER_TOKEN:
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", credentials.Unprotect(config.ProtectedBearerToken));
                break;
            case IntegrationAuthenticationType.API_KEY:
            {
                var value = credentials.Unprotect(config.ProtectedBearerToken);
                var designer = ParseDesigner(config.DesignerJson);
                var name = designer.ApiKeyHeader ?? "APIKey";
                if (!string.IsNullOrWhiteSpace(value)) request.Headers.TryAddWithoutValidation(name, value);
                break;
            }
        }
    }

    private static string BuildUrl(ApiIntegrationConfiguration config)
    {
        var path = string.IsNullOrWhiteSpace(config.ResourcePath) ? config.ServicePath : config.ResourcePath;
        if (string.IsNullOrWhiteSpace(path)) return config.BaseUrl.TrimEnd('/');
        return $"{config.BaseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
    }

    private static IntegrationDesignerDocument ParseDesigner(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new IntegrationDesignerDocument();
        try { return JsonSerializer.Deserialize<IntegrationDesignerDocument>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new IntegrationDesignerDocument(); }
        catch (JsonException) { return new IntegrationDesignerDocument(); }
    }

    private static string ReceivableId(PurchaseOrder po)
    {
        var raw = po.SourceLastChangedAtRaw?.Trim() ?? string.Empty;
        const string prefix = "SourceLastChangedAt-";
        if (raw.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            raw = raw[prefix.Length..].Trim();
        return raw;
    }

    private static string FormatQuantity(decimal value) => value.ToString("0.########", CultureInfo.InvariantCulture);

    private static string BuildSoap(string partition, string variant, string receiptNumber, string receivableId, IEnumerable<XElement> lineItems)
    {
        var envelope = new XDocument(
            new XDeclaration("1.0", "utf-8", "yes"),
            new XElement(Soap + "Envelope",
                new XAttribute(XNamespace.Xmlns + "soapenv", Soap),
                new XAttribute(XNamespace.Xmlns + "urn", Urn),
                new XElement(Soap + "Header",
                    new XElement(Urn + "Headers",
                        new XElement(Urn + "variant", variant),
                        new XComment("Optional:"),
                        new XElement(Urn + "partition", partition))),
                new XElement(Soap + "Body",
                    new XElement(Urn + "ExternalReceiptImportRequest",
                        new XAttribute("partition", partition),
                        new XAttribute("variant", variant),
                        new XElement(Urn + "ExternalReceiptInputBean_Item",
                            new XElement(Urn + "item",
                                new XElement(Urn + "ERPReceiptNumber", receiptNumber),
                                new XElement(Urn + "ExternalReceiptItems", lineItems),
                                new XElement(Urn + "OriginatingSystem", "ERP"),
                                new XElement(Urn + "ReceivableId", receivableId)))))));
        return envelope.Declaration + Environment.NewLine + envelope.ToString(SaveOptions.DisableFormatting);
    }

    private static readonly JsonSerializerOptions CaptureJson = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private static string CaptureRequest(string url, string soapAction, string requestXml, bool sent, string? responseXml = null) =>
        JsonSerializer.Serialize(new { url, soapAction, sent, requestXml, responseXml }, CaptureJson);

    private static ErpGoodsReceiptResult Fail(string code, string message, string? requestJson = null) =>
        new(true, false, false, null, null, null, requestJson, code, message);
    private static string? FirstNonEmpty(params string?[] values) => values.FirstOrDefault(item => !string.IsNullOrWhiteSpace(item))?.Trim();
    private static string? Value(IReadOnlyDictionary<string, string>? values, string key) =>
        values is null ? null : values.TryGetValue(key, out var value) ? value : null;
    private static string? JsonValue(string? json, string key)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.ValueKind == JsonValueKind.Object && document.RootElement.TryGetProperty(key, out var value)
                ? value.GetString()
                : null;
        }
        catch (JsonException) { return null; }
    }
    private static string? ElementValue(XDocument document, string localName) =>
        document.Descendants().FirstOrDefault(item => string.Equals(item.Name.LocalName, localName, StringComparison.OrdinalIgnoreCase))?.Value;
    private static string? AttributeValue(XDocument document, string localName) =>
        document.Descendants().SelectMany(item => item.Attributes()).FirstOrDefault(item => string.Equals(item.Name.LocalName, localName, StringComparison.OrdinalIgnoreCase))?.Value;

    private sealed record ParsedSoap(bool Fault, bool FailedStatus, string? UniqueName, string? ErpReceiptNumber, string? Message, string? ErrorCode);
}
