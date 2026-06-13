using System.Text.Json;

namespace AgentGenCli.Cli.Scaffolding;

internal sealed class FeatureScaffoldRequest
{
    public required string FeatureInput { get; init; }

    public bool WithDatabase { get; init; }

    public bool WithApi { get; init; }

    public string? Crud { get; init; }

    public string? ProjectFlag { get; init; }

    public bool Yes { get; init; }
}

internal static class FeatureScaffolder
{
    public static int Scaffold(FeatureScaffoldRequest request)
    {
        try
        {
            var context = ProjectContext.Resolve(projectFlag: request.ProjectFlag);
            var feature = FeatureNameNormalizer.Normalize(request.FeatureInput);
            var crudLetters = FeatureNameNormalizer.ParseCrudLetters(request.Crud, request.WithDatabase);

            if (Directory.Exists(context.FeatureRoot(feature)))
            {
                Console.Error.WriteLine(
                    $"Feature '{feature.FolderName}' already exists at '{context.FeatureRoot(feature)}'."
                );
                return 1;
            }

            PrintSummary(context, feature, request, crudLetters);
            if (!request.Yes && !Confirm())
            {
                Console.WriteLine("Aborted.");
                return 1;
            }

            Directory.CreateDirectory(context.FeatureRoot(feature));

            if (!CreateProjects(context, feature))
            {
                return 1;
            }

            ApplyTemplates(context, feature, request.WithDatabase, crudLetters);

            if (!AddProjectsToSolution(context, feature))
            {
                return 1;
            }

            if (!AddProjectReferences(context, feature, request.WithApi))
            {
                return 1;
            }

            ApiFeatureRegistrationPatcher.RegisterFeature(context, feature);

            if (request.WithApi)
            {
                WriteApiArtifacts(context, feature, crudLetters);
            }

            if (!FormatGeneratedCode(context))
            {
                return 1;
            }

            if (ProcessRunner.Run("dotnet", "build") != 0)
            {
                Console.Error.WriteLine("Error building solution.");
                return 1;
            }

            if (ProcessRunner.Run("dotnet", "test") != 0)
            {
                Console.Error.WriteLine("Error running tests.");
                return 1;
            }

            if (request.WithApi && Directory.Exists(context.FlutterAppDir))
            {
                Console.WriteLine("Syncing OpenAPI spec and Flutter client...");
                if (OpenApiSyncHelper.Sync(context, recordManifest: false) != 0)
                {
                    Console.Error.WriteLine("Error syncing OpenAPI.");
                    return 1;
                }

                ProjectManifest.RecordCommand(
                    context.Root,
                    new ManifestCommandEntry { Command = "project sync-openapi" }
                );
            }

            ProjectManifest.RecordFeature(
                context.Root,
                feature.PascalName,
                new ManifestCommandEntry
                {
                    Command = "new backend-feature",
                    Args = new Dictionary<string, JsonElement>
                    {
                        ["feature"] = JsonSerializer.SerializeToElement(feature.FolderName),
                        ["withDatabase"] = JsonSerializer.SerializeToElement(request.WithDatabase),
                        ["withApi"] = JsonSerializer.SerializeToElement(request.WithApi),
                        ["crud"] = JsonSerializer.SerializeToElement(crudLetters),
                    },
                }
            );

            AgentDocsGenerator.RefreshState(context.Root);

            Console.WriteLine();
            Console.WriteLine($"Feature '{feature.PascalName}' scaffolded successfully.");
            if (request.WithDatabase)
            {
                Console.WriteLine(
                    $"After adjusting the entity, run: agentGenCli new efmigration Add{feature.PascalName}"
                );
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static void PrintSummary(
        ProjectContext context,
        FeatureNameInfo feature,
        FeatureScaffoldRequest request,
        string crudLetters
    )
    {
        Console.WriteLine("Backend feature scaffold summary:");
        Console.WriteLine($"  Project: {context.ProjectName}");
        Console.WriteLine($"  Feature folder: features/{feature.FolderName}/");
        Console.WriteLine($"  Feature name: {feature.PascalName}");
        Console.WriteLine($"  withDatabase: {request.WithDatabase}");
        Console.WriteLine($"  withApi: {request.WithApi}");
        Console.WriteLine($"  crud: {(string.IsNullOrEmpty(crudLetters) ? "(none)" : crudLetters)}");
        Console.WriteLine("  Projects:");
        Console.WriteLine($"    - {context.FeatureContractsDir(feature)}");
        Console.WriteLine($"    - {context.FeatureProjectDir(feature)}");
        Console.WriteLine($"    - {context.FeatureTestsDir(feature)}");
        Console.WriteLine("  Api patches:");
        Console.WriteLine("    - FeatureRegistration.cs");

        var manifest = ProjectManifest.Load(context.Root);
        if (manifest.AuthInitialized && !string.Equals(feature.PascalName, "Users", StringComparison.Ordinal))
        {
            Console.WriteLine("  Users references:");
            Console.WriteLine(
                $"    - {context.FeatureContractsPath(feature)} → {context.UsersContractsPath}"
            );
            Console.WriteLine(
                $"    - {context.FeatureProjectPath(feature)} → {context.UsersContractsPath}, {context.UsersProjectPath}"
            );
        }
    }

    internal static IReadOnlyList<(string Project, string Reference)> GetUsersFeatureProjectReferences(
        ProjectContext context,
        FeatureNameInfo feature,
        bool authInitialized
    )
    {
        if (!authInitialized || string.Equals(feature.PascalName, "Users", StringComparison.Ordinal))
        {
            return [];
        }

        return
        [
            (context.FeatureContractsPath(feature), context.UsersContractsPath),
            (context.FeatureProjectPath(feature), context.UsersContractsPath),
            (context.FeatureProjectPath(feature), context.UsersProjectPath),
        ];
    }

    private static bool Confirm()
    {
        Console.Write("Proceed? [y/N]: ");
        var response = Console.ReadLine();
        return string.Equals(response, "y", StringComparison.OrdinalIgnoreCase)
            || string.Equals(response, "yes", StringComparison.OrdinalIgnoreCase);
    }

    private static bool CreateProjects(ProjectContext context, FeatureNameInfo feature)
    {
        var featureProject = context.FeatureProjectDir(feature);
        var contractsProject = context.FeatureContractsDir(feature);
        var testsProject = context.FeatureTestsDir(feature);
        var featureName = $"{context.ProjectName}.Features.{feature.PascalName}";
        var contractsName = $"{featureName}.Contracts";
        var testsName = $"{featureName}.Tests";

        return ProcessRunner.Run("dotnet", $"new classlib --name {contractsName} -o \"{contractsProject}\"") == 0
            && ProcessRunner.Run("dotnet", $"new classlib --name {featureName} -o \"{featureProject}\"") == 0
            && ProcessRunner.Run("dotnet", $"new xunit --name {testsName} -o \"{testsProject}\"") == 0;
    }

    private static void ApplyTemplates(
        ProjectContext context,
        FeatureNameInfo feature,
        bool withDatabase,
        string crudLetters
    )
    {
        var tokens = TemplateTokens.ForFeature(context.ProjectName, feature);

        var skipHandleScaffold = !string.IsNullOrEmpty(crudLetters);

        CopyTemplateDirectory(
            "features/FeatureName/ProjectName.Features.FeatureName.Contracts",
            context.FeatureContractsDir(feature),
            tokens,
            skipHandleScaffold
        );
        CopyTemplateDirectory(
            "features/FeatureName/ProjectName.Features.FeatureName",
            context.FeatureProjectDir(feature),
            tokens,
            skipHandleScaffold
        );
        CopyTemplateDirectory(
            "tests/ProjectName.Features.FeatureName.Tests",
            context.FeatureTestsDir(feature),
            tokens,
            skipHandleScaffold
        );

        RemoveIfExists(Path.Combine(context.FeatureContractsDir(feature), "Class1.cs"));
        RemoveIfExists(Path.Combine(context.FeatureProjectDir(feature), "Class1.cs"));
        RemoveIfExists(Path.Combine(context.FeatureTestsDir(feature), "UnitTest1.cs"));

        if (withDatabase)
        {
            CopyTemplateFile(
                "database/Entities/Feature.cs.template",
                Path.Combine(context.FeatureProjectDir(feature), "Entities", $"{feature.EntityPascalName}.cs"),
                tokens
            );
            CopyTemplateFile(
                "database/Configurations/FeatureConfiguration.cs.template",
                Path.Combine(
                    context.FeatureProjectDir(feature),
                    "Configurations",
                    $"{feature.EntityPascalName}Configuration.cs"
                ),
                tokens
            );
            CopyTemplateFile(
                "database/tests/{{FeatureName}}MapsterConfigTests.cs.template",
                Path.Combine(
                    context.FeatureTestsDir(feature),
                    "Mapping",
                    $"{feature.PascalName}MapsterConfigTests.cs"
                ),
                tokens
            );
        }

        ApplyCrudTemplates(context, feature, tokens, crudLetters);

        if (skipHandleScaffold)
        {
            ApplyCrudCoverageTestTemplates(context, feature, tokens);
        }
    }

    private static void ApplyCrudCoverageTestTemplates(
        ProjectContext context,
        FeatureNameInfo feature,
        TemplateTokens tokens
    )
    {
        CopyTemplateFile(
            "tests/ProjectName.Features.FeatureName.Tests/QueryHandlers/QueryHandlerCoverageTestData.crud.cs.template",
            Path.Combine(
                context.FeatureTestsDir(feature),
                "QueryHandlers",
                "QueryHandlerCoverageTestData.cs"
            ),
            tokens
        );
        CopyTemplateFile(
            "tests/ProjectName.Features.FeatureName.Tests/QueryHandlers/QueryHandlerCoverageTests.crud.cs.template",
            Path.Combine(
                context.FeatureTestsDir(feature),
                "QueryHandlers",
                "QueryHandlerCoverageTests.cs"
            ),
            tokens
        );
    }

    private static void ApplyCrudTemplates(
        ProjectContext context,
        FeatureNameInfo feature,
        TemplateTokens tokens,
        string crudLetters
    )
    {
        CopyTemplateFile(
            "tests/ProjectName.Features.FeatureName.Tests/Testing/{{FeatureName}}TestData.cs.template",
            Path.Combine(context.FeatureTestsDir(feature), "Testing", $"{feature.PascalName}TestData.cs"),
            tokens
        );

        if (crudLetters.Contains('C', StringComparison.Ordinal))
        {
            CopyTemplateFile(
                "crud/C/CreateFeatureQuery.cs.template",
                Path.Combine(context.FeatureContractsDir(feature), $"Create{feature.PascalName}Query.cs"),
                tokens
            );
            CopyTemplateFile(
                "crud/C/CreateFeatureQueryHandler.cs.template",
                Path.Combine(
                    context.FeatureProjectDir(feature),
                    "QueryHandlers",
                    $"Create{feature.PascalName}QueryHandler.cs"
                ),
                tokens
            );
            CopyTemplateFile(
                "crud/C/CreateFeatureQueryHandlerTests.cs.template",
                Path.Combine(
                    context.FeatureTestsDir(feature),
                    "QueryHandlers",
                    $"Create{feature.PascalName}QueryHandlerTests.cs"
                ),
                tokens
            );
        }

        if (crudLetters.Contains('R', StringComparison.Ordinal))
        {
            CopyTemplateFile(
                "crud/R/GetFeatureQuery.cs.template",
                Path.Combine(context.FeatureContractsDir(feature), $"Get{feature.PascalName}Query.cs"),
                tokens
            );
            CopyTemplateFile(
                "crud/R/ListFeatureQuery.cs.template",
                Path.Combine(context.FeatureContractsDir(feature), $"List{feature.PascalName}Query.cs"),
                tokens
            );
            CopyTemplateFile(
                "crud/R/GetFeatureQueryHandler.cs.template",
                Path.Combine(
                    context.FeatureProjectDir(feature),
                    "QueryHandlers",
                    $"Get{feature.PascalName}QueryHandler.cs"
                ),
                tokens
            );
            CopyTemplateFile(
                "crud/R/ListFeatureQueryHandler.cs.template",
                Path.Combine(
                    context.FeatureProjectDir(feature),
                    "QueryHandlers",
                    $"List{feature.PascalName}QueryHandler.cs"
                ),
                tokens
            );
            CopyTemplateFile(
                "crud/R/GetFeatureQueryHandlerTests.cs.template",
                Path.Combine(
                    context.FeatureTestsDir(feature),
                    "QueryHandlers",
                    $"Get{feature.PascalName}QueryHandlerTests.cs"
                ),
                tokens
            );
            CopyTemplateFile(
                "crud/R/ListFeatureQueryHandlerTests.cs.template",
                Path.Combine(
                    context.FeatureTestsDir(feature),
                    "QueryHandlers",
                    $"List{feature.PascalName}QueryHandlerTests.cs"
                ),
                tokens
            );
        }

        if (crudLetters.Contains('U', StringComparison.Ordinal))
        {
            CopyTemplateFile(
                "crud/U/UpdateFeatureQuery.cs.template",
                Path.Combine(context.FeatureContractsDir(feature), $"Update{feature.PascalName}Query.cs"),
                tokens
            );
            CopyTemplateFile(
                "crud/U/UpdateFeatureQueryHandler.cs.template",
                Path.Combine(
                    context.FeatureProjectDir(feature),
                    "QueryHandlers",
                    $"Update{feature.PascalName}QueryHandler.cs"
                ),
                tokens
            );
            CopyTemplateFile(
                "crud/U/UpdateFeatureQueryHandlerTests.cs.template",
                Path.Combine(
                    context.FeatureTestsDir(feature),
                    "QueryHandlers",
                    $"Update{feature.PascalName}QueryHandlerTests.cs"
                ),
                tokens
            );
        }

        if (crudLetters.Contains('D', StringComparison.Ordinal))
        {
            CopyTemplateFile(
                "crud/D/DeleteFeatureQuery.cs.template",
                Path.Combine(context.FeatureContractsDir(feature), $"Delete{feature.PascalName}Query.cs"),
                tokens
            );
            CopyTemplateFile(
                "crud/D/DeleteFeatureQueryHandler.cs.template",
                Path.Combine(
                    context.FeatureProjectDir(feature),
                    "QueryHandlers",
                    $"Delete{feature.PascalName}QueryHandler.cs"
                ),
                tokens
            );
            CopyTemplateFile(
                "crud/D/DeleteFeatureQueryHandlerTests.cs.template",
                Path.Combine(
                    context.FeatureTestsDir(feature),
                    "QueryHandlers",
                    $"Delete{feature.PascalName}QueryHandlerTests.cs"
                ),
                tokens
            );
        }
    }

    private static void WriteApiArtifacts(
        ProjectContext context,
        FeatureNameInfo feature,
        string crudLetters
    )
    {
        var controllerPath = Path.Combine(
            context.Root,
            "applications",
            $"{context.ProjectName}.Api",
            "Controllers",
            $"{feature.PascalName}Controller.cs"
        );
        var controllerTestsPath = Path.Combine(
            context.Root,
            "tests",
            $"{context.ProjectName}.Api.Tests",
            "Controllers",
            $"{feature.PascalName}ControllerTests.cs"
        );

        File.WriteAllText(
            controllerPath,
            FeatureApiGenerator.GenerateController(context, feature, crudLetters),
            new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true)
        );
        File.WriteAllText(
            controllerTestsPath,
            FeatureApiGenerator.GenerateControllerTests(context, feature, crudLetters),
            new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true)
        );
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

    private static bool AddProjectReferences(
        ProjectContext context,
        FeatureNameInfo feature,
        bool withApi
    )
    {
        var ok = RunDotnetAddReference(context.ApiProjectPath, context.FeatureProjectPath(feature));

        if (withApi)
        {
            ok =
                ok
                && RunDotnetAddReference(context.ApiProjectPath, context.FeatureContractsPath(feature));
        }

        var manifest = ProjectManifest.Load(context.Root);
        foreach (var (project, reference) in GetUsersFeatureProjectReferences(
                     context,
                     feature,
                     manifest.AuthInitialized
                 ))
        {
            if (!File.Exists(reference))
            {
                Console.Error.WriteLine(
                    $"Auth is initialized but Users project not found at '{reference}'. Run 'agentGenCli init auth' first."
                );
                return false;
            }

            ok = ok && RunDotnetAddReference(project, reference);
        }

        return ok;
    }

    private static bool RunDotnetAddReference(string project, string reference)
    {
        return ProcessRunner.Run("dotnet", $"add \"{project}\" reference \"{reference}\"") == 0;
    }

    private static bool FormatGeneratedCode(ProjectContext context)
    {
        Console.WriteLine("Formatting generated code (StyleCop / analyzers)...");
        if (ProcessRunner.Run("dotnet", "restore") != 0)
        {
            Console.Error.WriteLine("Error restoring solution before format.");
            return false;
        }

        return ProcessRunner.Run(
                "dotnet",
                "format analyzers --include applications/ --include common/ --include features/ --include tests/"
            )
            == 0;
    }

    private static void CopyTemplateDirectory(
        string templateRelativeDirectory,
        string destinationDirectory,
        TemplateTokens tokens,
        bool skipHandleScaffold = false
    )
    {
        var sourceRoot = Path.Combine(TemplateEngine.GetBackendFeatureTemplateRoot(), templateRelativeDirectory);
        if (!Directory.Exists(sourceRoot))
        {
            throw new DirectoryNotFoundException($"Template directory not found: {sourceRoot}");
        }

        foreach (var sourceFile in Directory.EnumerateFiles(sourceRoot, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceRoot, sourceFile);
            if (skipHandleScaffold && IsHandleScaffoldTemplate(relativePath))
            {
                continue;
            }
            var targetRelativePath = ResolveTemplateOutputPath(ReplaceTemplateTokens(relativePath, tokens));
            var targetPath = Path.Combine(destinationDirectory, targetRelativePath);
            var directory = Path.GetDirectoryName(targetPath);
            if (directory != null)
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(
                targetPath,
                ReplaceTemplateTokens(File.ReadAllText(sourceFile), tokens),
                new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true)
            );
        }
    }

    private static void CopyTemplateFile(
        string templateRelativePath,
        string destinationPath,
        TemplateTokens tokens,
        bool required = true
    )
    {
        var sourcePath = Path.Combine(TemplateEngine.GetBackendFeatureTemplateRoot(), templateRelativePath);
        if (!File.Exists(sourcePath))
        {
            if (required)
            {
                throw new FileNotFoundException($"Template file not found: {sourcePath}");
            }

            return;
        }

        var directory = Path.GetDirectoryName(destinationPath);
        if (directory != null)
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(
            destinationPath,
            ReplaceTemplateTokens(File.ReadAllText(sourcePath), tokens),
            new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true)
        );
    }

    private static string ReplaceTemplateTokens(string input, TemplateTokens tokens)
    {
        var result = input;
        foreach (var (token, value) in tokens.ToDictionary())
        {
            result = result.Replace(token, value, StringComparison.Ordinal);
        }

        result = result.Replace("ProjectName", tokens.ProjectName, StringComparison.Ordinal);
        if (!string.IsNullOrEmpty(tokens.FeatureNameLower))
        {
            result = result.Replace("FeatureNameLower", tokens.FeatureNameLower, StringComparison.Ordinal);
        }

        if (!string.IsNullOrEmpty(tokens.FeatureName))
        {
            result = result.Replace("FeatureName", tokens.FeatureName, StringComparison.Ordinal);
        }

        return result;
    }

    private static string ResolveTemplateOutputPath(string relativePath)
    {
        if (relativePath.EndsWith(".template", StringComparison.OrdinalIgnoreCase))
        {
            return relativePath[..^".template".Length];
        }

        return relativePath;
    }

    private static void RemoveIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private static bool IsHandleScaffoldTemplate(string relativePath) =>
        relativePath.Contains("HandleFeature", StringComparison.Ordinal)
        || relativePath.Contains(".crud.", StringComparison.Ordinal);
}
