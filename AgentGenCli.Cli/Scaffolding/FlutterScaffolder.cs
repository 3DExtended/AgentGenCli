namespace AgentGenCli.Cli.Scaffolding;

internal static class FlutterScaffolder
{
    public static int Scaffold(string projectName)
    {
        if (ExecutableResolver.Find("fvm") == null)
        {
            Console.Error.WriteLine(
                "FVM is required but was not found on PATH. Install FVM: https://fvm.app/documentation/getting-started/installation"
            );
            return 1;
        }

        var fvm = ExecutableResolver.Find("fvm")!;

        var tokens = new TemplateTokens { ProjectName = projectName };
        var root = Directory.GetCurrentDirectory();
        var flutterAppDir = Path.Combine(root, tokens.FlutterAppRelativePath);

        if (Directory.Exists(flutterAppDir) && Directory.EnumerateFileSystemEntries(flutterAppDir).Any())
        {
            Console.Error.WriteLine($"Flutter app directory already exists at '{flutterAppDir}'.");
            return 1;
        }

        Console.WriteLine($"Creating Flutter app at '{tokens.FlutterAppRelativePath}'...");
        Directory.CreateDirectory(Path.GetDirectoryName(flutterAppDir)!);

        var org = $"com.{tokens.ProjectNameLower.Replace('_', '.')}";
        if (
            ProcessRunner.Run(
                fvm,
                $"flutter create --project-name {tokens.ProjectNameSnake} --org {org} \"{flutterAppDir}\""
            )
            != 0
        )
        {
            Console.Error.WriteLine("Error creating Flutter project.");
            return 1;
        }

        Console.WriteLine("Pinning Flutter SDK with FVM...");
        if (ProcessRunner.Run(fvm, "use stable --force", flutterAppDir) != 0)
        {
            Console.Error.WriteLine("Error running 'fvm use stable'.");
            return 1;
        }

        Console.WriteLine("Applying Flutter templates...");
        ApplyFlutterTemplates(flutterAppDir, tokens);

        RemoveIfExists(Path.Combine(flutterAppDir, "test", "widget_test.dart"));

        Console.WriteLine("Fetching Flutter dependencies...");
        if (FlutterCommandHelper.RunFlutter(CreateContext(root, projectName), "pub get") != 0)
        {
            Console.Error.WriteLine("Error running flutter pub get.");
            return 1;
        }

        return 0;
    }

    private static ProjectContext CreateContext(string root, string projectName) =>
        new()
        {
            Root = root,
            ProjectName = projectName,
        };

    private static void ApplyFlutterTemplates(string flutterAppDir, TemplateTokens tokens)
    {
        var templateRoot = TemplateEngine.GetFlutterAppTemplateRoot();
        if (!Directory.Exists(templateRoot))
        {
            throw new DirectoryNotFoundException($"Flutter template root not found: {templateRoot}");
        }

        var replacements = tokens.ToDictionary();
        foreach (var sourceFile in Directory.EnumerateFiles(templateRoot, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(templateRoot, sourceFile);
            var targetRelativePath = ResolveTemplateOutputPath(TemplateEngineReplace(relativePath, tokens, replacements));
            var targetPath = Path.Combine(flutterAppDir, targetRelativePath);
            var directory = Path.GetDirectoryName(targetPath);
            if (directory != null)
            {
                Directory.CreateDirectory(directory);
            }

            var content = TemplateEngineReplace(File.ReadAllText(sourceFile), tokens, replacements);
            File.WriteAllText(targetPath, content);
        }

        Directory.CreateDirectory(Path.Combine(flutterAppDir, "swagger"));
        Directory.CreateDirectory(Path.Combine(flutterAppDir, "lib", "generated", "swaggen"));
        File.WriteAllText(Path.Combine(flutterAppDir, "swagger", ".gitkeep"), string.Empty);
    }

    private static void RemoveIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
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

    private static string TemplateEngineReplace(
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
        return result;
    }
}
