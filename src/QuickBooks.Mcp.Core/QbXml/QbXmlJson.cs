using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;

namespace QuickBooks.Mcp.Core.QbXml;

/// <summary>Turns qbXML records into JSON for tool results.</summary>
public static class QbXmlJson
{
    /// <summary>
    /// Converts a record: leaf elements become strings, repeated child names become arrays,
    /// attributes are ignored.
    /// </summary>
    public static JsonNode ToNode(XElement element)
    {
        if (!element.HasElements) return JsonValue.Create(element.Value)!;

        var obj = new JsonObject();
        foreach (var group in element.Elements().GroupBy(e => e.Name.LocalName))
        {
            var items = group.Select(ToNode).ToList();
            obj[group.Key] = group.Count() > 1 ? new JsonArray(items.ToArray()) : items[0];
        }

        return obj;
    }

    /// <summary>Converts records to an indented JSON array.</summary>
    public static string ToJson(IEnumerable<XElement> records) =>
        new JsonArray(records.Select(ToNode).ToArray()).ToJsonString(new JsonSerializerOptions { WriteIndented = true });
}
