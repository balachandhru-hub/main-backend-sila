using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Queries.GetPendingApprovals
{
    public record GetPendingApprovalsQuery(
        Guid UserId,
        string? Status ,
        string? SearchTerm,
        int Index = 0,
        int Limit = 10
    ) : IRequest<List<PendingApprovalDto>>;
}