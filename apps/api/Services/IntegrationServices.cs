using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Security;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.IO.Compression;
using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using SilaMe.Api.Data;
using SilaMe.Api.DTOs;
using SilaMe.Api.Models;
using SilaMe.Api.Tenancy;

namespace SilaMe.Api.Services;

public sealed class IntegrationException(string code, string message, int status = 400, string? detail = null) : Exception(message)
{
    public string Code { get; } = code;
    public int Status { get; } = status;
    public string? Detail { get; } = detail;
}

public sealed record IntegrationRunResult(
    ApiIntegrationExecution Execution,
    bool Claimed = true);

public sealed record ErpGoodsReceiptResult(
    bool Configured,
    bool Success,
    bool Unknown,
    int? HttpStatus,
    string? MaterialDocument,
    string? DocumentYear,
    string? ResponseJson,
    string? ErrorCode,
    string? ErrorMessage,
    string? ExternalReference = null,
    Guid? IntegrationRouteId = null,
    Guid? IntegrationConfigurationId = null,
    string? ExternalSystem = null);

public sealed class ProtectedIntegrationCredentialStore(IDataProtectionProvider provider)
{
    private readonly IDataProtector protector = provider.CreateProtector("SilaMe.Api.IntegrationCredentials.v1");
    public string? Protect(string? value) => string.IsNullOrWhiteSpace(value) ? null : protector.Protect(value);
    public string? Unprotect(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        try { return protector.Unprotect(value); }
        catch (System.Security.Cryptography.CryptographicException)
        {
            throw new IntegrationException("INTEGRATION_CREDENTIALS_INVALID",
                "Stored API credentials could not be decrypted. Re-enter the password on the integration configuration.", 400);
        }
    }
    public string State(ApiIntegrationConfiguration config) =>
        config.AuthenticationType == IntegrationAuthenticationType.NONE ? "Not required" :
        new[] { config.ProtectedPassword, config.ProtectedClientSecret, config.ProtectedBearerToken }.Any(value => !string.IsNullOrWhiteSpace(value)) ? "Configured" : "Missing";
}

public static class IntegrationTargetFieldRegistry
{
    public static readonly IReadOnlyList<IntegrationTargetFieldResponse> Fields =
    [
        new("PurchaseOrder.ExternalId", "Purchase order", "string", true, ["NONE", "TRIM"]),
        new("PurchaseOrder.PoNumber", "Purchase order", "string", true, ["NONE", "TRIM", "UPPER"]),
        new("PurchaseOrder.SupplierCode", "Purchase order", "string", true, ["NONE", "TRIM", "UPPER"]),
        new("PurchaseOrder.SupplierName", "Purchase order", "string", true, ["NONE", "TRIM"]),
        new("PurchaseOrder.Currency", "Purchase order", "string", true, ["NONE", "UPPER"]),
        new("PurchaseOrder.CompanyCode", "Purchase order", "string", true, ["NONE", "TRIM", "UPPER"]),
        new("PurchaseOrder.PurchaseOrderType", "Purchase order", "string", true, ["NONE", "TRIM", "UPPER"]),
        new("PurchaseOrder.TotalNetAmount", "Purchase order", "decimal", true, ["NONE"]),
        new("PurchaseOrder.TotalTaxAmount", "Purchase order", "decimal", true, ["NONE"]),
        new("PurchaseOrder.TotalAmount", "Purchase order", "decimal", true, ["NONE"]),
        new("PurchaseOrder.PoDate", "Purchase order", "date", true, ["NONE"]),
        new("PurchaseOrder.DeliveryDate", "Purchase order", "date", true, ["NONE"]),
        new("PurchaseOrder.SourceLastChangedAt", "Purchase order", "string", false, ["NONE", "TRIM"]),
        new("PurchaseOrderItem.ExternalId", "PO item", "string", false, ["NONE", "TRIM"]),
        new("PurchaseOrderItem.LineNumber", "PO item", "integer", true, ["NONE"]),
        new("PurchaseOrderItem.MaterialCode", "PO item", "string", true, ["NONE", "TRIM", "UPPER"]),
        new("PurchaseOrderItem.Description", "PO item", "string", true, ["NONE", "TRIM"]),
        new("PurchaseOrderItem.OrderedQuantity", "PO item", "decimal", true, ["NONE"]),
        new("PurchaseOrderItem.Uom", "PO item", "string", true, ["NONE", "TRIM", "UPPER"]),
        new("PurchaseOrderItem.UnitPrice", "PO item", "decimal", false, ["NONE"]),
        new("PurchaseOrderItem.SourceLastChangedAt", "PO item", "datetime", false, ["NONE"]),
        new("Supplier.ExternalId", "Supplier", "string", false, ["NONE", "TRIM"]),
        new("Supplier.SupplierCode", "Supplier", "string", true, ["NONE", "TRIM", "UPPER"]),
        new("Supplier.Name", "Supplier", "string", true, ["NONE", "TRIM"]),
        new("Supplier.LegalName", "Supplier", "string", false, ["NONE", "TRIM"]),
        new("Supplier.TaxNumber", "Supplier", "string", false, ["NONE", "TRIM"]),
        new("Supplier.Email", "Supplier", "string", false, ["NONE", "TRIM", "LOWER"]),
        new("Supplier.Phone", "Supplier", "string", false, ["NONE", "TRIM"]),
        new("Supplier.Country", "Supplier", "string", false, ["NONE", "TRIM"]),
        new("Supplier.Currency", "Supplier", "string", true, ["NONE", "UPPER"]),
        new("Supplier.SourceLastChangedAt", "Supplier", "datetime", false, ["NONE"]),
    ];

    public static bool Contains(string target) => Fields.Any(field => field.TargetField.Equals(target, StringComparison.OrdinalIgnoreCase));
}

