using Identity.Domain.Dto;
using MediatR;

namespace Identity.Application.Features.Commands.Organization
{
    public class UpdateOrganizationCommand : IRequest<bool>
    {
        public UpdateOrganizationDto Organization { get; set; } = new();
    }
}