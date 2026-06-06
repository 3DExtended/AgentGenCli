using System.Text.Json;

namespace AgentGenCli.Cli.Scaffolding;

internal static class OpenApiSyncHelper
{
    public static int Sync(ProjectContext context, bool recordManifest = true)
    {
        if (!Directory.Exists(context.FlutterAppDir))
        {
            Console.Error.WriteLine(
                $"Flutter app not found at '{context.FlutterAppRelativePath}'. Run 'agentGenCli init project' with frontend=flutter first."
            );
            return 1;
        }

        Console.WriteLine("Restoring .NET tools...");
        if (ProcessRunner.Run("dotnet", "tool restore", context.Root) != 0)
        {
            Console.Error.WriteLine("Error restoring .NET tools.");
            return 1;
        }

        Console.WriteLine("Building API project...");
        var apiProjectRelative = Path.GetRelativePath(context.Root, context.ApiProjectPath);
        if (ProcessRunner.Run("dotnet", $"build \"{apiProjectRelative}\"", context.Root) != 0)
        {
            Console.Error.WriteLine("Error building API project.");
            return 1;
        }

        var apiDir = Path.GetDirectoryName(context.ApiProjectPath)!;
        var configuration = "Debug";
        var dllPath = Path.Combine(
            apiDir,
            "bin",
            configuration,
            "net10.0",
            $"{context.ProjectName}.Api.dll"
        );

        if (!File.Exists(dllPath))
        {
            Console.Error.WriteLine($"API assembly not found at '{dllPath}'.");
            return 1;
        }

        Directory.CreateDirectory(Path.Combine(context.FlutterAppDir, "swagger"));
        var swaggerOutput = Path.Combine(context.FlutterAppDir, "swagger", "swagger.json");
        Console.WriteLine($"Exporting OpenAPI spec to '{swaggerOutput}'...");
        if (
            ProcessRunner.Run(
                "dotnet",
                $"swagger tofile --output \"{swaggerOutput}\" \"{dllPath}\" v1.0",
                apiDir,
                new Dictionary<string, string> { ["ASPNETCORE_ENVIRONMENT"] = "E2ETest" }
            )
            != 0
        )
        {
            Console.Error.WriteLine("Error exporting swagger.json.");
            return 1;
        }

        Console.WriteLine("Running Dart build_runner...");
        var swaggenDir = Path.Combine(context.FlutterAppDir, "lib", "generated", "swaggen");
        if (Directory.Exists(swaggenDir))
        {
            foreach (var file in Directory.EnumerateFiles(swaggenDir))
            {
                File.Delete(file);
            }
        }

        if (
            FlutterCommandHelper.RunDart(
                context,
                "run build_runner build --delete-conflicting-outputs"
            )
            != 0
        )
        {
            Console.Error.WriteLine("Error running build_runner.");
            return 1;
        }

        if (FlutterCommandHelper.RunFlutter(context, "gen-l10n") != 0)
        {
            Console.Error.WriteLine("Error running flutter gen-l10n.");
            return 1;
        }

        if (recordManifest)
        {
            ProjectManifest.RecordCommand(
                context.Root,
                new ManifestCommandEntry { Command = "project sync-openapi" }
            );
            AgentDocsGenerator.RefreshState(context.Root);
        }

        Console.WriteLine("OpenAPI sync completed.");
        return 0;
    }
}
