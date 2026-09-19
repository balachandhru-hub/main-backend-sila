using Buyer.Application.Contracts;
using Buyer.Domain.Common;
using Buyer.Domain.Dto;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.GetBuyerDashboardAnalytics
{
    public class GetBuyerDashboardAnalyticsQueryHandler
        : IRequestHandler<GetBuyerDashboardAnalyticsQuery, BuyerDashboardAnalyticsDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ISupplierApiClient _supplierApiClient;
        private readonly ILoggerManager _logger;

        public GetBuyerDashboardAnalyticsQueryHandler(
            IRepositoryWrapper repository,
            ISupplierApiClient supplierApiClient,
            ILoggerManager logger)
        {
            _repository = repository;
            _supplierApiClient = supplierApiClient;
            _logger = logger;
        }

        private sealed record RfqRow(
            Guid Id,
            string RFQNumber,
            string Title,
            string Status,
            string Department,
            DateTime StartDate,
            DateTime EndDate,
            DateTime DateCreated);

        public async Task<BuyerDashboardAnalyticsDto> Handle(
            GetBuyerDashboardAnalyticsQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Building buyer dashboard analytics for OrganizationId: {request.OrganizationId}");

            var buyer = _repository.BuyerBusinessProfile
                .FindFirstByCondition(x => x.OrganizationId == request.OrganizationId && x.IsActive);

            if (buyer == null)
            {
                _logger.LogError("Buyer not found.");
                throw new NotFoundCustomException("Buyer not found.", "Buyer does not exist.");
            }

            // Same visibility rule as the RFQ list: admins see the organization, users their own RFQs.
            var rfqQuery = _repository.RFQ.FindByCondition(x => x.BuyerId == buyer.Id);
            if (request.RoleId != Common.BUYER_ADMIN_ROLE_ID)
            {
                rfqQuery = rfqQuery.Where(x => x.CreatedBy == request.UserId);
            }

            var rfqs = await rfqQuery
                .Select(x => new RfqRow(
                    x.Id,
                    x.RFQNumber,
                    x.Title,
                    x.Status,
                    x.Department,
                    x.StartDate,
                    x.EndDate,
                    x.DateCreated))
                .ToListAsync(cancellationToken);

            var rfqIds = rfqs.Select(x => x.Id).ToList();
            var now = DateTime.UtcNow;

            // Awarded comes from the RFQ status (set by SaveRFQAwardCommandHandler); frozen from the
            // status posted by the Freeze Bid action; the rest from the bidding window dates.
            string StageOf(RfqRow rfq)
            {
                if (string.Equals(rfq.Status, Common.RFQ_AWARDED_STATUS, StringComparison.OrdinalIgnoreCase)) return Common.DASHBOARD_STAGE_AWARDED;
                if (string.Equals(rfq.Status, Common.RFQ_FREEZING_STATUS, StringComparison.OrdinalIgnoreCase)) return Common.DASHBOARD_STAGE_FROZEN;
                if (rfq.StartDate > now) return Common.DASHBOARD_STAGE_UPCOMING;
                if (rfq.EndDate >= now) return Common.DASHBOARD_STAGE_LIVE;
                return Common.DASHBOARD_STAGE_BIDDING_CLOSED;
            }

            var stages = rfqs.ToDictionary(x => x.Id, StageOf);

            // ---- Invited suppliers (registered + external)
            var supplierInvites = await _repository.RFQSupplierMapping
                .FindByCondition(x => rfqIds.Contains(x.RFQId) && x.IsActive)
                .Select(x => new { x.RFQId, x.SupplierId })
                .ToListAsync(cancellationToken);

            var externalInvites = await _repository.RFQExternalSupplier
                .FindByCondition(x => rfqIds.Contains(x.RFQId) && x.IsActive)
                .Select(x => new { x.RFQId, x.ExternalSupplierId })
                .ToListAsync(cancellationToken);

            var invitedPerRfq = supplierInvites.Select(x => x.RFQId)
                .Concat(externalInvites.Select(x => x.RFQId))
                .GroupBy(x => x)
                .ToDictionary(g => g.Key, g => g.Count());

            // ---- Awards and contracts (a contract is valued in its RFQ's currency)
            var awards = await _repository.RFQAward
                .FindByCondition(x => rfqIds.Contains(x.RFQId) && x.IsActive)
                .Select(x => new { x.RFQId, x.DateCreated })
                .ToListAsync(cancellationToken);

            var contracts = await _repository.Contract
                .FindByCondition(x => x.BuyerId == buyer.Id && rfqIds.Contains(x.RFQId) && x.IsActive)
                .Select(x => new { x.SupplierId, x.Status })
                .ToListAsync(cancellationToken);

            // ---- Department names: RFQ.Department stores the BuyerDepartment id.
            var departmentNames = await _repository.BuyerDepartment
                .FindByCondition(x => x.BuyerId == buyer.Id)
                .ToDictionaryAsync(x => x.Id, x => x.Department, cancellationToken);

            string DepartmentOf(string? value)
            {
                if (string.IsNullOrWhiteSpace(value)) return Common.DASHBOARD_UNASSIGNED_DEPARTMENT;
                if (Guid.TryParse(value, out var id))
                {
                    return departmentNames.TryGetValue(id, out var name) && !string.IsNullOrWhiteSpace(name)
                        ? name.Trim()
                        : Common.DASHBOARD_UNKNOWN_DEPARTMENT;
                }
                // Older RFQs stored the department name itself.
                return value.Trim();
            }

            // ---- Department names: RFQ.Department stores the BuyerDepartment id.
            var departmentNames = await _repository.BuyerDepartment
                .FindByCondition(x => x.BuyerId == buyer.Id)
                .ToDictionaryAsync(x => x.Id, x => x.Department, cancellationToken);

            string DepartmentOf(string? value)
            {
                if (string.IsNullOrWhiteSpace(value)) return Common.DASHBOARD_UNASSIGNED_DEPARTMENT;
                if (Guid.TryParse(value, out var id))
                {
                    return departmentNames.TryGetValue(id, out var name) && !string.IsNullOrWhiteSpace(name)
                        ? name.Trim()
                        : Common.DASHBOARD_UNKNOWN_DEPARTMENT;
                }
                // Older RFQs stored the department name itself.
                return value.Trim();
            }

            // ---- KPIs
            int awardedCount = stages.Values.Count(s => s == Common.DASHBOARD_STAGE_AWARDED);
            int endedCount = rfqs.Count(x => x.EndDate < now || stages[x.Id] == Common.DASHBOARD_STAGE_AWARDED);

            var result = new BuyerDashboardAnalyticsDto
            {
                Kpis = new BuyerDashboardKpiDto
                {
                    TotalRfqs = rfqs.Count,
                    LiveRfqs = stages.Values.Count(s => s == Common.DASHBOARD_STAGE_LIVE),
                    ClosingThisWeek = rfqs.Count(x => stages[x.Id] == Common.DASHBOARD_STAGE_LIVE && x.EndDate <= now.AddDays(Common.DASHBOARD_CLOSING_SOON_DAYS)),
                    AwaitingAward = stages.Values.Count(s => s == Common.DASHBOARD_STAGE_FROZEN || s == Common.DASHBOARD_STAGE_BIDDING_CLOSED),
                    AwardedRfqs = awardedCount,
                    AwardRate = endedCount == 0 ? 0 : Math.Round(awardedCount * 100m / endedCount, 1),
                    SuppliersEngaged = supplierInvites.Select(x => x.SupplierId).Distinct().Count()
                        + externalInvites.Select(x => x.ExternalSupplierId).Distinct().Count(),
                    Contracts = contracts.Count,
                    ActiveContracts = contracts.Count(x =>
                        !string.Equals(x.Status, Common.CONTRACT_REJECTED_STATUS, StringComparison.OrdinalIgnoreCase)),
                },
            };

            // ---- Monthly trend (last 12 calendar months)
            var firstMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-(Common.DASHBOARD_TREND_MONTHS - 1));
            for (int i = 0; i < Common.DASHBOARD_TREND_MONTHS; i++)
            {
                var monthStart = firstMonth.AddMonths(i);
                var monthEnd = monthStart.AddMonths(1);

                result.MonthlyTrend.Add(new BuyerMonthlyTrendDto
                {
                    Month = monthStart.ToString(Common.DASHBOARD_MONTH_FORMAT),
                    Created = rfqs.Count(x => x.DateCreated >= monthStart && x.DateCreated < monthEnd),
                    Awarded = awards.Where(a => a.DateCreated >= monthStart && a.DateCreated < monthEnd)
                        .Select(a => a.RFQId).Distinct().Count(),
                });
            }

            // ---- Lifecycle breakdown (fixed order so the chart reads left to right)
            result.StatusBreakdown = Common.DASHBOARD_STAGE_ORDER
                .Select(stage => new DashboardBreakdownDto
                {
                    Key = stage.ToUpperInvariant().Replace(' ', '_'),
                    Label = stage,
                    Count = stages.Values.Count(s => s == stage),
                })
                .ToList();

            // ---- RFQs by department
            result.RfqsByDepartment = rfqs
                .GroupBy(x => DepartmentOf(x.Department))
                .Select(g => new DashboardBreakdownDto { Key = g.Key, Label = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(Common.DASHBOARD_MAX_BREAKDOWN_ROWS)
                .ToList();

            // ---- Contracts by supplier (names resolved through the Supplier service)
            var supplierGroups = contracts
                .GroupBy(x => x.SupplierId)
                .Select(g => new { SupplierId = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(Common.DASHBOARD_MAX_BREAKDOWN_ROWS)
                .ToList();

            var supplierNames = new Dictionary<Guid, string>();
            if (supplierGroups.Count > 0)
            {
                try
                {
                    var names = await _supplierApiClient.GetSupplierNamesByIds(
                        supplierGroups.Select(x => x.SupplierId).ToList(), cancellationToken);
                    foreach (var name in names)
                    {
                        supplierNames[name.SupplierId] = name.SupplierName;
                    }
                }
                catch (Exception ex)
                {
                    // Names are cosmetic; the dashboard still renders with short ids.
                    _logger.LogError($"Could not resolve supplier names for dashboard: {ex.Message}");
                }
            }

            result.ContractsBySupplier = supplierGroups
                .Select(x => new DashboardBreakdownDto
                {
                    Key = x.SupplierId.ToString(),
                    Label = supplierNames.TryGetValue(x.SupplierId, out var name) && !string.IsNullOrWhiteSpace(name)
                        ? name
                        : $"{Common.DASHBOARD_UNKNOWN_SUPPLIER_PREFIX} {x.SupplierId.ToString()[..8]}",
                    Count = x.Count,
                })
                .ToList();

            // ---- Closing schedule for live RFQs
            var today = now.Date;
            var live = rfqs.Where(x => stages[x.Id] == Common.DASHBOARD_STAGE_LIVE).ToList();
            result.ClosingSchedule = Common.DASHBOARD_CLOSING_WINDOWS
                .Select(w => new DashboardBreakdownDto
                {
                    Key = w.Key,
                    Label = w.Label,
                    Count = live.Count(x =>
                    {
                        int days = (int)(x.EndDate.Date - today).TotalDays;
                        return days >= w.FromDay && days <= w.ToDay;
                    }),
                })
                .ToList();

            // ---- Soonest deadlines
            result.UpcomingDeadlines = live
                .OrderBy(x => x.EndDate)
                .Take(Common.DASHBOARD_UPCOMING_DEADLINES)
                .Select(x => new BuyerUpcomingDeadlineDto
                {
                    RfqId = x.Id,
                    RfqNumber = x.RFQNumber,
                    Title = x.Title,
                    Department = DepartmentOf(x.Department),
                    EndDate = x.EndDate,
                    InvitedSuppliers = invitedPerRfq.TryGetValue(x.Id, out var invited) ? invited : 0,
                })
                .ToList();

            _logger.LogInfo(
                $"Buyer dashboard analytics built for BuyerId: {buyer.Id}. RFQs: {rfqs.Count}, Live: {result.Kpis.LiveRfqs}");

            return result;
        }
    }
}
