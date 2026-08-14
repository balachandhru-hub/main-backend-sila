
namespace Buyer.Domain.Dto
{
    public class UpdateVerificationTemplateQuestionDto
    {
        public Guid? Id { get; set; }

        public Guid VerificationTemplateId { get; set; }

        public string Question { get; set; } 

        public string QuestionType { get; set; }

        public bool IsRequired { get; set; }

        public int DisplayOrder { get; set; }
        public bool IsDeleted { get; set; }

        public List<VerificationTemplateQuestionOptionDto>? Options { get; set; }
    }
    public class VerificationTemplateQuestionOptionDto
    {
        public Guid? Id { get; set; }

        public string OptionText { get; set; } 

        public int DisplayOrder { get; set; }
    }
}