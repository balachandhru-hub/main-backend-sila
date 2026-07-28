using SharedKernel.Models;
using System.ComponentModel.DataAnnotations;

namespace Supplier.Domain.Entities

{
    public class SupplierRFQQuestionAnswer : BaseModel
{
    [Key]
    [Required]
    public Guid Id { get; set; }

    public Guid SupplierRFQId { get; set; }

    public Guid RFQQuestionId { get; set; }

    public Guid BuyerRFQId { get; set; }

    public string RFQNumber { get; set; }

    public Guid SupplierId { get; set; }

    public string? Answer { get; set; }

    public Guid? AssetId { get; set; }

    public Guid? QuestionOptionId { get; set; }

    public DateTime? AnsweredOn { get; set; }

    public SupplierRFQQuestionAnswer() { }
}
}