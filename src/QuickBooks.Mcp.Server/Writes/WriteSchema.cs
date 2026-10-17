using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;

namespace QuickBooks.Mcp.Server.Writes;

/// <summary>
/// Knows the element order of every qbXML Add and Mod request (generated into
/// <c>write-schema.json</c>, see tools/write-schema) and turns a JSON object into correctly ordered qbXML.
/// </summary>
public sealed class WriteSchema
{
    private sealed record Field(string Name, bool Many, string Type);

    private readonly Dictionary<string, string> _entities = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Field[]> _types = [];
    private readonly Dictionary<string, string> _since = [];

    /// <summary>
    /// Card data is never accepted: no card numbers and no processing info (PCI scope, see docs/design/credit-card-processing.md).
    /// </summary>
    private static bool IsCardData(string name) =>
        name.StartsWith("CreditCardTxn", StringComparison.Ordinal) || name is "CreditCardInfo" or "CreditCardNumber";

    /// <summary>The embedded schema.</summary>
    public static WriteSchema Instance { get; } = Load();

    /// <summary>List item types ListDel accepts (e.g. Customer).</summary>
    public IReadOnlyList<string> ListDelTypes { get; private init; } = [];

    /// <summary>Transaction types TxnDel accepts (e.g. Invoice).</summary>
    public IReadOnlyList<string> TxnDelTypes { get; private init; } = [];

    /// <summary>List types the ListDisplay request can open in the QuickBooks window.</summary>
    public IReadOnlyList<string> ListDisplayTypes { get; private init; } = [];

    /// <summary>Transaction types the TxnDisplay request can open in the QuickBooks window.</summary>
    public IReadOnlyList<string> TxnDisplayTypes { get; private init; } = [];

    /// <summary>Transaction types TxnVoid accepts.</summary>
    public IReadOnlyList<string> TxnVoidTypes { get; private init; } = [];

    /// <summary>True if <c>{entity}{suffix}</c> exists, e.g. ("Customer", "Mod").</summary>
    public bool Has(string entity, string suffix) => _entities.ContainsKey(entity + suffix);

    /// <summary>
    /// Builds the <c>{entity}Add</c> or <c>{entity}Mod</c> element from <paramref name="data"/>.
    /// Keys are element names in any order; unknown keys are refused with the valid ones listed.
    /// Nested objects are aggregates (addresses, lines), arrays are repeated elements.
    /// </summary>
    public XElement Build(string entity, string suffix, JsonObject data) => Build(entity, suffix, data, null);

    /// <summary>As <see cref="Build(string, string, JsonObject)"/>, also listing the fields used that need a newer qbXML version.</summary>
    public XElement Build(string entity, string suffix, JsonObject data, List<(string Path, string Since)>? needs)
    {
        if (!_entities.TryGetValue(entity + suffix, out var typeId))
            throw new ArgumentException($"{entity} does not support {(suffix == "Add" ? "create" : "update")}.");
        var name = _entities.Keys.First(k => k.Equals(entity + suffix, StringComparison.OrdinalIgnoreCase));
        if (needs is not null && _since.TryGetValue(name, out var rootSince)) needs.Add((name, rootSince));
        return new XElement(name, Content(typeId, data, name, needs));
    }

    /// <summary>
    /// The field names of <c>{entity}{suffix}</c> as a tree for the model: a plain value is "", a repeated element is "[...]",
    /// and "12.0+" (or "_needs" inside a group) marks something that needs that qbXML version or newer.
    /// </summary>
    public JsonObject Describe(string entity, string suffix)
    {
        if (!_entities.TryGetValue(entity + suffix, out var typeId))
            throw new ArgumentException($"{entity} does not support {(suffix == "Add" ? "create" : "update")}.");
        var root = _entities.Keys.First(k => k.Equals(entity + suffix, StringComparison.OrdinalIgnoreCase));
        return Describe(typeId, 0, root);
    }

    private JsonObject Describe(string typeId, int depth, string path)
    {
        var obj = new JsonObject();
        foreach (var f in _types[typeId].Where(f => !IsCardData(f.Name)))
        {
            var childPath = $"{path}/{f.Name}";
            _since.TryGetValue(childPath, out var since);
            JsonNode node = f.Type == "" ? (since is null ? "" : $"{since}+") : depth >= 3 ? "{...}" : Describe(f.Type, depth + 1, childPath);
            if (f.Type != "" && since is not null && node is JsonObject group) group["_needs"] = $"qbXML {since}+";
            obj[f.Name] = f.Many ? new JsonArray(node) : node;
        }

        return obj;
    }

    private IEnumerable<XElement> Content(string typeId, JsonObject data, string path, List<(string Path, string Since)>? needs)
    {
        var fields = _types[typeId].Where(f => !IsCardData(f.Name)).ToArray();
        foreach (var key in data.Select(p => p.Key).Where(IsCardData))
            throw new ArgumentException($"{key}: card data is not accepted by this server. Record card payments as ordinary payments without card details.");

        foreach (var key in data.Select(p => p.Key))
            if (!fields.Any(f => f.Name.Equals(key, StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException($"'{key}' is not a field of {path}. Valid fields: {string.Join(", ", fields.Select(f => f.Name))}.");

        foreach (var field in fields)
        {
            var match = data.FirstOrDefault(p => p.Key.Equals(field.Name, StringComparison.OrdinalIgnoreCase));
            if (match.Value is null) continue;
            if (needs is not null && _since.TryGetValue($"{path}/{field.Name}", out var since)) needs.Add(($"{path}/{field.Name}", since));

            var items = match.Value is JsonArray array ? array.ToList() : [match.Value];
            if (items.Count > 1 && !field.Many) throw new ArgumentException($"{path}/{field.Name} takes a single value, not a list.");
            foreach (var item in items)
            {
                if (item is null) continue;
                yield return field.Type == ""
                    ? new XElement(field.Name, Scalar(item, $"{path}/{field.Name}"))
                    : new XElement(field.Name, Content(field.Type, item as JsonObject
                        ?? throw new ArgumentException($"{path}/{field.Name} must be an object."), $"{path}/{field.Name}", needs));
            }
        }
    }

    private static string Scalar(JsonNode node, string path) => node switch
    {
        JsonValue v when v.TryGetValue<bool>(out var b) => b ? "true" : "false",
        JsonValue v when v.TryGetValue<decimal>(out var d) => d.ToString(CultureInfo.InvariantCulture),
        JsonValue v when v.TryGetValue<string>(out var s) => s,
        _ => throw new ArgumentException($"{path} must be a text, number or true/false value."),
    };

    private static WriteSchema Load()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("write-schema.json")!;
        var root = JsonNode.Parse(stream)!;
        var schema = new WriteSchema
        {
            ListDelTypes = Strings(root["listDelTypes"]),
            TxnDelTypes = Strings(root["txnDelTypes"]),
            TxnVoidTypes = Strings(root["txnVoidTypes"]),
            ListDisplayTypes = Strings(root["listDisplayTypes"]),
            TxnDisplayTypes = Strings(root["txnDisplayTypes"]),
        };
        foreach (var (name, id) in root["entities"]!.AsObject()) schema._entities[name] = (string)id!;
        foreach (var (path, since) in root["since"]!.AsObject()) schema._since[path] = (string)since!;
        foreach (var (id, fields) in root["types"]!.AsObject())
            schema._types[id] = fields!.AsArray().Select(f => new Field((string)f![0]!, (int)f[1]! == 1, (string)f[2]!)).ToArray();
        return schema;
    }

    private static string[] Strings(JsonNode? node) => node!.AsArray().Select(n => (string)n!).ToArray();
}
