using MediatR;

namespace Identity.Application.Features.Commands.OrganizationStatus
{
    public class EnableDisableOrganizationCommand : IRequest<bool>
    {
        public Guid OrganizationId { get; set; }

        public bool IsActive { get; set; }
    }
}