using Buyer.Domain.Dto;
using MediatR;

namespace Buyer.Application.Features.Queries.GetApprovalKpi
{
    public record GetApprovalKpiQuery(
        Guid UserId
    ) : IRequest<ApprovalKpiDto>;
}
