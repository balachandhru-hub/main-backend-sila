using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Commands.ValidateMicrosoftConnection
{
    /// <summary>
    /// Checks read/write access to the chosen SharePoint folder and makes it the organization's invoice destination.
    /// </summary>
    public class ValidateMicrosoftConnectionCommand : IRequest<MicrosoftConnectionValidationResponseDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid ConnectionId { get; set; }
        public MicrosoftValidateConnectionRequestDto Request { get; set; } = new();
    }
}
