using System.Runtime.InteropServices;
using QuickBooks.Mcp.Core.QbXml;

namespace QuickBooks.Mcp.Core.Session;

/// <summary>
/// Talks to the installed QuickBooks through late-bound COM (<c>QBXMLRP2.RequestProcessor</c>),
/// so no interop assembly is shipped.
/// </summary>
public sealed class ComQbClient(QbOptions options) : IQbClient
{
    private const string ProgId = "QBXMLRP2.RequestProcessor";
    private const int LocalQbd = 1;

    /// <inheritdoc />
    public Task<string> SendAsync(Func<string, string> buildRequest, CancellationToken cancellationToken = default)
    {
        // COM objects here need an STA thread, so each call gets its own.
        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                tcs.SetResult(Run(buildRequest));
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        })
        { IsBackground = true, Name = "qb-com" };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return tcs.Task;
    }

    private string Run(Func<string, string> buildRequest)
    {
        var type = Type.GetTypeFromProgID(ProgId)
            ?? throw new InvalidOperationException(
                $"{ProgId} is not registered. Install QuickBooks Desktop; this process is " +
                $"{(Environment.Is64BitProcess ? "64" : "32")}-bit and must match the QuickBooks SDK components.");

        dynamic rp = Activator.CreateInstance(type)!;
        string? ticket = null;
        var connected = false;
        try
        {
            QbProgress.Report("Opening the QuickBooks connection (this can take a while if QuickBooks has to start)...");
            rp.OpenConnection2("", options.AppName, LocalQbd);
            connected = true;
            ticket = (string)rp.BeginSession(options.CompanyFile ?? "", (int)options.Mode);

            var versions = ((object[])rp.QBXMLVersionsForSession(ticket)).Cast<string>();
            return (string)rp.ProcessRequest(ticket, buildRequest(QbXmlRequest.PickVersion(versions)));
        }
        finally
        {
            if (ticket is not null) rp.EndSession(ticket);
            if (connected) rp.CloseConnection();
            Marshal.FinalReleaseComObject(rp);
        }
    }
}
