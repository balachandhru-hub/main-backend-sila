using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Commands.CreateMicrosoftAuthorization
{
    /// <summary>
    /// Starts the Microsoft sign-in: stores a one-time state and returns the Microsoft authorization URL.
    /// </summary>
    public class CreateMicrosoftAuthorizationCommand : IRequest<MicrosoftConnectResponseDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public MicrosoftConnectRequestDto Request { get; set; } = new();
    }
}
