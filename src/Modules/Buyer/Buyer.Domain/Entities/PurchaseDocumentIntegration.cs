using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class PurchaseDocumentIntegration : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        public Guid WishlistId { get; set; }

        [Required]
        public Guid BuyerOrganizationId { get; set; }

        public Guid SupplierOrganizationId { get; set; }

        [Required]
        public string IntegrationType { get; set; } = string.Empty;

        [Required]
        public string IdempotencyKey { get; set; } = string.Empty;

        public string? DocumentType { get; set; }

        public string? ExternalDocumentNumber { get; set; }

        [Required]
        public string Status { get; set; } = string.Empty;

        public Guid? ConfigurationId { get; set; }

        public int ConfigurationVersion { get; set; }

        public string? ResolvedBaseUrl { get; set; }

        public string? ResolvedPath { get; set; }

        public string? ResolvedHttpMethod { get; set; }

        public string? ResolvedErpType { get; set; }

        public string? ErrorMessage { get; set; }

        public int RetryCount { get; set; }

        public DateTime? LastAttemptOn { get; set; }

        public DateTime? NextAttemptOn { get; set; }

        public string? CorrelationId { get; set; }

        public string? ResponseBody { get; set; }

        public bool OutcomeUnknown { get; set; }
    }
}
