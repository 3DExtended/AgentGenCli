using System.CommandLine;
using AgentGenCli.Cli.Templates;

namespace AgentGenCli.Cli.Commands.New;

internal static class NewCommands
{
    public static Command Create()
    {
        var newCommand = new Command("new", "Scaffold a new feature from a template");

        newCommand.Options.Add(CliOptions.List);
        newCommand.Subcommands.Add(CreateBackendFeatureCommand());
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

    private static Command CreateBackendFeatureCommand()
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

        var backendFeatureCommand = new Command("backend-feature", "Scaffold a new backend feature")
        {
            nameArgument,
        };

        backendFeatureCommand.Options.Add(withDatabaseOption);
        backendFeatureCommand.Options.Add(withApiOption);

        backendFeatureCommand.SetAction(parseResult =>
        {
            var name = parseResult.GetValue(nameArgument);
            var withDatabase = parseResult.GetValue(withDatabaseOption);
            var withApi = parseResult.GetValue(withApiOption);

            Console.WriteLine(
                $"Creating backend feature '{name}' (withDatabase={withDatabase}, withApi={withApi})"
            );
            return 0;
        });

        return backendFeatureCommand;
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
}
