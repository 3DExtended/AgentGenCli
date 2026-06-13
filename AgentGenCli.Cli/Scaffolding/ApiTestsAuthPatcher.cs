namespace AgentGenCli.Cli.Scaffolding;

internal static class ApiTestsAuthPatcher
{
    public static void Apply(ProjectContext context)
    {
        var path = Path.Combine(
            context.Root,
            "tests",
            $"{context.ProjectName}.Api.Tests",
            $"{context.ProjectName}.Api.Tests.csproj"
        );
        if (!File.Exists(path))
        {
            return;
        }

        CsprojPackageHelper.AddPackageIfMissing(
            path,
            "Microsoft.AspNetCore.Authentication.JwtBearer",
            "10.0.5"
        );
        CsprojPackageHelper.AddPackageIfMissing(path, "Microsoft.Data.Sqlite", "10.0.5");
        CsprojPackageHelper.AddPackageIfMissing(path, "Microsoft.EntityFrameworkCore.Sqlite", "10.0.5");
        CsprojPackageHelper.AddPackageIfMissing(path, "NSubstitute", "5.3.0");
        CsprojPackageHelper.AddPackageIfMissing(path, "System.IdentityModel.Tokens.Jwt", "8.9.0");

        AddProjectReferenceIfMissing(
            path,
            context.FeatureProjectPath(FeatureNameNormalizer.Normalize("users"))
        );
        AddProjectReferenceIfMissing(
            path,
            context.FeatureContractsPath(FeatureNameNormalizer.Normalize("users"))
        );

        var sendGridProject = Path.Combine(
            context.Root,
            "common",
            $"{context.ProjectName}.Common.SendGrid",
            $"{context.ProjectName}.Common.SendGrid.csproj"
        );
        if (File.Exists(sendGridProject))
        {
            AddProjectReferenceIfMissing(path, sendGridProject);
        }
    }

    private static void AddProjectReferenceIfMissing(string projectPath, string referencePath)
    {
        var content = File.ReadAllText(projectPath);
        var referenceName = Path.GetFileNameWithoutExtension(referencePath);
        if (content.Contains(referenceName, StringComparison.Ordinal))
        {
            return;
        }

        ProcessRunner.Run("dotnet", $"add \"{projectPath}\" reference \"{referencePath}\"");
    }
}
