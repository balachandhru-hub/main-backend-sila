using System.ComponentModel.DataAnnotations;
using SharedKernel.Models;
namespace Buyer.Domain.Entities
{
    public class VerificationTemplate : BaseModel
    {
        [Key]
        public Guid Id { get; set; }

        public Guid BuyerId { get; set; }

        public string TemplateCode { get; set; }

        public string TemplateName { get; set; }

        public string? Description { get; set; }
        public VerificationTemplate(){}

      
    }
}
