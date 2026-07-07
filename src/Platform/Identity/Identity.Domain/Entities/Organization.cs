using SharedKernel.Models;
using Identity.Domain.Enum;
using System.ComponentModel.DataAnnotations;


namespace Identity.Domain.Entities
{
    public class Organization : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

         public string OrganizationName { get; set; } 

        public OrganizationType OrganizationType { get; set; }

        public string Email { get; set; } 

        public string Phone { get; set; } 

        public string Country { get; set; } 

    

        public bool EmailVerified { get; set; }
        public string AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PinCode { get; set; }
        public Organization(){}

       
    }
}