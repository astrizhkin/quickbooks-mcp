using System.Text.RegularExpressions;
using QuickBooks.Mcp.Server.Cli;

namespace QuickBooks.Mcp.Server.Tests;

/// <summary>Enforces docs/TOOL-GUIDELINES.md so the tool list stays small and predictable.</summary>
public partial class ToolGuidelineTests
{
    private static readonly string[] Verbs = ["search", "query", "get", "create", "update", "delete", "void", "describe", "open"];

    /// <summary>The most tools the server may have with everything enabled. Raise it on purpose, in a pull request that says why.</summary>
    private const int MaxTools = 60;

    /// <summary>The most tools a default (read-only) install may show a model.</summary>
    private const int MaxDefaultTools = 40;

    [GeneratedRegex(@"(?<=[.!?])\s+(?=[A-Z])")]
    private static partial Regex Sentences();

    [Fact]
    public void Names_are_verb_first_snake_case()
    {
        foreach (var tool in ToolReference.AllTools())
        {
            var name = tool.ProtocolTool.Name;
            Assert.Matches("^[a-z]+(_[a-z]+)+$", name);
            Assert.Contains(name.Split('_')[0], Verbs);
        }
    }

    [Fact]
    public void Descriptions_are_one_or_two_sentences_and_short() =>
        Assert.All(ToolReference.AllTools(), tool =>
        {
            var description = tool.ProtocolTool.Description!;
            Assert.True(Sentences().Split(description.Trim()).Length <= 2, $"{tool.ProtocolTool.Name} has more than two sentences");
            Assert.True(description.Length <= 320, $"{tool.ProtocolTool.Name}'s description is {description.Length} characters; keep it under 320");
        });

    [Fact]
    public void Parameter_descriptions_stay_short() =>
        Assert.All(ToolReference.AllTools(), tool =>
        {
            foreach (var p in tool.ProtocolTool.InputSchema.GetProperty("properties").EnumerateObject())
            {
                var text = p.Value.GetProperty("description").GetString()!;
                Assert.True(text.Length <= 400, $"{tool.ProtocolTool.Name}.{p.Name}'s description is {text.Length} characters");
            }
        });

    [Fact]
    public void Tool_names_are_unique() =>
        Assert.Equal(ToolReference.AllTools().Count, ToolReference.AllTools().Select(t => t.ProtocolTool.Name).Distinct().Count());

    [Fact]
    public void The_tool_count_is_within_budget()
    {
        var all = ToolReference.AllTools();
        var defaults = all.Count(t => WriteGuard(t.ProtocolTool.Name) && !t.ProtocolTool.Name.Contains("payroll", StringComparison.Ordinal));

        Assert.True(all.Count <= MaxTools, $"{all.Count} tools exceeds the budget of {MaxTools}; see docs/TOOL-GUIDELINES.md");
        Assert.True(defaults <= MaxDefaultTools, $"{defaults} default tools exceeds the budget of {MaxDefaultTools}");
    }

    private static bool WriteGuard(string name) =>
        !(name.StartsWith("create_", StringComparison.Ordinal) || name.StartsWith("update_", StringComparison.Ordinal)
          || name.StartsWith("delete_", StringComparison.Ordinal) || name.StartsWith("void_", StringComparison.Ordinal)
          || name == "describe_write_fields");

    [Fact]
    public void Search_tools_page_the_same_way() =>
        Assert.All(ToolReference.AllTools().Where(t => t.ProtocolTool.Name.StartsWith("search_", StringComparison.Ordinal)
                                                       && t.ProtocolTool.Name != "search_payroll_items"), tool =>
        {
            var names = tool.ProtocolTool.InputSchema.GetProperty("properties").EnumerateObject().Select(p => p.Name).ToList();
            Assert.Contains("maxResults", names);
            Assert.Contains("cursor", names);
        });

    [Fact]
    public void Read_only_tools_say_so_and_write_tools_do_not()
    {
        foreach (var tool in ToolReference.AllTools())
        {
            var write = !WriteGuard(tool.ProtocolTool.Name) && tool.ProtocolTool.Name != "describe_write_fields";
            Assert.Equal(!write, tool.ProtocolTool.Annotations?.ReadOnlyHint == true);
        }
    }
}
