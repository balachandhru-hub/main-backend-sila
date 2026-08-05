namespace Buyer.Domain.Dto
{
    public class VerificationTemplateQuestionDto
    {
        public Guid QuestionId { get; set; }

        public string Question { get; set; }

        public string QuestionKey { get; set; }

        public string QuestionType { get; set; }

        public int DisplayOrder { get; set; }

        public string? Answer { get; set; }
        public List<string>? Options { get; set; }
        public bool? IsRequired { get; set; }
    }
}