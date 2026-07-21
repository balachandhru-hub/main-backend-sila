using System.ComponentModel.DataAnnotations;
using SharedKernel.Models;

using System.ComponentModel.DataAnnotations.Schema;
namespace Buyer.Domain.Entities
{
    public class BuyerDepartment : BaseModel
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }
        public BuyerBusinessProfile BuyerBusinessProfile { get; set; }
        public string Department { get; set; }
        public BuyerDepartment() { }
    }
}