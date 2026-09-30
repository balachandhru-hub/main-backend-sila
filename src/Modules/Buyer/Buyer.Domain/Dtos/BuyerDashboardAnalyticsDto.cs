namespace Buyer.Domain.Dto
{
    /// <summary>
    /// Aggregated counts for the buyer dashboard. Deliberately count-only: RFQs are raised in
    /// many currencies (and some without one), so money is not totalled on the dashboard.
    /// </summary>
    public class BuyerDashboardAnalyticsDto
    {
        public BuyerDashboardKpiDto Kpis { get; set; } = new();

        /// <summary>Last 12 calendar months, oldest first.</summary>
        public List<BuyerMonthlyTrendDto> MonthlyTrend { get; set; } = new();

        /// <summary>RFQs by lifecycle stage (Upcoming, Live, Frozen, Bidding closed, Awarded).</summary>
        public List<DashboardBreakdownDto> StatusBreakdown { get; set; } = new();

        /// <summary>RFQ count per department (department names, not ids), most first.</summary>
        public List<DashboardBreakdownDto> RfqsByDepartment { get; set; } = new();

        /// <summary>Contract count per supplier, most first.</summary>
        public List<DashboardBreakdownDto> ContractsBySupplier { get; set; } = new();

        /// <summary>Live RFQs bucketed by how soon they close.</summary>
        public List<DashboardBreakdownDto> ClosingSchedule { get; set; } = new();

        /// <summary>The live RFQs closing soonest.</summary>
        public List<BuyerUpcomingDeadlineDto> UpcomingDeadlines { get; set; } = new();
    }

    public class BuyerDashboardKpiDto
    {
        public int TotalRfqs { get; set; }
        public int LiveRfqs { get; set; }
        public int ClosingThisWeek { get; set; }

        /// <summary>RFQs whose bidding is frozen or has closed but that have not been awarded yet.</summary>
        public int AwaitingAward { get; set; }

        /// <summary>RFQs with status AWARDED (set when the buyer saves an award).</summary>
        public int AwardedRfqs { get; set; }

        /// <summary>Awarded RFQs as a percentage of RFQs whose bidding window has ended.</summary>
        public decimal AwardRate { get; set; }

        /// <summary>Distinct registered and external suppliers invited to the RFQs.</summary>
        public int SuppliersEngaged { get; set; }

        public int Contracts { get; set; }

        /// <summary>Contracts that are not rejected.</summary>
        public int ActiveContracts { get; set; }
    }

    public class BuyerMonthlyTrendDto
    {
        /// <summary>Month in yyyy-MM format.</summary>
        public string Month { get; set; } = string.Empty;
        public int Created { get; set; }

        /// <summary>Distinct RFQs that received an award record in the month.</summary>
        public int Awarded { get; set; }
    }

    /// <summary>A labelled slice of a breakdown and how many items it holds.</summary>
    public class DashboardBreakdownDto
    {
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class BuyerUpcomingDeadlineDto
    {
        public Guid RfqId { get; set; }
        public string RfqNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public DateTime EndDate { get; set; }
        public int InvitedSuppliers { get; set; }
    }
}
