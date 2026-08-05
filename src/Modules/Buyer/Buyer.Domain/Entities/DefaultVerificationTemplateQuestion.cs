using System.ComponentModel.DataAnnotations;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class DefaultVerificationTemplateQuestion : BaseModel
    {
        [Key]
        public Guid Id { get; set; }

        public Guid DefaultVerificationTemplateId { get; set; }

        public string Question { get; set; }

        // Used for mapping with supplier table
        public string QuestionKey { get; set; }

        public string QuestionType { get; set; }

        public int DisplayOrder { get; set; }
    }
}