# Legal notes

## Not affiliated with Intuit
QuickBooks and Intuit are trademarks of Intuit Inc. This project is independent and is not affiliated with, endorsed by or sponsored by Intuit.

## Intuit SDK components
At runtime this server calls the QuickBooks SDK components that ship with an installed QuickBooks Desktop (`QBXMLRP2.RequestProcessor`). It does not bundle, copy or redistribute any Intuit binary. Intuit SDK files and sources are never committed to this repository.

Before 1.0 the Intuit SDK license terms must be reviewed against this project's distribution channels (tracked in the 1.0 checklist).

## qbXML
Requests and responses use the qbXML format defined by Intuit. Only element names needed for interoperability appear in this code; the SDK documentation is not reproduced.

## Related project
Intuit publishes an Apache-2.0 MCP server for QuickBooks Online. We learn from its design but write our own code. If code is ever copied, the license notice and attribution will be kept.
