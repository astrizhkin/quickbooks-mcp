using QuickBooks.Mcp.Core.Query;
using QuickBooks.Mcp.Core.Reports;
using QuickBooks.Mcp.Core.QbXml;
using QuickBooks.Mcp.TestSupport;

namespace QuickBooks.Mcp.Core.Tests;

/// <summary>Skips unless Intuit's qbXML schema is installed (see <see cref="QbXmlSchema"/>).</summary>
public sealed class RequiresSchemaFactAttribute : FactAttribute
{
    public RequiresSchemaFactAttribute()
    {
        if (!QbXmlSchema.Available) Skip = "qbXML schema not found; install the QuickBooks SDK or set QBXML_XSD.";
    }
}

public class SchemaConformanceTests
{
    private static string Build(Entity entity, QueryFilters filters) =>
        QbXmlRequest.Build(QbXmlSchema.Version, entity.Request, filters.ToElements(entity, 51));

    [RequiresSchemaFact]
    public void Every_entity_accepts_a_plain_query()
    {
        var bad = Entity.All
            .Select(e => (e.Name, Errors: QbXmlSchema.Validate(Build(e, new QueryFilters()))))
            .Where(r => r.Errors.Count > 0)
            .Select(r => $"{r.Name}: {r.Errors[0]}").ToList();

        Assert.True(bad.Count == 0, string.Join(Environment.NewLine, bad));
    }

    [RequiresSchemaFact]
    public void Every_entity_accepts_its_typical_filters()
    {
        var bad = new List<string>();
        foreach (var e in Entity.All)
        {
            var filters = e.Unfilterable ? new QueryFilters { Fields = ["Name"] } : e.IsTransaction
                ? new QueryFilters
                {
                    TxnDateFrom = "2025-01-01",
                    TxnDateTo = "2025-12-31",
                    EntityName = e.HasEntityFilter ? "Acme" : null,
                    AccountName = e.HasAccountFilter ? "Checking" : null,
                    IncludeLineItems = e.HasLineItems,
                    IncludeLinkedTxns = e.HasLinkedTxns,
                }
                : new QueryFilters { NameContains = "Ac", ModifiedFrom = "2025-01-01", Active = e.HasActiveStatus ? "all" : null, Fields = ["Name"] };
            foreach (var error in QbXmlSchema.Validate(Build(e, filters))) bad.Add($"{e.Name}: {error}");
        }

        Assert.True(bad.Count == 0, string.Join(Environment.NewLine, bad));
    }

    [RequiresSchemaFact]
    public void Lookups_by_id_and_name_are_valid()
    {
        var bad = new List<string>();
        foreach (var e in Entity.All)
        {
            var filters = e.Unfilterable ? new QueryFilters() : new QueryFilters { Ids = ["1A-1"], IncludeLineItems = e.HasLineItems, IncludeLinkedTxns = e.HasLinkedTxns };
            foreach (var error in QbXmlSchema.Validate(Build(e, filters))) bad.Add($"{e.Name} ids: {error}");
            if (e.NameLookup is not null)
                foreach (var error in QbXmlSchema.Validate(Build(e, new QueryFilters { FullNames = ["A:B"] }))) bad.Add($"{e.Name} names: {error}");
        }

        Assert.True(bad.Count == 0, string.Join(Environment.NewLine, bad));
    }
}

public class ReportSchemaConformanceTests
{
    private static string Build(ReportKind kind, string type, ReportOptions options) =>
        QbXmlRequest.Build(QbXmlSchema.Version, kind.Request, options.ToElements(kind, type));

    [RequiresSchemaFact]
    public void Every_report_accepts_a_period_and_basis()
    {
        var bad = new List<string>();
        foreach (var kind in ReportKind.All)
            foreach (var type in kind.Types)
            {
                var options = kind.Request switch
                {
                    "AgingReportQueryRq" => new ReportOptions { DateMacro = "ThisMonth", AgingAsOf = "ReportEndDate", Entities = ["Acme"], IncludeColumns = ["Amount"] },
                    "GeneralSummaryReportQueryRq" => new ReportOptions { From = "2025-01-01", To = "2025-03-31", Basis = "Accrual", SummarizeColumnsBy = "Month", Accounts = ["Checking"], Classes = ["East"], PostingStatus = "Posting" },
                    "GeneralDetailReportQueryRq" => new ReportOptions { From = "2025-01-01", To = "2025-03-31", Basis = "Cash", Accounts = ["Checking"], IncludeColumns = ["Amount"], SummarizeRowsBy = "Account" },
                    "JobReportQueryRq" => new ReportOptions { DateMacro = "ThisYear", SummarizeColumnsBy = "Month", Entities = ["Acme"] },
                    "TimeReportQueryRq" => new ReportOptions { DateMacro = "ThisYear", SummarizeColumnsBy = "Month", Entities = ["Pat"], IncludeColumns = ["Amount"] },
                    "BudgetSummaryReportQueryRq" => new ReportOptions { FiscalYear = 2025, BudgetCriterion = "Accounts", DateMacro = "ThisYear", Classes = ["East"], SummarizeColumnsBy = "Class", SummarizeRowsBy = "Account" },
                    "CustomSummaryReportQueryRq" => new ReportOptions { DateMacro = "ThisYear", SummarizeColumnsBy = "Month", SummarizeRowsBy = "Account", Basis = "Accrual" },
                    _ => new ReportOptions { DateMacro = "ThisYear", SummarizeRowsBy = "Account", IncludeColumns = ["Amount"], Basis = "Accrual" },
                };
                foreach (var error in QbXmlSchema.Validate(Build(kind, type, options))) bad.Add($"{type}: {error}");
            }

        Assert.True(bad.Count == 0, string.Join(Environment.NewLine, bad));
    }
}
