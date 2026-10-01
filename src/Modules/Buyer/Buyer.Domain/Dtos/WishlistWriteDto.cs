namespace Buyer.Domain.Dtos
{
    public class WishlistWriteDto
    {
        public Guid OutletId { get; set; }
        public string WishlistName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid? SupplierOrganizationId { get; set; }
        public string? SupplierName { get; set; }
        public Guid? MasterApprovalFlowId { get; set; }
        public string? Currency { get; set; }
        public string? DeliveryInstruction { get; set; }
        public DateTime? RequiredDate { get; set; }
        public List<WishlistItemWriteDto> Items { get; set; } = new();

        /// <summary>
        /// True keeps the wishlist as an editable draft. False submits it: the wishlist is frozen and approval starts.
        /// </summary>
        public bool SaveAsDraft { get; set; }
    }
}
