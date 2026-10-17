using System.Xml.Linq;
using QuickBooks.Mcp.Core.Session;

namespace QuickBooks.Mcp.TestSupport;

/// <summary>
/// A stand-in for QuickBooks: answers each request with <c>Fixtures/{RequestName}.xml</c>
/// (e.g. <c>CustomerQueryRq</c> returns <c>CustomerQueryRs.xml</c>) and records what was sent.
/// </summary>
public sealed class FixtureQbClient : IQbClient
{
    /// <summary>Spec version handed to request builders.</summary>
    public const string Version = "16.0";

    private readonly string _directory = Path.Combine(AppContext.BaseDirectory, "Fixtures");

    /// <summary>Every request sent, in order.</summary>
    public List<XDocument> Requests { get; } = [];

    /// <inheritdoc />
    public Task<string> SendAsync(Func<string, string> buildRequest, CancellationToken cancellationToken = default)
    {
        var xml = buildRequest(Version);
        // Every request any tool sends is checked against Intuit's schema when it is installed.
        if (QbXmlSchema.Available && QbXmlSchema.Validate(xml) is { Count: > 0 } errors)
            throw new InvalidOperationException("Request does not match the qbXML schema: " + string.Join(" ", errors));

        var request = XDocument.Parse(xml);
        Requests.Add(request);

        var name = request.Root!.Element("QBXMLMsgsRq")!.Elements().First().Name.LocalName;
        var path = Path.Combine(_directory, name[..^2] + "Rs.xml");
        return File.Exists(path)
            ? Task.FromResult(File.ReadAllText(path))
            : throw new FileNotFoundException($"No fixture for {name}; add {path}.");
    }
}
