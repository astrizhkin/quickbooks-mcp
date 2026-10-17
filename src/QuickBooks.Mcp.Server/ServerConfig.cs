using System.Collections;
using System.Globalization;
using System.Text.Json;
using QuickBooks.Mcp.Core.Session;

namespace QuickBooks.Mcp.Server;

/// <summary>Raised for an invalid setting; the message is meant for the user.</summary>
public sealed class ConfigException(string message) : Exception(message);

/// <summary>
/// Server settings. Precedence: command-line args, then <c>QB_*</c> environment variables,
/// then the JSON config file (<c>--config path</c> or <c>QB_CONFIG</c>).
/// Names are the same everywhere: <c>COMPANY_FILE</c>, <c>--company-file</c>, <c>"company_file"</c>.
/// </summary>
public sealed class ServerConfig
{
    private static readonly string[] Keys =
        ["COMPANY_FILE", "MODE", "ALLOW_WRITES", "REDACT", "TIMEOUT_SECONDS", "MAX_RETRIES", "LOG_LEVEL"];

    /// <summary>Connection settings passed to the QuickBooks client.</summary>
    public required QbOptions Qb { get; init; }

    /// <summary>Whether write tools are registered.</summary>
    public bool AllowWrites { get; init; }

    /// <summary>Whether personal data is redacted from results.</summary>
    public bool Redact { get; init; }

    /// <summary>Minimum log level name (Trace..Critical).</summary>
    public string LogLevel { get; init; } = "Information";

    /// <summary>Loads and validates the settings.</summary>
    public static ServerConfig Load(string[] args, IDictionary environment)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var cli = ParseArgs(args);

        var env = (string key) => environment["QB_" + key] as string;
        var configPath = cli.GetValueOrDefault("CONFIG") ?? env("CONFIG");
        if (configPath is not null) ReadFile(configPath, values);
        foreach (var key in Keys)
        {
            if (env(key) is { Length: > 0 } fromEnv) values[key] = fromEnv;
            if (cli.TryGetValue(key, out var fromCli)) values[key] = fromCli;
        }

        var defaults = new QbOptions();
        return new ServerConfig
        {
            Qb = new QbOptions
            {
                CompanyFile = values.GetValueOrDefault("COMPANY_FILE"),
                Mode = values.TryGetValue("MODE", out var mode)
                    ? ParseEnum<QbFileMode>("MODE", mode)
                    : defaults.Mode,
                Timeout = TimeSpan.FromSeconds(ParseInt(values, "TIMEOUT_SECONDS", (int)defaults.Timeout.TotalSeconds, 1)),
                MaxRetries = ParseInt(values, "MAX_RETRIES", defaults.MaxRetries, 0),
            },
            AllowWrites = ParseBool(values, "ALLOW_WRITES"),
            Redact = ParseBool(values, "REDACT"),
            LogLevel = values.TryGetValue("LOG_LEVEL", out var level)
                ? ParseEnum<LogLevelName>("LOG_LEVEL", level).ToString()
                : "Information",
        };
    }

    // "--allow-writes" is a flag; "--mode multi" and "--mode=multi" carry a value.
    private static Dictionary<string, string> ParseArgs(string[] args)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < args.Length; i++)
        {
            if (!args[i].StartsWith("--", StringComparison.Ordinal)) continue;
            var (name, inline) = args[i][2..].Split('=', 2) is [var n, var v] ? (n, v) : (args[i][2..], null);
            var key = name.Replace('-', '_').ToUpperInvariant();
            result[key] = inline ?? (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal)
                ? args[++i]
                : "true");
        }

        return result;
    }

    private static void ReadFile(string path, Dictionary<string, string> values)
    {
        if (!File.Exists(path)) throw new ConfigException($"Config file not found: {path}");
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            foreach (var p in doc.RootElement.EnumerateObject())
                values[p.Name.ToUpperInvariant()] = p.Value.ToString();
        }
        catch (JsonException ex)
        {
            throw new ConfigException($"Config file {path} is not valid JSON: {ex.Message}");
        }
    }

    private static bool ParseBool(Dictionary<string, string> values, string key) =>
        !values.TryGetValue(key, out var text) ? false
        : bool.TryParse(text, out var b) ? b
        : throw new ConfigException($"{key} must be true or false, not '{text}'.");

    private static int ParseInt(Dictionary<string, string> values, string key, int fallback, int min) =>
        !values.TryGetValue(key, out var text) ? fallback
        : int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var n) && n >= min ? n
        : throw new ConfigException($"{key} must be a whole number >= {min}, not '{text}'.");

    private static T ParseEnum<T>(string key, string text) where T : struct, Enum =>
        Enum.TryParse<T>(text, ignoreCase: true, out var v) && Enum.IsDefined(v) ? v
        : throw new ConfigException($"{key} must be one of {string.Join(", ", Enum.GetNames<T>())}, not '{text}'.");
}

/// <summary>Accepted LOG_LEVEL values (same names as Microsoft.Extensions.Logging).</summary>
internal enum LogLevelName { Trace, Debug, Information, Warning, Error, Critical }
