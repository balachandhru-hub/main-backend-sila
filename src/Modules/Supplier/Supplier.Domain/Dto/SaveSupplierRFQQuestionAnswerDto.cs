using SharedKernel.Dto;
namespace Supplier.Domain.Dto
{
public class SaveSupplierRFQQuestionAnswerDto
{
    public Guid RFQQuestionId { get; set; }

    // INPUT
    public string? Answer { get; set; }

    // RADIO / DROPDOWN
    public Guid? QuestionOptionId { get; set; }

    // CHECKBOX
    public List<Guid>? QuestionOptionIds { get; set; }

    // FILE
    public AssetUploadDto? Attachment { get; set; }
}
}