namespace Buyer.Domain.Dto
{
    public class GetDefaultVerificationTemplateDto
    {
        public string TemplateCode { get; set; }

        public string TemplateName { get; set; }

        public List<DefaultTemplateQuestionDto> Questions { get; set; } = new();
    }

    public class DefaultTemplateQuestionDto
    {
        public Guid QuestionId { get; set; }

        public string Question { get; set; }

        public string QuestionKey { get; set; }

        public string QuestionType { get; set; }

        public string? Answer { get; set; }

        public int DisplayOrder { get; set; }
    }
}