using SharedKernel.Dto;

namespace Supplier.Domain.Dto
{
    public class RFQESignDto
    {
        public Guid SupplierId { get; set; }

        public string? SupplierName { get; set; }

        public List<AssetDto> Attachments { get; set; } = new();
    }
}
