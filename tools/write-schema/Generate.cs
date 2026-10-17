using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Xml;
using System.Xml.Schema;

var xsd = args[0];
var outPath = args[1];
var set = new XmlSchemaSet { XmlResolver = new XmlUrlResolver() };
set.Add(null, xsd);
set.Compile();

var types = new Dictionary<string, string>();      // structure signature -> id
var typeDefs = new Dictionary<string, object[][]>(); // id -> children
string TypeOf(XmlSchemaElement el)
{
    if (el.ElementSchemaType is not XmlSchemaComplexType ct) return "";
    var kids = Children(ct);
    if (kids.Count == 0) return "";
    var rows = kids.Select(k => new object[] { k.name, k.many ? 1 : 0, k.type }).ToArray();
    var sig = string.Join("|", rows.Select(r => $"{r[0]},{r[1]},{r[2]}"));
    if (!types.TryGetValue(sig, out var id)) { id = "t" + types.Count; types[sig] = id; typeDefs[id] = rows; }
    return id;
}

List<(string name, bool many, string type)> Children(XmlSchemaComplexType ct)
{
    var list = new List<(string, bool, string)>();
    void Walk(XmlSchemaParticle? p, bool repeat)
    {
        switch (p)
        {
            case XmlSchemaElement e:
                if (!list.Any(x => x.Item1 == e.QualifiedName.Name))
                    list.Add((e.QualifiedName.Name, repeat || e.MaxOccurs > 1, TypeOf(e)));
                break;
            case XmlSchemaGroupBase g:
                foreach (XmlSchemaParticle c in g.Items) Walk(c, repeat || g.MaxOccurs > 1);
                break;
        }
    }
    Walk(ct.ContentTypeParticle, false);
    return list;
}

var entities = new SortedDictionary<string, string>();
foreach (XmlSchemaElement el in set.GlobalElements.Values)
{
    var n = el.QualifiedName.Name;
    if ((n.EndsWith("Add") || n.EndsWith("Mod")) && set.GlobalTypes.Contains(new XmlQualifiedName(n + "RqType")))
        entities[n] = TypeOf(el);
}

List<string> Enum(string name)
{
    var el = (XmlSchemaElement)set.GlobalElements[new XmlQualifiedName(name)]!;
    return ((XmlSchemaSimpleType)el.ElementSchemaType!).Content is XmlSchemaSimpleTypeRestriction r
        ? r.Facets.OfType<XmlSchemaEnumerationFacet>().Select(f => f.Value!).ToList() : [];
}

var result = new
{
    entities,
    types = typeDefs.OrderBy(t => int.Parse(t.Key[1..])).ToDictionary(t => t.Key, t => t.Value),
    listDelTypes = Enum("ListDelType"),
    txnDelTypes = Enum("TxnDelType"),
    txnVoidTypes = Enum("TxnVoidType"),
};
File.WriteAllText(outPath, JsonSerializer.Serialize(result, new JsonSerializerOptions { TypeInfoResolver = new DefaultJsonTypeInfoResolver() }));
Console.WriteLine($"{entities.Count} entities, {typeDefs.Count} types, {new FileInfo(outPath).Length} bytes");
