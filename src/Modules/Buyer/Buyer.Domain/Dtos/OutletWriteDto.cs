namespace Buyer.Domain.Dtos
{
    public class OutletWriteDto
    {
        public string OutletName { get; set; } = string.Empty;
        public string? OutletCode { get; set; }
        public string? Description { get; set; }
        public string? ExternalShipTo { get; set; }
        public string? AddressLine1 { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
    }
}
