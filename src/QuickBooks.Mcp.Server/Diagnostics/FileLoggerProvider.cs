using Microsoft.Extensions.Logging;

namespace QuickBooks.Mcp.Server.Diagnostics;

/// <summary>Appends log lines to a file, in addition to stderr. Used when QB_LOG_FILE is set.</summary>
public sealed class FileLoggerProvider(string path) : ILoggerProvider
{
    private readonly object _lock = new();

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName) => new FileLogger(this, categoryName);

    /// <inheritdoc />
    public void Dispose()
    {
    }

    private void Write(string line)
    {
        lock (_lock)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            File.AppendAllText(path, line + Environment.NewLine);
        }
    }

    private sealed class FileLogger(FileLoggerProvider provider, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;
            provider.Write($"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff} {logLevel,-11} {category}: {formatter(state, exception)}");
        }
    }
}
