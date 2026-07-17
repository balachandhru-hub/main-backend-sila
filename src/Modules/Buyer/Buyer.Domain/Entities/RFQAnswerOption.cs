using System.ComponentModel.DataAnnotations;
using SharedKernel.Models;
namespace Buyer.Domain.Entities
{
public class RFQQuestionAnswerOption : BaseModel
{
    [Key]
    public Guid Id { get; set; }

    public Guid RFQQuestionAnswerId { get; set; }

    public Guid RFQQuestionOptionId { get; set; }
    public RFQQuestionAnswerOption(){}
}
}