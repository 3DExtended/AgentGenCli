using System.Text.RegularExpressions;
using AgentGenCli.Cli.Scaffolding;
using Xunit;

namespace AgentGenCli.Cli.Tests;

public class ConfigureAwaitTemplateTests
{
    // CA2007 is an error in generated projects and `dotnet format analyzers` applies its code fix. For
    // `await using var x = expr;` that fix appends `.ConfigureAwait(false)` to `expr`, which turns `x` into a
    // ConfiguredAsyncDisposable and breaks every member access on it (CS1061). Production templates must
    // declare the variable first and dispose it via `await using var xDisposal = x.ConfigureAwait(false);`.
    private static readonly Regex UnsafeAwaitUsing = new(
        @"await using\s*\(\s*var\s|await using var \w+ = (?!\w+\.ConfigureAwait\(false\);)",
        RegexOptions.Compiled
    );

    [Fact]
    public void ProductionTemplates_AwaitUsingSurvivesCa2007CodeFix()
    {
        var templatesRoot = Path.GetDirectoryName(TemplateEngine.GetTemplateRoot())!;
        var offenders = Directory
            .EnumerateFiles(templatesRoot, "*.cs.template", SearchOption.AllDirectories)
            .Where(path => !IsTestTemplate(Path.GetRelativePath(templatesRoot, path)))
            .SelectMany(path => File.ReadLines(path).Select((line, index) => (path, line, index)))
            .Where(x => UnsafeAwaitUsing.IsMatch(x.line))
            .Select(x => $"{Path.GetRelativePath(templatesRoot, x.path)}:{x.index + 1}: {x.line.Trim()}")
            .ToList();

        Assert.Empty(offenders);
    }

    // Mirrors the CA2007 exclusions in Templates/Project/.editorconfig ([tests/**] and [**/*.Tests/**]).
    private static bool IsTestTemplate(string relativePath)
    {
        var segments = relativePath.Split(Path.DirectorySeparatorChar);
        return segments[..^1].Any(s => s == "tests" || s.EndsWith(".Tests", StringComparison.Ordinal))
            || segments[^1].EndsWith("Tests.cs.template", StringComparison.Ordinal);
    }
}
