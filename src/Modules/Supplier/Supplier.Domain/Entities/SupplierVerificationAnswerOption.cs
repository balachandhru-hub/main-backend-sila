
using System.ComponentModel.DataAnnotations;
using SharedKernel.Models;

namespace Supplier.Domain.Entities
{
    public class SupplierVerificationAnswerOption : BaseModel
{
    [Key]
    public Guid Id { get; set; }

    public Guid SupplierVerificationAnswerId { get; set; }

    public Guid VerificationTemplateQuestionOptionId { get; set; }
      public SupplierVerificationAnswerOption()
        {
        }
    }
}
