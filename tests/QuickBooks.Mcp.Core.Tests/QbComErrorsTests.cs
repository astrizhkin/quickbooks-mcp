using System.Runtime.InteropServices;
using QuickBooks.Mcp.Core.Session;

namespace QuickBooks.Mcp.Core.Tests;

public class QbComErrorsTests
{
    [Theory]
    [InlineData(0x8004041A, "--authorize")]
    [InlineData(0x80040414, "dialog")]
    [InlineData(0x80040154, "not registered")]
    [InlineData(0x80040422, "single-user")]
    [InlineData(0x80040408, "Open QuickBooks")]
    public void Known_codes_get_advice(uint code, string expected) =>
        Assert.Contains(expected, QbComErrors.Explain(new COMException("raw", unchecked((int)code))));

    [Fact]
    public void Unknown_codes_keep_the_original_text_and_point_at_doctor()
    {
        var text = QbComErrors.Explain(new COMException("something odd", 0x12345));

        Assert.StartsWith("something odd", text);
        Assert.Contains("--doctor", text);
    }
}
