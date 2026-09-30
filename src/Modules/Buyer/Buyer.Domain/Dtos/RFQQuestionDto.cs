using SharedKernel.Dto;
namespace Buyer.Domain.Dto
{
    public class RFQQuestionDto
    {
        public Guid Id { get; set; }
        public string Question { get; set; }

        public string QuestionType { get; set; }

        public bool IsRequired { get; set; }

        public int DisplayOrder { get; set; }

        public List<string>? Options { get; set; }
        public List<AssetUploadDto>? Attachments { get; set; }
    }
}
