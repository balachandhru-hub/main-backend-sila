using Buyer.Domain.Dto;
using MediatR;

namespace Buyer.Application.Features.Commands.Template
{
    public class CreateVerificationDefaultTemplateCommand : IRequest<Guid>
    {
       public Guid OrganizationId { get; set; }
    }
}