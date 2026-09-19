using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;
using Supplier.Domain.Common;
using Supplier.Domain.Dto;
using Supplier.Infrastructure.Contracts.IRepository;

namespace Supplier.Application.Features.Queries.GetSupplierDashboardAnalytics
{
    public class GetSupplierDashboardAnalyticsQueryHandler
        : IRequestHandler<GetSupplierDashboardAnalyticsQuery, SupplierDashboardAnalyticsDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetSupplierDashboardAnalyticsQueryHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        private sealed record InvitationRow(
            Guid Id,
            Guid BuyerRFQId,
            string RFQNumber,
            string Title,
            string BuyerName,
            string Status,
            DateTime StartDate,
            DateTime EndDate,
            DateTime DateCreated);

        public async Task<SupplierDashboardAnalyticsDto> Handle(
            GetSupplierDashboardAnalyticsQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Building supplier dashboard analytics for OrganizationId: {request.OrganizationId}");

            var supplier = _repository.SupplierBusinessProfile
                .FindFirstByCondition(x => x.OrganizationId == request.OrganizationId && x.IsActive);

            if (supplier == null)
            {
                throw new NotFoundCustomException("Supplier not found.", "Supplier does not exist.");
            }

            // Same visibility rule as the supplier RFQ list: admins see every invitation,
            // users only the ones they were assigned.
            var invitationQuery = _repository.SupplierRFQ
                .FindByCondition(x => x.SupplierId == supplier.Id && x.IsActive);

            if (!request.RoleId.Equals(Common.SUPPLIER_ADMIN_ROLE_ID))
            {
                var assignedIds = _repository.RFQOrganizationUserMapping
                    .FindByCondition(x => x.SupplierId == supplier.Id && x.UserId == request.UserId && x.IsActive)
                    .Select(x => x.SupplierRFQId);

                invitationQuery = invitationQuery.Where(x => assignedIds.Contains(x.Id));
            }

            var invitations = await invitationQuery
                .Select(x => new InvitationRow(
                    x.Id,
                    x.BuyerRFQId,
                    x.RFQNumber,
                    x.Title,
                    x.BuyerName,
                    x.Status,
                    x.StartDate,
                    x.EndDate,
                    x.DateCreated))
                .ToListAsync(cancellationToken);

            var invitationIds = invitations.Select(x => x.Id).ToList();
            var now = DateTime.UtcNow;

            var submitted = await _repository.SupplierQuotation
                .FindByCondition(x =>
                    x.SupplierId == supplier.Id &&
                    invitationIds.Contains(x.SupplierRFQId) &&
                    x.Status == Common.SUBMITTED_STATUS &&
                    x.IsActive)
                .Select(x => new { x.SupplierRFQId, x.DateUpdated })
                .ToListAsync(cancellationToken);

            // An RFQ is "won" when at least one of its line items was awarded to this supplier.
            // (Awarding marks every invited supplier's RFQ copy AWARDED, so status alone is not a win.)
            var wonItems = await _repository.SupplierRFQItem
                .FindByCondition(x =>
                    invitationIds.Contains(x.SupplierRFQId) &&
                    x.IsAwarded &&
                    x.AwardedSupplierId == supplier.Id &&
                    x.IsActive)
                .Select(x => new { x.SupplierRFQId, x.DateUpdated })
                .ToListAsync(cancellationToken);

            var quotedRfqIds = submitted.Select(x => x.SupplierRFQId).ToHashSet();
            var wonRfqIds = wonItems.Select(x => x.SupplierRFQId).ToHashSet();

            string StageOf(InvitationRow rfq)
            {
                if (wonRfqIds.Contains(rfq.Id)) return Common.DASHBOARD_STAGE_WON;
                if (string.Equals(rfq.Status, Common.AWARDED_STATUS, StringComparison.OrdinalIgnoreCase)) return Common.DASHBOARD_STAGE_NOT_AWARDED;
                if (string.Equals(rfq.Status, Common.RFQ_FREEZING_STATUS, StringComparison.OrdinalIgnoreCase)) return Common.DASHBOARD_STAGE_FROZEN;
                if (rfq.StartDate > now) return Common.DASHBOARD_STAGE_UPCOMING;
                if (rfq.EndDate >= now) return quotedRfqIds.Contains(rfq.Id) ? Common.DASHBOARD_STAGE_QUOTED : Common.DASHBOARD_STAGE_OPEN;
                return Common.DASHBOARD_STAGE_CLOSED;
            }

            var stages = invitations.ToDictionary(x => x.Id, StageOf);
            bool IsOpen(Guid id) => stages[id] == Common.DASHBOARD_STAGE_OPEN || stages[id] == Common.DASHBOARD_STAGE_QUOTED;

            int notAwarded = invitations.Count(x => quotedRfqIds.Contains(x.Id) && stages[x.Id] == Common.DASHBOARD_STAGE_NOT_AWARDED);
            int decided = wonRfqIds.Count + notAwarded;

            var result = new SupplierDashboardAnalyticsDto
            {
                Kpis = new SupplierDashboardKpiDto
                {
                    Invitations = invitations.Count,
                    OpenForBidding = invitations.Count(x => IsOpen(x.Id)),
                    ActionRequired = invitations.Count(x => stages[x.Id] == Common.DASHBOARD_STAGE_OPEN && x.EndDate <= now.AddDays(Common.DASHBOARD_CLOSING_SOON_DAYS)),
                    QuotationsSubmitted = quotedRfqIds.Count,
                    AwaitingDecision = invitations.Count(x => quotedRfqIds.Contains(x.Id) &&
                        (stages[x.Id] == Common.DASHBOARD_STAGE_FROZEN || stages[x.Id] == Common.DASHBOARD_STAGE_CLOSED)),
                    RfqsWon = wonRfqIds.Count,
                    RfqsNotAwarded = notAwarded,
                    WinRate = decided == 0 ? 0 : Math.Round(wonRfqIds.Count * 100m / decided, 1),
                },
            };

            // ---- Monthly trend
            var firstMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-(Common.DASHBOARD_TREND_MONTHS - 1));
            for (int i = 0; i < Common.DASHBOARD_TREND_MONTHS; i++)
            {
                var start = firstMonth.AddMonths(i);
                var end = start.AddMonths(1);

                result.MonthlyTrend.Add(new SupplierMonthlyTrendDto
                {
                    Month = start.ToString(Common.DASHBOARD_MONTH_FORMAT),
                    Invited = invitations.Count(x => x.DateCreated >= start && x.DateCreated < end),
                    Quoted = submitted.Where(q => q.DateUpdated >= start && q.DateUpdated < end)
                        .Select(q => q.SupplierRFQId).Distinct().Count(),
                    Won = wonItems.Where(w => w.DateUpdated >= start && w.DateUpdated < end)
                        .Select(w => w.SupplierRFQId).Distinct().Count(),
                });
            }

