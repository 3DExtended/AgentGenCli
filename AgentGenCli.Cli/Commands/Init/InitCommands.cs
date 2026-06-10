using System.CommandLine;
using AgentGenCli.Cli.Scaffolding;
using AgentGenCli.Cli.Templates;

namespace AgentGenCli.Cli.Commands.Init;

internal static class InitCommands
{
    public static Command Create()
    {
        var initCommand = new Command("init", "Initialize a new project or project component");

        initCommand.Options.Add(CliOptions.List);
        initCommand.Subcommands.Add(CreateProjectCommand());
        initCommand.Subcommands.Add(CreateEmailCommand());
        initCommand.Subcommands.Add(CreateAuthCommand());

        initCommand.SetAction(parseResult =>
        {
            if (parseResult.GetValue(CliOptions.List))
            {
                TemplateCatalog.PrintInitTemplates();
                return 0;
            }

            Console.Error.WriteLine(
                "Specify a subcommand. Run 'agentGenCli init --help' for usage."
            );
            return 1;
        });

        return initCommand;
    }

    private static Command CreateProjectCommand()
    {
        var nameArgument = new Argument<string>("name") { Description = "Project name" };

        var backendArgument = new Argument<string>("backend")
        {
            Description = "Backend template: dotnet (default)",
            DefaultValueFactory = _ => "dotnet",
        };

        var frontendArgument = new Argument<string>("frontend")
        {
            Description = "Frontend template: flutter (default) or none for backend-only",
            DefaultValueFactory = _ => "flutter",
        };

        var hereOption = new Option<bool>("--here")
        {
            Description = "Initialize in the current directory even when it is not empty",
        };

        var projectCommand = new Command(
            "project",
            "Initialize a new project (pass frontend 'none' for backend-only)"
        )
        {
            nameArgument,
            backendArgument,
            frontendArgument,
            hereOption,
        };

        projectCommand.SetAction(parseResult =>
        {
            var name = parseResult.GetValue(nameArgument);
            var backend = parseResult.GetValue(backendArgument)!;
            var frontend = parseResult.GetValue(frontendArgument)!;
            var initializeHere = parseResult.GetValue(hereOption);

            if (name == null)
            {
                Console.Error.WriteLine("Project name is required.");
                return 1;
            }

            Console.WriteLine(
                $"Initializing project '{name}' with backend={backend}, frontend={frontend}"
            );

            var currentDirectory = Directory.GetCurrentDirectory();
            var currentDirectoryIsEmpty =
                Directory.GetFileSystemEntries(currentDirectory).Length == 0;

            if (initializeHere || currentDirectoryIsEmpty)
            {
                if (currentDirectoryIsEmpty)
                {
                    Console.WriteLine("Current directory is empty, initializing project here.");
                }
                else
                {
                    Console.WriteLine(
                        "Initializing project in the current directory (--here)."
                    );
                }
            }
            else
            {
                var projectDir = Path.Combine(currentDirectory, name);
                if (!Directory.Exists(projectDir))
                {
                    Directory.CreateDirectory(projectDir);
                    Console.WriteLine($"Created directory '{projectDir}' for project.");
                }

                Directory.SetCurrentDirectory(projectDir);
            }

            Console.WriteLine("Creating project directories...");
            Directory.CreateDirectory(Path.Combine("applications"));
            Directory.CreateDirectory(Path.Combine("features"));
            Directory.CreateDirectory(Path.Combine("common"));
            Directory.CreateDirectory(Path.Combine("extern"));
            Directory.CreateDirectory(Path.Combine("tests"));

            if (backend == "dotnet")
            {
                return ProjectScaffolder.ScaffoldProject(name, frontend);
            }

            Console.WriteLine($"Unknown backend template '{backend}'");
            return 1;
        });

        return projectCommand;
    }

    private static Command CreateEmailCommand()
    {
        var projectOption = new Option<string?>("--project")
        {
            Description = "Project name (defaults to .agentGenCli.json / solution name)",
        };
        var yesOption = new Option<bool>("--yes")
        {
            Description = "Skip confirmation prompt",
        };

        var emailCommand = new Command("email", "Initialize SendGrid email scaffolding")
        {
            projectOption,
            yesOption,
        };

        emailCommand.SetAction(parseResult =>
        {
            return EmailScaffolder.Scaffold(
                new EmailScaffoldRequest
                {
                    ProjectFlag = parseResult.GetValue(projectOption),
                    Yes = parseResult.GetValue(yesOption),
                }
            );
        });

        return emailCommand;
    }

    private static Command CreateAuthCommand()
    {
        var projectOption = new Option<string?>("--project")
        {
            Description = "Project name (defaults to .agentGenCli.json / solution name)",
        };
        var yesOption = new Option<bool>("--yes")
        {
            Description = "Skip confirmation prompt",
        };

        var authCommand = new Command("auth", "Initialize authentication scaffolding")
        {
            projectOption,
            yesOption,
        };

        authCommand.SetAction(parseResult =>
        {
            return AuthScaffolder.Scaffold(
                new AuthScaffoldRequest
                {
                    ProjectFlag = parseResult.GetValue(projectOption),
                    Yes = parseResult.GetValue(yesOption),
                }
            );
        });

        return authCommand;
    }
}
