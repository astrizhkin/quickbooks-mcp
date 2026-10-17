# write-schema

`src/QuickBooks.Mcp.Server/Writes/write-schema.json` lists, for every `...Add` and `...Mod` request, the
element names in the order qbXML requires (QuickBooks rejects other orders), plus the valid list/transaction
delete and void types. The server uses it to turn a JSON object into correctly ordered qbXML and to reject
unknown field names with a useful message. It contains element names only.

Regenerate it from a QuickBooks SDK installation (the SDK schema file is not part of this repository):

```
dotnet run tools/write-schema/Generate.cs -- "C:\Program Files\Intuit\IDN\Common\tools\validator\qbxmlops170.xsd" src/QuickBooks.Mcp.Server/Writes/write-schema.json
```
