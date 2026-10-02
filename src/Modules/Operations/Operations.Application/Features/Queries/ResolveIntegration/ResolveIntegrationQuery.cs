using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Queries.ResolveIntegration
{
    /// <summary>
    /// Finds the active API of an organization for one API type. Asked by another service before it sends a document.
    /// </summary>
    public class ResolveIntegrationQuery : IRequest<ResolvedIntegrationDto>
    {
        public ResolveIntegrationRequestDto Request { get; set; } = new();
    }
}
