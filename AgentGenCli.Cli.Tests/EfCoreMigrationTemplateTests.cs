using AgentGenCli.Cli.Scaffolding;
using Xunit;

namespace AgentGenCli.Cli.Tests;

public class EfCoreMigrationTemplateTests
{
    [Fact]
    public void DependencyInjectionHelpersTemplate_DetectsEfToolsWithoutSubstringMatch()
    {
        var template = File.ReadAllText(
            Path.Combine(
                TemplateEngine.GetTemplateRoot(),
                "common",
                "ProjectName.Common",
                "DependencyInjectionHelpers.cs.template"
            )
        );

        // A substring check for "ef" matches "{{ProjectName}}.Api.dll" for names like RescueTheForest,
        // which made ApplyEfCoreMigrations skip migrations on every normal startup.
        Assert.DoesNotContain("Contains(\"ef\"", template, StringComparison.Ordinal);
        Assert.Contains("EF.IsDesignTime", template, StringComparison.Ordinal);
        Assert.Contains("Path.GetFileName(arg), \"ef.dll\"", template, StringComparison.Ordinal);
    }
}
