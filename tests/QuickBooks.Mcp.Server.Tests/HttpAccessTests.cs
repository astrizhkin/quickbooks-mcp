using QuickBooks.Mcp.Server.Http;

namespace QuickBooks.Mcp.Server.Tests;

public class HttpAccessTests
{
    private const string Key = "test-key-0123456789abcdef";

    [Theory]
    [InlineData("Bearer test-key-0123456789abcdef", null, true)]
    [InlineData("bearer test-key-0123456789abcdef", null, true)]
    [InlineData(null, "test-key-0123456789abcdef", true)]
    [InlineData("Bearer wrong", null, false)]
    [InlineData(null, "wrong", false)]
    [InlineData(null, null, false)]
    [InlineData("Basic test-key-0123456789abcdef", null, false)]
    public void The_key_is_accepted_as_a_bearer_token_or_an_api_key_header(string? authorization, string? apiKey, bool expected) =>
        Assert.Equal(expected, HttpAccess.IsAuthorized(authorization, apiKey, Key));

    [Fact]
    public void A_configured_key_wins()
    {
        Assert.Equal(Key, HttpAccess.ResolveKey(Key, Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt")));
    }

    [Fact]
    public void A_key_is_created_once_and_reused()
    {
        var dir = Path.Combine(Path.GetTempPath(), "qb-http-" + Guid.NewGuid());
        var file = Path.Combine(dir, "http-key.txt");
        try
        {
            var first = HttpAccess.ResolveKey(null, file);
            var second = HttpAccess.ResolveKey(null, file);

            Assert.True(first.Length >= 32);
            Assert.Equal(first, second);
            Assert.Equal(first, File.ReadAllText(file));
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }
}
