
using System.ComponentModel.DataAnnotations;
using SharedKernel.Models;
namespace Buyer.Domain.Entities
{public class RFQQuestionOption : BaseModel
{
    [Key]
    public Guid Id { get; set; }

    public Guid RFQQuestionId { get; set; }

    public string OptionText { get; set; }

    public int DisplayOrder { get; set; }
    public RFQQuestionOption(){}
}
}