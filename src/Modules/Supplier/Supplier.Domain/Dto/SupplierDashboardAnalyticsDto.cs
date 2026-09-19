namespace Supplier.Domain.Dto
{
    /// <summary>
    /// Aggregated figures for the supplier dashboard. RFQ invitations can be in any currency and
    /// the platform holds no exchange rates, so every money figure is reported per currency
    /// (<see cref="SupplierDashboardMoneyDto"/>) and never summed across currencies.
    /// </summary>
    public class SupplierDashboardAnalyticsDto
    {
        /// <summary>Currencies used by the supplier's invitations, most used first.</summary>
        public List<SupplierDashboardCurrencyDto> Currencies { get; set; } = new();

        public SupplierDashboardKpiDto Kpis { get; set; } = new();

        /// <summary>Last 12 calendar months, oldest first.</summary>
        public List<SupplierMonthlyTrendDto> MonthlyTrend { get; set; } = new();

        /// <summary>Invitations by stage (Upcoming, Open, Quoted, Frozen, Closed, Won, Not awarded).</summary>
        public List<SupplierDashboardBreakdownDto> StageBreakdown { get; set; } = new();

        /// <summary>Invited → Quoted → Won funnel.</summary>
        public List<SupplierDashboardBreakdownDto> Pipeline { get; set; } = new();

        /// <summary>Submitted quotation value per buyer.</summary>
        public List<SupplierDashboardBreakdownDto> QuotedValueByBuyer { get; set; } = new();

        /// <summary>Open invitations bucketed by how soon bidding closes.</summary>
        public List<SupplierDashboardBreakdownDto> ClosingSchedule { get; set; } = new();

        /// <summary>The open invitations closing soonest.</summary>
        public List<SupplierUpcomingDeadlineDto> UpcomingDeadlines { get; set; } = new();
    }

    /// <summary>An amount in a single currency.</summary>
    public class SupplierDashboardMoneyDto
    {
        public string Currency { get; set; } = string.Empty;
        public decimal Amount { get; set; }

        /// <summary>How many items (RFQs, contracts or quotations) make up the amount.</summary>
        public int Count { get; set; }
    }

    public class SupplierDashboardCurrencyDto
    {
        public string Code { get; set; } = string.Empty;
        public int RfqCount { get; set; }
    }

    public class SupplierDashboardKpiDto
    {
        public int Invitations { get; set; }
        public int OpenForBidding { get; set; }

        /// <summary>Open invitations closing within 7 days that have no submitted quotation yet.</summary>
        public int ActionRequired { get; set; }

        public int QuotationsSubmitted { get; set; }
        public int RfqsWon { get; set; }

        /// <summary>Won as a percentage of decided RFQs this supplier quoted on.</summary>
        public decimal WinRate { get; set; }

        /// <summary>Total of submitted quotations, per currency.</summary>
        public List<SupplierDashboardMoneyDto> QuotedValue { get; set; } = new();

        /// <summary>Quoted value of awarded line items, per currency.</summary>
        public List<SupplierDashboardMoneyDto> WonValue { get; set; } = new();
    }

    public class SupplierMonthlyTrendDto
    {
        /// <summary>Month in yyyy-MM format.</summary>
        public string Month { get; set; } = string.Empty;
        public int Invited { get; set; }
        public int Quoted { get; set; }
        public int Won { get; set; }

        /// <summary>Submitted quotation value that month, per currency.</summary>
        public List<SupplierDashboardMoneyDto> QuotedValue { get; set; } = new();
    }

    public class SupplierDashboardBreakdownDto
    {
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public int Count { get; set; }

        /// <summary>Items in this slice still waiting on the supplier (e.g. not yet quoted).</summary>
        public int PendingCount { get; set; }

        public List<SupplierDashboardMoneyDto> Values { get; set; } = new();
    }

    public class SupplierUpcomingDeadlineDto
    {
        public Guid SupplierRfqId { get; set; }
        public Guid BuyerRfqId { get; set; }
        public string RfqNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string BuyerName { get; set; } = string.Empty;
        public DateTime EndDate { get; set; }
        public bool QuotationSubmitted { get; set; }
    }
}
