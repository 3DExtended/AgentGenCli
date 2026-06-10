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

public class EfMigrationHelperTests
{
    [Fact]
    public void MigrationExists_ReturnsFalseWhenMigrationsFolderMissing()
    {
        using var root = new TempDirectory();
        var context = new ProjectContext
        {
            Root = root.Path,
            ProjectName = "TestApp",
            CommonProjectPath = Path.Combine(root.Path, "common", "TestApp.Common", "TestApp.Common.csproj"),
            ApiProjectPath = Path.Combine(root.Path, "applications", "TestApp.Api", "TestApp.Api.csproj"),
        };

        Assert.False(EfMigrationHelper.MigrationExists(context, "AddUsers"));
    }

    [Fact]
    public void MigrationExists_ReturnsTrueWhenMigrationFilePresent()
    {
        using var root = new TempDirectory();
        var commonDir = Path.Combine(root.Path, "common", "TestApp.Common");
        var migrationsDir = Path.Combine(commonDir, "Migrations");
        Directory.CreateDirectory(migrationsDir);
        File.WriteAllText(Path.Combine(migrationsDir, "20260101120000_AddUsers.cs"), "// migration");

        var context = new ProjectContext
        {
            Root = root.Path,
            ProjectName = "TestApp",
            CommonProjectPath = Path.Combine(commonDir, "TestApp.Common.csproj"),
            ApiProjectPath = Path.Combine(root.Path, "applications", "TestApp.Api", "TestApp.Api.csproj"),
        };

        Assert.True(EfMigrationHelper.MigrationExists(context, "AddUsers"));
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "agentGenCli-ef-tests-" + Guid.NewGuid().ToString("N")
            );
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

public class ApiProjectAuthPatcherTests
{
    [Fact]
    public void Apply_AddsAuthPackagesOnceWhenMultipleItemGroupsExist()
    {
        using var root = new TempDirectory();
        var apiDir = Path.Combine(root.Path, "applications", "TestApp.Api");
        Directory.CreateDirectory(apiDir);

        var apiProjectPath = Path.Combine(apiDir, "TestApp.Api.csproj");
        File.WriteAllText(
            apiProjectPath,
            """
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Swashbuckle.AspNetCore" Version="9.0.6" />
              </ItemGroup>
              <ItemGroup>
                <ProjectReference Include="..\..\common\TestApp.Common\TestApp.Common.csproj" />
              </ItemGroup>
            </Project>
            """
        );

        var context = new ProjectContext
        {
            Root = root.Path,
            ProjectName = "TestApp",
            ApiProjectPath = apiProjectPath,
        };

        ApiProjectAuthPatcher.Apply(context);

        var content = File.ReadAllText(apiProjectPath);
        Assert.Equal(1, CountOccurrences(content, "Google.Apis.Auth"));
        Assert.Equal(1, CountOccurrences(content, "Microsoft.AspNetCore.Authentication.JwtBearer"));
    }

    private static int CountOccurrences(string content, string value) =>
        content.Split(value, StringSplitOptions.None).Length - 1;

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "agentGenCli-api-patcher-tests-" + Guid.NewGuid().ToString("N")
            );
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

    [Fact]
    public void AuthEntityTemplates_IncludeKeyAttributesAndForeignKeyRelationship()
    {
        var authRoot = TemplateEngine.GetAuthTemplateRoot();
        var userEntity = File.ReadAllText(
            Path.Combine(
                authRoot,
                "Backend",
                "features",
                "users",
                "ProjectName.Features.Users",
                "Entities",
                "UserEntity.cs.template"
            )
        );
        var userCredentialsEntity = File.ReadAllText(
            Path.Combine(
                authRoot,
                "Backend",
                "features",
                "users",
                "ProjectName.Features.Users",
                "Entities",
                "UserCredentialsEntity.cs.template"
            )
        );
        var userCredentialsConfiguration = File.ReadAllText(
            Path.Combine(
                authRoot,
                "Backend",
                "features",
                "users",
                "ProjectName.Features.Users",
                "Configurations",
                "UserCredentialsEntityConfiguration.cs.template"
            )
        );

        Assert.Contains("[Key]", userEntity, StringComparison.Ordinal);
        Assert.Contains("UserCredentialsEntity? UserCredentials", userEntity, StringComparison.Ordinal);

        Assert.Contains("[Key]", userCredentialsEntity, StringComparison.Ordinal);
        Assert.Contains("[ForeignKey(nameof(User))]", userCredentialsEntity, StringComparison.Ordinal);
        Assert.Contains("UserEntity User", userCredentialsEntity, StringComparison.Ordinal);

        Assert.Contains("HasOne(entity => entity.User)", userCredentialsConfiguration, StringComparison.Ordinal);
        Assert.Contains(
            "HasForeignKey<UserCredentialsEntity>(entity => entity.UserId)",
            userCredentialsConfiguration,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void AuthModelTemplates_UseIdentifierPattern()
    {
        var authRoot = TemplateEngine.GetAuthTemplateRoot();
        var contractsDir = Path.Combine(
            authRoot,
            "Backend",
            "features",
            "users",
            "ProjectName.Features.Users.Contracts",
            "Models"
        );

        var user = File.ReadAllText(Path.Combine(contractsDir, "User.cs.template"));
        var userId = File.ReadAllText(Path.Combine(contractsDir, "UserId.cs.template"));
        var userCredentials = File.ReadAllText(Path.Combine(contractsDir, "UserCredentials.cs.template"));
        var userCredentialsId = File.ReadAllText(
            Path.Combine(contractsDir, "UserCredentialsId.cs.template")
        );
        var usersMapsterConfig = File.ReadAllText(
            Path.Combine(
                authRoot,
                "Backend",
                "features",
                "users",
                "ProjectName.Features.Users",
                "UsersMapsterConfig.cs.template"
            )
        );

        Assert.Contains("ModelBase<UserId, Guid>", user, StringComparison.Ordinal);
        Assert.Contains("record UserId : Identifier<Guid, UserId>", userId, StringComparison.Ordinal);
        Assert.Contains("ModelBase<UserCredentialsId, Guid>", userCredentials, StringComparison.Ordinal);
        Assert.Contains(
            "record UserCredentialsId : Identifier<Guid, UserCredentialsId>",
            userCredentialsId,
            StringComparison.Ordinal
        );
        Assert.DoesNotContain("UserIdentifier", user, StringComparison.Ordinal);
        Assert.Contains("UserId.From", usersMapsterConfig, StringComparison.Ordinal);
        Assert.Contains("UserCredentialsId.From", usersMapsterConfig, StringComparison.Ordinal);
    }
}
