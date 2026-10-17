# write-schema

Generates two small data files the server embeds. They contain element names and qbXML version numbers only:

- `src/QuickBooks.Mcp.Server/Writes/write-schema.json`: for every `...Add` and `...Mod` request, the element names in the order qbXML requires (QuickBooks rejects other orders), the valid list/transaction delete and void types, and the qbXML version each field first appeared in.
- `src/QuickBooks.Mcp.Core/QbXml/qbxml-request-versions.json`: the qbXML version each request first appeared in.

The server uses them to turn a JSON object into correctly ordered qbXML, to reject unknown field names with a useful message, and to tell an older QuickBooks which fields are too new.

Regenerate them from a QuickBooks SDK installation (the SDK schema files are not part of this repository):

```
dotnet run tools/write-schema/Generate.cs -- "C:\Program Files\Intuit\IDN\Common\tools\validator" src/QuickBooks.Mcp.Server/Writes/write-schema.json src/QuickBooks.Mcp.Core/QbXml/qbxml-request-versions.json
```
