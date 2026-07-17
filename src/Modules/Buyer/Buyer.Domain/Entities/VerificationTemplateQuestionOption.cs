using System.ComponentModel.DataAnnotations;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class VerificationTemplateQuestionOption : BaseModel
    {
        [Key]
        public Guid Id { get; set; }

        public Guid VerificationTemplateQuestionId { get; set; }

        public string OptionText { get; set; }

        public int DisplayOrder { get; set; }
        public VerificationTemplateQuestionOption (){}
    }
}