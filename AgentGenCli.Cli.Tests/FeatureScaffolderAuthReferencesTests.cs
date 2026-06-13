using AgentGenCli.Cli.Scaffolding;
using Xunit;

namespace AgentGenCli.Cli.Tests;

public class FeatureScaffolderAuthReferencesTests
{
    [Fact]
    public void GetUsersFeatureProjectReferences_WithoutAuth_ReturnsEmpty()
    {
        var context = CreateContext();
        var feature = FeatureNameNormalizer.Normalize("orders");

        var references = FeatureScaffolder.GetUsersFeatureProjectReferences(context, feature, authInitialized: false);

        Assert.Empty(references);
    }

    [Fact]
    public void GetUsersFeatureProjectReferences_WithAuth_AddsContractsAndFeatureReferences()
    {
        var context = CreateContext();
        var feature = FeatureNameNormalizer.Normalize("orders");

        var references = FeatureScaffolder.GetUsersFeatureProjectReferences(context, feature, authInitialized: true);

        Assert.Equal(3, references.Count);
        Assert.Contains(
            (context.FeatureContractsPath(feature), context.UsersContractsPath),
            references
        );
        Assert.Contains((context.FeatureProjectPath(feature), context.UsersContractsPath), references);
        Assert.Contains((context.FeatureProjectPath(feature), context.UsersProjectPath), references);
    }

    [Fact]
    public void GetUsersFeatureProjectReferences_WithAuth_SkipsUsersFeature()
    {
        var context = CreateContext();
        var feature = FeatureNameNormalizer.Normalize("users");

        var references = FeatureScaffolder.GetUsersFeatureProjectReferences(context, feature, authInitialized: true);

        Assert.Empty(references);
    }

    [Fact]
    public void UsersProjectPaths_UseFeaturesUsersFolder()
    {
        var context = CreateContext();

        Assert.EndsWith(
            Path.Combine("features", "users", "TestApp.Features.Users.Contracts", "TestApp.Features.Users.Contracts.csproj"),
            context.UsersContractsPath,
            StringComparison.Ordinal
        );
        Assert.EndsWith(
            Path.Combine("features", "users", "TestApp.Features.Users", "TestApp.Features.Users.csproj"),
            context.UsersProjectPath,
            StringComparison.Ordinal
        );
    }

    private static ProjectContext CreateContext() =>
        new()
        {
            Root = "/tmp/test-app",
            ProjectName = "TestApp",
        };
}
