using System.Xml;
using System.Xml.Schema;

namespace QuickBooks.Mcp.TestSupport;

/// <summary>
/// Validates requests against Intuit's qbXML schema, when the SDK is installed on the machine
/// (set <c>QBXML_XSD</c> to its path, or install the SDK in the default location). The schema is
/// never copied into this repository; tests that need it skip themselves otherwise.
/// </summary>
public static class QbXmlSchema
{
    private const string DefaultPath = @"C:\Program Files\Intuit\IDN\Common\tools\validator\qbxmlops170.xsd";
    private static readonly Lazy<XmlSchemaSet?> Schemas = new(Load);

    /// <summary>The schema's qbXML version, which requests must declare.</summary>
    public const string Version = "17.0";

    /// <summary>True when the schema file is available.</summary>
    public static bool Available => Schemas.Value is not null;

    /// <summary>Returns the validation errors for <paramref name="requestXml"/>; empty means valid.</summary>
    public static IReadOnlyList<string> Validate(string requestXml)
    {
        var errors = new List<string>();
        var settings = new XmlReaderSettings { Schemas = Schemas.Value!, ValidationType = ValidationType.Schema, DtdProcessing = DtdProcessing.Prohibit };
        settings.ValidationEventHandler += (_, e) => errors.Add(e.Message);
        using var reader = XmlReader.Create(new StringReader(requestXml), settings);
        while (reader.Read()) { }
        return errors;
    }

    private static XmlSchemaSet? Load()
    {
        var path = Environment.GetEnvironmentVariable("QBXML_XSD") ?? DefaultPath;
        if (!File.Exists(path)) return null;
        // The schema includes its sibling files by relative path, so it must be added by location.
        var set = new XmlSchemaSet { XmlResolver = new XmlUrlResolver() };
        set.Add(null, path);
        set.Compile();
        return set;
    }
}
