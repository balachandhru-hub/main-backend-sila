using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Queries.GetPendingApprovals
{
    public record GetPendingApprovalsQuery(
        Guid UserId,
        string? Status ,
        string? SearchTerm 
    ) : IRequest<List<PendingApprovalDto>>;
}