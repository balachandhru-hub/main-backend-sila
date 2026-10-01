using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Queries.GetMicrosoftReadiness
{
    /// <summary>
    /// Tells which Microsoft settings (tenant, client, secret, redirect URI, encryption key) are configured on the server.
    /// </summary>
    public class GetMicrosoftReadinessQuery : IRequest<MicrosoftReadinessResponseDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
    }
}
