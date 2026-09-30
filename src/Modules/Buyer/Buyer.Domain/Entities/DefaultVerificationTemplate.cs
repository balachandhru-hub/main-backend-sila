using System.ComponentModel.DataAnnotations;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class DefaultVerificationTemplate : BaseModel
    {
        [Key]
        public Guid Id { get; set; }

        public string TemplateCode { get; set; }

        public string TemplateName { get; set; }

        public string? Description { get; set; }
    }
}