using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Queries.GetPendingApprovals
{
    public record GetPendingApprovalsQuery(
        Guid UserId,
        string? Status ,
        string? SearchTerm,
        int Index = 0,
        int Limit = 10,
        // Optional "MANUAL" or "EXCEL" filter - Common.UPLOAD_TYPE_MANUAL/UPLOAD_TYPE_EXCEL. Null/empty returns both.
        string? Type = null
    ) : IRequest<List<PendingApprovalDto>>;
}