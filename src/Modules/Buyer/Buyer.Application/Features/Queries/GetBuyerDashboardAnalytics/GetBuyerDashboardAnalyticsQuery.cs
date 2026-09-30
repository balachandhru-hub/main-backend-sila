using Buyer.Domain.Dto;
using MediatR;

namespace Buyer.Application.Features.Queries.GetBuyerDashboardAnalytics
{
    public record GetBuyerDashboardAnalyticsQuery(
        Guid OrganizationId,
        Guid UserId,
        Guid RoleId
    ) : IRequest<BuyerDashboardAnalyticsDto>;
}
