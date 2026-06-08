namespace AgentGenCli.Cli.Scaffolding;

internal sealed class TemplateTokens
{
    public required string ProjectName { get; init; }

    public string? FeatureName { get; init; }

    public string? FeatureNameLower { get; init; }

    public string ProjectNameLower => ProjectName.ToLowerInvariant();

    public string ProjectNameSnake => NameFormatting.ToSnakeCase(ProjectName);

    public string FlutterAppRelativePath => $"applications/{ProjectNameLower}";

    public IReadOnlyDictionary<string, string> ToDictionary()
    {
        var dictionary = new Dictionary<string, string>
        {
            ["{{ProjectName}}"] = ProjectName,
            ["{{ProjectNameLower}}"] = ProjectNameLower,
            ["{{ProjectNameSnake}}"] = ProjectNameSnake,
            ["{{FlutterAppRelativePath}}"] = FlutterAppRelativePath,
        };

        if (!string.IsNullOrEmpty(FeatureName))
        {
            dictionary["{{FeatureName}}"] = FeatureName;
        }

        if (!string.IsNullOrEmpty(FeatureNameLower))
        {
            dictionary["{{FeatureNameLower}}"] = FeatureNameLower;
        }

        return dictionary;
    }

    public static TemplateTokens ForFeature(string projectName, FeatureNameInfo feature) =>
        new()
        {
            ProjectName = projectName,
            FeatureName = feature.PascalName,
            FeatureNameLower = feature.FolderName,
        };
}

internal static class TemplateEngine
{
    public static string GetTemplateRoot() => GetTemplateRoot("Project");

    public static string GetBackendFeatureTemplateRoot() => GetTemplateRoot("BackendFeature");

    public static string GetFlutterAppTemplateRoot() => GetTemplateRoot("FlutterApp");

    public static string GetFrontendFeatureTemplateRoot() => GetTemplateRoot("FrontendFeature");

    public static string GetEmailTemplateRoot() => GetTemplateRoot("Email");

    public static string GetAuthTemplateRoot() => GetTemplateRoot("Auth");

    private static string GetTemplateRoot(string folder)
    {
        var baseDir = AppContext.BaseDirectory;
        return Path.Combine(baseDir, "Templates", folder);
    }

    public static void CopyAll(string destinationRoot, TemplateTokens tokens)
    {
        var templateRoot = GetTemplateRoot();
        if (!Directory.Exists(templateRoot))
        {
            throw new DirectoryNotFoundException($"Template root not found: {templateRoot}");
        }

        var replacements = tokens.ToDictionary();
        foreach (var sourceFile in Directory.EnumerateFiles(templateRoot, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(templateRoot, sourceFile);
            var targetRelativePath = ReplaceTokens(relativePath, replacements);
            var targetPath = Path.Combine(destinationRoot, targetRelativePath);
            var directory = Path.GetDirectoryName(targetPath);
            if (directory != null)
            {
                Directory.CreateDirectory(directory);
            }

            var content = File.ReadAllText(sourceFile);
            content = ReplaceTokens(content, replacements);
            File.WriteAllText(targetPath, content);
        }
    }

    public static void CopyFile(string templateRelativePath, string destinationPath, TemplateTokens tokens)
    {
        CopyFileFrom(templateRelativePath, destinationPath, tokens, GetTemplateRoot());
    }

    public static void CopyFileFrom(
        string templateRelativePath,
        string destinationPath,
        TemplateTokens tokens,
        string templateRoot
    )
    {
        var sourcePath = Path.Combine(templateRoot, templateRelativePath);
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException($"Template file not found: {sourcePath}");
        }

        var directory = Path.GetDirectoryName(destinationPath);
        if (directory != null)
        {
            Directory.CreateDirectory(directory);
        }

        var content = ReplaceTokens(File.ReadAllText(sourcePath), tokens.ToDictionary());
        File.WriteAllText(destinationPath, content, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }

    private static string ReplaceTokens(string input, IReadOnlyDictionary<string, string> replacements)
    {
        var result = input;
        foreach (var (token, value) in replacements)
        {
            result = result.Replace(token, value, StringComparison.Ordinal);
        }

        if (replacements.TryGetValue("{{ProjectName}}", out var projectName))
        {
            result = result.Replace("ProjectName", projectName, StringComparison.Ordinal);
        }

        if (replacements.TryGetValue("{{FeatureNameLower}}", out var featureNameLower))
        {
            result = result.Replace("FeatureNameLower", featureNameLower, StringComparison.Ordinal);
        }

        if (replacements.TryGetValue("{{FeatureName}}", out var featureName))
        {
            result = result.Replace("FeatureName", featureName, StringComparison.Ordinal);
        }

        return result;
    }

    public static void CopyTemplateTree(
        string templateRootRelative,
        string destinationRoot,
        TemplateTokens tokens,
        Func<string, bool>? includeFile = null,
        string? templateRootKind = null
    )
    {
        var rootFolder = templateRootKind switch
        {
            "Email" => GetEmailTemplateRoot(),
            "Auth" => GetAuthTemplateRoot(),
            _ => GetBackendFeatureTemplateRoot(),
        };
        var sourceRoot = Path.Combine(rootFolder, templateRootRelative);
        if (!Directory.Exists(sourceRoot))
        {
            throw new DirectoryNotFoundException($"Template directory not found: {sourceRoot}");
        }

        var replacements = tokens.ToDictionary();
        foreach (var sourceFile in Directory.EnumerateFiles(sourceRoot, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceRoot, sourceFile);
            if (includeFile != null && !includeFile(relativePath))
            {
                continue;
            }

            var targetRelativePath = ResolveTemplateOutputPath(ReplaceTokens(relativePath, replacements));
            var targetPath = Path.Combine(destinationRoot, targetRelativePath);
            var directory = Path.GetDirectoryName(targetPath);
            if (directory != null)
            {
                Directory.CreateDirectory(directory);
            }

            var content = ReplaceTokens(File.ReadAllText(sourceFile), replacements);
            File.WriteAllText(
                targetPath,
                content,
                new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true)
            );
        }
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
