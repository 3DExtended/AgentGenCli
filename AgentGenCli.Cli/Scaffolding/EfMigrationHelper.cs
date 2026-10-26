using System.Text.Json;

namespace AgentGenCli.Cli.Scaffolding;

internal static class EfMigrationHelper
{
    public static bool MigrationExists(ProjectContext context, string migrationName)
    {
        var migrationsDir = Path.Combine(
            Path.GetDirectoryName(context.CommonProjectPath)!,
            "Migrations"
        );

        if (!Directory.Exists(migrationsDir))
        {
            return false;
        }

        return Directory
            .EnumerateFiles(migrationsDir, "*.cs")
            .Any(path => Path.GetFileName(path).Contains(migrationName, StringComparison.Ordinal));
    }

    public static int AddMigrationIfMissing(ProjectContext context, string migrationName)
    {
        if (MigrationExists(context, migrationName))
        {
            Console.WriteLine($"Migration '{migrationName}' already exists; skipping.");
            return 0;
        }

        return AddMigration(context, migrationName);
    }

    public static int AddMigration(ProjectContext context, string migrationName)
    {
        Console.WriteLine($"Creating EF Core migration '{migrationName}'...");
        var commonProject = Path.GetRelativePath(context.Root, context.CommonProjectPath);
        var apiProject = Path.GetRelativePath(context.Root, context.ApiProjectPath);
        var contextName = $"{context.ProjectName}DbContext";

        return ProcessRunner.Run(
            "dotnet",
            $"ef migrations add {migrationName} --project \"{commonProject}\" --startup-project \"{apiProject}\" --context {contextName}"
        );
    }

    public static int AddMigrationAndRecord(ProjectContext context, string migrationName) =>
        AddMigrationAndRecord(context, migrationName, AddMigration);

    // `addMigration` is injectable so tests can cover the manifest wiring without running `dotnet ef`.
    internal static int AddMigrationAndRecord(
        ProjectContext context,
        string migrationName,
        Func<ProjectContext, string, int> addMigration
    )
    {
        var exitCode = addMigration(context, migrationName);

        // The manifest is optional (ProjectContext.Resolve works without it), so only record when it exists.
        if (exitCode != 0 || !File.Exists(Path.Combine(context.Root, ProjectManifest.FileName)))
        {
            return exitCode;
        }

        ProjectManifest.RecordCommand(
            context.Root,
            new ManifestCommandEntry
            {
                Command = "new efmigration",
                Args = new Dictionary<string, JsonElement>
                {
                    ["name"] = JsonSerializer.SerializeToElement(migrationName),
                },
            }
        );
        AgentDocsGenerator.RefreshState(context.Root);

        return 0;
    }

    public static int ApplyMigrations(ProjectContext context)
    {
        Console.WriteLine("Applying EF Core migrations...");
        var commonProject = Path.GetRelativePath(context.Root, context.CommonProjectPath);
        var apiProject = Path.GetRelativePath(context.Root, context.ApiProjectPath);
        var contextName = $"{context.ProjectName}DbContext";

        return ProcessRunner.Run(
            "dotnet",
            $"ef database update --project \"{commonProject}\" --startup-project \"{apiProject}\" --context {contextName}"
        );
    }
}
