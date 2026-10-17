using System.Xml.Linq;
using QuickBooks.Mcp.Core.QbXml;

namespace QuickBooks.Mcp.Core.Session;

/// <summary>Sends qbXML to QuickBooks. One call is one short session.</summary>
public interface IQbClient
{
    /// <summary>
    /// Opens a session, builds the request for the negotiated qbXML version, processes it
    /// and returns the raw reply.
    /// </summary>
    Task<string> SendAsync(Func<string, string> buildRequest, CancellationToken cancellationToken = default);
}

/// <summary>Convenience helpers over <see cref="IQbClient"/>.</summary>
public static class QbClientExtensions
{
    /// <summary>Sends one request and parses the reply; throws <see cref="QbException"/> on error.</summary>
    public static async Task<QbXmlResponse> SendAsync(
        this IQbClient client,
        string requestName,
        IEnumerable<XElement>? children = null,
        CancellationToken cancellationToken = default)
    {
        var reply = await client.SendAsync(v => QbXmlRequest.Build(v, requestName, children), cancellationToken);
        return QbXmlResponse.Parse(reply);
    }
}
