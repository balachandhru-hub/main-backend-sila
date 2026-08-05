

namespace Supplier.Domain.Dto
{
    public class GetSupplierListDto
    {
        public int Index { get; set; } = 0;

        public int Limit { get; set; } = 10;

        public string? SearchTerm { get; set; }

        // Optional
        public long? Segment { get; set; }

        // Optional
        public long? Family { get; set; }

        // Optional
        // null = All
        // true = Verified
        // false = Non Verified
        public string? Type{ get; set; }
        public Guid BuyerId {get;set;}
    }
}