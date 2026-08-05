using SharedKernel.Dto;

namespace Supplier.Domain.Dto
{
    public class SupplierRFQAnswerResponseDto
    {
        public Guid SupplierRFQId { get; set; }

        public List<SupplierQuestionAnswerDto> Answers { get; set; } = new();
    }

    public class SupplierQuestionAnswerDto
    {
        public Guid RFQQuestionId { get; set; }

        public string? Answer { get; set; }

        // Radio / Dropdown
        public Guid? QuestionOptionId { get; set; }

        // Checkbox
        public List<Guid> QuestionOptionIds { get; set; } = new();

        // File
        public AssetDto? Attachment { get; set; }
    }
}