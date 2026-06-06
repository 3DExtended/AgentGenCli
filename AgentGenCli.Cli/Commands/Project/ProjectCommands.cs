using System.CommandLine;
using AgentGenCli.Cli.Scaffolding;

namespace AgentGenCli.Cli.Commands.Project;

internal static class ProjectCommands
{
    public static Command Create()
    {
        var projectCommand = new Command("project", "Project maintenance commands");

        var projectOption = CreateProjectOption();
        projectCommand.Options.Add(projectOption);
        projectCommand.Subcommands.Add(CreateEfMigrateCommand(projectOption));
        projectCommand.Subcommands.Add(CreateSyncOpenApiCommand(projectOption));

        return projectCommand;
    }

    private static Command CreateSyncOpenApiCommand(Option<string?> projectOption)
    {
        var syncOpenApiCommand = new Command("sync-openapi", "Export swagger.json and regenerate Flutter API client");

        syncOpenApiCommand.Options.Add(projectOption);
        syncOpenApiCommand.SetAction(parseResult =>
        {
            try
            {
                var context = ProjectContext.Resolve(projectFlag: parseResult.GetValue(projectOption));
                return OpenApiSyncHelper.Sync(context);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 1;
            }
        });

        return syncOpenApiCommand;
    }

    private static Command CreateEfMigrateCommand(Option<string?> projectOption)
    {
        var efMigrateCommand = new Command("efmigrate", "Apply pending EF Core migrations");

        efMigrateCommand.Options.Add(projectOption);
        efMigrateCommand.SetAction(parseResult =>
        {
            try
            {
                var context = ProjectContext.Resolve(projectFlag: parseResult.GetValue(projectOption));
                return EfMigrationHelper.ApplyMigrations(context);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 1;
            }
        });

        return efMigrateCommand;
    }

    private static Option<string?> CreateProjectOption() =>
        new("--project")
        {
            Description = "Project name (defaults to .agentGenCli.json / solution name)",
        };
}
