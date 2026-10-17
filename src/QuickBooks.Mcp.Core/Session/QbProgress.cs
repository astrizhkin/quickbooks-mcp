namespace QuickBooks.Mcp.Core.Session;

/// <summary>
/// Lets the layers that wait on QuickBooks (the file lock, starting QuickBooks, the request itself) say what they are
/// doing, so the host can pass it on as progress. Flows with the current call; does nothing when no one is listening.
/// </summary>
public static class QbProgress
{
    private static readonly AsyncLocal<Action<string>?> Listener = new();

    /// <summary>Sends <paramref name="message"/> to whoever is listening for the current call.</summary>
    public static void Report(string message) => Listener.Value?.Invoke(message);

    /// <summary>Listens for the current call and everything it starts until the returned object is disposed.</summary>
    public static IDisposable Listen(Action<string> listener)
    {
        var previous = Listener.Value;
        Listener.Value = listener;
        return new Scope(previous);
    }

    private sealed class Scope(Action<string>? previous) : IDisposable
    {
        public void Dispose() => Listener.Value = previous;
    }
}
