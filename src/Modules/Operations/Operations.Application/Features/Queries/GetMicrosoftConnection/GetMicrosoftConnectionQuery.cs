using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Queries.GetMicrosoftConnection
{
    /// <summary>
    /// Returns the state of a Microsoft storage connection: site, library, folder and whether it is validated.
    /// </summary>
    public class GetMicrosoftConnectionQuery : IRequest<MicrosoftConnectionValidationResponseDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid ConnectionId { get; set; }
    }
}
