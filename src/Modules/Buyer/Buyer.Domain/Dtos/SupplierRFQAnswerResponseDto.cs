using SharedKernel.Dto;

namespace Buyer.Domain.Dto
{

    public class SupplierRFQAnswerDto
{
    public List<SupplierRFQAnswerResponseDto> SupplierAnswers { get; set; } = new();
   
}
    public class SupplierRFQAnswerResponseDto
    {
        public Guid SupplierRFQId { get; set; }
        public Guid SupplierId { get; set; }

        public List<SupplierQuestionAnswerDto> Answers { get; set; } = new();
    }

    public class SupplierQuestionAnswerDto
    {
        public Guid RFQQuestionId { get; set; }

        public string? Answer { get; set; }

        
        public Guid? QuestionOptionId { get; set; }

        
        public List<Guid> QuestionOptionIds { get; set; } = new();

     
        public AssetDto? Attachment { get; set; }
    }
}