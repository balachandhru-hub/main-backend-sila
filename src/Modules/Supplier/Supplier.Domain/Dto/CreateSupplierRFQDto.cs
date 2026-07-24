
namespace Supplier.Domain.Dto
{
    public class CreateSupplierRFQDto
    {
        public Guid BuyerRFQId { get; set; }

        public string RFQNumber { get; set; }

        public Guid BuyerId { get; set; }
        public Guid SupplierId { get; set; }
        public string BuyerName { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public bool AddLotOption { get; set; }

        public string Status { get; set; }
        public string DeliveryLocation {get;set;}



        public List<SupplierRFQItemDto> Items { get; set; }
    }
}