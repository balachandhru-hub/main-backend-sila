
namespace Supplier.Domain.Dto
{
    public class GetVerifiedSupplierRequestDto
    {
        public int Index { get; set; } = 0;

        public int Limit { get; set; } = 10;
        public Guid BuyerId {get;set;}

       
    }
}