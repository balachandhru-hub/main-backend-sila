namespace Supplier.Domain.Dto
{
    /// <summary>
    /// Aggregated figures for the supplier dashboard. Money values are in <see cref="Currency"/>,
    /// the currency used by most of the supplier's RFQ invitations; other currencies are excluded
    /// from money totals rather than summed across currencies.
    /// </summary>
    public class SupplierDashboardAnalyticsDto
    {
        public string Currency { get; set; } = string.Empty;

        public SupplierDashboardKpiDto Kpis { get; set; } = new();

        /// <summary>Last 12 calendar months, oldest first.</summary>
        public List<SupplierMonthlyTrendDto> MonthlyTrend { get; set; } = new();

        /// <summary>Invitations by stage (Upcoming, Open, Quoted, Frozen, Closed, Won, Not awarded).</summary>
        public List<SupplierDashboardBreakdownDto> StageBreakdown { get; set; } = new();

        /// <summary>Invited → Quoted → Won funnel.</summary>
        public List<SupplierDashboardBreakdownDto> Pipeline { get; set; } = new();

        /// <summary>Submitted quotation value per buyer, largest first (top 6).</summary>
        public List<SupplierDashboardBreakdownDto> QuotedValueByBuyer { get; set; } = new();

        /// <summary>Open invitations bucketed by how soon bidding closes.</summary>
        public List<SupplierDashboardBreakdownDto> ClosingSchedule { get; set; } = new();

        /// <summary>The open invitations closing soonest.</summary>
        public List<SupplierUpcomingDeadlineDto> UpcomingDeadlines { get; set; } = new();
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

        /// <summary>Total of submitted quotations, in the dashboard currency.</summary>
        public decimal QuotedValue { get; set; }

        /// <summary>Quoted value of awarded line items, in the dashboard currency.</summary>
        public decimal WonValue { get; set; }
    }

    public class SupplierMonthlyTrendDto
    {
        /// <summary>Month in yyyy-MM format.</summary>
        public string Month { get; set; } = string.Empty;
        public int Invited { get; set; }
        public int Quoted { get; set; }
        public int Won { get; set; }

        /// <summary>Submitted quotation value that month, in the dashboard currency.</summary>
        public decimal QuotedValue { get; set; }
    }

    public class SupplierDashboardBreakdownDto
    {
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Value { get; set; }
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
