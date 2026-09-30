using System.ComponentModel.DataAnnotations;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class ExternalSupplier : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        public string SupplierName { get; set; }

        public string Email { get; set; }

        public string PhoneNumber { get; set; }

        public string Address { get; set; }

        public ExternalSupplier() { }
    }
}
