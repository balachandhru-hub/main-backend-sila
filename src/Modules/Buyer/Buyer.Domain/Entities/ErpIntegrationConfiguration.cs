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

        /// <summary>
        /// What this API is for, for example "Create purchase order".
        /// </summary>
        [Required]
        public string ApiName { get; set; } = string.Empty;

        /// <summary>
        /// Operation this API performs, for example PO_CREATE.
        /// One system is stored for this operation. PO_CREATE is either S/4, Ariba, or another ERP name.
        /// </summary>
        [Required]
        public string Process { get; set; } = string.Empty;

        /// <summary>
        /// Target system name, for example SAP_S4, ARIBA, or another ERP. The name does not choose the payload.
        /// </summary>
        [Required]
        public string ErpType { get; set; } = string.Empty;

        /// <summary>
        /// Null for the buyer ERP (Ariba, S/4, or another system).
        /// Set when this row is the destination for one supplier, because each supplier has its own URL.
        /// </summary>
        public Guid? SupplierOrganizationId { get; set; }

        /// <summary>
        /// JSON for a REST API, SOAP, or CXML. Chosen on the configuration, not from the system name.
        /// </summary>
        public string PayloadFormat { get; set; } = "JSON";

        /// <summary>
        /// Body configured for this API. Ariba and S/4 usually store cXML. Other systems store JSON.
        /// </summary>
        public string? RequestBody { get; set; }

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
