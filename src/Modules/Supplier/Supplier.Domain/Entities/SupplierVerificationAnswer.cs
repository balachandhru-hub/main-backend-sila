using System.ComponentModel.DataAnnotations;
using SharedKernel.Models;

namespace Supplier.Domain.Entities
{
    public class SupplierVerificationAnswer : BaseModel
    {
        [Key]
        public Guid Id { get; set; }

        public Guid SupplierVerificationRequestId { get; set; }

        public Guid SupplierId { get; set; }

        public Guid VerificationTemplateQuestionId { get; set; }

        public Guid TemplateId { get; set; }

        public string? Answer { get; set; }

        public Guid? AssetId { get; set; }

        // Selected option for Dropdown/Radio
        public Guid? VerificationTemplateQuestionOptionId { get; set; }

        public DateTime? AnsweredOn { get; set; }

        public SupplierVerificationAnswer()
        {
        }
    }
}