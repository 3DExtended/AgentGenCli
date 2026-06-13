using System.Text.Json;

namespace AgentGenCli.Cli.Scaffolding;

internal sealed class AuthScaffoldRequest
{
    public string? ProjectFlag { get; init; }

    public bool Yes { get; init; }
}

internal static class AuthScaffolder
{
    public static int Scaffold(AuthScaffoldRequest request)
    {
        try
        {
            var context = ProjectContext.Resolve(projectFlag: request.ProjectFlag);
            ProjectManifest.EnsureAuthNotInitialized(context.Root);

            var manifest = ProjectManifest.Load(context.Root);
            if (!manifest.EmailInitialized)
            {
                Console.WriteLine("Email scaffolding not found; running init email first...");
                if (EmailScaffolder.Scaffold(new EmailScaffoldRequest { ProjectFlag = request.ProjectFlag, Yes = true }) != 0)
                {
                    return 1;
                }
            }

            var feature = FeatureNameNormalizer.Normalize("users");
            var usersFeatureExists = Directory.Exists(context.FeatureRoot(feature));

            if (usersFeatureExists)
            {
                Console.WriteLine("Users feature already present; resuming auth initialization...");
            }
            else
            {
                Console.WriteLine("Auth scaffold summary:");
                Console.WriteLine($"  Project: {context.ProjectName}");
                Console.WriteLine("  Backend: Users feature + JWT + IUserContext");
                Console.WriteLine("  Flutter: auth screens (when Flutter app exists)");

                if (!request.Yes && !Confirm())
                {
                    Console.WriteLine("Aborted.");
                    return 1;
                }

                var tokens = new TemplateTokens
                {
                    ProjectName = context.ProjectName,
                    FeatureName = "Users",
                    FeatureNameLower = "users",
                };

                if (!CreateUsersProjects(context, feature))
                {
                    return 1;
                }

                ApplyAuthTemplates(context, feature, tokens);
                ApplyProjectPatchers(context);
                ApiFeatureRegistrationPatcher.RegisterFeature(context, feature);

                if (!AddUsersProjectReferences(context, feature))
                {
                    return 1;
                }

                if (!FormatGeneratedCode(context))
                {
                    return 1;
                }
            }

            if (!VerifyBackendAuth(context))
            {
                return 1;
            }

            FinalizeAuthManifest(context);

            if (Directory.Exists(context.FlutterAppDir))
            {
                var tokens = new TemplateTokens
                {
                    ProjectName = context.ProjectName,
                    FeatureName = "Users",
                    FeatureNameLower = "users",
                };

                Console.WriteLine("Applying Flutter auth templates...");
                if (FlutterAuthScaffolder.Apply(context, tokens) != 0)
                {
                    return 1;
                }

                Console.WriteLine("Syncing OpenAPI spec and Flutter client...");
                if (OpenApiSyncHelper.Sync(context, recordManifest: false) != 0)
                {
                    Console.Error.WriteLine("Error syncing OpenAPI.");
                    return 1;
                }

                if (FlutterCommandHelper.RunFlutter(context, "pub get") != 0)
                {
                    Console.Error.WriteLine("Error running flutter pub get.");
                    return 1;
                }

                Console.WriteLine("Generating auth golden screenshots...");
                if (FlutterCommandHelper.RunFlutter(context, "test --update-goldens") != 0)
                {
                    Console.Error.WriteLine("Error generating auth golden screenshots.");
                    return 1;
                }

                Console.WriteLine("Running Flutter tests...");
                if (FlutterCommandHelper.RunFlutter(context, "test") != 0)
                {
                    Console.Error.WriteLine("Error running Flutter tests.");
                    return 1;
                }

                ProjectManifest.RecordCommand(
                    context.Root,
                    new ManifestCommandEntry { Command = "project sync-openapi" }
                );
                AgentDocsGenerator.RefreshState(context.Root);
            }

            Console.WriteLine("Auth scaffolding initialized successfully.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static void FinalizeAuthManifest(ProjectContext context)
    {
        ProjectManifest.MarkAuthInitialized(
            context.Root,
            new ManifestCommandEntry { Command = "init auth" }
        );
        AgentDocsGenerator.RefreshState(context.Root);
    }

    private static bool VerifyBackendAuth(ProjectContext context)
    {
        Console.WriteLine("Building solution before AddUsers migration...");
        if (ProcessRunner.Run("dotnet", "build") != 0)
        {
            Console.Error.WriteLine("Error building solution before AddUsers migration.");
            return false;
        }

        if (EfMigrationHelper.AddMigrationIfMissing(context, "AddUsers") != 0)
        {
            Console.Error.WriteLine("Error creating EF Core migration for Users.");
            return false;
        }

        if (ProcessRunner.Run("dotnet", "build") != 0)
        {
            Console.Error.WriteLine("Error building solution after auth scaffold.");
            return false;
        }

        if (ProcessRunner.Run("dotnet", "test") != 0)
        {
            Console.Error.WriteLine("Error running tests after auth scaffold.");
            return false;
        }

        return true;
    }

    private static bool Confirm()
    {
        Console.Write("Proceed? [y/N]: ");
        var response = Console.ReadLine();
        return string.Equals(response, "y", StringComparison.OrdinalIgnoreCase)
            || string.Equals(response, "yes", StringComparison.OrdinalIgnoreCase);
    }

    private static bool CreateUsersProjects(ProjectContext context, FeatureNameInfo feature)
    {
        Directory.CreateDirectory(context.FeatureRoot(feature));
        var featureName = $"{context.ProjectName}.Features.{feature.PascalName}";
        var contractsName = $"{featureName}.Contracts";
        var testsName = $"{featureName}.Tests";

        return ProcessRunner.Run(
                "dotnet",
                $"new classlib --name {contractsName} -o \"{context.FeatureContractsDir(feature)}\""
            ) == 0
            && ProcessRunner.Run(
                "dotnet",
                $"new classlib --name {featureName} -o \"{context.FeatureProjectDir(feature)}\""
            ) == 0
            && ProcessRunner.Run(
                "dotnet",
                $"new xunit --name {testsName} -o \"{context.FeatureTestsDir(feature)}\""
            ) == 0
            && AddProjectsToSolution(context, feature);
    }

    private static bool AddProjectsToSolution(ProjectContext context, FeatureNameInfo feature)
    {
        var projects = new[]
        {
            Path.GetRelativePath(context.Root, context.FeatureContractsPath(feature)),
            Path.GetRelativePath(context.Root, context.FeatureProjectPath(feature)),
            Path.GetRelativePath(context.Root, context.FeatureTestsPath(feature)),
        };

        foreach (var project in projects)
        {
            if (ProcessRunner.Run("dotnet", $"sln add \"{project}\"") != 0)
            {
                Console.Error.WriteLine($"Error adding project to solution: {project}");
                return false;
            }
        }

        return true;
    }

    private static void ApplyAuthTemplates(ProjectContext context, FeatureNameInfo feature, TemplateTokens tokens)
    {
        CopyAuthTree("Common", Path.Combine(context.Root, "common", $"{context.ProjectName}.Common"), tokens);

        CopyAuthTree(
            "Backend/features/users/ProjectName.Features.Users.Contracts",
            context.FeatureContractsDir(feature),
            tokens
        );
        CopyAuthTree(
            "Backend/features/users/ProjectName.Features.Users",
            context.FeatureProjectDir(feature),
            tokens
        );
        CopyAuthTree(
            "Backend/tests/ProjectName.Features.Users.Tests",
            context.FeatureTestsDir(feature),
            tokens
        );

        CopyAuthFile(
            "Backend/features/users/ProjectName.Features.Users.Contracts/ProjectName.Features.Users.Contracts.csproj.template",
            context.FeatureContractsPath(feature),
            tokens
        );
        CopyAuthFile(
            "Backend/features/users/ProjectName.Features.Users/ProjectName.Features.Users.csproj.template",
            context.FeatureProjectPath(feature),
            tokens
        );
        CopyAuthFile(
            "Backend/tests/ProjectName.Features.Users.Tests/ProjectName.Features.Users.Tests.csproj.template",
            context.FeatureTestsPath(feature),
            tokens
        );

        var apiDir = Path.Combine(context.Root, "applications", $"{context.ProjectName}.Api");
        CopyAuthTree("Api/Controllers", Path.Combine(apiDir, "Controllers"), tokens);
        CopyAuthTree("Api/Services", Path.Combine(apiDir, "Services"), tokens);
        CopyAuthTree("Api/Models", Path.Combine(apiDir, "Models"), tokens);
        CopyAuthTree(
            "Api/tests/ProjectName.Api.Tests/Controllers",
            Path.Combine(context.Root, "tests", $"{context.ProjectName}.Api.Tests", "Controllers"),
            tokens
        );
        CopyAuthTree(
            "Api/tests/ProjectName.Api.Tests/Base",
            Path.Combine(context.Root, "tests", $"{context.ProjectName}.Api.Tests", "Base"),
            tokens
        );

        RemoveIfExists(Path.Combine(context.FeatureContractsDir(feature), "Class1.cs"));
        RemoveIfExists(Path.Combine(context.FeatureProjectDir(feature), "Class1.cs"));
        RemoveIfExists(Path.Combine(context.FeatureTestsDir(feature), "UnitTest1.cs"));
    }

    private static void ApplyProjectPatchers(ProjectContext context)
    {
        CommonProjectAuthPatcher.Apply(context);
        ApiProjectAuthPatcher.Apply(context);
        ApiTestsAuthPatcher.Apply(context);
        AppsettingsAuthPatcher.Apply(context);
        DependencyInjectionAuthPatcher.Apply(context);
        StartupAuthPatcher.Apply(context);
        HealthControllerAuthPatcher.Apply(context);
    }

    private static bool AddUsersProjectReferences(ProjectContext context, FeatureNameInfo feature)
    {
        return RunDotnetAddReference(context.ApiProjectPath, context.FeatureProjectPath(feature))
            && RunDotnetAddReference(context.ApiProjectPath, context.FeatureContractsPath(feature))
            && RunDotnetAddReference(context.FeatureTestsPath(feature), context.FeatureProjectPath(feature))
            && RunDotnetAddReference(context.FeatureTestsPath(feature), context.FeatureContractsPath(feature));
    }

    private static bool RunDotnetAddReference(string project, string reference) =>
        ProcessRunner.Run("dotnet", $"add \"{project}\" reference \"{reference}\"") == 0;

    private static bool FormatGeneratedCode(ProjectContext context)
    {
        Console.WriteLine("Formatting generated code...");
        if (ProcessRunner.Run("dotnet", "restore") != 0)
        {
            return false;
        }

        return ProcessRunner.Run(
                "dotnet",
                "format analyzers --include applications/ --include common/ --include features/ --include tests/"
            ) == 0;
    }

    private static void CopyAuthTree(string relativePath, string destinationRoot, TemplateTokens tokens)
    {
        TemplateEngine.CopyTemplateTree(relativePath, destinationRoot, tokens, templateRootKind: "Auth");
    }

    private static void CopyAuthFile(string relativePath, string destinationPath, TemplateTokens tokens)
    {
        TemplateEngine.CopyFileFrom(relativePath, destinationPath, tokens, TemplateEngine.GetAuthTemplateRoot());
    }

    private static void RemoveIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}

internal static class CommonProjectAuthPatcher
{
    public static void Apply(ProjectContext context)
    {
        CsprojPackageHelper.AddPackageIfMissing(
            context.CommonProjectPath,
            "Microsoft.Extensions.Identity.Core",
            "10.0.0"
        );
        CsprojPackageHelper.AddPackageIfMissing(
            context.CommonProjectPath,
            "System.IdentityModel.Tokens.Jwt",
            "8.9.0"
        );
        CsprojPackageHelper.AddPackageIfMissing(context.CommonProjectPath, "Google.Apis.Auth", "1.69.0");
    }
}

internal static class ApiProjectAuthPatcher
{
    public static void Apply(ProjectContext context)
    {
        var path = context.ApiProjectPath;

        CsprojPackageHelper.AddPackageIfMissing(path, "Google.Apis.Auth", "1.69.0");
        CsprojPackageHelper.AddPackageIfMissing(path, "Microsoft.AspNetCore.Authentication.JwtBearer", "10.0.5");

        var sendGridProject = Path.Combine(
            context.Root,
            "common",
            $"{context.ProjectName}.Common.SendGrid",
            $"{context.ProjectName}.Common.SendGrid.csproj"
        );
        if (File.Exists(sendGridProject))
        {
            var content = File.ReadAllText(path);
            if (!content.Contains("Common.SendGrid", StringComparison.Ordinal))
            {
                ProcessRunner.Run("dotnet", $"add \"{path}\" reference \"{sendGridProject}\"");
            }
        }
    }
}

internal static class CsprojPackageHelper
{
    public static void AddPackageIfMissing(string projectPath, string packageId, string version)
    {
        var content = File.ReadAllText(projectPath);
        if (content.Contains(packageId, StringComparison.Ordinal))
        {
            return;
        }

        ProcessRunner.Run("dotnet", $"add \"{projectPath}\" package {packageId} --version {version}");
    }
}

internal static class AppsettingsAuthPatcher
{
    public static void Apply(ProjectContext context)
    {
        var apiDir = Path.Combine(context.Root, "applications", $"{context.ProjectName}.Api");
        PatchAppsettings(Path.Combine(apiDir, "appsettings.json"));
        PatchAppsettings(Path.Combine(apiDir, "appsettings.Development.json"));
    }

    private static void PatchAppsettings(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        var content = File.ReadAllText(path);
        if (content.Contains("\"Jwt\"", StringComparison.Ordinal))
        {
            return;
        }

        const string sections =
            """
              "Jwt": {
                "Key": "dev-dev-dev-dev-dev-dev-dev-dev-dev-dev-dev-dev-dev-dev-dev-dev",
                "Issuer": "api",
                "Audience": "api",
                "NumberOfSecondsToExpire": 86400
              },
              "Encryption": {
                "DataKey": "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=",
                "Pepper": "dev-pepper-change-me"
              },
              "Google": {
                "ClientIds": []
              },
              "Apple": {
                "ClientId": "com.example.app"
              },
            """;

        if (content.Contains("\"Sql\":", StringComparison.Ordinal))
        {
            content = content.Replace("  \"Sql\":", sections + "  \"Sql\":", StringComparison.Ordinal);
        }
        else if (content.Contains("\"AllowedHosts\"", StringComparison.Ordinal))
        {
            content = content.Replace(
                "  \"AllowedHosts\": \"*\"",
                sections + "  \"AllowedHosts\": \"*\"",
                StringComparison.Ordinal
            );
        }
        else
        {
            return;
        }

        File.WriteAllText(path, content);
    }
}
