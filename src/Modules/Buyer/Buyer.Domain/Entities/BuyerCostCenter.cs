using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;
namespace Buyer.Domain.Entities
{
    public class BuyerCostCenter : BaseModel
    {
        [Key]
        public Guid Id { get; set; }
        [Required]
        [ForeignKey("BuyerDepartment")]
         public Guid DepartmentId { get; set; }
         public BuyerDepartment BuyerDepartment{get;set;}
        public string CostCenter { get; set; }

        public BuyerCostCenter() { }
    }
}