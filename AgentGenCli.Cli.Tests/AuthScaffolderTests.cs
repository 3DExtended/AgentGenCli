using AgentGenCli.Cli.Scaffolding;
using Xunit;

namespace AgentGenCli.Cli.Tests;

public class ProjectManifestAuthTests
{
    [Fact]
    public void CreateInitial_HasEmailAndAuthFlagsFalse()
    {
        var manifest = ProjectManifest.CreateInitial("TestApp");

        Assert.False(manifest.EmailInitialized);
        Assert.False(manifest.AuthInitialized);
    }

    [Fact]
    public void EnsureEmailNotInitialized_ThrowsWhenAlreadyInitialized()
    {
        using var root = new TempDirectory();
        ProjectManifest.Save(root.Path, ProjectManifest.CreateInitial("TestApp"));
        ProjectManifest.MarkEmailInitialized(
            root.Path,
            new ManifestCommandEntry { Command = "init email" }
        );

        var exception = Assert.Throws<InvalidOperationException>(
            () => ProjectManifest.EnsureEmailNotInitialized(root.Path)
        );
        Assert.Contains("emailInitialized", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EnsureAuthNotInitialized_ThrowsWhenAlreadyInitialized()
    {
        using var root = new TempDirectory();
        ProjectManifest.Save(root.Path, ProjectManifest.CreateInitial("TestApp"));
        ProjectManifest.MarkAuthInitialized(
            root.Path,
            new ManifestCommandEntry { Command = "init auth" }
        );

        var exception = Assert.Throws<InvalidOperationException>(
            () => ProjectManifest.EnsureAuthNotInitialized(root.Path)
        );
        Assert.Contains("authInitialized", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MarkAuthInitialized_RecordsUsersAndAuthFeatures()
    {
        using var root = new TempDirectory();
        ProjectManifest.Save(root.Path, ProjectManifest.CreateInitial("TestApp"));

        ProjectManifest.MarkAuthInitialized(
            root.Path,
            new ManifestCommandEntry { Command = "init auth" }
        );

        var manifest = ProjectManifest.Load(root.Path);
        Assert.True(manifest.AuthInitialized);
        Assert.Contains("Users", manifest.Features);
        Assert.Contains("Auth", manifest.FrontendFeatures);
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "agentGenCli-auth-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}

public class AuthTemplateCatalogTests
{
    [Fact]
    public void AuthTemplates_IncludeCoreBackendAndFlutterFiles()
    {
        var authRoot = TemplateEngine.GetAuthTemplateRoot();
        Assert.True(Directory.Exists(authRoot));
        Assert.True(File.Exists(Path.Combine(authRoot, "Common", "Services", "EmailProtectionService.cs.template")));
        Assert.True(File.Exists(Path.Combine(authRoot, "Api", "Controllers", "Authorization", "SignInController.cs.template")));
        Assert.True(File.Exists(Path.Combine(authRoot, "Flutter", "lib", "features", "auth", "login_screen.dart.template")));
        Assert.True(File.Exists(Path.Combine(authRoot, "Api", "tests", "ProjectName.Api.Tests", "Controllers", "Authorization", "RegisterControllerTests.cs.template")));
    }

    [Fact]
    public void EmailTemplates_IncludeSendGridHandler()
    {
        var emailRoot = TemplateEngine.GetEmailTemplateRoot();
        Assert.True(Directory.Exists(emailRoot));
        Assert.True(File.Exists(Path.Combine(emailRoot, "QueryHandlers", "EmailSendQueryHandler.cs.template")));
    }
}

public class AuthScaffolderArtifactTests
{
    [Fact]
    public void AuthTemplates_IncludeApiControllerTestsAndStartupMarkers()
    {
        var authRoot = TemplateEngine.GetAuthTemplateRoot();
        Assert.True(
            File.Exists(
                Path.Combine(
                    authRoot,
                    "Api",
                    "tests",
                    "ProjectName.Api.Tests",
                    "Controllers",
                    "Authorization",
                    "RegisterControllerTests.cs.template"
                )
            )
        );
        Assert.True(
            File.Exists(
                Path.Combine(
                    authRoot,
                    "Backend",
                    "tests",
                    "ProjectName.Features.Users.Tests",
                    "QueryHandlers",
                    "UserRegisterQueryHandlerTests.cs.template"
                )
            )
        );
    }
}
