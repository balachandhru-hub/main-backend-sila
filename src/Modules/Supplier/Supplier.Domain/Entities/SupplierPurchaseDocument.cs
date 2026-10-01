using System.ComponentModel.DataAnnotations;
using SharedKernel.Models;

namespace Supplier.Domain.Entities
{
    public class SupplierPurchaseDocument : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        public Guid WishlistId { get; set; }

        [Required]
        public Guid BuyerOrganizationId { get; set; }

        [Required]
        public Guid SupplierOrganizationId { get; set; }

        [Required]
        public string IdempotencyKey { get; set; } = string.Empty;

        public string? BuyerDocumentType { get; set; }

        public string? BuyerDocumentNumber { get; set; }

        public string? SupplierDocumentType { get; set; }

        public string? SupplierDocumentNumber { get; set; }

        [Required]
        public string Status { get; set; } = string.Empty;

        public Guid? ConfigurationId { get; set; }

        public int ConfigurationVersion { get; set; }

        public string? ResolvedBaseUrl { get; set; }

        public string? ResolvedOrderPath { get; set; }

        public string? ErrorMessage { get; set; }

        public int RetryCount { get; set; }

        public DateTime? LastAttemptOn { get; set; }

        public string? CorrelationId { get; set; }

        public string? ResponseBody { get; set; }

        public bool OutcomeUnknown { get; set; }
    }
}
