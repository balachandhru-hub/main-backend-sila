namespace Buyer.Domain.Dto
{
    public class SupplierVerificationRequestDetailQuestinandAnswerDto
    {
        public Guid RequestId { get; set; }

        public Guid RFQId { get; set; }

        public string RFQNumber { get; set; }

        public Guid BuyerId { get; set; }

        public Guid SupplierOrganizationId { get; set; }

        public Guid TemplateId { get; set; }

        public string Status { get; set; }

        public string? Remarks { get; set; }

        public DateTime? DueDate { get; set; }

        public List<SupplierVerificationQuestionAndAnswerDto> Questions { get; set; } = new();
    }

    public class SupplierVerificationQuestionAndAnswerDto
    {
        public Guid VerificationTemplateQuestionId { get; set; }

        public string Question { get; set; }

        public string QuestionType { get; set; }

        public bool IsRequired { get; set; }

        public int DisplayOrder { get; set; }

        // Supplier Answer
        public string? Answer { get; set; }

        // Attachment uploaded by supplier
        public Guid? AssetId { get; set; }

        // Selected option for Radio/Dropdown/Checkbox
        public Guid? VerificationTemplateQuestionOptionId { get; set; }

        public List<SupplierVerificationQuestionOptionDto> Options { get; set; } 
    }

 
}