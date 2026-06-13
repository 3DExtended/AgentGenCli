namespace AgentGenCli.Cli.Scaffolding;

internal sealed class ProjectContext
{
    public required string Root { get; init; }

    public required string ProjectName { get; init; }

    public string SolutionPath { get; init; } = string.Empty;

    public string CommonProjectPath { get; init; } = string.Empty;

    public string ApiProjectPath { get; init; } = string.Empty;

    public string CommonTestsProjectPath { get; init; } = string.Empty;

    public string ApiTestsProjectPath { get; init; } = string.Empty;

    public static ProjectContext Resolve(string? workingDirectory = null, string? projectFlag = null)
    {
        var root = Path.GetFullPath(workingDirectory ?? Directory.GetCurrentDirectory());
        ValidateLayout(root);

        var slnFiles = Directory.GetFiles(root, "*.sln");
        if (slnFiles.Length != 1)
        {
            throw new InvalidOperationException(
                $"Expected exactly one .sln file in '{root}', found {slnFiles.Length}."
            );
        }

        var projectName = ResolveProjectName(root, projectFlag, Path.GetFileNameWithoutExtension(slnFiles[0]));

        return new ProjectContext
        {
            Root = root,
            ProjectName = projectName,
            SolutionPath = slnFiles[0],
            CommonProjectPath = Path.Combine(root, "common", $"{projectName}.Common", $"{projectName}.Common.csproj"),
            ApiProjectPath = Path.Combine(root, "applications", $"{projectName}.Api", $"{projectName}.Api.csproj"),
            CommonTestsProjectPath = Path.Combine(
                root,
                "tests",
                $"{projectName}.Common.Tests",
                $"{projectName}.Common.Tests.csproj"
            ),
            ApiTestsProjectPath = Path.Combine(
                root,
                "tests",
                $"{projectName}.Api.Tests",
                $"{projectName}.Api.Tests.csproj"
            ),
        };
    }

    public string FeatureRoot(FeatureNameInfo feature) =>
        Path.Combine(Root, "features", feature.FolderName);

    public string FeatureProjectDir(FeatureNameInfo feature) =>
        Path.Combine(FeatureRoot(feature), $"{ProjectName}.Features.{feature.PascalName}");

    public string FeatureContractsDir(FeatureNameInfo feature) =>
        Path.Combine(FeatureRoot(feature), $"{ProjectName}.Features.{feature.PascalName}.Contracts");

    public string FeatureTestsDir(FeatureNameInfo feature) =>
        Path.Combine(Root, "tests", $"{ProjectName}.Features.{feature.PascalName}.Tests");

    public string FeatureProjectPath(FeatureNameInfo feature) =>
        Path.Combine(FeatureProjectDir(feature), $"{ProjectName}.Features.{feature.PascalName}.csproj");

    public string FeatureContractsPath(FeatureNameInfo feature) =>
        Path.Combine(
            FeatureContractsDir(feature),
            $"{ProjectName}.Features.{feature.PascalName}.Contracts.csproj"
        );

    public string FeatureTestsPath(FeatureNameInfo feature) =>
        Path.Combine(FeatureTestsDir(feature), $"{ProjectName}.Features.{feature.PascalName}.Tests.csproj");

    public string FeatureRegistrationPath =>
        Path.Combine(Root, "applications", $"{ProjectName}.Api", "FeatureRegistration.cs");

    public string UsersContractsPath =>
        Path.Combine(
            Root,
            "features",
            "users",
            $"{ProjectName}.Features.Users.Contracts",
            $"{ProjectName}.Features.Users.Contracts.csproj"
        );

    public string UsersProjectPath =>
        Path.Combine(
            Root,
            "features",
            "users",
            $"{ProjectName}.Features.Users",
            $"{ProjectName}.Features.Users.csproj"
        );

    public string FlutterAppDir => Path.Combine(Root, "applications", ProjectName.ToLowerInvariant());

    public string FlutterAppRelativePath =>
        Path.Combine("applications", ProjectName.ToLowerInvariant());

    public string FlutterRouterPath =>
        Path.Combine(FlutterAppDir, "lib", "core", "router", "app_router.dart");

    private static void ValidateLayout(string root)
    {
        var requiredDirectories = new[] { "applications", "common", "features", "tests" };
        foreach (var directory in requiredDirectories)
        {
            if (!Directory.Exists(Path.Combine(root, directory)))
            {
                throw new InvalidOperationException(
                    $"Missing required directory '{directory}/'. Run this command from an agentGenCli project root."
                );
            }
        }
    }

    private static string ResolveProjectName(string root, string? projectFlag, string slnName)
    {
        if (!string.IsNullOrWhiteSpace(projectFlag))
        {
            if (!string.Equals(projectFlag, slnName, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"--project '{projectFlag}' does not match solution name '{slnName}'."
                );
            }

            return projectFlag;
        }

        if (File.Exists(Path.Combine(root, ProjectManifest.FileName)))
        {
            var manifest = ProjectManifest.Load(root);
            if (!string.Equals(manifest.ProjectName, slnName, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Manifest projectName '{manifest.ProjectName}' does not match solution name '{slnName}'."
                );
            }

            return manifest.ProjectName;
        }

        return slnName;
    }
}
