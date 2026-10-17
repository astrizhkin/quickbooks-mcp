using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using ModelContextProtocol;
using QuickBooks.Mcp.Core.QbXml;
using QuickBooks.Mcp.Core.Session;

namespace QuickBooks.Mcp.Server.Writes;

/// <summary>One change to send to QuickBooks, e.g. a <c>CustomerAddRq</c> with its children.</summary>
/// <param name="Op">The kind of change.</param>
/// <param name="Entity">The entity being changed, for the allowlist and the audit log, e.g. "Customer".</param>
/// <param name="Request">The request element name, e.g. <c>CustomerAddRq</c>.</param>
/// <param name="Children">The request's child elements.</param>
public sealed record WriteCall(WriteOp Op, string Entity, string Request, IReadOnlyList<XElement> Children);

/// <summary>
/// The single path every write tool takes: gate check, optional dry run, a two-step confirmation
/// for deletes and voids, the call itself, and the audit entry.
/// </summary>
public sealed class WriteExecutor(IQbClient qb, WriteGate gate, AuditLog audit, IdempotencyStore? idempotency = null)
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(5);
    private readonly ConcurrentDictionary<string, (string Hash, DateTimeOffset Expires)> _tokens = new();

    /// <summary>
    /// Runs <paramref name="call"/>. With <paramref name="dryRun"/> nothing is sent and the request is shown.
    /// Deletes and voids first return a preview and a token; calling again with that token performs them.
    /// A create with an <paramref name="idempotencyKey"/> already used for the same request returns the earlier result instead of creating again.
    /// </summary>
    public async Task<string> ExecuteAsync(
        WriteCall call, bool dryRun, string? confirmationToken, CancellationToken cancellationToken, string? idempotencyKey = null)
    {
        gate.Require(call.Op, call.Entity);

        // The preview uses the newest spec; the real call uses the version QuickBooks negotiates.
        var preview = QbXmlRequest.Build(QbXmlRequest.MaxSupportedVersion.ToString(2), call.Request, call.Children);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(preview)))[..16];

        if (dryRun)
            return new JsonObject { ["dry_run"] = true, ["request"] = preview }.ToJsonString();

        if (call.Op == WriteOp.Delete)
        {
            if (confirmationToken is null)
            {
                var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
                _tokens[token] = (hash, DateTimeOffset.UtcNow + TokenLifetime);
                audit.Append("previewed", call.Op, call.Entity, hash, []);
                return new JsonObject
                {
                    ["confirmation_required"] = true,
                    ["request"] = preview,
                    ["confirmation_token"] = token,
                    ["message"] = "Nothing was changed. Show this to the user and, if they agree, call again with confirmation_token.",
                }.ToJsonString();
            }

            if (!_tokens.TryRemove(confirmationToken, out var issued) || issued.Hash != hash || issued.Expires < DateTimeOffset.UtcNow)
                throw new McpException("The confirmation token is invalid, expired or for a different request. Request a new preview.");
        }

        if (idempotencyKey is not null && idempotency is not null && call.Op == WriteOp.Create
            && idempotency.Find(idempotencyKey, hash) is { } earlier)
        {
            audit.Append("duplicate", call.Op, call.Entity, hash, []);
            return new JsonObject { ["duplicate"] = true, ["message"] = "This idempotency key was already used; nothing was created.", ["result"] = JsonNode.Parse(earlier) }.ToJsonString();
        }

        try
        {
            var reply = await qb.SendAsync(v => QbXmlRequest.Build(v, call.Request, call.Children), cancellationToken);
            var response = QbXmlResponse.Parse(reply);
            var ids = response.Records.Select(r => (string?)r.Element("ListID") ?? (string?)r.Element("TxnID")).OfType<string>().ToList();
            audit.Append("ok", call.Op, call.Entity, hash, ids);
            var result = new JsonObject { ["status"] = "ok", ["records"] = JsonNode.Parse(QbXmlJson.ToJson(response.Records)) }.ToJsonString();
            if (idempotencyKey is not null && call.Op == WriteOp.Create) idempotency?.Add(idempotencyKey, hash, result);
            return result;
        }
        catch (Exception ex)
        {
            audit.Append("failed", call.Op, call.Entity, hash, [], ex.GetType().Name);
            throw;
        }
    }
}
