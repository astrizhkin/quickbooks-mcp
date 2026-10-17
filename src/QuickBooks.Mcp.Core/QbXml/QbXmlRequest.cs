using System.Xml.Linq;

namespace QuickBooks.Mcp.Core.QbXml;

/// <summary>Builds qbXML request envelopes.</summary>
public static class QbXmlRequest
{
    /// <summary>Newest qbXML spec this server knows how to talk.</summary>
    public static readonly Version MaxSupportedVersion = new(17, 0);

    /// <summary>
    /// Picks the highest spec version in <paramref name="installed"/> (as returned by
    /// QBXMLVersionsForSession) that is not newer than <see cref="MaxSupportedVersion"/>.
    /// </summary>
    public static string PickVersion(IEnumerable<string> installed) =>
        installed.Select(v => (Text: v, Parsed: Version.TryParse(v, out var p) ? p : null))
            .Where(v => v.Parsed is not null && v.Parsed <= MaxSupportedVersion)
            .OrderByDescending(v => v.Parsed)
            .FirstOrDefault().Text
        ?? throw new InvalidOperationException("No supported qbXML version is installed.");

    /// <summary>
    /// Builds one request, e.g. <c>CustomerQueryRq</c> with the given child elements in order.
    /// Pass <paramref name="iterator"/> ("Start", "Continue" or "Stop") to page large results.
    /// </summary>
    public static string Build(
        string version,
        string requestName,
        IEnumerable<XElement>? children = null,
        string? iterator = null,
        string? iteratorId = null,
        bool continueOnError = false)
    {
        var request = new XElement(requestName, children ?? []);
        if (iterator is not null) request.SetAttributeValue("iterator", iterator);
        if (iteratorId is not null) request.SetAttributeValue("iteratorID", iteratorId);
        request.SetAttributeValue("requestID", "1");

        var doc = new XDocument(
            new XDeclaration("1.0", null, null),
            new XProcessingInstruction("qbxml", $"version=\"{version}\""),
            new XElement("QBXML",
                new XElement("QBXMLMsgsRq",
                    new XAttribute("onError", continueOnError ? "continueOnError" : "stopOnError"),
                    request)));
        return doc.Declaration + Environment.NewLine + string.Concat(doc.Nodes());
    }
}
