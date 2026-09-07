namespace Supplier.Domain.Dto
{
    public class SupplierListDto
    {
        public Guid SupplierId { get; set; }

        public string SupplierName { get; set; }


       public string Email {get;set;}
        public bool IsVerified { get; set; }
        public string SNID { get; set; }
        public Guid OrganizationId { get; set; }
    }
}