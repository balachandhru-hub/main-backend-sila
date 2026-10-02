using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Commands.SendIntegrationRequest
{
    /// <summary>
    /// Sends one document to an organization's API on behalf of another service, with the saved sign-in.
    /// </summary>
    public class SendIntegrationRequestCommand : IRequest<SendIntegrationResponseDto>
    {
        public SendIntegrationRequestDto Request { get; set; } = new();
    }
}
