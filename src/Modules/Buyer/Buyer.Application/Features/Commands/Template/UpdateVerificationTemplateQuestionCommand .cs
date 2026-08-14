using MediatR;
using Buyer.Domain.Dto;

namespace Buyer.Application.Features.Commands.Template
{
    public class UpdateVerificationTemplateQuestionCommand : IRequest<Guid>
    {
        public UpdateVerificationTemplateQuestionDto VerificationTemplateQuestionDto { get; set; }

        public UpdateVerificationTemplateQuestionCommand(
            UpdateVerificationTemplateQuestionDto verificationTemplateQuestionDto)
        {
            VerificationTemplateQuestionDto = verificationTemplateQuestionDto;
        }
    }
}