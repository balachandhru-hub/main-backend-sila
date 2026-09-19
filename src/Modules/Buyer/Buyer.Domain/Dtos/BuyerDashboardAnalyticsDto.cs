namespace Buyer.Domain.Dto
{
    /// <summary>
    /// Aggregated figures for the buyer dashboard. Money values are in <see cref="Currency"/>,
    /// the currency used by most of the buyer's RFQs; amounts in other currencies are excluded
    /// from money totals rather than summed across currencies.
    /// </summary>
    public class BuyerDashboardAnalyticsDto
    {
        public string Currency { get; set; } = string.Empty;

        public BuyerDashboardKpiDto Kpis { get; set; } = new();

        /// <summary>Last 12 calendar months, oldest first.</summary>
        public List<BuyerMonthlyTrendDto> MonthlyTrend { get; set; } = new();

        /// <summary>RFQs by lifecycle stage (Live, Upcoming, Bidding closed, Frozen, Awarded).</summary>
        public List<DashboardBreakdownDto> StatusBreakdown { get; set; } = new();

        /// <summary>RFQ budget per department, largest first (top 8).</summary>
        public List<DashboardBreakdownDto> BudgetByDepartment { get; set; } = new();

        /// <summary>Contract value per supplier, largest first (top 6).</summary>
        public List<DashboardBreakdownDto> ContractValueBySupplier { get; set; } = new();

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
        public int AwardedRfqs { get; set; }

        /// <summary>Awarded RFQs as a percentage of RFQs whose bidding window has ended.</summary>
        public decimal AwardRate { get; set; }

        /// <summary>Budget of RFQs that are live or upcoming, in the dashboard currency.</summary>
        public decimal OpenBudget { get; set; }

        /// <summary>Distinct registered and external suppliers invited to the RFQs.</summary>
        public int SuppliersEngaged { get; set; }

        public int ActiveContracts { get; set; }

        /// <summary>Total value of contracts, in the dashboard currency.</summary>
        public decimal ContractValue { get; set; }
    }

    public class BuyerMonthlyTrendDto
    {
        /// <summary>Month in yyyy-MM format.</summary>
        public string Month { get; set; } = string.Empty;
        public int Created { get; set; }
        public int Awarded { get; set; }

        /// <summary>Budget of RFQs created that month, in the dashboard currency.</summary>
        public decimal Budget { get; set; }
    }

    /// <summary>A labelled slice of a breakdown: how many items and, where relevant, their value.</summary>
    public class DashboardBreakdownDto
    {
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Value { get; set; }
    }

    public class BuyerUpcomingDeadlineDto
    {
        public Guid RfqId { get; set; }
        public string RfqNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public DateTime EndDate { get; set; }
        public int InvitedSuppliers { get; set; }
        public decimal Budget { get; set; }
        public string Currency { get; set; } = string.Empty;
    }
}
