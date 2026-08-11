namespace Buyer.Domain.Dto
{
    public class UpdateDeliveryLocationDto
    {

        public Guid BuyerId { get; set; }
        public string? LocationName { get; set; } 

        public string? AddressLine1 { get; set; }

        public string? AddressLine2 { get; set; }

        public string? City { get; set; }

        public string? State { get; set; } 

        public string? Country { get; set; } 

        public string ?PinCode { get; set; } 

        public string? ContactPerson { get; set; }

        public string? ContactPhone { get; set; }

        public bool? IsDefault { get; set; }
    }
}