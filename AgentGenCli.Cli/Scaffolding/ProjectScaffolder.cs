namespace AgentGenCli.Cli.Scaffolding;

using System.Text.Json;

internal static class ProjectScaffolder
{
    private const string CqrsSubmoduleUrl =
        "https://github.com/3DExtended/Prodot.Patterns.Cqrs.git";

    public static int ScaffoldProject(string projectName, string frontend)
    {
        var backendResult = ScaffoldDotnetBackend(projectName, frontend);
        if (backendResult != 0)
        {
            return backendResult;
        }

        if (string.Equals(frontend, "none", StringComparison.OrdinalIgnoreCase))
        {
            PrintSuccessMessage();
            return 0;
        }

        if (!string.Equals(frontend, "flutter", StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine($"Unknown frontend template '{frontend}'. Supported: flutter, none.");
            return 1;
        }

        var root = Directory.GetCurrentDirectory();
        var flutterResult = FlutterScaffolder.Scaffold(projectName);
        if (flutterResult != 0)
        {
            return flutterResult;
        }

        Console.WriteLine("Syncing OpenAPI spec and Flutter client...");
        var context = ProjectContext.Resolve(workingDirectory: root);
        if (OpenApiSyncHelper.Sync(context, recordManifest: false) != 0)
        {
            Console.Error.WriteLine("Error syncing OpenAPI.");
            return 1;
        }

        Console.WriteLine("Generating initial golden screenshots...");
        if (FlutterCommandHelper.RunFlutter(context, "test --update-goldens") != 0)
        {
            Console.Error.WriteLine("Error generating golden screenshots.");
            return 1;
        }

        Console.WriteLine("Running Flutter tests...");
        if (FlutterCommandHelper.RunFlutter(context, "test") != 0)
        {
            Console.Error.WriteLine("Error running Flutter tests.");
            return 1;
        }

        ProjectManifest.RecordCommand(
            root,
            new ManifestCommandEntry
            {
                Command = "init flutter",
                Args = new Dictionary<string, JsonElement>
                {
                    ["projectName"] = JsonSerializer.SerializeToElement(projectName),
                },
            }
        );

        ProjectManifest.RecordCommand(
            root,
            new ManifestCommandEntry { Command = "project sync-openapi" }
        );

        AgentDocsGenerator.RefreshState(root);

        PrintSuccessMessage();
        return 0;
    }

    public static int ScaffoldDotnetBackend(string projectName, string frontend = "none")
    {
        var tokens = new TemplateTokens { ProjectName = projectName };
        var root = Directory.GetCurrentDirectory();

        Console.WriteLine("Initializing git repository...");
        if (ProcessRunner.Run("git", "init") != 0)
        {
            Console.Error.WriteLine("Error initializing git repository.");
            return 1;
        }

        Console.WriteLine("Writing root project files...");
        CopyRootFiles(root, tokens);

        Console.WriteLine("Creating .NET solution...");
        if (ProcessRunner.Run("dotnet", $"new sln --name {projectName}") != 0)
        {
            Console.Error.WriteLine("Error creating .NET solution.");
            return 1;
        }

        Console.WriteLine("Adding submodule for Prodot.Patterns.Cqrs...");
        if (
            ProcessRunner.Run(
                "git",
                $"submodule add {CqrsSubmoduleUrl} extern/Prodot.Patterns.Cqrs"
            ) != 0
        )
        {
            Console.Error.WriteLine("Error adding git submodule.");
            return 1;
        }

        var apiDir = Path.Combine("applications", $"{projectName}.Api");
        Console.WriteLine("Creating .NET Web API project...");
        if (ProcessRunner.Run("dotnet", $"new webapi --name {projectName}.Api -o {apiDir}") != 0)
        {
            Console.Error.WriteLine("Error creating .NET Web API project.");
            return 1;
        }

        if (!RemoveDefaultOpenApiPackage(projectName))
        {
            return 1;
        }

        var commonDir = Path.Combine("common", $"{projectName}.Common");
        Console.WriteLine("Creating common project...");
        if (ProcessRunner.Run("dotnet", $"new classlib --name {projectName}.Common -o {commonDir}") != 0)
        {
            Console.Error.WriteLine("Error creating common project.");
            return 1;
        }

        var commonTestsDir = Path.Combine("tests", $"{projectName}.Common.Tests");
        var apiTestsDir = Path.Combine("tests", $"{projectName}.Api.Tests");
        Console.WriteLine("Creating test projects...");
        if (ProcessRunner.Run("dotnet", $"new xunit --name {projectName}.Common.Tests -o {commonTestsDir}") != 0)
        {
            Console.Error.WriteLine("Error creating common test project.");
            return 1;
        }

        if (ProcessRunner.Run("dotnet", $"new xunit --name {projectName}.Api.Tests -o {apiTestsDir}") != 0)
        {
            Console.Error.WriteLine("Error creating API test project.");
            return 1;
        }

        Console.WriteLine("Applying project templates...");
        ApplyProjectTemplates(root, tokens);

        Console.WriteLine("Adding projects to solution...");
        if (!AddProjectsToSolution(projectName))
        {
            return 1;
        }

        Console.WriteLine("Adding project references...");
        if (!AddProjectReferences(projectName))
        {
            return 1;
        }

        Console.WriteLine("Adding NuGet packages...");
        if (!AddNuGetPackages(projectName))
        {
            return 1;
        }

        Console.WriteLine("Writing Docker files...");
        CopyDockerFiles(root, tokens);

        if (!FormatGeneratedCode())
        {
            return 1;
        }

        Console.WriteLine("Building solution before migrations...");
        if (ProcessRunner.Run("dotnet", "build") != 0)
        {
            Console.Error.WriteLine("Error building solution before migrations.");
            return 1;
        }

        Console.WriteLine("Creating initial EF Core migration...");
        if (
            ProcessRunner.Run(
                "dotnet",
                $"ef migrations add Initial --project {commonDir}/{projectName}.Common.csproj --startup-project {apiDir}/{projectName}.Api.csproj"
            ) != 0
        )
        {
            Console.Error.WriteLine("Error creating EF Core migration.");
            return 1;
        }

        if (!FormatGeneratedCode())
        {
            return 1;
        }

        Console.WriteLine("Building solution...");
        if (ProcessRunner.Run("dotnet", "build") != 0)
        {
            Console.Error.WriteLine("Error building solution.");
            return 1;
        }

        Console.WriteLine("Running tests...");
        if (ProcessRunner.Run("dotnet", "test") != 0)
        {
            Console.Error.WriteLine("Error running tests.");
            return 1;
        }

        Console.WriteLine("Writing project manifest...");
        ProjectManifest.Save(root, ProjectManifest.CreateInitial(projectName));
        ProjectManifest.RecordCommand(
            root,
            new ManifestCommandEntry
            {
                Command = "init project",
                Args = new Dictionary<string, JsonElement>
                {
                    ["projectName"] = JsonSerializer.SerializeToElement(projectName),
                    ["backend"] = JsonSerializer.SerializeToElement("dotnet"),
                    ["frontend"] = JsonSerializer.SerializeToElement(frontend),
                },
            }
        );

        Console.WriteLine("Writing agent onboarding docs...");
        AgentDocsGenerator.ScaffoldInitial(
            root,
            tokens,
            hasBackend: true,
            hasFlutter: string.Equals(frontend, "flutter", StringComparison.OrdinalIgnoreCase)
        );

        return 0;
    }

    private static void PrintSuccessMessage()
    {
        Console.WriteLine();
        Console.WriteLine("Project scaffolded successfully.");
        Console.WriteLine("  docker compose up -d --build");
        Console.WriteLine("  http://localhost:8080/health");
    }

    private static void CopyRootFiles(string root, TemplateTokens tokens)
    {
        TemplateEngine.CopyFile(".gitignore", Path.Combine(root, ".gitignore"), tokens);
        TemplateEngine.CopyFile(
            "Directory.Build.props.template",
            Path.Combine(root, "Directory.Build.props"),
            tokens
        );
        TemplateEngine.CopyFile("stylecop.json", Path.Combine(root, "stylecop.json"), tokens);
        TemplateEngine.CopyFile(
            "pesser-Default.ruleset",
            Path.Combine(root, "pesser-Default.ruleset"),
            tokens
        );

        File.WriteAllText(Path.Combine(root, "features", ".gitkeep"), string.Empty);

        var dotnetToolsDir = Path.Combine(root, ".config");
        Directory.CreateDirectory(dotnetToolsDir);
        TemplateEngine.CopyFile(".config/dotnet-tools.json", Path.Combine(dotnetToolsDir, "dotnet-tools.json"), tokens);
    }

    private static void ApplyProjectTemplates(string root, TemplateTokens tokens)
    {
        CopyTemplateDirectory("common/ProjectName.Common", Path.Combine(root, "common", $"{tokens.ProjectName}.Common"), tokens);
        CopyTemplateDirectory(
            "applications/ProjectName.Api",
            Path.Combine(root, "applications", $"{tokens.ProjectName}.Api"),
            tokens
        );
        CopyTemplateDirectory(
            "tests/ProjectName.Common.Tests",
            Path.Combine(root, "tests", $"{tokens.ProjectName}.Common.Tests"),
            tokens
        );
        CopyTemplateDirectory(
            "tests/ProjectName.Api.Tests",
            Path.Combine(root, "tests", $"{tokens.ProjectName}.Api.Tests"),
            tokens
        );

        RemoveIfExists(Path.Combine(root, "common", $"{tokens.ProjectName}.Common", "Class1.cs"));
        RemoveIfExists(Path.Combine(root, "tests", $"{tokens.ProjectName}.Common.Tests", "UnitTest1.cs"));
        RemoveIfExists(Path.Combine(root, "tests", $"{tokens.ProjectName}.Api.Tests", "UnitTest1.cs"));
        RemoveWeatherForecast(root, tokens.ProjectName);
    }

    private static void CopyDockerFiles(string root, TemplateTokens tokens)
    {
        TemplateEngine.CopyFile(".dockerignore", Path.Combine(root, ".dockerignore"), tokens);
        TemplateEngine.CopyFile("docker-compose.yml", Path.Combine(root, "docker-compose.yml"), tokens);
        TemplateEngine.CopyFile(
            "applications/ProjectName.Api/Dockerfile",
            Path.Combine(root, "applications", $"{tokens.ProjectName}.Api", "Dockerfile"),
            tokens
        );
    }

    private static void CopyTemplateDirectory(
        string templateRelativePath,
        string destinationPath,
        TemplateTokens tokens
    )
    {
        var sourceRoot = Path.Combine(TemplateEngine.GetTemplateRoot(), templateRelativePath);
        if (!Directory.Exists(sourceRoot))
        {
            throw new DirectoryNotFoundException($"Template directory not found: {sourceRoot}");
        }

        foreach (var sourceFile in Directory.EnumerateFiles(sourceRoot, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceRoot, sourceFile);
            var targetRelativePath = ResolveTemplateOutputPath(TemplateEngineReplace(relativePath, tokens));
            var targetPath = Path.Combine(destinationPath, targetRelativePath);
            var directory = Path.GetDirectoryName(targetPath);
            if (directory != null)
            {
                Directory.CreateDirectory(directory);
            }

            var content = TemplateEngineReplace(File.ReadAllText(sourceFile), tokens);
            WriteUtf8WithBom(targetPath, content);
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

    private static void WriteUtf8WithBom(string path, string content)
    {
        File.WriteAllText(path, content, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }

    private static string TemplateEngineReplace(string input, TemplateTokens tokens)
    {
        var result = input;
        foreach (var (token, value) in tokens.ToDictionary())
        {
            result = result.Replace(token, value, StringComparison.Ordinal);
        }

        result = result.Replace("ProjectName", tokens.ProjectName, StringComparison.Ordinal);
        return result;
    }

    private static bool AddProjectsToSolution(string projectName)
    {
        var projects = new[]
        {
            $"applications/{projectName}.Api/{projectName}.Api.csproj",
            $"common/{projectName}.Common/{projectName}.Common.csproj",
            $"tests/{projectName}.Common.Tests/{projectName}.Common.Tests.csproj",
            $"tests/{projectName}.Api.Tests/{projectName}.Api.Tests.csproj",
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

    private static bool AddProjectReferences(string projectName)
    {
        var apiProject = $"applications/{projectName}.Api/{projectName}.Api.csproj";
        var commonProject = $"common/{projectName}.Common/{projectName}.Common.csproj";
        var cqrsDiProject =
            "extern/Prodot.Patterns.Cqrs/Prodot.Patterns.Cqrs.MicrosoftExtensionsDependencyInjection/Prodot.Patterns.Cqrs.MicrosoftExtensionsDependencyInjection.csproj";

        return RunDotnetAddReference(apiProject, commonProject)
            && RunDotnetAddReference(apiProject, cqrsDiProject);
    }

    private static bool RunDotnetAddReference(string project, string reference)
    {
        var exitCode = ProcessRunner.Run("dotnet", $"add \"{project}\" reference \"{reference}\"");
        return exitCode == 0;
    }

    private static bool RemoveDefaultOpenApiPackage(string projectName)
    {
        var apiProject = $"applications/{projectName}.Api/{projectName}.Api.csproj";
        return ProcessRunner.Run("dotnet", $"remove \"{apiProject}\" package Microsoft.AspNetCore.OpenApi") == 0;
    }

    private static bool AddNuGetPackages(string projectName)
    {
        var apiProject = $"applications/{projectName}.Api/{projectName}.Api.csproj";
        return ProcessRunner.Run(
                "dotnet",
                $"add \"{apiProject}\" package Microsoft.EntityFrameworkCore.Design --version 10.0.5"
            )
            == 0
            && ProcessRunner.Run(
                "dotnet",
                $"add \"{apiProject}\" package Swashbuckle.AspNetCore --version 9.0.6"
            )
            == 0;
    }

    private static bool FormatGeneratedCode()
    {
        Console.WriteLine("Formatting generated code (StyleCop / analyzers)...");

        if (ProcessRunner.Run("dotnet", "restore") != 0)
        {
            Console.Error.WriteLine("Error restoring solution before format.");
            return false;
        }

        if (
            ProcessRunner.Run(
                "dotnet",
                "format analyzers --include applications/ --include common/ --include tests/"
            ) != 0
        )
        {
            Console.Error.WriteLine("Error running dotnet format analyzers.");
            return false;
        }

        return true;
    }

    private static void RemoveWeatherForecast(string root, string projectName)
    {
        var apiDir = Path.Combine(root, "applications", $"{projectName}.Api");
        var weatherForecast = Path.Combine(apiDir, "WeatherForecast.cs");
        RemoveIfExists(weatherForecast);

        foreach (var file in Directory.EnumerateFiles(Path.Combine(apiDir, "Controllers"), "*.cs"))
        {
            if (file.Contains("WeatherForecast", StringComparison.OrdinalIgnoreCase))
            {
                RemoveIfExists(file);
            }
        }
    }

    private static void RemoveIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
