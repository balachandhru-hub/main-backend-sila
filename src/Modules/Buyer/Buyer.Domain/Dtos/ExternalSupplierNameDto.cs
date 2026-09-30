namespace Buyer.Domain.Dto
{
    public class ExternalSupplierNameDto
    {
        public Guid ExternalSupplierId { get; set; }

        public string ExternalSupplierName { get; set; }

        /// <summary>Always "EXTERNAL_SUPPLIER" - included so the frontend can badge this entry the same way as SupplierIds' VerificationStatus.</summary>
        public string SupplierType { get; set; }
    }
}
