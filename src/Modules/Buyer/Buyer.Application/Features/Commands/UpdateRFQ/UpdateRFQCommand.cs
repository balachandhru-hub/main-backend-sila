using Buyer.Domain.Dto;
using MediatR;

namespace Buyer.Application.Features.Commands.UpdateRFQ
{
    public record UpdateRFQCommand(
        Guid RFQId,
        Guid OrganizationId,
        UpdateRFQDto RFQ
    ) : IRequest<Guid>;
}