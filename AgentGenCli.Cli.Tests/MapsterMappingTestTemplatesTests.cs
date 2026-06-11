using AgentGenCli.Cli.Scaffolding;
using Xunit;

namespace AgentGenCli.Cli.Tests;

public class MapsterMappingTestTemplatesTests
{
    [Fact]
    public void CommonTestsTemplates_IncludeMapsterMappingHelpers()
    {
        var templateRoot = Path.Combine(TemplateEngine.GetTemplateRoot(), "tests", "ProjectName.Common.Tests", "Mapping");
        var assertTemplate = File.ReadAllText(Path.Combine(templateRoot, "MapsterMappingAssert.cs.template"));
        var factoryTemplate = File.ReadAllText(Path.Combine(templateRoot, "MapsterTestMapperFactory.cs.template"));
        var csprojTemplate = File.ReadAllText(
            Path.Combine(
                TemplateEngine.GetTemplateRoot(),
                "tests",
                "ProjectName.Common.Tests",
                "ProjectName.Common.Tests.csproj.template"
            )
        );

        Assert.Contains("AssertRoundTrip", assertTemplate, StringComparison.Ordinal);
        Assert.Contains("MapsterTestMapperFactory", factoryTemplate, StringComparison.Ordinal);
        Assert.Contains("Mapster", csprojTemplate, StringComparison.Ordinal);
        Assert.Contains("ProjectName.Cqrs.EfCore", csprojTemplate, StringComparison.Ordinal);
    }

    [Fact]
    public void BackendFeatureTemplate_IncludesMapsterConfigRoundTripTests()
    {
        var templatePath = Path.Combine(
            TemplateEngine.GetBackendFeatureTemplateRoot(),
            "database",
            "tests",
            "{{FeatureName}}MapsterConfigTests.cs.template"
        );
        var template = File.ReadAllText(templatePath);
        var featureTestsCsproj = File.ReadAllText(
            Path.Combine(
                TemplateEngine.GetBackendFeatureTemplateRoot(),
                "tests",
                "ProjectName.Features.FeatureName.Tests",
                "ProjectName.Features.FeatureName.Tests.csproj.template"
            )
        );

        Assert.Contains("ExcludedProperties", template, StringComparison.Ordinal);
        Assert.Contains("MapsterTestMapperFactory.Create", template, StringComparison.Ordinal);
        Assert.Contains("MapsterMappingAssert.AssertRoundTrip", template, StringComparison.Ordinal);
        Assert.Contains("AssertRoundTrip<{{FeatureEntityName}}, {{FeatureName}}Dto>", template, StringComparison.Ordinal);
        Assert.Contains("AssertRoundTrip<{{FeatureName}}Dto, {{FeatureEntityName}}>", template, StringComparison.Ordinal);
        Assert.Contains("ProjectName.Common.Tests", featureTestsCsproj, StringComparison.Ordinal);
    }
}
