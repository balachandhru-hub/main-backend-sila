using MediatR;
using Supplier.Domain.Dto;

namespace Supplier.Application.Features.Queries.GetSupplierDashboardAnalytics
{
    public record GetSupplierDashboardAnalyticsQuery(
        Guid OrganizationId,
        Guid UserId,
        Guid RoleId
    ) : IRequest<SupplierDashboardAnalyticsDto>;
}
