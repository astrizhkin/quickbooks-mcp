namespace QuickBooks.Mcp.Core.Reports;

/// <summary>One qbXML report request family and the report types it accepts.</summary>
/// <param name="Request">Request element, e.g. <c>AgingReportQueryRq</c>.</param>
/// <param name="TypeElement">Element that names the report, e.g. <c>AgingReportType</c>.</param>
/// <param name="Types">Report type values QuickBooks accepts.</param>
public sealed record ReportKind(string Request, string TypeElement, IReadOnlyList<string> Types)
{
    /// <summary>Every report family, keyed by what it is called in tool arguments.</summary>
    public static readonly IReadOnlyList<ReportKind> All =
    [
        new("AgingReportQueryRq", "AgingReportType",
            ["ARAgingSummary", "ARAgingDetail", "APAgingSummary", "APAgingDetail", "CollectionsReport"]),
        new("GeneralSummaryReportQueryRq", "GeneralSummaryReportType",
        [
            "BalanceSheetByClass", "BalanceSheetPrevYearComp", "BalanceSheetStandard", "BalanceSheetSummary",
            "CustomerBalanceSummary", "ExpenseByVendorSummary", "IncomeByCustomerSummary", "InventoryStockStatusByItem",
            "InventoryStockStatusByVendor", "IncomeTaxSummary", "InventoryValuationSummary",
            "InventoryValuationSummaryBySite", "LotNumberInStockBySite", "PhysicalInventoryWorksheet",
            "ProfitAndLossByClass", "ProfitAndLossByJob", "ProfitAndLossPrevYearComp", "ProfitAndLossStandard",
            "ProfitAndLossYTDComp", "PurchaseByItemSummary", "PurchaseByVendorSummary", "SalesByCustomerSummary",
            "SalesByItemSummary", "SalesByRepSummary", "SalesTaxLiability", "SalesTaxRevenueSummary",
            "SerialNumberInStockBySite", "TrialBalance", "VendorBalanceSummary",
        ]),
        new("GeneralDetailReportQueryRq", "GeneralDetailReportType",
        [
            "1099Detail", "AuditTrail", "BalanceSheetDetail", "CheckDetail", "CustomerBalanceDetail", "DepositDetail",
            "EstimatesByJob", "ExpenseByVendorDetail", "GeneralLedger", "IncomeByCustomerDetail", "IncomeTaxDetail",
            "InventoryValuationDetail", "JobProgressInvoicesVsEstimates", "Journal", "MissingChecks", "OpenInvoices",
            "OpenPOs", "OpenPOsByJob", "OpenSalesOrderByCustomer", "OpenSalesOrderByItem", "PendingSales",
            "ProfitAndLossDetail", "PurchaseByItemDetail", "PurchaseByVendorDetail", "SalesByCustomerDetail",
            "SalesByItemDetail", "SalesByRepDetail", "TxnDetailByAccount", "TxnListByCustomer", "TxnListByDate",
            "TxnListByVendor", "UnpaidBillsDetail", "UnbilledCostsByJob", "VendorBalanceDetail",
        ]),
        new("JobReportQueryRq", "JobReportType",
        [
            "ItemEstimatesVsActuals", "ItemProfitability", "JobEstimatesVsActualsDetail",
            "JobEstimatesVsActualsSummary", "JobProfitabilityDetail", "JobProfitabilitySummary",
        ]),
        new("TimeReportQueryRq", "TimeReportType", ["TimeByItem", "TimeByJobDetail", "TimeByJobSummary", "TimeByName"]),
        new("BudgetSummaryReportQueryRq", "BudgetSummaryReportType",
        [
            "BalanceSheetBudgetOverview", "BalanceSheetBudgetVsActual", "ProfitAndLossBudgetOverview",
            "ProfitAndLossBudgetPerformance", "ProfitAndLossBudgetVsActual",
        ]),
        new("CustomSummaryReportQueryRq", "CustomSummaryReportType", ["CustomSummary"]),
        new("CustomDetailReportQueryRq", "CustomDetailReportType", ["CustomTxnDetail"]),
    ];

    /// <summary>Payroll reports, kept apart from <see cref="All"/> because payroll has its own opt-in.</summary>
    public static readonly IReadOnlyList<ReportKind> Payroll =
    [
        new("PayrollSummaryReportQueryRq", "PayrollSummaryReportType", ["EmployeeEarningsSummary", "PayrollLiabilityBalances", "PayrollSummary"]),
        new("PayrollDetailReportQueryRq", "PayrollDetailReportType",
            ["EmployeeStateTaxesDetail", "PayrollItemDetail", "PayrollReviewDetail", "PayrollTransactionDetail", "PayrollTransactionsByPayee"]),
    ];

    /// <summary>Finds a report type by name (ignoring case, spaces, underscores and '&amp;') and its family.</summary>
    public static (ReportKind Kind, string Type)? Find(string name, IReadOnlyList<ReportKind>? kinds = null)
    {
        var wanted = Normalize(name);
        foreach (var kind in kinds ?? All)
        {
            var type = kind.Types.FirstOrDefault(t => Normalize(t) == wanted);
            if (type is not null) return (kind, type);
        }

        return null;
    }

    private static string Normalize(string s) =>
        s.Replace(" ", "").Replace("_", "").Replace("&", "And").ToUpperInvariant();
}
