namespace Buyer.Domain.Dtos
{
    public class UpdateItemBuyerMasterDto
    {
        public Guid Id { get; set; }

        public Guid? BuyerId { get; set; }

        public string Description { get; set; }

        public string MaterialCode { get; set; }

        public string MaterialGroup { get; set; }
    }
}