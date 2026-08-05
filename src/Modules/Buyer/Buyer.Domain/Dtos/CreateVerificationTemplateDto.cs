
namespace Buyer.Domain.Dto
{
public class CreateVerificationTemplateDto
{
    public string TemplateName { get; set; }

    public string? Description { get; set; }
    public string? Category{get;set;}
}
}