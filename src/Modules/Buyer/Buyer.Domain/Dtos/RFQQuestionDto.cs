namespace Buyer.Domain.Dtos
{
public class RFQQuestionDto
{
    public string Question { get; set; }

    public string QuestionType { get; set; }

    public bool IsRequired { get; set; }

    public int DisplayOrder { get; set; }

    public List<string>? Options { get; set; }
}
}