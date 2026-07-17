using System.ComponentModel.DataAnnotations;
using SharedKernel.Models;
namespace Buyer.Domain.Entities
{
public class RFQQuestionAnswer : BaseModel
    {
        [Key]
        public Guid Id { get; set; }

        public Guid RFQQuestionId { get; set; }

        public Guid RFQId { get; set; }

        public string RFQNumber { get; set; }

        public Guid SupplierOrganizationId { get; set; }

        public string? Answer { get; set; }

        // File upload answer
        public Guid? AssetId { get; set; }

        // Radio / Dropdown selected option
        public Guid? QuestionOptionId { get; set; }

        public DateTime? AnsweredOn { get; set; }
        public RFQQuestionAnswer(){}
    }
}
