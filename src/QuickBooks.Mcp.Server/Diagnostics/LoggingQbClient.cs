using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using QuickBooks.Mcp.Core.QbXml;
using QuickBooks.Mcp.Core.Session;

namespace QuickBooks.Mcp.Server.Diagnostics;

/// <summary>
/// Logs each QuickBooks call: which request, how long, and whether it worked. It never logs the request or the
/// reply, because those hold company data; error messages are logged only at debug level.
/// </summary>
public sealed class LoggingQbClient(IQbClient inner, ILogger<LoggingQbClient> logger) : IQbClient
{
    /// <inheritdoc />
    public async Task<string> SendAsync(Func<string, string> buildRequest, CancellationToken cancellationToken = default)
    {
        string request = "?";
        var clock = Stopwatch.StartNew();
        try
        {
            var reply = await inner.SendAsync(v =>
            {
                var xml = buildRequest(v);
                request = NameOf(xml);
                return xml;
            }, cancellationToken);
            logger.LogInformation("QuickBooks {Request} answered in {Elapsed} ms", request, clock.ElapsedMilliseconds);
            return reply;
        }
        catch (Exception ex)
        {
            logger.LogWarning("QuickBooks {Request} failed after {Elapsed} ms: {Kind}{Code}", request, clock.ElapsedMilliseconds, ex.GetType().Name,
                ex switch { COMException com => $" 0x{com.HResult:X8}", QbException qb => $" status {qb.StatusCode}", _ => "" });
            logger.LogDebug("QuickBooks {Request} error text: {Message}", request, ex.Message);
            throw;
        }
    }

    private static string NameOf(string xml)
    {
        try
        {
            return XDocument.Parse(xml).Root?.Element("QBXMLMsgsRq")?.Elements().FirstOrDefault()?.Name.LocalName ?? "?";
        }
        catch (System.Xml.XmlException)
        {
            return "?";
        }
    }
}
