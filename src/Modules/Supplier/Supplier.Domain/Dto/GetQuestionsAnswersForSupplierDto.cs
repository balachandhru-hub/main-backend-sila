using SharedKernel.Dto;

namespace Supplier.Domain.Dto
{
    public class GetQuestionsAnswersForSupplierDto
    {
        public Guid SupplierVerificationRequestId { get; set; }

        public List<QuestionAnswerDto> Questions { get; set; } 
    }

    public class QuestionAnswerDto
    {
        public Guid VerificationTemplateQuestionId { get; set; }

        public Guid TemplateId { get; set; }

        public string? Answer { get; set; }

        public Guid? AssetId { get; set; }

        public Guid? VerificationTemplateQuestionOptionId { get; set; }
    }
}