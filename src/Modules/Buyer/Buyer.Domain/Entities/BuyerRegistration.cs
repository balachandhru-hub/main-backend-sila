using SharedKernel.Models;
using System.ComponentModel.DataAnnotations;

namespace Buyer.Domain.Entities
{
    public class BuyerRegistration : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }
        public Guid BuyerId { get; set; }
        public string RegistrationNumber { get; set; }
        public string RegistrationName { get; set; }
        public string RegistrationType { get; set; }
        public Guid? AssetId { get; set; }
        public bool IsVerified { get; set; }
        public DateTime? VerifiedOn { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public BuyerRegistration()
        {
        }
    }
}

      
    
