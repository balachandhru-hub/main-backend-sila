namespace Buyer.Domain.Dto
{
    public class VerificationTemplateResponseDto
    {
        public Guid TemplateId { get; set; }

        public string TemplateCode { get; set; }

        public string TemplateName { get; set; }

        // Default / Buyer
        public string TemplateType { get; set; }

        public List<VerificationTemplateQuestionDto> Questions { get; set; } = new();
    }
}