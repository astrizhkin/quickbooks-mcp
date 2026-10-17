namespace QuickBooks.Mcp.Core.QbXml;

/// <summary>A QuickBooks response with an error status code.</summary>
public sealed class QbException(int statusCode, string? statusMessage)
    : Exception($"QuickBooks error {statusCode}: {statusMessage}")
{
    /// <summary>The qbXML statusCode attribute.</summary>
    public int StatusCode { get; } = statusCode;

    /// <summary>The qbXML statusMessage attribute.</summary>
    public string? StatusMessage { get; } = statusMessage;
}
