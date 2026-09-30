using System.ComponentModel.DataAnnotations;
using SharedKernel.Models;

namespace Supplier.Domain.Entities
{
    public class SupplierErpIntegrationConfiguration : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        public Guid SupplierOrganizationId { get; set; }

        [Required]
        public string ErpType { get; set; } = string.Empty;

        public string PayloadFormat { get; set; } = "JSON";

        [Required]
        public string BaseUrl { get; set; } = string.Empty;

        public string? AuthPath { get; set; }

        [Required]
        public string OrderPath { get; set; } = string.Empty;

        public string HttpMethod { get; set; } = "POST";

        [Required]
        public string AuthType { get; set; } = string.Empty;

        public string? TokenUrl { get; set; }

        public string? Username { get; set; }

        public string? Password { get; set; }

        public string? ClientId { get; set; }

        public string? ClientSecret { get; set; }

        public string? Scope { get; set; }

        public string? ApiKeyHeader { get; set; }

        public string? ApiKey { get; set; }

        public string? AccessToken { get; set; }

        public string? DefaultShipTo { get; set; }

        public string OrderDateFormat { get; set; } = "dd/MM/yyyy";

        public string? HeadersJson { get; set; }

        public int TimeoutSeconds { get; set; } = 60;

        public int MaxRetryCount { get; set; } = 3;

        public int Version { get; set; } = 1;
    }
}
