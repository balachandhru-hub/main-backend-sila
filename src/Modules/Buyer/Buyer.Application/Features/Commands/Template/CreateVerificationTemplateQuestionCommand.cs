using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Commands.Template
{
    public class CreateVerificationTemplateQuestionCommand : IRequest<Guid>
    {
        public CreateVerificationTemplateQuestionDto VerificationTemplateQuestionDto { get; set; }
    }
}