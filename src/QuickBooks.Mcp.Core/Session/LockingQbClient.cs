using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace QuickBooks.Mcp.Core.Session;

/// <summary>
/// Serializes calls to one company file across processes with a named mutex, retries
/// transient COM failures with backoff, and enforces a per-call timeout.
/// </summary>
public sealed class LockingQbClient(IQbClient inner, QbOptions options) : IQbClient
{
    private readonly string _mutexName = "Global\\quickbooks-mcp-" + Convert.ToHexString(
        SHA256.HashData(Encoding.UTF8.GetBytes((options.CompanyFile ?? "").Trim().ToUpperInvariant())))[..16];

    /// <inheritdoc />
    public Task<string> SendAsync(Func<string, string> buildRequest, CancellationToken cancellationToken = default)
    {
        var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(options.Timeout);
        var token = timeout.Token;

        // A mutex belongs to the thread that took it, so lock, call and release on one thread.
        return Task.Factory.StartNew(() =>
        {
            using (timeout)
            using (var mutex = new Mutex(false, _mutexName))
            {
                Acquire(mutex, token);
                try
                {
                    return SendWithRetries(buildRequest, token);
                }
                finally
                {
                    mutex.ReleaseMutex();
                }
            }
        }, token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    private static void Acquire(Mutex mutex, CancellationToken token)
    {
        while (true)
        {
            token.ThrowIfCancellationRequested();
            try
            {
                if (mutex.WaitOne(TimeSpan.FromMilliseconds(100))) return;
            }
            catch (AbandonedMutexException)
            {
                return; // the previous owner died; the lock is ours now
            }
        }
    }

    private string SendWithRetries(Func<string, string> buildRequest, CancellationToken token)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                return inner.SendAsync(buildRequest, token).GetAwaiter().GetResult();
            }
            catch (COMException) when (attempt < options.MaxRetries)
            {
                // QuickBooks answers with COM errors while busy or showing a dialog.
                token.WaitHandle.WaitOne(TimeSpan.FromMilliseconds(250 * Math.Pow(2, attempt)));
                token.ThrowIfCancellationRequested();
            }
        }
    }
}
