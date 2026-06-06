using System.Text.Json;

namespace AgentGenCli.Cli.Scaffolding;

internal sealed class FrontendFeatureScaffoldRequest
{
    public required string FeatureInput { get; init; }

    public bool WithApi { get; init; }

    public string? ProjectFlag { get; init; }

    public bool Yes { get; init; }
}

internal static class FrontendFeatureScaffolder
{
    public static int Scaffold(FrontendFeatureScaffoldRequest request)
    {
        try
        {
            var context = ProjectContext.Resolve(projectFlag: request.ProjectFlag);
            var feature = FeatureNameNormalizer.Normalize(request.FeatureInput);
            var tokens = TemplateTokens.ForFeature(context.ProjectName, feature);
            var featureDir = Path.Combine(context.FlutterAppDir, "lib", "features", feature.FolderName);

            if (!Directory.Exists(context.FlutterAppDir))
            {
                Console.Error.WriteLine(
                    $"Flutter app not found at '{context.FlutterAppRelativePath}'. Run 'agentGenCli init project' with frontend=flutter first."
                );
                return 1;
            }

            if (Directory.Exists(featureDir))
            {
                Console.Error.WriteLine($"Frontend feature '{feature.FolderName}' already exists.");
                return 1;
            }

            PrintSummary(context, feature, request);
            if (!request.Yes && !Confirm())
            {
                Console.WriteLine("Aborted.");
                return 1;
            }

            ApplyTemplates(context, feature, tokens, request.WithApi);
            FlutterRouterPatcher.AddFeatureRoute(context, feature);

            Console.WriteLine("Running Flutter tests...");
            if (FlutterCommandHelper.RunFlutter(context, "test --update-goldens") != 0)
            {
                Console.Error.WriteLine("Error updating golden screenshots.");
                return 1;
            }

            if (FlutterCommandHelper.RunFlutter(context, "test") != 0)
            {
                Console.Error.WriteLine("Error running Flutter tests.");
                return 1;
            }

            ProjectManifest.RecordFrontendFeature(
                context.Root,
                feature.PascalName,
                new ManifestCommandEntry
                {
                    Command = "new frontend-feature",
                    Args = new Dictionary<string, JsonElement>
                    {
                        ["feature"] = JsonSerializer.SerializeToElement(feature.FolderName),
                        ["withApi"] = JsonSerializer.SerializeToElement(request.WithApi),
                    },
                }
            );

            AgentDocsGenerator.RefreshState(context.Root);

            Console.WriteLine();
            Console.WriteLine($"Frontend feature '{feature.PascalName}' scaffolded successfully.");
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
        FrontendFeatureScaffoldRequest request
    )
    {
        Console.WriteLine("Frontend feature scaffold summary:");
        Console.WriteLine($"  Project: {context.ProjectName}");
        Console.WriteLine($"  Feature folder: lib/features/{feature.FolderName}/");
        Console.WriteLine($"  withApi: {request.WithApi}");
        Console.WriteLine("  Router patch: core/router/app_router.dart");
    }

    private static bool Confirm()
    {
        Console.Write("Proceed? [y/N]: ");
        var response = Console.ReadLine();
        return string.Equals(response, "y", StringComparison.OrdinalIgnoreCase)
            || string.Equals(response, "yes", StringComparison.OrdinalIgnoreCase);
    }

    private static void ApplyTemplates(
        ProjectContext context,
        FeatureNameInfo feature,
        TemplateTokens tokens,
        bool withApi
    )
    {
        var templateRoot = TemplateEngine.GetFrontendFeatureTemplateRoot();
        var flutterRoot = context.FlutterAppDir;
        var featureDir = Path.Combine(flutterRoot, "lib", "features", feature.FolderName);
        Directory.CreateDirectory(featureDir);
        Directory.CreateDirectory(Path.Combine(featureDir, "widgets"));
        Directory.CreateDirectory(
            Path.Combine(flutterRoot, "test", "features", feature.FolderName, "widgets")
        );

        var replacements = tokens.ToDictionary();

        foreach (var sourceFile in Directory.EnumerateFiles(templateRoot, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(templateRoot, sourceFile);
            if (relativePath.Contains("FeatureService", StringComparison.Ordinal))
            {
                continue;
            }

            var targetRelativePath = ResolveTemplateOutputPath(
                ReplaceTokens(relativePath, tokens, replacements)
            );
            var targetPath = Path.Combine(flutterRoot, targetRelativePath);
            var directory = Path.GetDirectoryName(targetPath);
            if (directory != null)
            {
                Directory.CreateDirectory(directory);
            }

            var content = ReplaceTokens(File.ReadAllText(sourceFile), tokens, replacements);
            File.WriteAllText(targetPath, content);
        }

        var servicePath = Path.Combine(
            flutterRoot,
            "lib",
            "features",
            feature.FolderName,
            $"{feature.FolderName}_service.dart"
        );
        File.WriteAllText(
            servicePath,
            FrontendFeatureServiceGenerator.Generate(context, feature, withApi)
        );
    }

    private static string ReplaceTokens(
        string input,
        TemplateTokens tokens,
        IReadOnlyDictionary<string, string> replacements
    )
    {
        var result = input;
        foreach (var (token, value) in replacements)
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
}
