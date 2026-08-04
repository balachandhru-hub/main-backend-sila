
using MediatR;
using Buyer.Domain.Dto;
namespace Buyer.Application.Features.Commands.Template
{
    public class CreateVerificationTemplateCommand : IRequest<Guid>
    {
        public Guid OrganizationId { get; set; }
        public CreateVerificationTemplateDto VerificationTemplateDto { get; set; }
    }
}