public sealed partial class IntegrationService(
    SilaMeDbContext db,
    IHttpClientFactory httpClientFactory,
    ProtectedIntegrationCredentialStore credentials,
    IntegrationRouteResolver routes,
    AribaPostGrnAdapter aribaPostGrn,
    S4HanaPostGrnAdapter s4HanaPostGrn)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };
    private static readonly Regex SafePath = new(@"^[A-Za-z0-9_./$-]+$", RegexOptions.Compiled);

    public async Task<IReadOnlyList<IntegrationConfigurationResponse>> ListAsync(Guid organizationId, CancellationToken cancellationToken, string? environmentCode = null)
    {
        var environment = environmentCode;
        var items = await db.ApiIntegrationConfigurations.AsNoTracking()
            .Where(item => item.OrganizationId == organizationId &&
                (environment == null || item.EnvironmentCode == null || item.EnvironmentCode == environment))
            .OrderBy(item => item.Name)
            .ToListAsync(cancellationToken);
        return items.Select(item => ToResponse(item, credentials)).ToList();
    }

    public async Task<IntegrationConfigurationResponse?> GetAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        var item = await db.ApiIntegrationConfigurations.AsNoTracking().SingleOrDefaultAsync(candidate => candidate.Id == id && candidate.OrganizationId == organizationId, cancellationToken);
        return item is null ? null : ToResponse(item, credentials);
    }

    public async Task<IntegrationConfigurationResponse> SaveAsync(Guid organizationId, Guid? id, IntegrationConfigurationInput input, CancellationToken cancellationToken)
    {
        if (!SafePath.IsMatch(input.ResourcePath ?? string.Empty) && !string.IsNullOrWhiteSpace(input.ResourcePath))
            throw new IntegrationException("INVALID_RESOURCE_PATH", "The resource path contains unsupported characters.");
        var configuration = id is null
            ? new ApiIntegrationConfiguration { Id = Guid.NewGuid(), OrganizationId = organizationId, EntityCode = "ALL", Name = string.Empty, BaseUrl = string.Empty, CreatedAt = DateTime.UtcNow }
            : await db.ApiIntegrationConfigurations.SingleOrDefaultAsync(item => item.Id == id && item.OrganizationId == organizationId, cancellationToken)
              ?? throw new IntegrationException("INTEGRATION_NOT_FOUND", "The integration configuration was not found.", 404);
        var duplicate = await db.ApiIntegrationConfigurations.AnyAsync(item =>
            item.Id != configuration.Id && item.OrganizationId == organizationId && item.EntityCode == input.EntityCode &&
            item.ProcessType == input.ProcessType, cancellationToken);
        if (duplicate) throw new IntegrationException("INTEGRATION_ALREADY_EXISTS", "An integration already exists for this organization, entity, and process.");
        if (input.ProcessType is not (IntegrationProcessType.GET_PO or IntegrationProcessType.GET_SUPPLIER or IntegrationProcessType.POST_GRN))
            throw new IntegrationException("PROCESS_NOT_IMPLEMENTED", "This integration process is not enabled in the current release.");
        if (!Uri.TryCreate(input.BaseUrl, UriKind.Absolute, out var baseUri) || baseUri.Scheme is not ("http" or "https"))
            throw new IntegrationException("INVALID_BASE_URL", "Base URL must be an absolute HTTPS or HTTP URL.");
        if (configuration.Id == id) configuration.Status = configuration.Status == IntegrationConfigurationStatus.ACTIVE ? IntegrationConfigurationStatus.ACTIVE : IntegrationConfigurationStatus.DRAFT;
        configuration.OrganizationUnitId = input.OrganizationUnitId;
        configuration.EntityCode = input.EntityCode.Trim();
        configuration.Name = input.Name.Trim();
        configuration.ProcessType = input.ProcessType;
        configuration.Protocol = input.Protocol;
        configuration.BaseUrl = baseUri.ToString().TrimEnd('/');
        configuration.ResourcePath = input.ResourcePath?.Trim('/');
        configuration.AuthenticationType = input.AuthenticationType;
        configuration.Username = input.Username?.Trim();
        configuration.ProtectedPassword = credentials.Protect(input.Password) ?? configuration.ProtectedPassword;
        configuration.ProtectedClientId = credentials.Protect(input.ClientId) ?? configuration.ProtectedClientId;
        configuration.ProtectedClientSecret = credentials.Protect(input.ClientSecret) ?? configuration.ProtectedClientSecret;
        configuration.ProtectedBearerToken = credentials.Protect(input.BearerToken) ?? configuration.ProtectedBearerToken;
        configuration.TokenEndpoint = input.TokenEndpoint;
        configuration.TokenScope = input.TokenScope;
        configuration.TokenHeadersJson = input.TokenHeaders is null ? configuration.TokenHeadersJson : JsonSerializer.Serialize(input.TokenHeaders, JsonOptions);
        configuration.TokenBodyJson = input.TokenBody is null ? configuration.TokenBodyJson : JsonSerializer.Serialize(input.TokenBody, JsonOptions);
        configuration.TimeoutSeconds = input.TimeoutSeconds;
        configuration.RetryCount = input.RetryCount;
        configuration.PageSize = input.PageSize;
        configuration.WatermarkField = input.WatermarkField;
        configuration.ScheduleCron = input.ScheduleCron;
        configuration.UpdatedAt = DateTime.UtcNow;
        if (id is null) db.ApiIntegrationConfigurations.Add(configuration);
        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(configuration, credentials);
    }

    public async Task<IntegrationDeleteResponse> DeleteAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        var referenced = await db.IntegrationRoutes.AnyAsync(
            item => item.OrganizationId == organizationId && item.ApiIntegrationConfigurationId == id,
            cancellationToken);
        if (referenced)
        {
            throw new IntegrationException(
                "API_CONFIGURATION_IN_USE",
                "This API configuration is currently used by one or more Integration Routes. Remove the routing configuration before deleting this API configuration.",
                409);
        }

        var configuration = await db.ApiIntegrationConfigurations
            .Include(item => item.SchemaSnapshots)
            .Include(item => item.FieldMappings)
            .Include(item => item.Executions)
            .SingleOrDefaultAsync(item => item.Id == id && item.OrganizationId == organizationId, cancellationToken)
            ?? throw new IntegrationException("INTEGRATION_NOT_FOUND", "The integration configuration was not found.", 404);

        var tests = await db.IntegrationConnectionTests.Where(item => item.ConfigurationId == id).ToListAsync(cancellationToken);
        db.IntegrationConnectionTests.RemoveRange(tests);
        db.ApiIntegrationConfigurations.Remove(configuration);
        await db.SaveChangesAsync(cancellationToken);
        return new IntegrationDeleteResponse(true, id);
    }

    public async Task<IntegrationTestResponse> TestAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        var configuration = await GetTrackedAsync(organizationId, id, cancellationToken);
        var execution = new ApiIntegrationExecution
        {
            Id = Guid.NewGuid(), ConfigurationId = id, Trigger = IntegrationExecutionTrigger.TEST,
            Status = IntegrationExecutionStatus.RUNNING, StartedAt = DateTime.UtcNow
        };
        db.ApiIntegrationExecutions.Add(execution);
        try
        {
            var response = configuration.ProcessType == IntegrationProcessType.POST_GRN
                ? await SendAsync(configuration, configuration.BaseUrl.TrimEnd('/'), HttpMethod.Get, null, true, cancellationToken)
                : await SendAsync(configuration, testOnly: true, cancellationToken);
            var success = response.IsSuccessStatusCode;
            var message = success ? "API connection succeeded." : $"The API returned HTTP {(int)response.StatusCode}.";
            execution.Status = success ? IntegrationExecutionStatus.SUCCESS : IntegrationExecutionStatus.FAILED;
            execution.CompletedAt = DateTime.UtcNow;
            execution.ErrorCode = success ? null : "REMOTE_HTTP_ERROR";
            execution.ErrorMessageSafe = success ? null : message;
            configuration.Status = success ? IntegrationConfigurationStatus.TESTED : IntegrationConfigurationStatus.TEST_FAILED;
            configuration.TestedAt = DateTime.UtcNow;
            configuration.LastErrorSafe = success ? null : message;
            await db.SaveChangesAsync(cancellationToken);
            return new IntegrationTestResponse(success, message, (int)response.StatusCode, configuration.TestedAt.Value);
        }
        catch (IntegrationException exception)
        {
            execution.Status = IntegrationExecutionStatus.FAILED;
            execution.CompletedAt = DateTime.UtcNow;
            execution.ErrorCode = exception.Code;
            execution.ErrorMessageSafe = exception.Message;
            configuration.Status = IntegrationConfigurationStatus.TEST_FAILED;
            configuration.TestedAt = DateTime.UtcNow;
            configuration.LastErrorSafe = exception.Message;
            await db.SaveChangesAsync(cancellationToken);
            return new IntegrationTestResponse(false, exception.Message, exception.Status, configuration.TestedAt.Value);
        }
    }

    public async Task<IntegrationSchemaResponse> DiscoverSchemaAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        var configuration = await GetTrackedAsync(organizationId, id, cancellationToken);
        if (configuration.Protocol != IntegrationProtocol.ODATA_V4)
            throw new IntegrationException("SCHEMA_NOT_SUPPORTED", "Schema discovery is available for OData V4 configurations.");
        var metadataUrl = Combine(configuration.BaseUrl, "$metadata");
        var response = await SendAsync(configuration, metadataUrl, HttpMethod.Get, null, false, cancellationToken);
        var xml = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode) throw new IntegrationException("SCHEMA_REQUEST_FAILED", "The OData metadata request failed.", (int)response.StatusCode);
        var entities = ParseMetadata(xml);
        var snapshot = new IntegrationSchemaSnapshot { Id = Guid.NewGuid(), ConfigurationId = id, MetadataUrl = metadataUrl, SchemaJson = JsonSerializer.Serialize(entities, JsonOptions), DiscoveredAt = DateTime.UtcNow };
        db.IntegrationSchemaSnapshots.Add(snapshot);
        await db.SaveChangesAsync(cancellationToken);
        return new IntegrationSchemaResponse(id, metadataUrl, snapshot.DiscoveredAt, entities);
    }

    public async Task<IntegrationSchemaResponse?> LatestSchemaAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        var exists = await db.ApiIntegrationConfigurations.AnyAsync(item => item.Id == id && item.OrganizationId == organizationId, cancellationToken);
        if (!exists) return null;
        var snapshot = await db.IntegrationSchemaSnapshots.AsNoTracking().Where(item => item.ConfigurationId == id).OrderByDescending(item => item.DiscoveredAt).FirstOrDefaultAsync(cancellationToken);
        if (snapshot is null) return null;
        return new IntegrationSchemaResponse(id, snapshot.MetadataUrl, snapshot.DiscoveredAt,
            JsonSerializer.Deserialize<List<IntegrationSchemaEntity>>(snapshot.SchemaJson, JsonOptions) ?? []);
    }

    public async Task<IReadOnlyList<IntegrationMappingResponse>> GetMappingsAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        await EnsureConfigurationAsync(organizationId, id, cancellationToken);
        var mappings = await db.ApiFieldMappings.AsNoTracking().Where(item => item.ConfigurationId == id).OrderBy(item => item.TargetField).ToListAsync(cancellationToken);
        return mappings.Select(ToMapping).ToList();
    }

    public async Task<IReadOnlyList<IntegrationMappingResponse>> SaveMappingsAsync(Guid organizationId, Guid id, IReadOnlyList<FieldMappingInput> inputs, CancellationToken cancellationToken)
    {
        await EnsureConfigurationAsync(organizationId, id, cancellationToken);
        var configuration = await db.ApiIntegrationConfigurations.AsNoTracking().SingleAsync(item => item.Id == id && item.OrganizationId == organizationId, cancellationToken);
        var push = IntegrationDesignerCatalog.IsPush(configuration.ProcessType);
        var invalid = inputs.Where(input =>
            (!push && !IntegrationTargetFieldRegistry.Contains(input.TargetField)) ||
            input.SourceField.Contains(' ') || input.SourceField.Length > 250 || input.TargetField.Length > 250).ToList();
        if (invalid.Count != 0) throw new IntegrationException("INVALID_FIELD_MAPPING", "Every mapping must use a registered target field and a valid source field.");
        if (configuration.ProcessType == IntegrationProcessType.GET_PO)
        {
            if (!inputs.Any(item => item.TargetField.Equals("PurchaseOrder.PoNumber", StringComparison.OrdinalIgnoreCase)))
                throw new IntegrationException("MISSING_REQUIRED_MAPPING", "PurchaseOrder.PoNumber must be mapped.");
            if (!inputs.Any(item => item.TargetField.Equals("PurchaseOrder.SupplierCode", StringComparison.OrdinalIgnoreCase) || item.TargetField.Equals("PurchaseOrder.SupplierName", StringComparison.OrdinalIgnoreCase)))
                throw new IntegrationException("MISSING_REQUIRED_MAPPING", "A supplier code or supplier name mapping is required.");
        }
        if (configuration.ProcessType == IntegrationProcessType.GET_SUPPLIER &&
            (!inputs.Any(item => item.TargetField.Equals("Supplier.SupplierCode", StringComparison.OrdinalIgnoreCase)) ||
             !inputs.Any(item => item.TargetField.Equals("Supplier.Name", StringComparison.OrdinalIgnoreCase))))
            throw new IntegrationException("MISSING_REQUIRED_MAPPING", "Supplier code and supplier name mappings are required for supplier imports.");
        await db.ApiFieldMappings.Where(item => item.ConfigurationId == id).ExecuteDeleteAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var mappings = inputs.Select(input => new ApiFieldMapping
        {
            Id = Guid.NewGuid(), ConfigurationId = id, SourceField = input.SourceField.Trim(), TargetField = input.TargetField.Trim(),
            Transformation = input.Transformation?.ToUpperInvariant() switch { "TRIM" or "UPPER" or "LOWER" => input.Transformation.ToUpperInvariant(), _ => null },
            NullPolicy = input.NullPolicy, DefaultValue = input.DefaultValue, IsValidated = true, UpdatedAt = now,
            SourceKind = input.SourceKind, SourceStructure = input.SourceStructure, IsCollection = input.IsCollection
        }).ToList();
        db.ApiFieldMappings.AddRange(mappings);
        await db.SaveChangesAsync(cancellationToken);
        return mappings.Select(ToMapping).ToList();
    }

    public async Task<ApiIntegrationExecution?> RunAsync(Guid organizationId, Guid id, IntegrationExecutionTrigger trigger, bool fullSync, CancellationToken cancellationToken)
    {
        var claimed = await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE "api_integration_configurations"
            SET "IsRunning" = TRUE, "RunningSince" = {DateTime.UtcNow}, "LastAttemptAt" = {DateTime.UtcNow}, "UpdatedAt" = {DateTime.UtcNow}
            WHERE "Id" = {id} AND "OrganizationId" = {organizationId} AND "IsRunning" = FALSE AND "Status" <> 'INACTIVE'
            """, cancellationToken);
        if (claimed == 0) return null;
        var configuration = await GetTrackedAsync(organizationId, id, cancellationToken);
        var execution = new ApiIntegrationExecution
        {
            Id = Guid.NewGuid(), ConfigurationId = id, Trigger = trigger, Status = IntegrationExecutionStatus.RUNNING,
            StartedAt = DateTime.UtcNow, WatermarkBefore = fullSync ? null : configuration.LastWatermark
        };
        db.ApiIntegrationExecutions.Add(execution);
        await db.SaveChangesAsync(cancellationToken);
        try
        {
            if (configuration.ProcessType is not (IntegrationProcessType.GET_PO or IntegrationProcessType.GET_SUPPLIER))
                throw new IntegrationException("PROCESS_NOT_IMPLEMENTED", "Only inbound purchase-order and supplier pulls can be run from this screen.");
            var mappings = await db.ApiFieldMappings.AsNoTracking().Where(item => item.ConfigurationId == id).ToListAsync(cancellationToken);
            var response = await SendAsync(configuration, testOnly: false, cancellationToken);
            if (!response.IsSuccessStatusCode) throw new IntegrationException("REMOTE_HTTP_ERROR", $"The API returned HTTP {(int)response.StatusCode}.", (int)response.StatusCode);
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);
            var records = ReadRecords(payload);
            var maxWatermark = configuration.LastWatermark;
            foreach (var record in records)
            {
                execution.RecordsRead++;
                try
                {
                    ValidateMappedRecord(configuration, mappings, record);
                    var created = configuration.ProcessType == IntegrationProcessType.GET_SUPPLIER
                        ? await UpsertSupplierAsync(configuration, mappings, record, cancellationToken, "API_INTEGRATION")
                        : await UpsertPurchaseOrderAsync(configuration, mappings, record, cancellationToken, "API_INTEGRATION");
                    if (created) execution.RecordsCreated++; else execution.RecordsUpdated++;
                    maxWatermark = Max(maxWatermark, ReadDate(record, configuration.WatermarkField));
                }
                catch (Exception exception) when (exception is FormatException or InvalidOperationException or DbUpdateException)
                {
                    execution.RecordsFailed++;
                    execution.ErrorMessageSafe = "One or more records could not be imported.";
                }
            }
            execution.Status = execution.RecordsFailed > 0 ? IntegrationExecutionStatus.PARTIAL : IntegrationExecutionStatus.SUCCESS;
            execution.WatermarkAfter = maxWatermark;
            execution.CompletedAt = DateTime.UtcNow;
            configuration.LastWatermark = maxWatermark;
            configuration.LastSuccessfulRunAt = DateTime.UtcNow;
            configuration.LastErrorSafe = execution.RecordsFailed > 0 ? execution.ErrorMessageSafe : null;
            configuration.NextRunAt = NextRun(configuration.ScheduleCron);
            await db.SaveChangesAsync(cancellationToken);
            configuration.IsRunning = false;
            configuration.RunningSince = null;
            await db.SaveChangesAsync(cancellationToken);
            return execution;
        }
        catch (Exception exception)
        {
            execution.Status = IntegrationExecutionStatus.FAILED;
            execution.CompletedAt = DateTime.UtcNow;
            execution.ErrorCode = exception is IntegrationException integration ? integration.Code : "INTEGRATION_FAILED";
            execution.ErrorMessageSafe = exception is IntegrationException safe ? safe.Message : "The integration run failed.";
            configuration.LastErrorSafe = execution.ErrorMessageSafe;
            configuration.IsRunning = false;
            configuration.RunningSince = null;
            await db.SaveChangesAsync(cancellationToken);
            throw exception is IntegrationException ? exception : new IntegrationException("INTEGRATION_FAILED", "The integration run failed.");
        }
    }

    public async Task<IReadOnlyList<IntegrationExecutionResponse>> ExecutionsAsync(Guid organizationId, Guid? configurationId, CancellationToken cancellationToken)
    {
        var executions = await db.ApiIntegrationExecutions.AsNoTracking()
            .Where(item => item.Configuration.OrganizationId == organizationId && (configurationId == null || item.ConfigurationId == configurationId))
            .OrderByDescending(item => item.StartedAt).Take(100).ToListAsync(cancellationToken);
        return executions.Select(ToExecution).ToList();
    }

    public async Task<IntegrationDataUpdateResponse?> DataUpdateAsync(
        Guid organizationId,
        Guid id,
        IntegrationImportKind kind,
        string? search,
        string? status,
        string? sortBy,
        bool descending,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var config = await db.ApiIntegrationConfigurations.AsNoTracking().SingleOrDefaultAsync(item => item.OrganizationId == organizationId && item.Id == id, cancellationToken);
        if (config is null) return null;
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 10, 100);
        var term = search?.Trim();
        var purchaseOrders = db.PurchaseOrders.AsNoTracking()
            .Where(item => item.OrganizationId == organizationId &&
                (config.EntityCode == "ALL" || item.EntityCode == config.EntityCode))
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(term))
            purchaseOrders = purchaseOrders.Where(item => item.PoNumber.Contains(term) || item.Supplier.Name.Contains(term) || item.Currency.Contains(term));
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<PurchaseOrderStatus>(status, true, out var poStatus))
            purchaseOrders = purchaseOrders.Where(item => item.Status == poStatus);
        purchaseOrders = sortBy?.ToLowerInvariant() switch
        {
            "ponumber" => descending ? purchaseOrders.OrderByDescending(item => item.PoNumber) : purchaseOrders.OrderBy(item => item.PoNumber),
            "supplier" => descending ? purchaseOrders.OrderByDescending(item => item.Supplier.Name) : purchaseOrders.OrderBy(item => item.Supplier.Name),
            "status" => descending ? purchaseOrders.OrderByDescending(item => item.Status) : purchaseOrders.OrderBy(item => item.Status),
            "currency" => descending ? purchaseOrders.OrderByDescending(item => item.Currency) : purchaseOrders.OrderBy(item => item.Currency),
            "podate" => descending ? purchaseOrders.OrderByDescending(item => item.PoDate) : purchaseOrders.OrderBy(item => item.PoDate),
            _ => descending ? purchaseOrders.OrderByDescending(item => item.LastSyncedAt) : purchaseOrders.OrderBy(item => item.LastSyncedAt),
        };
        var suppliers = db.Suppliers.AsNoTracking()
            .Where(item => item.OrganizationId == organizationId &&
                (config.EntityCode == "ALL" || item.EntityCode == config.EntityCode) &&
                !item.IsDeleted)
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(term))
            suppliers = suppliers.Where(item => item.SupplierCode.Contains(term) || item.Name.Contains(term) || (item.TaxNumber != null && item.TaxNumber.Contains(term)));
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<StatusKind>(status, true, out var supplierStatus))
            suppliers = suppliers.Where(item => item.Status == supplierStatus);
        suppliers = sortBy?.ToLowerInvariant() switch
        {
            "suppliercode" => descending ? suppliers.OrderByDescending(item => item.SupplierCode) : suppliers.OrderBy(item => item.SupplierCode),
            "status" => descending ? suppliers.OrderByDescending(item => item.Status) : suppliers.OrderBy(item => item.Status),
            "updatedat" => descending ? suppliers.OrderByDescending(item => item.UpdatedAt) : suppliers.OrderBy(item => item.UpdatedAt),
            _ => descending ? suppliers.OrderByDescending(item => item.Name) : suppliers.OrderBy(item => item.Name),
        };
        var totalRows = kind == IntegrationImportKind.SUPPLIERS ? await suppliers.CountAsync(cancellationToken) : await purchaseOrders.CountAsync(cancellationToken);
        var purchaseOrderRows = kind == IntegrationImportKind.SUPPLIERS
            ? []
            : await purchaseOrders.Skip((page - 1) * pageSize).Take(pageSize)
                .Select(item => new IntegrationPurchaseOrderRow(item.Id, item.PoNumber, item.Supplier.Name, item.Status, item.Currency, item.PoDate, item.LastSyncedAt))
                .ToListAsync(cancellationToken);
        var supplierRows = kind == IntegrationImportKind.SUPPLIERS
            ? await suppliers.Skip((page - 1) * pageSize).Take(pageSize)
                .Select(item => new IntegrationSupplierRow(item.Id, item.SupplierCode, item.Name, item.TaxNumber, item.Status, item.UpdatedAt))
                .ToListAsync(cancellationToken)
            : [];
        var lastSynced = config.ProcessType == IntegrationProcessType.GET_SUPPLIER
            ? await db.Suppliers.Where(item => item.OrganizationId == organizationId && (config.EntityCode == "ALL" || item.EntityCode == config.EntityCode)).MaxAsync(item => (DateTime?)item.UpdatedAt, cancellationToken)
            : await db.PurchaseOrders.Where(item => item.OrganizationId == organizationId && (config.EntityCode == "ALL" || item.EntityCode == config.EntityCode)).MaxAsync(item => (DateTime?)item.LastSyncedAt, cancellationToken);
        return new IntegrationDataUpdateResponse(id, await db.PurchaseOrders.CountAsync(item => item.OrganizationId == organizationId && (config.EntityCode == "ALL" || item.EntityCode == config.EntityCode), cancellationToken), await db.Suppliers.CountAsync(item => item.OrganizationId == organizationId && (config.EntityCode == "ALL" || item.EntityCode == config.EntityCode) && !item.IsDeleted, cancellationToken), lastSynced, config.LastWatermark, config.IsRunning, config.LastErrorSafe, purchaseOrderRows, supplierRows, kind.ToString(), totalRows, page, pageSize);
    }

    public async Task<IntegrationImportPreviewResponse> PreviewImportAsync(
        Guid organizationId, Guid id, IntegrationImportKind kind, Stream file, string fileName, CancellationToken cancellationToken)
    {
        var config = await GetTrackedAsync(organizationId, id, cancellationToken);
        var rows = await ReadSpreadsheetAsync(file, fileName, kind, cancellationToken);
        if (rows.Count == 0) throw new IntegrationException("IMPORT_EMPTY", "The spreadsheet does not contain any data rows.");
        if (rows.Count > 5000) throw new IntegrationException("IMPORT_TOO_LARGE", "A spreadsheet can contain at most 5,000 data rows.");
        var entityCode = string.IsNullOrWhiteSpace(config.EntityCode) ? "ALL" : config.EntityCode;
        var normalized = await ClassifyImportRowsAsync(organizationId, entityCode, kind, NormalizeImportRows(kind, rows), cancellationToken);
        return new IntegrationImportPreviewResponse(
            config.Id, kind.ToString(), fileName, normalized.Count,
            normalized.Count(item => item.IsValid), normalized.Count(item => !item.IsValid),
            ImportColumns(kind), normalized,
            normalized.Count(item => item.IsValid && item.Action == "NEW"),
            normalized.Count(item => item.IsValid && item.Action == "UPDATE"),
            normalized.Count(item => item.IsValid && item.Action == "UNCHANGED"),
            entityCode);
    }

    public async Task<byte[]> CorrectionReportAsync(
        Guid organizationId, Guid id, IntegrationImportCorrectionReportInput input, CancellationToken cancellationToken)
    {
        await GetTrackedAsync(organizationId, id, cancellationToken);
        if (input.Rows.Count > 5000) throw new IntegrationException("IMPORT_TOO_LARGE", "A spreadsheet can contain at most 5,000 data rows.");

        var columns = ImportColumns(input.Kind);
        var invalidRows = input.Rows.Where(row => !row.IsValid).ToList();
        var csv = new StringBuilder();
        csv.Append('\uFEFF');
        AppendCsvRow(csv, ["Row number", .. columns, "Validation errors"]);
        foreach (var row in invalidRows)
        {
            var values = columns.Select(column => row.Values.TryGetValue(column, out var value) ? value : null);
            AppendCsvRow(csv, [row.RowNumber.ToString(CultureInfo.InvariantCulture), .. values, string.Join(" | ", row.Errors)]);
        }
        return Encoding.UTF8.GetBytes(csv.ToString());
    }

    public async Task<IntegrationImportCommitResponse> CommitImportAsync(
        Guid organizationId, Guid id, IntegrationImportCommitInput input, CancellationToken cancellationToken)
    {
        var config = await GetTrackedAsync(organizationId, id, cancellationToken);
        if (input.Rows.Count == 0) throw new IntegrationException("IMPORT_EMPTY", "There are no rows to commit.");
        if (input.Rows.Count > 5000) throw new IntegrationException("IMPORT_TOO_LARGE", "A spreadsheet can contain at most 5,000 data rows.");
        var normalized = NormalizeImportRows(input.Kind, input.Rows);
        if (normalized.Any(item => !item.IsValid))
            throw new IntegrationException("IMPORT_VALIDATION_FAILED", "The spreadsheet contains invalid rows. No records were changed.");
        var mappings = ImportMappings(input.Kind);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var execution = new ApiIntegrationExecution
        {
            Id = Guid.NewGuid(), ConfigurationId = id, Trigger = IntegrationExecutionTrigger.EXCEL_IMPORT,
            Status = IntegrationExecutionStatus.RUNNING, StartedAt = DateTime.UtcNow
        };
        db.ApiIntegrationExecutions.Add(execution);
        try
        {
            if (input.Kind == IntegrationImportKind.SUPPLIERS)
            {
                foreach (var row in normalized)
                {
                    execution.RecordsRead++;
                    var created = await UpsertSupplierAsync(config, mappings, ToCanonicalRecord(input.Kind, row.Values), cancellationToken, "EXCEL_UPLOAD");
                    if (created) execution.RecordsCreated++; else execution.RecordsUpdated++;
                }
            }
            else
            {
                foreach (var group in normalized.GroupBy(row => row.Values["PO_NUMBER"] ?? string.Empty, StringComparer.OrdinalIgnoreCase))
                {
                    execution.RecordsRead += group.Count();
                    var record = ToCanonicalPurchaseOrderRecord(group.Select(row => row.Values).ToList());
                    var created = await UpsertPurchaseOrderAsync(config, mappings, record, cancellationToken, "EXCEL_UPLOAD");
                    if (created) execution.RecordsCreated++; else execution.RecordsUpdated++;
                }
            }
            execution.Status = IntegrationExecutionStatus.SUCCESS;
            execution.CompletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new IntegrationImportCommitResponse(ToExecution(execution), normalized.Count);
        }
        catch (IntegrationException) { await transaction.RollbackAsync(cancellationToken); throw; }
        catch (Exception exception) when (exception is InvalidOperationException or DbUpdateException or FormatException)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new IntegrationException("IMPORT_COMMIT_FAILED", "The spreadsheet could not be committed. No records were changed.", 400);
        }
    }

    public async Task<byte[]> TemplateAsync(IntegrationImportKind kind, CancellationToken cancellationToken) =>
        await Task.FromResult(BuildMasterTemplate(kind));

    public async Task<byte[]> ExportAsync(
        Guid organizationId, Guid id, IntegrationImportKind kind, string? search, string? status,
        string? sortBy, bool descending, CancellationToken cancellationToken)
    {
        var config = await db.ApiIntegrationConfigurations.AsNoTracking().SingleOrDefaultAsync(item => item.OrganizationId == organizationId && item.Id == id, cancellationToken)
            ?? throw new IntegrationException("INTEGRATION_NOT_FOUND", "The integration configuration was not found.", 404);
        var rows = new List<Dictionary<string, string?>>();
        if (kind == IntegrationImportKind.SUPPLIERS)
        {
            var query = db.Suppliers.AsNoTracking().Where(item => item.OrganizationId == organizationId &&
                (config.EntityCode == "ALL" || item.EntityCode == config.EntityCode));
            if (!string.IsNullOrWhiteSpace(search)) query = query.Where(item => item.SupplierCode.Contains(search) || item.Name.Contains(search) || (item.TaxNumber != null && item.TaxNumber.Contains(search)));
            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<StatusKind>(status, true, out var supplierStatus)) query = query.Where(item => item.Status == supplierStatus);
            var values = await query.OrderBy(item => item.Name).Take(10000).ToListAsync(cancellationToken);
            rows.AddRange(values.Select(item => new Dictionary<string, string?>
            {
                ["SUPPLIER_CODE"] = item.SupplierCode, ["NAME"] = item.Name, ["LEGAL_NAME"] = item.LegalName,
                ["TAX_NUMBER"] = item.TaxNumber, ["EMAIL"] = item.Email, ["PHONE"] = item.Phone,
                ["COUNTRY"] = item.Country, ["CURRENCY"] = item.Currency, ["EXTERNAL_ID"] = item.ExternalId,
            }));
        }
        else
        {
            var query = db.PurchaseOrders.AsNoTracking().Where(item => item.OrganizationId == organizationId &&
                (config.EntityCode == "ALL" || item.EntityCode == config.EntityCode));
            if (!string.IsNullOrWhiteSpace(search)) query = query.Where(item => item.PoNumber.Contains(search) || item.Supplier.Name.Contains(search) || item.Currency.Contains(search));
            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<PurchaseOrderStatus>(status, true, out var poStatus)) query = query.Where(item => item.Status == poStatus);
            var values = await query.Include(item => item.Supplier).OrderByDescending(item => item.LastSyncedAt).Take(10000).ToListAsync(cancellationToken);
            rows.AddRange(values.Select(item => new Dictionary<string, string?>
            {
                ["EXTERNAL_ID"] = item.ExternalId, ["PO_NUMBER"] = item.PoNumber, ["SUPPLIER_CODE"] = item.Supplier.SupplierCode,
                ["SUPPLIER_NAME"] = item.SupplierName ?? item.Supplier.Name, ["CURRENCY"] = item.Currency,
                ["COMPANY_CODE"] = item.CompanyCode, ["PURCHASE_ORDER_TYPE"] = item.PurchaseOrderType,
                ["TOTAL_NET_AMOUNT"] = item.TotalNetAmount?.ToString(CultureInfo.InvariantCulture),
                ["TOTAL_TAX_AMOUNT"] = item.TotalTaxAmount?.ToString(CultureInfo.InvariantCulture),
                ["TOTAL_AMOUNT"] = item.TotalAmount?.ToString(CultureInfo.InvariantCulture),
                ["PO_DATE"] = item.PoDate?.ToString("yyyy-MM-dd"),
                ["DELIVERY_DATE"] = item.DeliveryDate?.ToString("yyyy-MM-dd"),
                ["SOURCE_LAST_CHANGED_AT"] = !string.IsNullOrWhiteSpace(item.SourceLastChangedAtRaw)
                    ? item.SourceLastChangedAtRaw
                    : item.SourceLastChangedAt?.ToString("O"),
            }));
        }
        _ = config;
        return BuildSpreadsheet(ImportColumns(kind), rows);
    }

    public async Task<bool> ActivateAsync(Guid organizationId, Guid id, bool active, CancellationToken cancellationToken)
    {
        var config = await GetTrackedAsync(organizationId, id, cancellationToken);
        if (active && config.Status is not IntegrationConfigurationStatus.TESTED and not IntegrationConfigurationStatus.VALIDATED and not IntegrationConfigurationStatus.ACTIVE)
            throw new IntegrationException("TEST_REQUIRED", "Test API successfully before activating this configuration.");
        config.Status = active ? IntegrationConfigurationStatus.ACTIVE : IntegrationConfigurationStatus.DISABLED;
        config.NextRunAt = active ? DateTime.UtcNow : null;
        config.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return active;
    }

    public async Task<ErpGoodsReceiptResult> PostGoodsReceiptAsync(
        Guid organizationId,
        string entityCode,
        GoodsReceipt goodsReceipt,
        CancellationToken cancellationToken)
    {
        if (goodsReceipt.PurchaseOrder is null)
            await db.Entry(goodsReceipt).Reference(item => item.PurchaseOrder).LoadAsync(cancellationToken);
        if (goodsReceipt.PurchaseOrder is null)
            return new ErpGoodsReceiptResult(false, false, false, 404, goodsReceipt.ErpMaterialDocument, null, null, "PO_NOT_FOUND", "The purchase order for this GRN was not found.");
        if (goodsReceipt.Invoice is null && goodsReceipt.InvoiceId is not null)
            await db.Entry(goodsReceipt).Reference(item => item.Invoice).LoadAsync(cancellationToken);
        if (goodsReceipt.Lines.Count == 0)
            await db.Entry(goodsReceipt).Collection(item => item.Lines).Query().Include(item => item.PurchaseOrderItem).LoadAsync(cancellationToken);

        ApiIntegrationConfiguration? configuration;
        ResolvedIntegrationRoute? resolved = null;
        if (await routes.HasActiveRoutesAsync(organizationId, IntegrationProcessType.POST_GRN, cancellationToken))
        {
            try
            {
                resolved = await routes.ResolveAsync(organizationId, IntegrationProcessType.POST_GRN, goodsReceipt.PurchaseOrder.CompanyCode, cancellationToken);
            }
            catch (IntegrationException exception)
            {
                return new ErpGoodsReceiptResult(false, false, false, exception.Status, goodsReceipt.ErpMaterialDocument, null, null, exception.Code, exception.Message);
            }
            configuration = resolved.Configuration;
        }
        else
        {
            var configurations = await db.ApiIntegrationConfigurations
                .Where(item => item.OrganizationId == organizationId &&
                    item.ProcessType == IntegrationProcessType.POST_GRN &&
                    item.Status == IntegrationConfigurationStatus.ACTIVE &&
                    (item.EntityCode == entityCode || item.EntityCode == "ALL"))
                .ToListAsync(cancellationToken);
            configuration = ResolvePostGrnConfiguration(configurations, entityCode);
        }

        if (configuration is null)
            return new ErpGoodsReceiptResult(false, false, false, null, null, null, null,
                "POST_GRN_CONFIGURATION_NOT_FOUND",
                "GRN posting is not configured for this organization. Contact your administrator.");

        if (resolved is not null && resolved.SystemKind == IntegrationSystemKind.SAP_ARIBA)
            return await aribaPostGrn.PostAsync(resolved, goodsReceipt, cancellationToken);
        if (S4HanaPostGrnAdapter.AppliesTo(configuration))
            return await s4HanaPostGrn.PostAsync(configuration, goodsReceipt, resolved?.RouteId, cancellationToken);

        var payload = JsonSerializer.Serialize(new
        {
            goodsReceipt.GrnNumber,
            goodsReceipt.ReceiptDate,
            goodsReceipt.PurchaseOrderId,
            goodsReceipt.SupplierId,
            goodsReceipt.ErpMaterialDocument,
            Lines = goodsReceipt.Lines.Select(line => new
            {
                line.PurchaseOrderItemId,
                line.MaterialCode,
                line.ReceivedQuantity,
                line.AcceptedQuantity,
                line.DamagedQuantity,
                line.RejectedQuantity,
                line.Uom,
                line.BatchNumber,
                line.ExpiryDate,
            }),
        }, JsonOptions);
        try
        {
            var response = await SendAsync(configuration, BuildUrl(configuration, false), HttpMethod.Post, payload, false, cancellationToken);
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var safeResponse = responseJson.Length > 20000 ? responseJson[..20000] : responseJson;
            if (!response.IsSuccessStatusCode)
                return new ErpGoodsReceiptResult(true, false, false, (int)response.StatusCode, goodsReceipt.ErpMaterialDocument, null, safeResponse, "ERP_POST_FAILED", "The ERP rejected the goods receipt.", null, resolved?.RouteId, configuration.Id, configuration.SystemKind.ToString());
            string? Read(params string[] names)
            {
                using var json = JsonDocument.Parse(responseJson);
                foreach (var name in names)
                    if (json.RootElement.TryGetProperty(name, out var value)) return value.ToString();
                return null;
            }
            return new ErpGoodsReceiptResult(true, true, false, (int)response.StatusCode, Read("MaterialDocument", "materialDocument", "MaterialDocumentNumber") ?? goodsReceipt.ErpMaterialDocument, Read("DocumentYear", "documentYear"), safeResponse, null, null, Read("UniqueName", "uniqueName", "ExternalReference"), resolved?.RouteId, configuration.Id, configuration.SystemKind.ToString());
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new ErpGoodsReceiptResult(true, false, true, null, goodsReceipt.ErpMaterialDocument, null, null, "ERP_POST_UNKNOWN", "The ERP response timed out.", null, resolved?.RouteId, configuration.Id, configuration.SystemKind.ToString());
        }
        catch (HttpRequestException)
        {
            return new ErpGoodsReceiptResult(true, false, true, null, goodsReceipt.ErpMaterialDocument, null, null, "ERP_POST_UNKNOWN", "The ERP could not be reached.", null, resolved?.RouteId, configuration.Id, configuration.SystemKind.ToString());
        }
        catch (IntegrationException exception)
        {
            return new ErpGoodsReceiptResult(true, false, true, exception.Status, goodsReceipt.ErpMaterialDocument, null, null, exception.Code, exception.Message, null, resolved?.RouteId, configuration.Id, configuration.SystemKind.ToString());
        }
    }

    public async Task<(ApiIntegrationConfiguration Configuration, Guid? RouteId)?> TryResolveUpdateStockAsync(
        Guid organizationId, string? companyCode, CancellationToken cancellationToken)
    {
        var named = await db.ApiIntegrationConfigurations.AsNoTracking()
            .Where(item => item.OrganizationId == organizationId &&
                item.Status == IntegrationConfigurationStatus.ACTIVE &&
                item.Name == RecipeSapUpdateStock.ConfigurationName)
            .OrderBy(item => item.ProcessType == RecipeSapUpdateStock.ProcessType ? 0 : 1)
            .ThenBy(item => item.Id)
            .FirstOrDefaultAsync(cancellationToken);
        ResolvedIntegrationRoute? routed = null;
        if (!string.IsNullOrWhiteSpace(companyCode))
        {
            try { routed = await routes.ResolveAsync(organizationId, RecipeSapUpdateStock.ProcessType, companyCode, cancellationToken); }
            catch (IntegrationException) { routed = null; }
        }
        if (named is not null) return (named, routed?.RouteId);
        if (routed is not null) return (routed.Configuration, routed.RouteId);
        return null;
    }

    public async Task<ErpGoodsReceiptResult> PostJsonOnceAsync(ApiIntegrationConfiguration configuration, string body, CancellationToken cancellationToken)
    {
        var url = Combine(configuration.BaseUrl, configuration.ResourcePath ?? string.Empty);
        try
        {
            using var response = await SendAsync(configuration, url, HttpMethod.Post, body, false, cancellationToken, false);
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var safe = responseJson.Length > 20000 ? responseJson[..20000] : responseJson;
            var (document, year) = RecipeSapUpdateStock.ReadDocument(safe);
            if (!response.IsSuccessStatusCode)
                return new ErpGoodsReceiptResult(true, false, false, (int)response.StatusCode, document, year, safe, "SAP_POST_FAILED", "SAP posting failed");
            return new ErpGoodsReceiptResult(true, true, false, (int)response.StatusCode, document, year, safe, null, null);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new ErpGoodsReceiptResult(true, false, true, null, null, null, null, "POSTING_UNKNOWN", "SAP posting confirmation unknown");
        }
        catch (HttpRequestException)
        {
            return new ErpGoodsReceiptResult(true, false, true, null, null, null, null, "POSTING_UNKNOWN", "SAP posting confirmation unknown");
        }
        catch (IntegrationException exception)
        {
            return new ErpGoodsReceiptResult(true, false, true, exception.Status, null, null, null, "POSTING_UNKNOWN", "SAP posting confirmation unknown");
        }
    }

    public static ApiIntegrationConfiguration? ResolvePostGrnConfiguration(
        IEnumerable<ApiIntegrationConfiguration> configurations,
        string entityCode)
    {
        return configurations
            .Where(item => item.ProcessType == IntegrationProcessType.POST_GRN &&
                item.Status == IntegrationConfigurationStatus.ACTIVE &&
                (item.EntityCode == entityCode || item.EntityCode == "ALL"))
            .OrderBy(item => string.Equals(item.EntityCode, entityCode, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Id)
            .FirstOrDefault();
    }

    private async Task<ApiIntegrationConfiguration> GetTrackedAsync(Guid organizationId, Guid id, CancellationToken cancellationToken) =>
        await db.ApiIntegrationConfigurations.SingleOrDefaultAsync(item => item.Id == id && item.OrganizationId == organizationId, cancellationToken)
        ?? throw new IntegrationException("INTEGRATION_NOT_FOUND", "The integration configuration was not found.", 404);

    private async Task EnsureConfigurationAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        if (!await db.ApiIntegrationConfigurations.AnyAsync(item => item.Id == id && item.OrganizationId == organizationId, cancellationToken))
            throw new IntegrationException("INTEGRATION_NOT_FOUND", "The integration configuration was not found.", 404);
    }

    private async Task<HttpResponseMessage> SendAsync(ApiIntegrationConfiguration config, bool testOnly, CancellationToken cancellationToken) =>
        await SendAsync(config, BuildUrl(config, testOnly), HttpMethod.Get, null, testOnly, cancellationToken);

    private async Task<HttpResponseMessage> SendAsync(ApiIntegrationConfiguration config, string url, HttpMethod method, string? body, bool testOnly, CancellationToken cancellationToken, bool retry = true)
    {
        using var client = httpClientFactory.CreateClient("api-integrations");
        client.Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds);
        var attempts = retry ? Math.Max(1, config.RetryCount + 1) : 1;
        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            using var request = new HttpRequestMessage(method, url);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            await AddAuthenticationAsync(config, request, client, cancellationToken);
            if (body is not null) request.Content = new StringContent(body, Encoding.UTF8, "application/json");
            try
            {
                var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                if ((int)response.StatusCode >= 500 && attempt < attempts) { response.Dispose(); await Task.Delay(TimeSpan.FromMilliseconds(150 * attempt), cancellationToken); continue; }
                return response;
            }
            catch (HttpRequestException) when (attempt < attempts) { await Task.Delay(TimeSpan.FromMilliseconds(150 * attempt), cancellationToken); }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested && attempt < attempts) { await Task.Delay(TimeSpan.FromMilliseconds(150 * attempt), cancellationToken); }
        }
        throw new IntegrationException("REMOTE_UNAVAILABLE", "The configured API could not be reached.", 502);
    }

    private async Task AddAuthenticationAsync(ApiIntegrationConfiguration config, HttpRequestMessage request, HttpClient client, CancellationToken cancellationToken)
    {
        switch (config.AuthenticationType)
        {
            case IntegrationAuthenticationType.BASIC:
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{config.Username}:{credentials.Unprotect(config.ProtectedPassword)}")));
                break;
            case IntegrationAuthenticationType.BEARER_TOKEN:
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", credentials.Unprotect(config.ProtectedBearerToken));
                break;
            case IntegrationAuthenticationType.OAUTH2_CLIENT_CREDENTIALS:
            case IntegrationAuthenticationType.CUSTOM_TOKEN_ENDPOINT:
            {
                if (string.IsNullOrWhiteSpace(config.TokenEndpoint)) throw new IntegrationException("TOKEN_ENDPOINT_REQUIRED", "A token endpoint is required for this authentication type.");
                using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, config.TokenEndpoint);
                tokenRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials",
                    ["client_id"] = credentials.Unprotect(config.ProtectedClientId) ?? string.Empty,
                    ["client_secret"] = credentials.Unprotect(config.ProtectedClientSecret) ?? string.Empty,
                    ["scope"] = config.TokenScope ?? string.Empty
                });
                using var tokenResponse = await client.SendAsync(tokenRequest, cancellationToken);
                if (!tokenResponse.IsSuccessStatusCode) throw new IntegrationException("TOKEN_REQUEST_FAILED", "The token endpoint rejected the configured credentials.", (int)tokenResponse.StatusCode);
                using var tokenJson = JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync(cancellationToken));
                if (!tokenJson.RootElement.TryGetProperty("access_token", out var token)) throw new IntegrationException("TOKEN_RESPONSE_INVALID", "The token endpoint did not return an access token.");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.GetString());
                break;
            }
        }
    }

    private static string BuildUrl(ApiIntegrationConfiguration config, bool testOnly)
    {
        var url = Combine(config.BaseUrl, config.ResourcePath ?? string.Empty);
        if (config.Protocol == IntegrationProtocol.ODATA_V4)
        {
            var query = testOnly ? "$top=1" : $"$top={config.PageSize ?? 100}";
            if (!testOnly && !string.IsNullOrWhiteSpace(config.WatermarkField) && config.LastWatermark is not null)
                query += $"&$filter={Uri.EscapeDataString(config.WatermarkField)}%20gt%20{Uri.EscapeDataString(config.LastWatermark.Value.ToUniversalTime().ToString("O"))}";
            url += (url.Contains('?') ? "&" : "?") + query;
        }
        return url;
    }

    private static string Combine(string baseUrl, string path) => $"{baseUrl.TrimEnd('/')}/{path.TrimStart('/')}";

    private static IReadOnlyList<IntegrationSchemaEntity> ParseMetadata(string xml)
    {
        var document = XDocument.Parse(xml);
        return document.Descendants().Where(item => item.Name.LocalName == "EntityType").Select(entity =>
        {
            var properties = entity.Elements().Where(item => item.Name.LocalName == "Property")
                .Select(property => new IntegrationSchemaProperty((string?)property.Attribute("Name") ?? string.Empty, ((string?)property.Attribute("Type") ?? "Edm.String").Replace("Edm.", string.Empty), (string?)property.Attribute("Nullable") != "false")).ToList();
            var keys = entity.Elements().Where(item => item.Name.LocalName == "Key").Elements().Select(item => (string?)item.Attribute("PropertyRef") ?? string.Empty).Where(item => item.Length > 0).ToList();
            return new IntegrationSchemaEntity((string?)entity.Attribute("Name") ?? string.Empty, null, properties, keys);
        }).ToList();
    }

    private static List<JsonElement> ReadRecords(string payload)
    {
        using var document = JsonDocument.Parse(payload);
        if (document.RootElement.ValueKind == JsonValueKind.Array) return document.RootElement.EnumerateArray().Select(item => item.Clone()).ToList();
        if (document.RootElement.TryGetProperty("value", out var value) && value.ValueKind == JsonValueKind.Array) return value.EnumerateArray().Select(item => item.Clone()).ToList();
        return [document.RootElement.Clone()];
    }

    private async Task<bool> UpsertPurchaseOrderAsync(ApiIntegrationConfiguration config, IReadOnlyList<ApiFieldMapping> mappings, JsonElement record, CancellationToken cancellationToken, string sourceSystem = "API_INTEGRATION")
    {
        string? Get(string target) => MapValue(mappings.FirstOrDefault(item => item.TargetField.Equals(target, StringComparison.OrdinalIgnoreCase)), record);
        var poNumber = Get("PurchaseOrder.PoNumber") ?? throw new InvalidOperationException("Purchase order number is missing.");
        var externalId = Get("PurchaseOrder.ExternalId");
        var supplierCode = Get("PurchaseOrder.SupplierCode") ?? throw new InvalidOperationException("Supplier code is missing.");
        var supplierName = Get("PurchaseOrder.SupplierName");
        var currency = Get("PurchaseOrder.Currency")?.Trim().ToUpperInvariant()
            ?? throw new InvalidOperationException("Currency is missing.");
        if (!Regex.IsMatch(currency, "^[A-Z]{3}$"))
            throw new InvalidOperationException("Currency must be a three-letter ISO code.");
        var supplier = await FindSupplierAsync(config.OrganizationId, config.EntityCode, supplierCode, cancellationToken)
            ?? throw new IntegrationException("SUPPLIER_NOT_FOUND", $"Supplier {supplierCode} does not exist in entity {config.EntityCode}.");
        if (supplier.IsBlocked || supplier.IsDeleted || supplier.Status != StatusKind.ACTIVE)
            throw new IntegrationException("SUPPLIER_INACTIVE", $"Supplier {supplierCode} is not active.");
        var entityCode = string.IsNullOrWhiteSpace(config.EntityCode) ? supplier.EntityCode : config.EntityCode;
        var po = await db.PurchaseOrders.Include(item => item.Items).SingleOrDefaultAsync(item =>
            item.OrganizationId == config.OrganizationId && ((externalId != null && item.SourceConfigurationId == config.Id && item.ExternalId == externalId) ||
            (item.EntityCode == entityCode && item.PoNumber == poNumber)), cancellationToken);
        var created = po is null;
        po ??= new PurchaseOrder { Id = Guid.NewGuid(), OrganizationId = config.OrganizationId, PoNumber = poNumber, Currency = currency, SourceSystem = sourceSystem, Status = PurchaseOrderStatus.OPEN, CreatedAt = DateTime.UtcNow };
        po.EntityCode = entityCode;
        po.SourceConfigurationId = config.Id == Guid.Empty ? po.SourceConfigurationId : config.Id;
        po.ExternalId = externalId;
        po.SupplierId = supplier.Id;
        po.Supplier = supplier;
        po.Currency = currency;
        po.SupplierName = !string.IsNullOrWhiteSpace(supplierName) && !InvoiceOcrFieldReader.IsSupplierIdLabel(supplierName)
            ? supplierName
            : (po.SupplierName ?? supplier.Name);
        po.ErpSupplierId = supplier.SupplierCode;
        po.CompanyCode = Get("PurchaseOrder.CompanyCode") ?? po.CompanyCode;
        po.PurchaseOrderType = Get("PurchaseOrder.PurchaseOrderType") ?? po.PurchaseOrderType;
        po.PurchasingOrganization = Get("PurchaseOrder.PurchasingOrganization") ?? po.PurchasingOrganization;
        po.PurchasingGroup = Get("PurchaseOrder.PurchasingGroup") ?? po.PurchasingGroup;
        po.PaymentTerms = Get("PurchaseOrder.PaymentTerms") ?? po.PaymentTerms;
        po.PoCategory = Get("PurchaseOrder.PoCategory") ?? po.PoCategory;
        po.TotalNetAmount = ParseDecimal(Get("PurchaseOrder.TotalNetAmount")) ?? po.TotalNetAmount;
        po.TotalTaxAmount = ParseDecimal(Get("PurchaseOrder.TotalTaxAmount")) ?? po.TotalTaxAmount;
        po.TotalAmount = ParseDecimal(Get("PurchaseOrder.TotalAmount")) ?? po.TotalAmount;
        po.PoDate = ParseDate(Get("PurchaseOrder.PoDate")) ?? po.PoDate;
        po.DeliveryDate = ParseDate(Get("PurchaseOrder.DeliveryDate")) ?? po.DeliveryDate;
        var sourceLastChanged = Get("PurchaseOrder.SourceLastChangedAt");
        if (!string.IsNullOrWhiteSpace(sourceLastChanged))
        {
            po.SourceLastChangedAtRaw = sourceLastChanged.Trim();
            po.SourceLastChangedAt = ParseDateTime(sourceLastChanged) ?? po.SourceLastChangedAt;
        }
        po.LastSyncedAt = DateTime.UtcNow;
        po.UpdatedAt = DateTime.UtcNow;
        po.SourceSystem = sourceSystem;
        var statusText = Get("PurchaseOrder.PoStatus") ?? Get("PurchaseOrder.Status");
        if (!string.IsNullOrWhiteSpace(statusText) && TryParsePoStatus(statusText, out var poStatus)) po.Status = poStatus;
        po.SourceHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(record.GetRawText())));
        if (created) db.PurchaseOrders.Add(po);
        var itemRecords = record.EnumerateObject().Select(property => property.Value).Where(value => value.ValueKind == JsonValueKind.Array).SelectMany(value => value.EnumerateArray()).ToList();
        var itemMappings = mappings.Concat(ItemImportMappings()).Where(item => item.TargetField.StartsWith("PurchaseOrderItem.", StringComparison.OrdinalIgnoreCase)).ToList();
        foreach (var itemRecord in itemRecords)
        {
            string? Item(string target)
            {
                var mapped = MapValue(itemMappings.FirstOrDefault(mapping => mapping.TargetField.Equals(target, StringComparison.OrdinalIgnoreCase)), itemRecord);
                if (!string.IsNullOrWhiteSpace(mapped)) return mapped;
                var name = target[(target.LastIndexOf('.') + 1)..];
                return itemRecord.ValueKind == JsonValueKind.Object && itemRecord.TryGetProperty(name, out var value) ? value.ToString() : null;
            }
            var lineNumber = ParseInt(Item("PurchaseOrderItem.LineNumber")) ?? po.Items.Count + 1;
            var line = po.Items.SingleOrDefault(candidate => candidate.LineNumber == lineNumber || candidate.ItemNumber == lineNumber.ToString(CultureInfo.InvariantCulture));
            var materialCode = Item("PurchaseOrderItem.MaterialCode");
            var description = Item("PurchaseOrderItem.Description");
            var uom = Item("PurchaseOrderItem.Uom");
            var orderedQuantity = ParseDecimal(Item("PurchaseOrderItem.OrderedQuantity"));
            if (string.IsNullOrWhiteSpace(materialCode) && string.IsNullOrWhiteSpace(description))
                throw new IntegrationException("PO_ITEM_IDENTIFICATION_REQUIRED", "Material or Item Description is required for every PO item.");
            if (string.IsNullOrWhiteSpace(uom) || orderedQuantity is null || orderedQuantity <= 0)
                throw new InvalidOperationException($"PO item {lineNumber} requires a positive quantity and UOM.");
            var itemCurrency = Item("PurchaseOrderItem.Currency")?.Trim().ToUpperInvariant();
            if (!string.IsNullOrWhiteSpace(itemCurrency) && itemCurrency != currency)
                throw new IntegrationException("PO_CURRENCY_MISMATCH", $"PO {poNumber} header currency {currency} does not match item {lineNumber} currency {itemCurrency}.");
            var receivedQuantity = ParseDecimal(Item("PurchaseOrderItem.ReceivedQuantity")) ?? line?.ReceivedQuantity ?? 0;
            var openQuantity = ParseDecimal(Item("PurchaseOrderItem.OpenQuantity")) ?? orderedQuantity.Value - receivedQuantity;
            if (receivedQuantity < 0 || openQuantity < 0)
                throw new InvalidOperationException($"PO item {lineNumber} quantities cannot be negative.");
            line ??= new PurchaseOrderItem { Id = Guid.NewGuid(), PurchaseOrder = po, LineNumber = lineNumber, MaterialCode = materialCode ?? string.Empty, Description = description ?? string.Empty, Uom = uom, OrderedQuantity = orderedQuantity.Value, Status = PurchaseOrderItemStatus.OPEN, CreatedAt = DateTime.UtcNow };
            line.ItemNumber = lineNumber.ToString(CultureInfo.InvariantCulture);
            line.ExternalId = Item("PurchaseOrderItem.ExternalId");
            line.MaterialCode = materialCode ?? line.MaterialCode;
            line.Description = description ?? line.Description;
            line.OrderedQuantity = orderedQuantity ?? line.OrderedQuantity;
            line.ReceivedQuantity = receivedQuantity;
            line.OpenQuantity = openQuantity;
            line.Uom = uom ?? line.Uom;
            line.UnitPrice = ParseDecimal(Item("PurchaseOrderItem.UnitPrice")) ?? line.UnitPrice;
            line.PriceQuantity = ParseDecimal(Item("PurchaseOrderItem.PriceQuantity")) ?? line.PriceQuantity;
            line.ItemAmount = ParseDecimal(Item("PurchaseOrderItem.ItemAmount")) ?? line.ItemAmount;
            line.TaxCode = Item("PurchaseOrderItem.TaxCode") ?? line.TaxCode;
            line.TaxAmount = ParseDecimal(Item("PurchaseOrderItem.TaxAmount")) ?? line.TaxAmount;
            line.GrossItemAmount = ParseDecimal(Item("PurchaseOrderItem.GrossItemAmount")) ?? line.GrossItemAmount;
            line.Currency = itemCurrency ?? currency;
            line.MaterialGroup = Item("PurchaseOrderItem.MaterialGroup") ?? line.MaterialGroup;
            line.Plant = Item("PurchaseOrderItem.Plant") ?? line.Plant;
            line.StorageLocation = Item("PurchaseOrderItem.StorageLocation") ?? line.StorageLocation;
            line.ItemCategory = Item("PurchaseOrderItem.ItemCategory") ?? line.ItemCategory;
            line.AccountAssignmentCategory = Item("PurchaseOrderItem.AccountAssignmentCategory") ?? line.AccountAssignmentCategory;
            line.GoodsReceiptExpected = ParseBool(Item("PurchaseOrderItem.GoodsReceiptExpected")) ?? line.GoodsReceiptExpected;
            line.InvoiceExpected = ParseBool(Item("PurchaseOrderItem.InvoiceExpected")) ?? line.InvoiceExpected;
            line.DeliveryCompleted = ParseBool(Item("PurchaseOrderItem.DeliveryCompleted")) ?? line.DeliveryCompleted;
            line.DeletionIndicator = ParseBool(Item("PurchaseOrderItem.DeletionIndicator")) ?? line.DeletionIndicator;
            line.SourceLastChangedAt = ParseDateTime(Item("PurchaseOrderItem.SourceLastChangedAt"));
            line.LastSyncedAt = DateTime.UtcNow;
            line.UpdatedAt = DateTime.UtcNow;
            line.SourceHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(itemRecord.GetRawText())));
            if (!po.Items.Contains(line)) po.Items.Add(line);
        }
        po.TotalOrderedQuantity = po.Items.Sum(item => item.OrderedQuantity);
        po.TotalReceivedQuantity = po.Items.Sum(item => item.ReceivedQuantity);
        return created;
    }

    private async Task<bool> UpsertSupplierAsync(ApiIntegrationConfiguration config, IReadOnlyList<ApiFieldMapping> mappings, JsonElement record, CancellationToken cancellationToken, string sourceSystem = "API_INTEGRATION")
    {
        string? Get(string target) => MapValue(mappings.FirstOrDefault(item => item.TargetField.Equals(target, StringComparison.OrdinalIgnoreCase)), record);
        var code = Get("Supplier.SupplierCode") ?? throw new InvalidOperationException("Supplier code is missing.");
        var name = Get("Supplier.Name") ?? throw new InvalidOperationException("Supplier name is missing.");
        var externalId = Get("Supplier.ExternalId");
        var supplier = await FindSupplierAsync(config.OrganizationId, config.EntityCode, code, cancellationToken);
        if (supplier is null && externalId is not null)
        {
            supplier = await db.Suppliers.SingleOrDefaultAsync(item =>
                item.OrganizationId == config.OrganizationId && item.EntityCode == config.EntityCode &&
                item.ExternalId == externalId, cancellationToken);
        }
        var created = supplier is null;
        supplier ??= new Supplier
        {
            Id = Guid.NewGuid(),
            OrganizationId = config.OrganizationId,
            EntityCode = config.EntityCode,
            SupplierCode = code,
            Name = name,
            NormalizedName = NormalizeSupplier(name),
            Status = StatusKind.ACTIVE,
            IsActive = true,
            SourceSystem = sourceSystem,
            CreatedAt = DateTime.UtcNow,
        };
        supplier.SupplierCode = code;
        if (!string.IsNullOrWhiteSpace(name) && !InvoiceOcrFieldReader.IsSupplierIdLabel(name))
        {
            supplier.Name = name;
            supplier.NormalizedName = NormalizeSupplier(name);
        }
        else if (created)
        {
            supplier.Name = InvoiceOcrFieldReader.NormalizeSupplierCandidate(name) ?? code;
            supplier.NormalizedName = NormalizeSupplier(supplier.Name);
        }
        supplier.SearchName = Get("Supplier.SearchName") ?? supplier.SearchName;
        supplier.BusinessPartnerId = Get("Supplier.BusinessPartnerId") ?? supplier.BusinessPartnerId;
        supplier.LegalName = Get("Supplier.LegalName") ?? supplier.LegalName;
        supplier.TaxNumber = Get("Supplier.TaxNumber") ?? supplier.TaxNumber;
        supplier.Trn = Get("Supplier.Trn") ?? supplier.Trn;
        supplier.Email = Get("Supplier.Email") ?? supplier.Email;
        supplier.Phone = Get("Supplier.Phone") ?? supplier.Phone;
        supplier.Country = Get("Supplier.Country") ?? supplier.Country;
        supplier.City = Get("Supplier.City") ?? supplier.City;
        supplier.PostalCode = Get("Supplier.PostalCode") ?? supplier.PostalCode;
        supplier.Street = Get("Supplier.Street") ?? supplier.Street;
        supplier.PurchasingOrganization = Get("Supplier.PurchasingOrganization") ?? supplier.PurchasingOrganization;
        supplier.CompanyCode = Get("Supplier.CompanyCode") ?? supplier.CompanyCode;
        supplier.PaymentTerms = Get("Supplier.PaymentTerms") ?? supplier.PaymentTerms;
        supplier.Currency = Get("Supplier.Currency") ?? supplier.Currency;
        supplier.IsBlocked = ParseBool(Get("Supplier.IsBlocked")) ?? supplier.IsBlocked;
        supplier.IsActive = ParseBool(Get("Supplier.IsActive")) ?? supplier.IsActive;
        supplier.ExternalId = externalId ?? supplier.ExternalId;
        supplier.SourceSystem = sourceSystem;
        supplier.LastSyncedAt = DateTime.UtcNow;
        supplier.UpdatedAt = DateTime.UtcNow;
        if (created) db.Suppliers.Add(supplier);
        var normalizedAlias = NormalizeSupplier(name);
        if (!await db.SupplierAliases.AnyAsync(item => item.OrganizationId == config.OrganizationId && item.EntityCode == config.EntityCode && item.NormalizedAlias == normalizedAlias, cancellationToken))
        {
            db.SupplierAliases.Add(new SupplierAlias
            {
                Id = Guid.NewGuid(), OrganizationId = config.OrganizationId, SupplierId = supplier.Id,
                EntityCode = config.EntityCode, Alias = name, NormalizedAlias = normalizedAlias, SourceSystem = sourceSystem, IsConfirmed = true, CreatedAt = DateTime.UtcNow
            });
        }
        return created;
    }

    private static string? MapValue(ApiFieldMapping? mapping, JsonElement source)
    {
        if (mapping is null) return mapping?.DefaultValue;
        var value = ReadPath(source, mapping.SourceField);
        if (value is null || value.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return mapping.NullPolicy == IntegrationNullPolicy.DEFAULT_VALUE ? mapping.DefaultValue : null;
        var text = value.Value.ValueKind == JsonValueKind.String ? value.Value.GetString() : value.Value.ToString();
        return mapping.Transformation switch { "TRIM" => text?.Trim(), "UPPER" => text?.Trim().ToUpperInvariant(), "LOWER" => text?.Trim().ToLowerInvariant(), _ => text };
    }

    private static void ValidateMappedRecord(ApiIntegrationConfiguration config, IReadOnlyList<ApiFieldMapping> mappings, JsonElement record)
    {
        string? Get(string target) => MapValue(mappings.FirstOrDefault(item => item.TargetField.Equals(target, StringComparison.OrdinalIgnoreCase)), record);
        if (config.ProcessType == IntegrationProcessType.GET_SUPPLIER)
        {
            if (string.IsNullOrWhiteSpace(Get("Supplier.SupplierCode")) || string.IsNullOrWhiteSpace(Get("Supplier.Name")))
                throw new InvalidOperationException("Supplier code and name are required.");
        }
        else if (string.IsNullOrWhiteSpace(Get("PurchaseOrder.PoNumber")) ||
                 string.IsNullOrWhiteSpace(Get("PurchaseOrder.SupplierCode")) ||
                 string.IsNullOrWhiteSpace(Get("PurchaseOrder.SupplierName")) ||
                 string.IsNullOrWhiteSpace(Get("PurchaseOrder.Currency")))
        {
            throw new InvalidOperationException("PO number, supplier code, supplier name, and currency are required.");
        }
        else if (!Regex.IsMatch(Get("PurchaseOrder.Currency")!.Trim().ToUpperInvariant(), "^[A-Z]{3}$"))
            throw new InvalidOperationException("Currency must be a three-letter ISO code.");
    }

    private static JsonElement? ReadPath(JsonElement source, string path)
    {
        var current = source;
        foreach (var segment in path.Split('.', '/', StringSplitOptions.RemoveEmptyEntries))
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(segment, out current)) return null;
        }
        return current;
    }

    private static DateOnly? ParseDate(string? value) => DateOnly.TryParse(value, out var result) ? result : null;
    private static DateTime? ParseDateTime(string? value) => DateTime.TryParse(value, null, System.Globalization.DateTimeStyles.AssumeUniversal, out var result) ? result.ToUniversalTime() : null;
    private static int? ParseInt(string? value) => int.TryParse(value, out var result) ? result : null;
    private static decimal? ParseDecimal(string? value) => decimal.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var result) ? result : null;
    private static DateTime? ReadDate(JsonElement record, string? field) => field is null ? null : ParseDateTime(ReadPath(record, field)?.ToString());
    private static DateTime? Max(DateTime? first, DateTime? second) => second is null || first >= second ? first : second;
    private static DateTime? NextRun(string? schedule) => string.IsNullOrWhiteSpace(schedule) ? null : DateTime.UtcNow.AddMinutes(15);
    private static string NormalizeSupplier(string value) => Regex.Replace(value.Trim().ToUpperInvariant(), @"[^A-Z0-9]+", " ").Trim();

    private static IReadOnlyList<string> ImportColumns(IntegrationImportKind kind) => kind == IntegrationImportKind.SUPPLIERS
        ? ["SUPPLIER_CODE", "NAME", "BUSINESS_PARTNER_ID", "LEGAL_NAME", "TAX_NUMBER", "TRN", "COUNTRY", "CITY", "POSTAL_CODE", "STREET", "EMAIL", "PHONE", "PURCHASING_ORGANIZATION", "COMPANY_CODE", "CURRENCY", "PAYMENT_TERMS", "IS_BLOCKED", "IS_ACTIVE", "SOURCE_LAST_CHANGED_AT", "EXTERNAL_ID"]
        : ["EXTERNAL_ID", "PO_NUMBER", "PURCHASE_ORDER_TYPE", "SUPPLIER_CODE", "SUPPLIER_NAME", "COMPANY_CODE", "CURRENCY", "TOTAL_AMOUNT", "PO_STATUS", "PO_DATE", "PURCHASING_ORGANIZATION", "PURCHASING_GROUP", "PAYMENT_TERMS", "PO_CATEGORY", "TOTAL_NET_AMOUNT", "TOTAL_TAX_AMOUNT", "DELIVERY_DATE", "SOURCE_LAST_CHANGED_AT", "LINE_NUMBER", "MATERIAL_CODE", "DESCRIPTION", "ORDERED_QUANTITY", "UOM", "ITEM_AMOUNT", "TAX_CODE", "GOODS_RECEIPT_EXPECTED", "MATERIAL_GROUP", "PLANT", "STORAGE_LOCATION", "ITEM_CATEGORY", "ACCOUNT_ASSIGNMENT_CATEGORY", "RECEIVED_QUANTITY", "OPEN_QUANTITY", "UNIT_PRICE", "PRICE_QUANTITY", "TAX_AMOUNT", "GROSS_ITEM_AMOUNT", "INVOICE_EXPECTED", "DELIVERY_COMPLETED", "DELETION_INDICATOR", "ITEM_STATUS", "ITEM_CURRENCY"];

    private static IReadOnlyList<ApiFieldMapping> ImportMappings(IntegrationImportKind kind)
    {
        var columns = kind == IntegrationImportKind.SUPPLIERS ? ImportColumns(kind) : PurchaseOrderHeaderColumns;
        var mappings = columns.Select(column =>
        {
            var target = kind == IntegrationImportKind.SUPPLIERS ? $"Supplier.{ToPascal(column)}" : $"PurchaseOrder.{ToPascal(column)}";
            if (kind == IntegrationImportKind.PURCHASE_ORDERS && column == "SUPPLIER_CODE") target = "PurchaseOrder.SupplierCode";
            if (kind == IntegrationImportKind.PURCHASE_ORDERS && column == "SUPPLIER_NAME") target = "PurchaseOrder.SupplierName";
            if (kind == IntegrationImportKind.PURCHASE_ORDERS && column == "PO_STATUS") target = "PurchaseOrder.PoStatus";
            return new ApiFieldMapping { Id = Guid.NewGuid(), ConfigurationId = Guid.Empty, SourceField = target, TargetField = target, NullPolicy = IntegrationNullPolicy.IGNORE_NULL, IsValidated = true, UpdatedAt = DateTime.UtcNow };
        }).ToList();
        if (kind == IntegrationImportKind.PURCHASE_ORDERS) mappings.AddRange(ItemImportMappings());
        return mappings;
    }

    private static string ToPascal(string value) =>
        string.Concat(value.Split('_', StringSplitOptions.RemoveEmptyEntries).Select(part => char.ToUpperInvariant(part[0]) + part[1..].ToLowerInvariant()));

    private static List<IntegrationImportRowResponse> NormalizeImportRows(
        IntegrationImportKind kind, IEnumerable<IReadOnlyDictionary<string, string?>> rows)
    {
        var normalized = rows.Select((row, index) =>
        {
            var errors = new List<string>();
            var values = NormalizeImportRow(kind, row, out errors);
            var codes = errors.Select(ErrorCodeOf).Where(code => code is not null).Cast<string>().ToList();
            return new IntegrationImportRowResponse(index + 2, errors.Count == 0, values, errors.Select(ErrorMessageOf).ToList(), codes);
        }).ToList();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < normalized.Count; index++)
        {
            if (!normalized[index].IsValid) continue;
            var key = kind == IntegrationImportKind.SUPPLIERS
                ? normalized[index].Values["SUPPLIER_CODE"] ?? string.Empty
                : DuplicateKey(normalized[index].Values);
            if (seen.Add(key)) continue;
            var errors = normalized[index].Errors.Concat(["Duplicate key in this spreadsheet."]).ToList();
            normalized[index] = normalized[index] with { IsValid = false, Errors = errors, ErrorCodes = (normalized[index].ErrorCodes ?? []).Concat(["DUPLICATE_KEY"]).ToList() };
        }
        return normalized;
    }

    private static Dictionary<string, string?> NormalizeImportRow(
        IntegrationImportKind kind, IReadOnlyDictionary<string, string?> source, out List<string> errors)
    {
        errors = [];
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var column in ImportColumns(kind))
        {
            var match = source.FirstOrDefault(item => CanonicalImportKey(item.Key, kind) == column);
            var value = string.IsNullOrWhiteSpace(match.Key) ? null : match.Value?.Trim();
            if (column is "SUPPLIER_CODE" or "PO_NUMBER" or "PURCHASE_ORDER_TYPE" or "COMPANY_CODE" or "PO_STATUS" or "TAX_CODE" or "UOM" or "MATERIAL_CODE") value = value?.ToUpperInvariant();
            if (column is "CURRENCY" or "ITEM_CURRENCY") value = value?.ToUpperInvariant();
            values[column] = string.IsNullOrWhiteSpace(value) ? null : value;
        }
        if (kind == IntegrationImportKind.SUPPLIERS)
        {
            if (string.IsNullOrWhiteSpace(values["SUPPLIER_CODE"])) AddError(errors, "SUPPLIER_ID_REQUIRED", "Supplier code is required.");
            if (string.IsNullOrWhiteSpace(values["NAME"])) AddError(errors, "SUPPLIER_NAME_REQUIRED", "Supplier name is required.");
            if (!string.IsNullOrWhiteSpace(values["CURRENCY"]) && !Regex.IsMatch(values["CURRENCY"]!, "^[A-Z]{3}$")) AddError(errors, "CURRENCY_INVALID", "Currency must be a three-letter ISO code.");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(values["PO_NUMBER"])) AddError(errors, "PO_NUMBER_REQUIRED", "PO number is required.");
            if (string.IsNullOrWhiteSpace(values["SUPPLIER_CODE"])) AddError(errors, "SUPPLIER_ID_REQUIRED", "Supplier code is required.");
            if (string.IsNullOrWhiteSpace(values["CURRENCY"])) AddError(errors, "CURRENCY_REQUIRED", "Currency is required.");
            else if (!Regex.IsMatch(values["CURRENCY"]!, "^[A-Z]{3}$")) AddError(errors, "CURRENCY_INVALID", "Currency must be a three-letter ISO code.");
            if (string.IsNullOrWhiteSpace(values["COMPANY_CODE"])) AddError(errors, "COMPANY_CODE_REQUIRED", "Company code is required.");
            if (string.IsNullOrWhiteSpace(values["PURCHASE_ORDER_TYPE"])) AddError(errors, "PO_TYPE_REQUIRED", "Purchase order type is required.");
            if (string.IsNullOrWhiteSpace(values["TOTAL_AMOUNT"]) || !decimal.TryParse(values["TOTAL_AMOUNT"], NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                AddError(errors, "TOTAL_AMOUNT_REQUIRED", "TOTAL_AMOUNT is required and must be a valid decimal.");
            foreach (var amountColumn in new[] { "TOTAL_NET_AMOUNT", "TOTAL_TAX_AMOUNT" })
                if (!string.IsNullOrWhiteSpace(values[amountColumn]) && !decimal.TryParse(values[amountColumn], NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                    AddError(errors, "AMOUNT_INVALID", $"{amountColumn} must be a valid decimal.");
            if (!string.IsNullOrWhiteSpace(values["PO_STATUS"]) && !TryParsePoStatus(values["PO_STATUS"]!, out _))
                AddError(errors, "PO_STATUS_INVALID", "PO status must be OPEN, PARTIALLY_RECEIVED, CLOSED, or CANCELLED.");
            if (HasPoItemData(values))
            {
                if (string.IsNullOrWhiteSpace(values["LINE_NUMBER"])) AddError(errors, "PO_ITEM_REQUIRED", "Purchase order item is required.");
                if (string.IsNullOrWhiteSpace(values["MATERIAL_CODE"]) && string.IsNullOrWhiteSpace(values["DESCRIPTION"]))
                    AddError(errors, "PO_ITEM_IDENTIFICATION_REQUIRED", "Material or Item Description is required for every PO item.");
                if (string.IsNullOrWhiteSpace(values["ORDERED_QUANTITY"]) || !decimal.TryParse(values["ORDERED_QUANTITY"], NumberStyles.Any, CultureInfo.InvariantCulture, out var ordered) || ordered <= 0)
                    AddError(errors, "ORDER_QUANTITY_REQUIRED", "Order quantity must be greater than zero.");
                if (string.IsNullOrWhiteSpace(values["UOM"])) AddError(errors, "UOM_REQUIRED", "UOM is required.");
                if (string.IsNullOrWhiteSpace(values["ITEM_AMOUNT"]) || !decimal.TryParse(values["ITEM_AMOUNT"], NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                    AddError(errors, "ITEM_AMOUNT_REQUIRED", "Item amount is required.");
                if (string.IsNullOrWhiteSpace(values["TAX_CODE"])) AddError(errors, "TAX_CODE_REQUIRED", "Tax code is required.");
                if (string.IsNullOrWhiteSpace(values["GOODS_RECEIPT_EXPECTED"])) values["GOODS_RECEIPT_EXPECTED"] = "TRUE";
                else if (ParseBool(values["GOODS_RECEIPT_EXPECTED"]) is null) AddError(errors, "GR_EXPECTED_REQUIRED", "GoodsReceiptExpected must be TRUE or FALSE.");
                if (!string.IsNullOrWhiteSpace(values["ITEM_CURRENCY"]) && values["ITEM_CURRENCY"] != values["CURRENCY"])
                    AddError(errors, "PO_CURRENCY_MISMATCH", "Header and item currency must agree.");
                if (!string.IsNullOrWhiteSpace(values["RECEIVED_QUANTITY"]) && (!decimal.TryParse(values["RECEIVED_QUANTITY"], NumberStyles.Any, CultureInfo.InvariantCulture, out var received) || received < 0))
                    AddError(errors, "RECEIVED_QUANTITY_INVALID", "Received quantity cannot be negative.");
                if (!string.IsNullOrWhiteSpace(values["OPEN_QUANTITY"]) && (!decimal.TryParse(values["OPEN_QUANTITY"], NumberStyles.Any, CultureInfo.InvariantCulture, out var open) || open < 0))
                    AddError(errors, "OPEN_QUANTITY_INVALID", "Open quantity cannot be negative.");
            }
        }
        foreach (var dateColumn in new[] { "PO_DATE", "DELIVERY_DATE" })
        {
            if (!values.TryGetValue(dateColumn, out var date) || string.IsNullOrWhiteSpace(date)) continue;
            if (!DateOnly.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                errors.Add($"{dateColumn} is not a valid date.");
        }
        if (kind == IntegrationImportKind.SUPPLIERS &&
            values.TryGetValue("SOURCE_LAST_CHANGED_AT", out var sourceLastChanged) &&
            !string.IsNullOrWhiteSpace(sourceLastChanged) &&
            !DateTime.TryParse(sourceLastChanged, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out _))
            errors.Add("SOURCE_LAST_CHANGED_AT is not a valid date.");
        return values;
    }

    private static string NormalizeHeader(string value) => Regex.Replace(value.Trim().ToUpperInvariant(), @"[^A-Z0-9]", string.Empty);

    private static JsonElement ToCanonicalRecord(IntegrationImportKind kind, IReadOnlyDictionary<string, string?> values)
    {
        var target = kind == IntegrationImportKind.SUPPLIERS ? "Supplier" : "PurchaseOrder";
        var fields = new Dictionary<string, string?>();
        foreach (var column in ImportColumns(kind))
        {
            var property = ToPascal(column);
            if (kind == IntegrationImportKind.PURCHASE_ORDERS && column == "SUPPLIER_CODE") property = "SupplierCode";
            if (kind == IntegrationImportKind.PURCHASE_ORDERS && column == "SUPPLIER_NAME") property = "SupplierName";
            fields[property] = values.TryGetValue(column, out var value) ? value : null;
        }
        return JsonDocument.Parse(JsonSerializer.Serialize(new Dictionary<string, object?> { [target] = fields })).RootElement.Clone();
    }

    private static async Task<List<Dictionary<string, string?>>> ReadSpreadsheetAsync(
        Stream input, string fileName, IntegrationImportKind kind, CancellationToken cancellationToken)
    {
        await using var memory = new MemoryStream();
        await input.CopyToAsync(memory, cancellationToken);
        if (memory.Length > 10 * 1024 * 1024) throw new IntegrationException("IMPORT_TOO_LARGE", "The spreadsheet must be smaller than 10 MB.");
        var bytes = memory.ToArray();
        return fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)
            ? ParseCsv(Encoding.UTF8.GetString(bytes))
            : ParseXlsx(bytes, kind);
    }

    private static List<Dictionary<string, string?>> ParseXlsx(byte[] bytes, IntegrationImportKind kind)
    {
        try
        {
            using var archive = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);
            var sheets = ReadWorkbookSheets(archive);
            if (sheets.Count == 0) return [];
            if (kind == IntegrationImportKind.SUPPLIERS)
            {
                var sheet = sheets.FirstOrDefault(item => item.Name.Equals("SUPPLIERS", StringComparison.OrdinalIgnoreCase)) ?? sheets[0];
                return RowsToDictionaries(sheet.Rows);
            }

            var headers = sheets.FirstOrDefault(item => item.Name.Equals("PO_HEADERS", StringComparison.OrdinalIgnoreCase));
            var items = sheets.FirstOrDefault(item => item.Name.Equals("PO_ITEMS", StringComparison.OrdinalIgnoreCase));
            if (headers is null) return RowsToDictionaries(sheets[0].Rows);
            var headerRows = RowsToDictionaries(headers.Rows);
            if (items is null) return headerRows;
            var itemRows = RowsToDictionaries(items.Rows);
            var headerByPo = headerRows
                .Where(row => row.TryGetValue("PurchaseOrder", out _) || row.Keys.Any(key => CanonicalImportKey(key, kind) == "PO_NUMBER"))
                .GroupBy(row => CanonicalImportKey("PO_NUMBER", kind) == "PO_NUMBER"
                    ? row.FirstOrDefault(item => CanonicalImportKey(item.Key, kind) == "PO_NUMBER").Value ?? string.Empty
                    : string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
            if (headerByPo.Count == 0)
            {
                headerByPo = headerRows.GroupBy(row =>
                {
                    var pair = row.FirstOrDefault(item => CanonicalImportKey(item.Key, kind) == "PO_NUMBER");
                    return pair.Value ?? string.Empty;
                }, StringComparer.OrdinalIgnoreCase).ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
            }

            var merged = new List<Dictionary<string, string?>>();
            foreach (var item in itemRows)
            {
                var po = item.FirstOrDefault(entry => CanonicalImportKey(entry.Key, kind) == "PO_NUMBER").Value ?? string.Empty;
                var row = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                if (headerByPo.TryGetValue(po, out var header))
                    foreach (var pair in header) row[pair.Key] = pair.Value;
                foreach (var pair in item)
                {
                    if (string.IsNullOrWhiteSpace(pair.Value) &&
                        row.TryGetValue(pair.Key, out var existing) &&
                        !string.IsNullOrWhiteSpace(existing))
                        continue;
                    row[pair.Key] = pair.Value;
                }
                merged.Add(row);
            }
            return merged.Count > 0 ? merged : headerRows;
        }
        catch (Exception exception) when (exception is InvalidDataException or InvalidOperationException or XmlException or FormatException or KeyNotFoundException)
        {
            throw new IntegrationException("IMPORT_FORMAT_INVALID", "The upload is not a valid .xlsx spreadsheet.");
        }
    }

    private static List<Dictionary<string, string?>> ParseCsv(string value)
    {
        var records = new List<List<string>>();
        var current = new List<string>();
        var cell = new StringBuilder();
        var quoted = false;
        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            if (character == '"' && quoted && index + 1 < value.Length && value[index + 1] == '"') { cell.Append('"'); index++; continue; }
            if (character == '"') { quoted = !quoted; continue; }
            if (character == ',' && !quoted) { current.Add(cell.ToString()); cell.Clear(); continue; }
            if ((character == '\n' || character == '\r') && !quoted)
            {
                if (character == '\r' && index + 1 < value.Length && value[index + 1] == '\n') index++;
                current.Add(cell.ToString()); cell.Clear();
                if (current.Any(item => !string.IsNullOrWhiteSpace(item))) records.Add(current);
                current = [];
                continue;
            }
            cell.Append(character);
        }
        if (cell.Length > 0 || current.Count > 0) { current.Add(cell.ToString()); records.Add(current); }
        if (records.Count == 0) return [];
        return records.Skip(1).Where(row => row.Any(item => !string.IsNullOrWhiteSpace(item))).Select(row =>
        {
            var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < records[0].Count; index++) result[records[0][index]] = index < row.Count ? row[index] : null;
            return result;
        }).ToList();
    }

    private static byte[] BuildSpreadsheet(IReadOnlyList<string> columns, IReadOnlyList<Dictionary<string, string?>> rows)
    {
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            AddZipEntry(archive, "[Content_Types].xml", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/></Types>
                """);
            AddZipEntry(archive, "_rels/.rels", """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>""");
            AddZipEntry(archive, "xl/workbook.xml", """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="Data" sheetId="1" r:id="rId1"/></sheets></workbook>""");
            AddZipEntry(archive, "xl/_rels/workbook.xml.rels", """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/></Relationships>""");
            var sheet = new StringBuilder("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?><worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><sheetData>""");
            AppendSpreadsheetRow(sheet, columns.Select(item => (string?)item).ToList(), 1);
            for (var index = 0; index < rows.Count; index++) AppendSpreadsheetRow(sheet, columns.Select(column => rows[index].TryGetValue(column, out var value) ? value : null).ToList(), index + 2);
            sheet.Append("</sheetData></worksheet>");
            AddZipEntry(archive, "xl/worksheets/sheet1.xml", sheet.ToString());
        }
        return output.ToArray();
    }

    private static void AppendSpreadsheetRow(StringBuilder sheet, IReadOnlyList<string?> values, int rowNumber)
    {
        sheet.Append($"<row r=\"{rowNumber}\">");
        for (var index = 0; index < values.Count; index++)
        {
            var value = SecurityElement.Escape(values[index] ?? string.Empty);
            sheet.Append($"<c r=\"{ColumnName(index + 1)}{rowNumber}\" t=\"inlineStr\"><is><t>{value}</t></is></c>");
        }
        sheet.Append("</row>");
    }

    private static void AppendCsvRow(StringBuilder csv, IEnumerable<string?> values)
    {
        csv.AppendLine(string.Join(',', values.Select(value =>
        {
            var normalized = value ?? string.Empty;
            return $"\"{normalized.Replace("\"", "\"\"")}\"";
        })));
    }

    private static string ColumnName(int number)
    {
        var result = string.Empty;
        while (number > 0) { var remainder = (number - 1) % 26; result = (char)('A' + remainder) + result; number = (number - 1) / 26; }
        return result;
    }

    private static void AddZipEntry(ZipArchive archive, string name, string content)
    {
        using var writer = new StreamWriter(archive.CreateEntry(name, CompressionLevel.Fastest).Open(), new UTF8Encoding(false));
        writer.Write(content);
    }

    private static IntegrationConfigurationResponse ToResponse(ApiIntegrationConfiguration item, ProtectedIntegrationCredentialStore credentials)
    {
        IntegrationDesignerDocument? designer = null;
        if (!string.IsNullOrWhiteSpace(item.DesignerJson))
        {
            designer = JsonSerializer.Deserialize<IntegrationDesignerDocument>(item.DesignerJson, JsonOptions);
            if (designer?.Headers is not null)
                designer = designer with { Headers = designer.Headers.Select(header => new IntegrationHeaderInput { Key = header.Key, IsSecret = header.IsSecret, Configured = header.IsSecret || header.Configured, Value = header.IsSecret ? null : header.Value }).ToList() };
        }
        return new(item.Id, item.OrganizationId, item.OrganizationUnitId, item.EntityCode, item.Name, item.ProcessType, item.Protocol, item.BaseUrl, item.ResourcePath, item.AuthenticationType, item.Username, credentials.State(item), item.TimeoutSeconds, item.RetryCount, item.PageSize, item.WatermarkField, item.LastWatermark, item.LastAttemptAt, item.LastSuccessfulRunAt, item.NextRunAt, item.IsRunning, item.LastErrorSafe, item.ScheduleCron, item.Status, item.TestedAt, item.CreatedAt, item.UpdatedAt,
            item.SystemKind, item.Description, item.HttpMethod, item.ServicePath, item.EntitySet, item.Priority, item.CompanyCode, item.Plant, item.PropertyCode, item.EnvironmentCode, item.ValidationFingerprint, item.ValidatedAt, item.ValidatedBy, item.ValidationStatus, item.LastSuccessfulTestAt, designer, item.ConnectionStatus);
    }

    private static IntegrationMappingResponse ToMapping(ApiFieldMapping item) => new(item.Id, item.ConfigurationId, item.SourceField, item.TargetField, item.Transformation, item.NullPolicy, item.DefaultValue, item.IsValidated, item.UpdatedAt, item.SourceKind, item.SourceStructure, item.IsCollection);
    private static IntegrationExecutionResponse ToExecution(ApiIntegrationExecution item) => new(item.Id, item.ConfigurationId, item.Trigger, item.Status, item.StartedAt, item.CompletedAt, item.RecordsRead, item.RecordsCreated, item.RecordsUpdated, item.RecordsFailed, item.WatermarkBefore, item.WatermarkAfter, item.ErrorCode, item.ErrorMessageSafe);
}

