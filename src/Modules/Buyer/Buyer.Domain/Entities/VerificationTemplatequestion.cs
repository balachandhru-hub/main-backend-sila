using System.ComponentModel.DataAnnotations;
using SharedKernel.Models;
namespace Buyer.Domain.Entities
{
    public class VerificationTemplateQuestion : BaseModel
    {
        [Key]
        public Guid Id { get; set; }

        public Guid VerificationTemplateId { get; set; }

        public string Question { get; set; }

        public string QuestionType { get; set; }

        public bool IsRequired { get; set; }

        public int DisplayOrder { get; set; }

        public VerificationTemplateQuestion() { }
    }
}
