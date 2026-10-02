using System.ComponentModel.DataAnnotations;
using SilaMe.Api.Models;

namespace SilaMe.Api.DTOs;

public sealed record IntegrationCatalogOption(string Value, string Label);
public sealed record IntegrationSourceField(string Name, string Label, bool Derived);
public sealed record IntegrationSourceStructure(string Code, string Label, bool Collection, IReadOnlyList<IntegrationSourceField> Fields);
public sealed record IntegrationSourceRecommendation(IReadOnlyList<string> Primary, IReadOnlyList<string> Recommended, IReadOnlyList<string> Optional);
public sealed record IntegrationDesignerCatalogResponse(
    IReadOnlyList<IntegrationCatalogOption> Systems,
    IReadOnlyDictionary<string, List<string>> InterfacesBySystem,
    IReadOnlyList<IntegrationCatalogOption> ApiTypes,
    IReadOnlyList<IntegrationSourceStructure> SourceStructures,
    IReadOnlyDictionary<string, IntegrationSourceRecommendation> SourceRecommendations,
    IReadOnlyDictionary<string, List<string>> AuthenticationMatrix);

public sealed class IntegrationHeaderInput
{
    [Required] public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public bool IsSecret { get; set; }
    public bool Configured { get; set; }
}

public sealed class IntegrationResponseMappingInput
{
    [Required] public string SourcePath { get; set; } = string.Empty;
    [Required] public string TargetField { get; set; } = string.Empty;
}

public sealed class DesignerMappingInput
{
    public IntegrationMappingSourceKind SourceKind { get; set; } = IntegrationMappingSourceKind.FIELD;
    public string? SourceStructure { get; set; }
    public string SourceField { get; set; } = string.Empty;
    public string TargetField { get; set; } = string.Empty;
    public bool IsCollection { get; set; }
    public string? DefaultValue { get; set; }
    public IntegrationNullPolicy NullPolicy { get; set; } = IntegrationNullPolicy.IGNORE_NULL;
}

public sealed record IntegrationDesignerDocument
{
    public string? ContentType { get; init; }
    public string? Accept { get; init; }
    public int? RetryDelaySeconds { get; init; }
    public bool IdempotencyEnabled { get; init; }
    public bool CsrfRequired { get; init; }
    public string? CsrfFetchMethod { get; init; }
    public string? CsrfFetchPath { get; init; }
    public string? CsrfHeaderName { get; init; }
    public string? CsrfHeaderValue { get; init; }
    public string? CsrfResponseHeader { get; init; }
    public string? CookieHandling { get; init; }
    public string? WsdlUrl { get; init; }
    public string? SoapAction { get; init; }
    public string? SoapOperation { get; init; }
    public string? SoapVersion { get; init; }
    public string? OpenApiUrl { get; init; }
    public string? SamplePayload { get; init; }
    public string? RequestTemplate { get; init; }
    public IntegrationRequestBodyMode RequestBodyMode { get; init; } = IntegrationRequestBodyMode.FIELD_MAPPING;
    public IntegrationExecutionMode ExecutionMode { get; init; } = IntegrationExecutionMode.MANUAL;
    public string? TokenHttpMethod { get; init; }
    public string? TokenAuthenticationType { get; init; }
    public string? TokenResponsePath { get; init; }
    public string? TokenType { get; init; }
    public string? TokenExpiryPath { get; init; }
    public string? ApiKeyHeader { get; init; }
    public string? ApiKeyPlacement { get; init; }
    public string? CustomHeaderName { get; init; }
    public string? AuthorizationHeaderName { get; init; }
    public string? SafeTestPath { get; init; }
    public IReadOnlyList<IntegrationHeaderInput>? Headers { get; init; }
    public IReadOnlyDictionary<string, string>? QueryParameters { get; init; }
    public IReadOnlyDictionary<string, string>? PathParameters { get; init; }
    public IReadOnlyList<string>? SelectedSourceStructures { get; init; }
    public IReadOnlyList<int>? SuccessHttpCodes { get; init; }
    public string? ExternalDocumentNumberPath { get; init; }
    public string? ExternalDocumentYearPath { get; init; }
    public string? SoapPartition { get; init; }
    public string? SoapVariant { get; init; }
    public string? StatusPath { get; init; }
    public string? MessagePath { get; init; }
    public string? ErrorCodePath { get; init; }
    public string? ErrorMessagePath { get; init; }
    public IReadOnlyList<IntegrationResponseMappingInput>? ResponseMappings { get; init; }
}

public sealed class IntegrationDesignerDraft
{
    [Required] public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public IntegrationSystemKind SystemKind { get; set; } = IntegrationSystemKind.CUSTOM;
    public IntegrationProcessType ProcessType { get; set; } = IntegrationProcessType.POST_GRN;
    public IntegrationProtocol Protocol { get; set; } = IntegrationProtocol.REST;
    [Required] public string BaseUrl { get; set; } = string.Empty;
    public string? ResourcePath { get; set; }
    public string? ServicePath { get; set; }
    public string? EntitySet { get; set; }
    public string HttpMethod { get; set; } = "GET";
    public IntegrationAuthenticationType AuthenticationType { get; set; } = IntegrationAuthenticationType.NONE;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? BearerToken { get; set; }
    public string? ApiKey { get; set; }
    public string? TokenEndpoint { get; set; }
    public string? TokenScope { get; set; }
    public Dictionary<string, string>? TokenHeaders { get; set; }
    public Dictionary<string, string>? TokenBody { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
    public int RetryCount { get; set; } = 2;
    public string? ScheduleCron { get; set; }
    public Guid? OrganizationUnitId { get; set; }
    public string EntityCode { get; set; } = "ALL";
    public int Priority { get; set; } = 100;
    public string? CompanyCode { get; set; }
    public string? Plant { get; set; }
    public string? PropertyCode { get; set; }
    public string? ValidationFingerprint { get; set; }
    public Guid? TestId { get; set; }
    public IntegrationDesignerDocument Designer { get; set; } = new();
    public List<DesignerMappingInput> Mappings { get; set; } = [];
    public List<IntegrationResponseMappingInput> ResponseMappings { get; set; } = [];
}

public sealed record IntegrationDesignerTestResponse(
    bool Success,
    string Message,
    string TestType,
    int? HttpStatus,
    DateTime TestedAt,
    string? Fingerprint,
    bool AuthenticationSuccessful,
    bool TokenAcquired,
    bool CsrfAcquired,
    bool SessionEstablished,
    bool SchemaLoaded,
    string? SchemaSource,
    DateTime? SchemaLoadedAt,
    IReadOnlyList<IntegrationSchemaEntity>? Entities);

public sealed record IntegrationRequestPreviewResponse(
    string Url,
    string Method,
    IReadOnlyDictionary<string, string> Headers,
    string? Body,
    bool PreviewOnly,
    string Notice);

public sealed record IntegrationDesignerSaveResponse(IntegrationConfigurationResponse Configuration, string Fingerprint);
