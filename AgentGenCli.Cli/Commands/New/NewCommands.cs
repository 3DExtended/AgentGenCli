using System.CommandLine;
using AgentGenCli.Cli.Scaffolding;
using AgentGenCli.Cli.Templates;

namespace AgentGenCli.Cli.Commands.New;

internal static class NewCommands
{
    public static Command Create()
    {
        var newCommand = new Command("new", "Scaffold a new feature from a template");

        var projectOption = CreateProjectOption();
        var yesOption = CreateYesOption();

        newCommand.Options.Add(CliOptions.List);
        newCommand.Options.Add(projectOption);
        newCommand.Subcommands.Add(CreateBackendFeatureCommand(projectOption, yesOption));
        newCommand.Subcommands.Add(CreateEfMigrationCommand(projectOption));
        newCommand.Subcommands.Add(CreateFrontendFeatureCommand());

        newCommand.SetAction(parseResult =>
        {
            if (parseResult.GetValue(CliOptions.List))
            {
                TemplateCatalog.PrintNewTemplates();
                return 0;
            }

            Console.Error.WriteLine(
                "Specify a subcommand. Run 'agentGenCli new --help' for usage."
            );
            return 1;
        });

        return newCommand;
    }

    private static Command CreateBackendFeatureCommand(Option<string?> projectOption, Option<bool> yesOption)
    {
        var nameArgument = new Argument<string>("name") { Description = "Feature name" };

        var withDatabaseOption = new Option<bool>("--withDatabase")
        {
            Description = "Store something for that feature in a database",
        };

        var withApiOption = new Option<bool>("--withApi")
        {
            Description = "Create API endpoints for that feature",
        };

        var crudOption = new Option<string?>("--crud")
        {
            Description = "CRUD letters to scaffold when --withDatabase is set (default CRUD)",
        };

        var backendFeatureCommand = new Command("backend-feature", "Scaffold a new backend feature")
        {
            nameArgument,
        };

        backendFeatureCommand.Options.Add(withDatabaseOption);
        backendFeatureCommand.Options.Add(withApiOption);
        backendFeatureCommand.Options.Add(crudOption);
        backendFeatureCommand.Options.Add(projectOption);
        backendFeatureCommand.Options.Add(yesOption);

        backendFeatureCommand.SetAction(parseResult =>
        {
            var name = parseResult.GetValue(nameArgument);
            if (name == null)
            {
                Console.Error.WriteLine("Feature name is required.");
                return 1;
            }

            return FeatureScaffolder.Scaffold(
                new FeatureScaffoldRequest
                {
                    FeatureInput = name,
                    WithDatabase = parseResult.GetValue(withDatabaseOption),
                    WithApi = parseResult.GetValue(withApiOption),
                    Crud = parseResult.GetValue(crudOption),
                    ProjectFlag = parseResult.GetValue(projectOption),
                    Yes = parseResult.GetValue(yesOption),
                }
            );
        });

        return backendFeatureCommand;
    }

    private static Command CreateEfMigrationCommand(Option<string?> projectOption)
    {
        var nameArgument = new Argument<string>("name") { Description = "Migration name" };

        var efMigrationCommand = new Command("efmigration", "Create a new EF Core migration")
        {
            nameArgument,
        };

        efMigrationCommand.Options.Add(projectOption);
        efMigrationCommand.SetAction(parseResult =>
        {
            var name = parseResult.GetValue(nameArgument);
            if (name == null)
            {
                Console.Error.WriteLine("Migration name is required.");
                return 1;
            }

            try
            {
                var context = ProjectContext.Resolve(projectFlag: parseResult.GetValue(projectOption));
                return EfMigrationHelper.AddMigration(context, name);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 1;
            }
        });

        return efMigrationCommand;
    }

    private static Command CreateFrontendFeatureCommand()
    {
        var nameArgument = new Argument<string>("name") { Description = "Feature name" };

        var frontendFeatureCommand = new Command(
            "frontend-feature",
            "Scaffold a new frontend feature"
        )
        {
            nameArgument,
        };

        frontendFeatureCommand.SetAction(parseResult =>
        {
            var name = parseResult.GetValue(nameArgument);
            Console.WriteLine($"Creating frontend feature '{name}'");
            return 0;
        });

        return frontendFeatureCommand;
    }

    private static Option<string?> CreateProjectOption() =>
        new("--project")
        {
            Description = "Project name (defaults to .agentGenCli.json / solution name)",
        };

    private static Option<bool> CreateYesOption() =>
        new("--yes")
        {
            Description = "Skip confirmation prompt",
        };
}
