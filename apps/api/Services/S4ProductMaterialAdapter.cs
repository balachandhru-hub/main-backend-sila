using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using SilaMe.Api.DTOs;
using SilaMe.Api.Models;

namespace SilaMe.Api.Services;

public sealed class S4ProductMaterialAdapter(
    IHttpMessageHandlerFactory handlerFactory,
    IntegrationDesignerService designer,
    IntegrationAuthenticationResolver authentication)
{
    public async Task<IReadOnlyList<MaterialNormalizedRecord>> PullAsync(
        ApiIntegrationConfiguration configuration,
        string? companyCode,
        CancellationToken cancellationToken)
    {
        var draft = designer.RuntimeDraft(configuration);
        var serviceRoot = IntegrationODataUrl.ServiceRoot(draft.BaseUrl, FirstNonEmpty(draft.ServicePath, "/sap/opu/odata/sap/API_PRODUCT_SRV"), "A_Product");
        using var client = CreateClient(draft.TimeoutSeconds);
        var authContext = new IntegrationAuthContext();
        var handler = authentication.Resolve(draft.AuthenticationType);
        async Task ApplyAuthAsync(HttpRequestMessage request) =>
            await handler.AuthenticateAsync(client, request, draft, authContext, cancellationToken);

        var products = await ReadEntitySetAsync(client, CombineEntity(serviceRoot, "A_Product"), ApplyAuthAsync, cancellationToken);
        var valuations = await ReadEntitySetAsync(client, CombineEntity(serviceRoot, "A_ProductValuation"), ApplyAuthAsync, cancellationToken);
        var productRows = products.Select(ToProduct).Where(item => item is not null).Select(item => item!).ToList();
        var valuationRows = valuations.Select(row => ToValuation(row, companyCode)).Where(item => item is not null).Select(item => item!).ToList();
        var joined = new List<MaterialNormalizedRecord>();
        foreach (var product in productRows)
        {
            var matches = valuationRows.Where(item => string.Equals(item.MaterialCode, product.MaterialCode, StringComparison.OrdinalIgnoreCase)).ToList();
            if (matches.Count == 0)
            {
                joined.Add(product);
                continue;
            }
            foreach (var valuation in matches)
                joined.Add(product with
                {
                    CompanyCode = valuation.CompanyCode ?? companyCode,
                    ValuationArea = valuation.ValuationArea,
                    ValuationClass = valuation.ValuationClass,
                    PriceControl = valuation.PriceControl,
                    StandardPrice = valuation.StandardPrice,
                    MovingAveragePrice = valuation.MovingAveragePrice,
                    Currency = valuation.Currency ?? product.Currency,
                    UnitCost = MaterialCosting.FromPriceControl(valuation.PriceControl, valuation.StandardPrice, valuation.MovingAveragePrice, product.UnitCost),
                });
        }
        return MaterialNormalized.MergeByProduct(joined);
    }

    private static MaterialNormalizedRecord? ToProduct(JsonElement row)
    {
        var code = Text(row, "Product", "Material", "ProductID");
        if (string.IsNullOrWhiteSpace(code)) return null;
        var description = NestedText(row, "to_Description") ?? Text(row, "ProductDescription", "MaterialDescription", "Description");
        return new MaterialNormalizedRecord(
            code.Trim(),
            description ?? code.Trim(),
            description,
            Text(row, "ProductType", "MaterialType"),
            Text(row, "ProductGroup", "MaterialGroup"),
            null,
            Text(row, "BaseUnit", "BaseUnitOfMeasure") ?? "EA",
            null,
            null, null, null, null, null, null, null, null,
            MaterialAcquisitionSource.ERP,
            "SAP_S4HANA",
            Date(row, "LastChangeDateTime", "LastChangeDate"));
    }

    private static MaterialNormalizedRecord? ToValuation(JsonElement row, string? companyCode)
    {
        var code = Text(row, "Product", "Material", "ProductID");
        var area = Text(row, "ValuationArea");
        if (string.IsNullOrWhiteSpace(code)) return null;
        return new MaterialNormalizedRecord(
            code.Trim(), code.Trim(), null, null, null, null, "EA", null,
            companyCode,
            area,
            Text(row, "ValuationClass"),
            Text(row, "InventoryValuationType", "PriceControl", "ValuationPriceControl"),
            Number(row, "StandardPrice"),
            Number(row, "MovingAveragePrice"),
            Text(row, "Currency", "CurrencyCode"),
            null,
            MaterialAcquisitionSource.ERP,
            "SAP_S4HANA",
            Date(row, "LastChangeDateTime"));
    }

    private async Task<List<JsonElement>> ReadEntitySetAsync(
        HttpClient client,
        string startUrl,
        Func<HttpRequestMessage, Task> authenticate,
        CancellationToken cancellationToken)
    {
        var rows = new List<JsonElement>();
        var url = startUrl;
        for (var page = 0; page < 50 && !string.IsNullOrWhiteSpace(url); page++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            await authenticate(request);
            using var response = await client.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new IntegrationException("ERP_MATERIAL_PULL_FAILED",
                    $"S/4 product API returned {(int)response.StatusCode} for {url}.", (int)response.StatusCode, body);
            using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
            rows.AddRange(ReadResults(document.RootElement).Select(item => item.Clone()));
            url = NextLink(document.RootElement);
        }
        return rows;
    }

    private static IEnumerable<JsonElement> ReadResults(JsonElement root)
    {
        if (root.TryGetProperty("value", out var value) && value.ValueKind == JsonValueKind.Array)
            return value.EnumerateArray();
        if (root.TryGetProperty("d", out var d) && d.TryGetProperty("results", out var results) && results.ValueKind == JsonValueKind.Array)
            return results.EnumerateArray();
        return [];
    }

    private static string? NextLink(JsonElement root)
    {
        if (root.TryGetProperty("@odata.nextLink", out var v4)) return v4.GetString();
        if (root.TryGetProperty("odata.nextLink", out var alt)) return alt.GetString();
        if (root.TryGetProperty("d", out var d) && d.TryGetProperty("__next", out var v2)) return v2.GetString();
        return null;
    }

    private HttpClient CreateClient(int timeoutSeconds)
    {
        var inner = handlerFactory.CreateHandler("api-integrations");
        return new HttpClient(new CookieSessionHandler(inner), disposeHandler: true)
        {
            Timeout = TimeSpan.FromSeconds(Math.Clamp(timeoutSeconds <= 0 ? 60 : timeoutSeconds, 5, 300)),
        };
    }

    private static string CombineEntity(string serviceRoot, string entitySet)
    {
        var url = IntegrationODataUrl.Combine(serviceRoot.TrimEnd('/'), entitySet);
        return url.Contains('?', StringComparison.Ordinal) ? url : url + "?$format=json&$top=200";
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(item => !string.IsNullOrWhiteSpace(item));

    private static string? Text(JsonElement row, params string[] names)
    {
        foreach (var name in names)
            if (row.TryGetProperty(name, out var value) && value.ValueKind is JsonValueKind.String or JsonValueKind.Number)
            {
                var text = value.ValueKind == JsonValueKind.Number ? value.GetRawText() : value.GetString();
                if (!string.IsNullOrWhiteSpace(text)) return text.Trim();
            }
        return null;
    }

    private static string? NestedText(JsonElement row, string nav)
    {
        if (!row.TryGetProperty(nav, out var node)) return null;
        if (node.TryGetProperty("results", out var results) && results.ValueKind == JsonValueKind.Array)
            return results.EnumerateArray().Select(item => Text(item, "ProductDescription", "MaterialDescription")).FirstOrDefault(item => item is not null);
        return Text(node, "ProductDescription", "MaterialDescription");
    }

    private static decimal? Number(JsonElement row, string name)
    {
        if (!row.TryGetProperty(name, out var value)) return null;
        if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number)) return number;
        return decimal.TryParse(value.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
    }

    private static DateTime? Date(JsonElement row, params string[] names)
    {
        var text = Text(row, names);
        if (string.IsNullOrWhiteSpace(text)) return null;
        if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
            return DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
        return null;
    }
}
