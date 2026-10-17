using QuickBooks.Mcp.Core.Session;

namespace QuickBooks.Mcp.Core.Tests;

public class ElevationTests
{
    [Fact]
    public void Reading_our_own_token_agrees_with_the_current_process_check() =>
        Assert.Equal(Elevation.IsCurrentProcessElevated(), Elevation.IsProcessElevated(Environment.ProcessId));

    [Fact]
    public void A_process_that_does_not_exist_is_unknown() =>
        Assert.Null(Elevation.IsProcessElevated(int.MaxValue));

    [Theory]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(false, false, false)]
    public void A_mismatch_is_reported_in_plain_words(bool server, bool quickBooks, bool mismatch)
    {
        var text = Elevation.Describe(server, [new Elevation.QuickBooksProcess(1, quickBooks)]);

        Assert.Equal(mismatch, text is not null);
        if (text is not null) Assert.Contains("administrator", text);
    }

    [Fact]
    public void An_uninspectable_quickbooks_is_assumed_elevated_when_we_are_not() =>
        Assert.Contains("QuickBooks is running as administrator", Elevation.Describe(false, [new Elevation.QuickBooksProcess(1, null)]));

    [Fact]
    public void An_uninspectable_quickbooks_is_assumed_matching_when_we_are_elevated() =>
        Assert.Null(Elevation.Describe(true, [new Elevation.QuickBooksProcess(1, null)]));

    [Fact]
    public void No_quickbooks_means_no_warning() =>
        Assert.Null(Elevation.Describe(true, []));
}
