using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    /// <summary>
    /// Per buyer-organization ERP endpoint configuration. URLs and credentials live here, not in source code.
    /// </summary>
    public class ErpIntegrationConfiguration : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        public Guid BuyerOrganizationId { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        [Required]
        public string ErpType { get; set; } = string.Empty;

        /// <summary>
        /// Null for the buyer ERP (Ariba, S/4, or another system).
        /// Set when this row is the destination for one supplier, because each supplier has its own URL.
        /// </summary>
        public Guid? SupplierOrganizationId { get; set; }

        /// <summary>
        /// JSON or CXML. Ariba and S/4 use CXML. Other systems use JSON unless this is set.
        /// </summary>
        public string PayloadFormat { get; set; } = "JSON";

        [Required]
        public string DocumentType { get; set; } = string.Empty;

        [Required]
        public string BaseUrl { get; set; } = string.Empty;

        [Required]
        public string CreateDocumentPath { get; set; } = string.Empty;

        [Required]
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

        public string? HeadersJson { get; set; }

        public int TimeoutSeconds { get; set; } = 60;

        public int MaxRetryCount { get; set; } = 3;

        public int Version { get; set; } = 1;
    }
}
