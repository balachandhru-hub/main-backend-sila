using SharedKernel.Dto;

namespace Buyer.Domain.Dto
{
    public class RFQESignDto
    {
        public Guid SupplierId { get; set; }

        public string? SupplierName { get; set; }

        public List<AssetDto> Attachments { get; set; } = new();
    }
}
