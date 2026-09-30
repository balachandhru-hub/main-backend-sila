using SharedKernel.Dto;

namespace Supplier.Domain.Dto
{
    public class SubmitVerificationDto
    {
        public Guid VerificationRequestId { get; set; }

        public Guid SupplierId { get; set; }

        public List<VerificationAnswerDto>? Answers { get; set; } 
         public string Status { get; set; }
    }

    public class VerificationAnswerDto
    {
        public Guid VerificationTemplateQuestionId { get; set; }

        public Guid TemplateId { get; set; }

        public string? Answer { get; set; }

        public Guid? VerificationTemplateQuestionOptionId { get; set; }

        public AssetUploadDto? Attachment { get; set; }
    }
}