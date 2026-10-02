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

        /// <summary>
        /// Property (plant) the outlet belongs to.
        /// </summary>
        public Guid? PropertyId { get; set; }

        /// <summary>
        /// Storage location code of the outlet, for example J12.
        /// </summary>
        public string? StorageLocation { get; set; }
    }
}
