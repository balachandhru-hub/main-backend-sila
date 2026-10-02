using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Operations.Domain.Enums;
using SharedKernel.Models;

namespace Operations.Domain.Entities
{
    /// <summary>
    /// REST / OData endpoint of an ERP, per organization, entity and process.
    /// </summary>
    public class ApiIntegrationConfiguration : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        public Guid OrganizationId { get; set; }

        public Guid? OrganizationUnitId { get; set; }

        [Required]
        [MaxLength(100)]
        public string EntityCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Column(TypeName = "nvarchar(30)")]
        public IntegrationProcessType ProcessType { get; set; }

        [Column(TypeName = "nvarchar(20)")]
        public IntegrationProtocol Protocol { get; set; }

        /// <summary>The external system the API belongs to, for example "SAP S/4".</summary>
        [MaxLength(100)]
        public string? SystemName { get; set; }

        [Required]
        [MaxLength(2000)]
        public string BaseUrl { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? ResourcePath { get; set; }

        [Column(TypeName = "nvarchar(40)")]
        public IntegrationAuthenticationType AuthenticationType { get; set; }

        [MaxLength(250)]
        public string? Username { get; set; }

        public string? ProtectedPassword { get; set; }

        public string? ProtectedClientId { get; set; }

        public string? ProtectedClientSecret { get; set; }

        public string? ProtectedBearerToken { get; set; }

        [MaxLength(2000)]
        public string? TokenEndpoint { get; set; }

        [MaxLength(500)]
        public string? TokenScope { get; set; }

        public string? TokenHeadersJson { get; set; }

        public string? TokenBodyJson { get; set; }

        /// <summary>Header that carries the API key when the API signs in with one.</summary>
        [MaxLength(100)]
        public string? ApiKeyHeader { get; set; }

        public string? ProtectedApiKey { get; set; }

        /// <summary>Method of the call: GET for an API that is read, POST / PUT / PATCH for one the application sends to.</summary>
        [MaxLength(10)]
        public string HttpMethod { get; set; } = "GET";

        /// <summary>Format of the body the application sends: JSON, SOAP or CXML.</summary>
        [MaxLength(10)]
        public string PayloadFormat { get; set; } = "JSON";

        /// <summary>
        /// Body template of an API the application sends to. Its {{tokens}} are replaced with the
        /// document being sent. Empty means the application's standard JSON body.
        /// </summary>
        public string? RequestBody { get; set; }

        /// <summary>Extra request headers, as a JSON object. Never credentials.</summary>
        public string? HeadersJson { get; set; }

        public int TimeoutSeconds { get; set; } = 30;

        public int RetryCount { get; set; } = 2;

        public int? PageSize { get; set; } = 100;

        [MaxLength(250)]
        public string? WatermarkField { get; set; }

        public DateTime? LastWatermark { get; set; }

        public DateTime? LastAttemptAt { get; set; }

        public DateTime? LastSuccessfulRunAt { get; set; }

        public DateTime? NextRunAt { get; set; }

        public bool IsRunning { get; set; }

        public DateTime? RunningSince { get; set; }

        [MaxLength(2000)]
        public string? LastErrorSafe { get; set; }

        [MaxLength(100)]
        public string? ScheduleCron { get; set; }

        [Column(TypeName = "nvarchar(20)")]
        public IntegrationConfigurationStatus Status { get; set; } = IntegrationConfigurationStatus.DRAFT;

        public DateTime? TestedAt { get; set; }
    }
}
