
using SharedKernel.Dto;

namespace Buyer.Domain.Dtos
{
    public class RFQItemDto
    {
        public string Description { get; set; }

        public decimal Quantity { get; set; }

        public string UOM { get; set; }

        public string MaterialCode { get; set; }

        public string MaterialGroup { get; set; }
        public string CostCenter { get; set; }
        public List<AssetUploadDto>? Attachments { get; set; }
       
    }
}
