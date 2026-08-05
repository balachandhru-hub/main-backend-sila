namespace Supplier.Domain.Dto
{
public class SupplierVerificationRequestDetailDto
{
    public Guid RequestId { get; set; }
    public Guid RFQId { get; set; }
    public string RFQNumber { get; set; }
    public Guid SupplierOrganizationId { get; set; }
    public Guid TemplateId { get; set; }
    public string TemplateName { get; set; }
    public string Status { get; set; }
    public string? Remarks { get; set; }
    public DateTime? DueDate { get; set; }
    public Guid BuyerId { get; set; }

    public List<SupplierVerificationQuestionAnswerDto> Questions { get; set; } = new();
}

public class SupplierVerificationQuestionAnswerDto
{
    public Guid QuestionId { get; set; }
    public string Question { get; set; }
    public string QuestionType { get; set; }
    public bool IsRequired { get; set; }

    public string? Answer { get; set; }
    public Guid? AssetId { get; set; }
}
}