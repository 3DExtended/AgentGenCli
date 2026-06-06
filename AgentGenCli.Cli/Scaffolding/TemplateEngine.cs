namespace AgentGenCli.Cli.Scaffolding;

internal sealed class TemplateTokens
{
    public required string ProjectName { get; init; }

    public string ProjectNameLower => ProjectName.ToLowerInvariant();

    public IReadOnlyDictionary<string, string> ToDictionary() =>
        new Dictionary<string, string>
        {
            ["{{ProjectName}}"] = ProjectName,
            ["{{ProjectNameLower}}"] = ProjectNameLower,
        };
}

internal static class TemplateEngine
{
    public static string GetTemplateRoot()
    {
        var baseDir = AppContext.BaseDirectory;
        return Path.Combine(baseDir, "Templates", "Project");
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
        var sourcePath = Path.Combine(GetTemplateRoot(), templateRelativePath);
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

        return result;
    }
}
