using AgentGenCli.Cli.Scaffolding;
using Xunit;

namespace AgentGenCli.Cli.Tests;

public class FeatureApiGeneratorTests
{
    [Fact]
    public void GenerateController_WithoutAuth_DoesNotAddAuthorizeAttribute()
    {
        using var root = new TempProjectRoot();
        SaveManifest(root.RootPath, authInitialized: false);
        var context = CreateContext(root.RootPath);
        var feature = FeatureNameNormalizer.Normalize("orders");

        var controller = FeatureApiGenerator.GenerateController(context, feature, crudLetters: "CRUD");

        Assert.DoesNotContain("[Authorize]", controller);
        Assert.DoesNotContain("Microsoft.AspNetCore.Authorization", controller);
    }

    [Fact]
    public void GenerateController_WithAuth_AddsAuthorizeAttribute()
    {
        using var root = new TempProjectRoot();
        SaveManifest(root.RootPath, authInitialized: true);
        var context = CreateContext(root.RootPath);
        var feature = FeatureNameNormalizer.Normalize("orders");

        var controller = FeatureApiGenerator.GenerateController(context, feature, crudLetters: "CRUD");

        Assert.Contains("[Authorize]", controller);
        Assert.Contains("using Microsoft.AspNetCore.Authorization;", controller);
        Assert.Contains("public class OrdersController : ControllerBase", controller);
    }

    [Fact]
    public void GenerateControllerTests_WithAuth_UsesControllerTestBase()
    {
        using var root = new TempProjectRoot();
        SaveManifest(root.RootPath, authInitialized: true);
        var context = CreateContext(root.RootPath);
        var feature = FeatureNameNormalizer.Normalize("orders");

        var tests = FeatureApiGenerator.GenerateControllerTests(context, feature, crudLetters: "");

        Assert.Contains(": ControllerTestBase", tests);
        Assert.Contains("RegisterUserAsync()", tests);
        Assert.Contains("AuthenticationHeaderValue", tests);
    }

    [Fact]
    public void GenerateControllerTests_WithoutAuth_DoesNotUseControllerTestBase()
    {
        using var root = new TempProjectRoot();
        SaveManifest(root.RootPath, authInitialized: false);
        var context = CreateContext(root.RootPath);
        var feature = FeatureNameNormalizer.Normalize("orders");

        var tests = FeatureApiGenerator.GenerateControllerTests(context, feature, crudLetters: "");

        Assert.DoesNotContain("ControllerTestBase", tests);
        Assert.DoesNotContain("RegisterUserAsync", tests);
    }

    private static void SaveManifest(string rootPath, bool authInitialized)
    {
        var manifest = ProjectManifest.CreateInitial("TestApp");
        manifest.AuthInitialized = authInitialized;
        ProjectManifest.Save(rootPath, manifest);
    }

    private static ProjectContext CreateContext(string rootPath) =>
        new()
        {
            Root = rootPath,
            ProjectName = "TestApp",
        };

    private sealed class TempProjectRoot : IDisposable
    {
        public TempProjectRoot()
        {
            RootPath = Path.Combine(Path.GetTempPath(), "agentGenCli-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(RootPath);
        }

        public string RootPath { get; }

        public void Dispose()
        {
            if (Directory.Exists(RootPath))
            {
                Directory.Delete(RootPath, recursive: true);
            }
        }
    }
}
