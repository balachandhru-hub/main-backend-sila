namespace Buyer.Domain.Dto
{
public class SupplierVerificationRequestListBySupplierIdDto
{
    public Guid RequestId { get; set; }

    public string RFQNumber { get; set; }

    public Guid SupplierOrganizationId { get; set; }

    public string SupplierName { get; set; }

    public Guid BuyerOrganizationId { get; set; }

    public string TemplateName { get; set; }

    public string Status { get; set; }

    public DateTime? DueDate { get; set; }

    public DateTime DateCreated { get; set; }

    public List<SupplierVerificationQuestionDto> Questions { get; set; } = new();
}

public class SupplierVerificationQuestionDto
{
    public Guid VerificationTemplateQuestionId { get; set; }

    public string Question { get; set; }

    public string QuestionType { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsRequired { get; set; }

    public List<SupplierVerificationQuestionOptionDto> Options { get; set; } = new();
}

public class SupplierVerificationQuestionOptionDto
{
    public Guid Id { get; set; }

    public string OptionText { get; set; }

    public int DisplayOrder { get; set; }
}
}