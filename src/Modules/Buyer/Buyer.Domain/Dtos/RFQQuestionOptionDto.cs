namespace Buyer.Domain.Dto{
public class RFQQuestionOptionDto
{
    public Guid OptionId { get; set; }

    public string OptionText { get; set; }

    public int DisplayOrder { get; set; }
}
}