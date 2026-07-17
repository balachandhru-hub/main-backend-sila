using System.ComponentModel.DataAnnotations;
using SharedKernel.Models;
namespace Buyer.Domain.Entities
{
    public class VerificationAnswer : BaseModel
    {
        [Key]
        public Guid Id { get; set; }

        public Guid SupplierVerificationRequestId { get; set; }

        public Guid VerificationTemplateQuestionId { get; set; }
	    public Guid  TemplateId {get; set;}

        public string? Answer { get; set; }

        public Guid? AssetId { get; set; }

        public Guid? VerificationTemplateQuestionOptionId { get; set; }

        public DateTime? AnsweredOn { get; set; }
        public VerificationAnswer(){}
    }
}
