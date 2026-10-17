using System.Globalization;
using System.Text.Json.Nodes;
using System.Xml.Linq;

namespace QuickBooks.Mcp.Core.Reports;

/// <summary>Turns a qbXML <c>ReportRet</c> into a compact table.</summary>
public static class ReportParser
{
    /// <summary>
    /// Returns <c>{ title, subtitle, basis, columns: [{ id, title, type }], rows: [{ kind, cells }] }</c>.
    /// Row kinds are data, subtotal, total and text; <c>cells</c> lines up with <c>columns</c>.
    /// </summary>
    public static JsonObject Parse(XElement report)
    {
        static string DataTypeName(string? qb) => qb switch
        {
            "AMTTYPE" => "amount",
            "PERCENTTYPE" => "percent",
            "QUANTYPE" => "quantity",
            "DATETYPE" => "date",
            "PRICETYPE" => "price",
            "BOOLTYPE" => "boolean",
            _ => "text",
        };

        var columns = report.Elements("ColDesc").Select(c => new
        {
            Id = (int)c.Attribute("colID")!,
            Title = string.Join(" ", c.Elements("ColTitle").Select(t => (string?)t.Attribute("value")).Where(v => !string.IsNullOrWhiteSpace(v))),
            Type = (string?)c.Element("ColType") ?? "",
            DataType = DataTypeName((string?)c.Attribute("dataType")),
        }).OrderBy(c => c.Id).ToList();

        var rows = new JsonArray();
        foreach (var row in report.Element("ReportData")?.Elements() ?? [])
        {
            var kind = row.Name.LocalName switch
            {
                "DataRow" => "data",
                "SubtotalRow" => "subtotal",
                "TotalRow" => "total",
                "TextRow" => "text",
                _ => null,
            };
            if (kind is null) continue;

            var cells = columns.ToDictionary(c => c.Id, _ => "");
            // The row label arrives as RowData (column 1) or, for text rows, as the row's own value.
            if (row.Name.LocalName == "TextRow") cells[columns.FirstOrDefault()?.Id ?? 1] = (string?)row.Attribute("value") ?? "";
            foreach (var d in row.Elements("RowData").Concat(row.Elements("ColData")))
            {
                var id = d.Name.LocalName == "RowData" ? columns.FirstOrDefault()?.Id ?? 1 : (int?)d.Attribute("colID") ?? 0;
                if (cells.ContainsKey(id)) cells[id] = (string?)d.Attribute("value") ?? "";
            }

            rows.Add(new JsonObject
            {
                ["kind"] = kind,
                ["cells"] = new JsonArray(columns.Select(c => (JsonNode)JsonValue.Create(cells[c.Id])!).ToArray()),
            });
        }

        return new JsonObject
        {
            ["title"] = (string?)report.Element("ReportTitle"),
            ["subtitle"] = (string?)report.Element("ReportSubtitle"),
            ["basis"] = (string?)report.Element("ReportBasis"),
            ["columns"] = new JsonArray(columns.Select(c => (JsonNode)new JsonObject
            {
                ["id"] = c.Id.ToString(CultureInfo.InvariantCulture),
                ["title"] = c.Title,
                ["type"] = c.Type,
                ["dataType"] = c.DataType,
            }).ToArray()),
            ["rows"] = rows,
        };
    }
}
