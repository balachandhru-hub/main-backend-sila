using MediatR;

namespace Operations.Application.Features.Commands.DisconnectMicrosoftConnection
{
    /// <summary>
    /// Disconnects a Microsoft storage connection: the stored sign-in is removed and its destinations stop receiving documents.
    /// </summary>
    public class DisconnectMicrosoftConnectionCommand : IRequest<Guid>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid ConnectionId { get; set; }
    }
}
