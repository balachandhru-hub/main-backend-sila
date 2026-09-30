namespace Buyer.Domain.Dtos
{
    public class WishlistListItemDto
    {
        public Guid Id { get; set; }
        public string WishlistName { get; set; } = string.Empty;
        public string? OutletName { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ApprovalName { get; set; }
        public string? BuyerErpDocumentNumber { get; set; }
        public string? SupplierErpDocumentNumber { get; set; }
        public string? LastError { get; set; }
    }
}
