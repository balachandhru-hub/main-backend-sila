using SharedKernel.Dto;
namespace Buyer.Domain.Dtos
{
public class CreateRFQDto
{
    // Header

    public string Title { get; set; }

    public string Description { get; set; }

    public string Department { get; set; }

    public string Region { get; set; }

    public string Currency { get; set; }

    public string DeliveryLocation { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public DateTime DeliveryTargetDate { get; set; }

    public decimal Budget { get; set; }

    public bool AddLotOption { get; set; }

    // Hardcoded template for now

    public Guid TemplateId { get; set; }

    // Attachments

    public List<AssetUploadDto>? TechnicalSpecificationDocuments { get; set; }

    public List<AssetUploadDto>? TermsConditionDocuments { get; set; }

    // Dynamic Questions

    public List<RFQQuestionDto> Questions { get; set; }

    // Line Items

    public List<RFQItemDto> Items { get; set; }



}
}