using SharedKernel.Dto;
namespace Supplier.Domain.Dto
{

public class RFQQuestionResponseDto
{
    public Guid QuestionId { get; set; }

    public string Question { get; set; }

    public string QuestionType { get; set; }

    public bool IsRequired { get; set; }

    public int DisplayOrder { get; set; }

    public List<RFQQuestionOptionDto> Options { get; set; } 
    public List<AssetDto> Attachments { get; set; } 

}
}