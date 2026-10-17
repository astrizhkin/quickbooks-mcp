using QuickBooks.Mcp.Core.Session;

namespace QuickBooks.Mcp.Core.Tests;

public class QbOptionsTests
{
    [Fact]
    public void The_application_name_is_pinned_because_quickbooks_authorizes_by_name() =>
        Assert.Equal("QuickBooks MCP", new QbOptions().AppName);
}
