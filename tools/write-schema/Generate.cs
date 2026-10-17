#:property TreatWarningsAsErrors=false
#:property RestorePackagesWithLockFile=false
#:property GenerateDocumentationFile=false
// Generates the two small data files the server embeds from a QuickBooks SDK installation:
//   write-schema.json         element order for every Add/Mod request, delete/void types, and the qbXML version each
//                             field first appeared in (so older QuickBooks can be told a field is too new)
//   qbxml-request-versions.json  the qbXML version each request first appeared in
// usage: dotnet run tools/write-schema/Generate.cs -- <dir with qbxmlops*.xsd> <write-schema.json> <qbxml-request-versions.json>
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Xml;
using System.Xml.Schema;

var dir = args[0];
var schemaOut = args[1];
var requestsOut = args[2];

// Oldest to newest. The file name is the version without the dot.
string[] versions = ["1.1", "2.0", "2.1", "3.0", "4.0", "4.1", "5.0", "6.0", "7.0", "8.0", "9.0", "10.0", "11.0", "12.0", "13.0", "14.0", "15.0", "16.0", "17.0"];

XmlSchemaSet Load(string version)
{
    var set = new XmlSchemaSet { XmlResolver = new XmlUrlResolver() };
    set.Add(null, Path.Combine(dir, $"qbxmlops{version.Replace(".", "")}.xsd"));
    set.Compile();
    return set;
}

var latest = Load(versions[^1]);

// ---- element order, shared by all entities (types are de-duplicated by structure) ----
var types = new Dictionary<string, string>();
var typeDefs = new Dictionary<string, object[][]>();

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

static bool IsEntity(XmlSchemaSet set, string n) =>
    (n.EndsWith("Add") || n.EndsWith("Mod")) && set.GlobalTypes.Contains(new XmlQualifiedName(n + "RqType"));

var entities = new SortedDictionary<string, string>();
foreach (XmlSchemaElement el in latest.GlobalElements.Values)
    if (IsEntity(latest, el.QualifiedName.Name)) entities[el.QualifiedName.Name] = TypeOf(el);

List<string> Enum(string name)
{
    var el = (XmlSchemaElement)latest.GlobalElements[new XmlQualifiedName(name)]!;
    return ((XmlSchemaSimpleType)el.ElementSchemaType!).Content is XmlSchemaSimpleTypeRestriction r
        ? r.Facets.OfType<XmlSchemaEnumerationFacet>().Select(f => f.Value!).ToList() : [];
}

// ---- first version each path appears in ----
List<string> ChildNames(XmlSchemaComplexType ct){    var list = new List<string>();    void Walk(XmlSchemaParticle? p)    {        switch (p)        {            case XmlSchemaElement e:                if (!list.Contains(e.QualifiedName.Name)) list.Add(e.QualifiedName.Name);                break;            case XmlSchemaGroupBase g:                foreach (XmlSchemaParticle c in g.Items) Walk(c);                break;        }    }    Walk(ct.ContentTypeParticle);    return list;}
void Paths(XmlSchemaComplexType ct, string prefix, HashSet<string> into, int depth)
{
    if (depth > 6) return;
    foreach (var name in ChildNames(ct))
    {
        var path = prefix + "/" + name;
        into.Add(path);
    }
    void Walk(XmlSchemaParticle? p)
    {
        switch (p)
        {
            case XmlSchemaElement e when e.ElementSchemaType is XmlSchemaComplexType child:
                Paths(child, prefix + "/" + e.QualifiedName.Name, into, depth + 1);
                break;
            case XmlSchemaGroupBase g:
                foreach (XmlSchemaParticle c in g.Items) Walk(c);
                break;
        }
    }
    Walk(ct.ContentTypeParticle);
}

var fieldSince = new SortedDictionary<string, string>();
var requestSince = new SortedDictionary<string, string>();
var seenFields = new Dictionary<string, string>();   // path -> first version
foreach (var version in versions)
{
    var set = Load(version);
    foreach (XmlSchemaElement el in set.GlobalElements.Values)
    {
        var n = el.QualifiedName.Name;
        if (IsEntity(set, n) && el.ElementSchemaType is XmlSchemaComplexType ct)
        {
            var paths = new HashSet<string> { n };
            Paths(ct, n, paths, 0);
            foreach (var p in paths) seenFields.TryAdd(p, version);
        }
    }

    // request elements are local to the message wrapper, so look for them in the wrapper's types
    foreach (XmlSchemaType t in set.GlobalTypes.Values)
        if (t.Name is { } tn && tn.EndsWith("RqType")) requestSince.TryAdd(tn[..^4], version);
}

// keep a path only if it is newer than its parent: a new entity records itself, not every field in it
foreach (var (path, version) in seenFields)
{
    var slash = path.LastIndexOf('/');
    var parentVersion = slash < 0 ? versions[0] : seenFields.GetValueOrDefault(path[..slash], versions[0]);
    if (Array.IndexOf(versions, version) > Array.IndexOf(versions, parentVersion)) fieldSince[path] = version;
}

var options = new JsonSerializerOptions { TypeInfoResolver = new DefaultJsonTypeInfoResolver() };
var schema = new
{
    entities,
    types = typeDefs.OrderBy(t => int.Parse(t.Key[1..])).ToDictionary(t => t.Key, t => t.Value),
    since = fieldSince.Where(kv => kv.Value != versions[0]).ToDictionary(kv => kv.Key, kv => kv.Value),
    listDelTypes = Enum("ListDelType"),
    txnDelTypes = Enum("TxnDelType"),
    txnVoidTypes = Enum("TxnVoidType"),
};
File.WriteAllText(schemaOut, JsonSerializer.Serialize(schema, options));
File.WriteAllText(requestsOut, JsonSerializer.Serialize(requestSince.Where(kv => kv.Value != versions[0]).ToDictionary(kv => kv.Key, kv => kv.Value), options));
Console.WriteLine($"{entities.Count} entities, {typeDefs.Count} types, {fieldSince.Count} versioned fields, {requestSince.Count} requests");
