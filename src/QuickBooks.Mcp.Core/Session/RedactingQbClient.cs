using System.Xml.Linq;

namespace QuickBooks.Mcp.Core.Session;

/// <summary>
/// Replaces sensitive values in every reply before any tool sees them: tax IDs, bank and card
/// numbers, and payroll details. Money and dates are left as QuickBooks sends them (decimal
/// strings and ISO 8601).
/// </summary>
public sealed class RedactingQbClient(IQbClient inner) : IQbClient
{
    /// <summary>The text that replaces a sensitive value.</summary>
    public const string Mask = "[redacted]";

    // shortcut: matches element names only; extend the list when new sensitive fields turn up.
    private static readonly HashSet<string> Sensitive = new(StringComparer.Ordinal)
    {
        "SSN", "SocialSecurityNumber", "VendorTaxIdent", "TaxIdent", "TaxRegistrationNumber",
        "BankNumber", "AccountNumber", "RoutingNumber", "CreditCardNumber", "CreditCardNumberLast4",
        "CardNumber", "DriversLicenseNumber", "PayrollInfo", "EmployeePayrollInfo",
    };

    /// <inheritdoc />
    public async Task<string> SendAsync(Func<string, string> buildRequest, CancellationToken cancellationToken = default)
    {
        var reply = await inner.SendAsync(buildRequest, cancellationToken);
        var doc = XDocument.Parse(reply);
        foreach (var element in doc.Descendants().Where(e => Sensitive.Contains(e.Name.LocalName)).ToList())
            element.ReplaceAll(Mask);
        return doc.Declaration is null ? doc.ToString(SaveOptions.DisableFormatting) : doc.Declaration + doc.ToString(SaveOptions.DisableFormatting);
    }
}
