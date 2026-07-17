using System.ComponentModel.DataAnnotations;
using SharedKernel.Models;
namespace Buyer.Domain.Entities
{
    public class SupplierVerificationRequest : BaseModel
    {
        [Key]
        public Guid Id { get; set; }

        public Guid RFQId { get; set; }

        public Guid BuyerOrganizationId { get; set; }

        public Guid SupplierOrganizationId { get; set; }

        public Guid RFQVerificationTemplateId { get; set; }

        public string Status { get; set; }

        public string? Remarks { get; set; }

        public DateTime? DueDate { get; set; }

        public Guid? VerifiedBy { get; set; }

        public DateTime? VerifiedOn { get; set; }
        public SupplierVerificationRequest(){}
    }
}
