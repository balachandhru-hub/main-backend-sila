namespace Buyer.Domain.Dtos
{
    public class CreateVerificationTemplateQuestionDto
    {
        public Guid VerificationTemplateId { get; set; }

        public string Question { get; set; }

        public string QuestionType { get; set; }

        public bool IsRequired { get; set; }

        public int DisplayOrder { get; set; }

        public string? Placeholder { get; set; }

        public List<string>? Options { get; set; }
    }
}