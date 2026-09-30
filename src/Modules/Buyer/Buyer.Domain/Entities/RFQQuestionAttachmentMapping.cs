using System.ComponentModel.DataAnnotations;
    using SharedKernel.Models;
namespace Buyer.Domain.Entities
{
public class RFQQuestionAttachmentMapping : BaseModel
{
    [Key]
    public Guid Id { get; set; }

    public Guid RFQQuestionId { get; set; }

    public Guid AssetId { get; set; }

    public string Type { get; set; }
}
}