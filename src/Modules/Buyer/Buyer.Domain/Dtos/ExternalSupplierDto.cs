using System.ComponentModel.DataAnnotations;

namespace Buyer.Domain.Dto
{
    public class ExternalSupplierDto
    {
        [Required]
        public string SupplierName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public string PhoneNumber { get; set; }

        public string Address { get; set; }
    }
}
