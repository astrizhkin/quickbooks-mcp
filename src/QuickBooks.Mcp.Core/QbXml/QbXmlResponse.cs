using System.Globalization;
using System.Xml.Linq;

namespace QuickBooks.Mcp.Core.QbXml;

/// <summary>The single response element of a qbXML reply.</summary>
public sealed class QbXmlResponse
{
    private const int NoMatch = 1;

    /// <summary>The <c>...Rs</c> element, e.g. <c>CustomerQueryRs</c>.</summary>
    public required XElement Element { get; init; }

    /// <summary>The statusCode attribute (0 = ok, 1 = no match).</summary>
    public int StatusCode { get; init; }

    /// <summary>The iteratorID attribute, when an iterator was started.</summary>
    public string? IteratorId { get; init; }

    /// <summary>How many rows are left to fetch with iterator=Continue.</summary>
    public int? IteratorRemainingCount { get; init; }

    /// <summary>Child elements of the response (the returned records).</summary>
    public IEnumerable<XElement> Records => Element.Elements();

    /// <summary>
    /// Parses a reply. Throws <see cref="QbException"/> on error statuses;
    /// status 1 (no match) yields an empty response and warnings are accepted.
    /// </summary>
    public static QbXmlResponse Parse(string xml)
    {
        var root = XDocument.Parse(xml).Root;
        var rs = (root?.Element("QBXMLMsgsRs") ?? root?.Element("QBXMLSubscriptionMsgsRs"))?.Elements().FirstOrDefault()
            ?? throw new FormatException("Reply has no QBXMLMsgsRs response element.");

        var code = int.Parse(rs.Attribute("statusCode")?.Value ?? "0", CultureInfo.InvariantCulture);
        var severity = rs.Attribute("statusSeverity")?.Value;
        if (code != 0 && code != NoMatch && severity != "Warn" && severity != "Info")
            throw new QbException(code, rs.Attribute("statusMessage")?.Value);

        return new QbXmlResponse
        {
            Element = rs,
            StatusCode = code,
            IteratorId = rs.Attribute("iteratorID")?.Value,
            IteratorRemainingCount = int.TryParse(rs.Attribute("iteratorRemainingCount")?.Value,
                CultureInfo.InvariantCulture, out var n) ? n : null,
        };
    }
}
