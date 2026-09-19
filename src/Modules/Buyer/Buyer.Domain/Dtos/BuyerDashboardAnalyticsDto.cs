namespace Buyer.Domain.Dto
{
    /// <summary>
    /// Aggregated figures for the buyer dashboard. RFQs can be raised in any currency and the
    /// platform holds no exchange rates, so every money figure is reported per currency
    /// (<see cref="DashboardMoneyDto"/>) and never summed across currencies.
    /// </summary>
    public class BuyerDashboardAnalyticsDto
    {
        /// <summary>Currencies used by the buyer's RFQs, most used first.</summary>
        public List<DashboardCurrencyDto> Currencies { get; set; } = new();

        public BuyerDashboardKpiDto Kpis { get; set; } = new();

        /// <summary>Last 12 calendar months, oldest first.</summary>
        public List<BuyerMonthlyTrendDto> MonthlyTrend { get; set; } = new();

        /// <summary>RFQs by lifecycle stage (Upcoming, Live, Frozen, Bidding closed, Awarded).</summary>
        public List<DashboardBreakdownDto> StatusBreakdown { get; set; } = new();

        /// <summary>RFQ count and budget per department (department names, not ids).</summary>
        public List<DashboardBreakdownDto> BudgetByDepartment { get; set; } = new();

        /// <summary>Contract count and value per supplier.</summary>
        public List<DashboardBreakdownDto> ContractValueBySupplier { get; set; } = new();

        /// <summary>Live RFQs bucketed by how soon they close.</summary>
        public List<DashboardBreakdownDto> ClosingSchedule { get; set; } = new();

        /// <summary>The live RFQs closing soonest.</summary>
        public List<BuyerUpcomingDeadlineDto> UpcomingDeadlines { get; set; } = new();
    }

    /// <summary>An amount in a single currency.</summary>
    public class DashboardMoneyDto
    {
        public string Currency { get; set; } = string.Empty;
        public decimal Amount { get; set; }

        /// <summary>How many items (RFQs, contracts or quotations) make up the amount.</summary>
        public int Count { get; set; }
    }

    public class DashboardCurrencyDto
    {
        public string Code { get; set; } = string.Empty;
        public int RfqCount { get; set; }
    }

    public class BuyerDashboardKpiDto
    {
        public int TotalRfqs { get; set; }
        public int LiveRfqs { get; set; }
        public int ClosingThisWeek { get; set; }
        public int AwardedRfqs { get; set; }

        /// <summary>Awarded RFQs as a percentage of RFQs whose bidding window has ended.</summary>
        public decimal AwardRate { get; set; }

        /// <summary>Budget of live and upcoming RFQs, per currency.</summary>
        public List<DashboardMoneyDto> OpenBudget { get; set; } = new();

        /// <summary>Distinct registered and external suppliers invited to the RFQs.</summary>
        public int SuppliersEngaged { get; set; }

        public int ActiveContracts { get; set; }

        /// <summary>Total contract value, per currency (a contract takes its RFQ's currency).</summary>
        public List<DashboardMoneyDto> ContractValue { get; set; } = new();
    }

    public class BuyerMonthlyTrendDto
    {
        /// <summary>Month in yyyy-MM format.</summary>
        public string Month { get; set; } = string.Empty;
        public int Created { get; set; }
        public int Awarded { get; set; }

        /// <summary>Budget of RFQs created that month, per currency.</summary>
        public List<DashboardMoneyDto> Budget { get; set; } = new();
    }

    /// <summary>A labelled slice of a breakdown: how many items and their value per currency.</summary>
    public class DashboardBreakdownDto
    {
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public int Count { get; set; }
        public List<DashboardMoneyDto> Values { get; set; } = new();
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