            // ---- Stage breakdown (fixed order: earliest to latest in the lifecycle)
            result.StageBreakdown = Common.DASHBOARD_STAGE_ORDER
                .Select(stage => new SupplierDashboardBreakdownDto
                {
                    Key = stage.ToUpperInvariant().Replace(' ', '_'),
                    Label = stage,
                    Count = stages.Values.Count(s => s == stage),
                })
                .ToList();

            // ---- Funnel
            result.Pipeline = new List<SupplierDashboardBreakdownDto>
            {
                new() { Key = Common.DASHBOARD_FUNNEL_INVITED.ToUpperInvariant(), Label = Common.DASHBOARD_FUNNEL_INVITED, Count = invitations.Count },
                new() { Key = Common.DASHBOARD_FUNNEL_QUOTED.ToUpperInvariant(), Label = Common.DASHBOARD_FUNNEL_QUOTED, Count = quotedRfqIds.Count },
                new() { Key = Common.DASHBOARD_FUNNEL_WON.ToUpperInvariant(), Label = Common.DASHBOARD_FUNNEL_WON, Count = wonRfqIds.Count },
            };

            // ---- RFQs quoted on, per buyer
            var buyerByRfq = invitations.ToDictionary(x => x.Id, x => string.IsNullOrWhiteSpace(x.BuyerName) ? Common.DASHBOARD_UNKNOWN_BUYER : x.BuyerName.Trim());
            result.QuotationsByBuyer = quotedRfqIds
                .GroupBy(id => buyerByRfq[id])
                .Select(g => new SupplierDashboardBreakdownDto { Key = g.Key, Label = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(Common.DASHBOARD_MAX_BREAKDOWN_ROWS)
                .ToList();

            // ---- Closing schedule for open invitations
            var today = now.Date;
            var open = invitations.Where(x => IsOpen(x.Id)).ToList();
            result.ClosingSchedule = Common.DASHBOARD_CLOSING_WINDOWS
                .Select(w =>
                {
                    var matching = open.Where(x =>
                    {
                        int days = (int)(x.EndDate.Date - today).TotalDays;
                        return days >= w.FromDay && days <= w.ToDay;
                    }).ToList();
                    return new SupplierDashboardBreakdownDto
                    {
                        Key = w.Key,
                        Label = w.Label,
                        Count = matching.Count,
                        PendingCount = matching.Count(x => !quotedRfqIds.Contains(x.Id)),
                    };
                })
                .ToList();

            // ---- Soonest deadlines
            result.UpcomingDeadlines = open
                .OrderBy(x => x.EndDate)
                .Take(Common.DASHBOARD_UPCOMING_DEADLINES)
                .Select(x => new SupplierUpcomingDeadlineDto
                {
                    SupplierRfqId = x.Id,
                    BuyerRfqId = x.BuyerRFQId,
                    RfqNumber = x.RFQNumber,
                    Title = x.Title,
                    BuyerName = buyerByRfq[x.Id],
                    EndDate = x.EndDate,
                    QuotationSubmitted = quotedRfqIds.Contains(x.Id),
                })
                .ToList();

            _logger.LogInfo(
                $"Supplier dashboard analytics built for SupplierId: {supplier.Id}. Invitations: {invitations.Count}, Won: {wonRfqIds.Count}");

            return result;
        }
    }
}