public sealed class IntegrationSchedulerWorker(TenantJobRunner jobs, ILogger<IntegrationSchedulerWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await jobs.ForEachActiveEnvironmentAsync(async (services, context, token) =>
                {
                    var db = services.GetRequiredService<SilaMeDbContext>();
                    var due = await db.ApiIntegrationConfigurations.AsNoTracking()
                        .Where(item => item.Status == IntegrationConfigurationStatus.ACTIVE &&
                                       (item.ProcessType == IntegrationProcessType.GET_PO || item.ProcessType == IntegrationProcessType.GET_SUPPLIER) &&
                                       item.ScheduleCron != null && item.NextRunAt <= DateTime.UtcNow && !item.IsRunning)
                        .Select(item => new { item.OrganizationId, item.Id }).Take(10).ToListAsync(token);
                    foreach (var item in due)
                    {
                        try { await services.GetRequiredService<IntegrationService>().RunAsync(item.OrganizationId, item.Id, IntegrationExecutionTrigger.SCHEDULED, false, token); }
                        catch (Exception exception) { logger.LogWarning("Scheduled integration {IntegrationId} failed Tenant={TenantId}: {Message}", item.Id, context.TenantId, exception.Message); }
                    }
                }, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception exception) { logger.LogWarning("Integration scheduler cycle failed: {Message}", exception.Message); }
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}