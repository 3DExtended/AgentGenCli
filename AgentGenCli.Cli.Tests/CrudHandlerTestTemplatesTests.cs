using AgentGenCli.Cli.Scaffolding;
using Xunit;

namespace AgentGenCli.Cli.Tests;

public class CrudHandlerTestTemplatesTests
{
    [Fact]
    public void CommonTestsTemplates_IncludeHandlerTestHelpers()
    {
        var testingRoot = Path.Combine(
            TemplateEngine.GetTemplateRoot(),
            "tests",
            "ProjectName.Common.Tests",
            "Testing"
        );
        var scopeTemplate = File.ReadAllText(Path.Combine(testingRoot, "InMemoryFeatureTestScope.cs.template"));
        var assertTemplate = File.ReadAllText(Path.Combine(testingRoot, "QueryHandlerResultAssert.cs.template"));

        Assert.Contains("InMemoryFeatureTestScope", scopeTemplate, StringComparison.Ordinal);
        Assert.Contains("EnsureCreatedAsync", scopeTemplate, StringComparison.Ordinal);
        Assert.Contains("GetHandler", scopeTemplate, StringComparison.Ordinal);
        Assert.Contains("AssertSome", assertTemplate, StringComparison.Ordinal);
        Assert.Contains("AssertNone", assertTemplate, StringComparison.Ordinal);
        Assert.Contains("AssertUnit", assertTemplate, StringComparison.Ordinal);
        Assert.Contains("AssertIdentifierNonEmpty", assertTemplate, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("crud/C/CreateFeatureQueryHandlerTests.cs.template", "RunQueryAsync_PersistsEntity_ReturnsNewId", "InMemoryFeatureTestScope", "QueryHandlerResultAssert", "{{FeatureName}}TestData")]
    [InlineData("crud/R/GetFeatureQueryHandlerTests.cs.template", "RunQueryAsync_WhenEntityExists_ReturnsMappedDto", "RunQueryAsync_WhenEntityMissing_ReturnsNone", "InMemoryFeatureTestScope", "QueryHandlerResultAssert")]
    [InlineData("crud/R/ListFeatureQueryHandlerTests.cs.template", "RunQueryAsync_WhenSeeded_ReturnsAllItems", "RunQueryAsync_WhenPartialIdSet_ReturnsNone", "InMemoryFeatureTestScope", "QueryHandlerResultAssert")]
    [InlineData("crud/U/UpdateFeatureQueryHandlerTests.cs.template", "RunQueryAsync_WhenEntityExists_UpdatesAndReturnsUnit", "RunQueryAsync_WhenEntityMissing_ReturnsNone", "AssertUnit", "InMemoryFeatureTestScope")]
    [InlineData("crud/D/DeleteFeatureQueryHandlerTests.cs.template", "RunQueryAsync_WhenEntityExists_DeletesAndReturnsUnit", "RunQueryAsync_WhenEntityMissing_ReturnsNone", "AssertUnit", "InMemoryFeatureTestScope")]
    public void CrudHandlerTestTemplates_IncludeRequiredPatterns(
        string relativePath,
        string requiredPattern1,
        string requiredPattern2,
        string requiredPattern3,
        string requiredPattern4
    )
    {
        var template = File.ReadAllText(
            Path.Combine(TemplateEngine.GetBackendFeatureTemplateRoot(), relativePath)
        );

        Assert.DoesNotContain("new ServiceCollection()", template, StringComparison.Ordinal);
        Assert.Contains(requiredPattern1, template, StringComparison.Ordinal);
        Assert.Contains(requiredPattern2, template, StringComparison.Ordinal);
        Assert.Contains(requiredPattern3, template, StringComparison.Ordinal);
        Assert.Contains(requiredPattern4, template, StringComparison.Ordinal);
    }

    [Fact]
    public void BackendFeatureTemplate_IncludesFeatureTestData()
    {
        var templatePath = Path.Combine(
            TemplateEngine.GetBackendFeatureTemplateRoot(),
            "tests",
            "ProjectName.Features.FeatureName.Tests",
            "Testing",
            "{{FeatureName}}TestData.cs.template"
        );
        var template = File.ReadAllText(templatePath);

        Assert.Contains("SampleName", template, StringComparison.Ordinal);
        Assert.Contains("UpdatedName", template, StringComparison.Ordinal);
        Assert.Contains("SeedAsync", template, StringComparison.Ordinal);
    }
}
