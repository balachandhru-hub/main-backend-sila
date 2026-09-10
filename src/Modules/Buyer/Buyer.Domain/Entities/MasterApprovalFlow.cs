using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class MasterApprovalFlow : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        public string ApprovalCode { get; set; }

        public string ApprovalName { get; set; }
          [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; }

        
       

      
        public MasterApprovalFlow() { }
    }
}
