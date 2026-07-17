
using System.ComponentModel.DataAnnotations;
using SharedKernel.Models;
namespace Buyer.Domain.Entities
{
public class VerificationAnswerOption : BaseModel
{
    [Key]
    public Guid Id { get; set; }

    public Guid VerificationAnswerId { get; set; }

    public Guid VerificationTemplateQuestionOptionId { get; set; }
    public VerificationAnswerOption
    (){}
}
}