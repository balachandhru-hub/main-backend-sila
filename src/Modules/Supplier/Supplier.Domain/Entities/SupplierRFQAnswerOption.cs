using SharedKernel.Models;
using System.ComponentModel.DataAnnotations;
namespace Supplier.Domain.Entities
{
    public class SupplierRFQAnswerOption : BaseModel
{
    [Key]
    [Required]
    public Guid Id { get; set; }

    public Guid SupplierRFQQuestionAnswerId { get; set; }

    public Guid RFQQuestionOptionId { get; set; }

    public SupplierRFQAnswerOption() { }
}
}