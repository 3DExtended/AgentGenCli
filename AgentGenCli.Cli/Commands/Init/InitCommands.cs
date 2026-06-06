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
            Description = "Backend template",
            DefaultValueFactory = _ => "dotnet",
        };

        var frontendArgument = new Argument<string>("frontend")
        {
            Description = "Frontend template",
            DefaultValueFactory = _ => "flutter",
        };

        var projectCommand = new Command("project", "Initialize a new full-stack project")
        {
            nameArgument,
            backendArgument,
            frontendArgument,
        };

        projectCommand.SetAction(parseResult =>
        {
            var name = parseResult.GetValue(nameArgument);
            var backend = parseResult.GetValue(backendArgument)!;
            var frontend = parseResult.GetValue(frontendArgument)!;

            if (name == null)
            {
                Console.Error.WriteLine("Project name is required.");
                return 1;
            }

            Console.WriteLine(
                $"Initializing project '{name}' with backend={backend}, frontend={frontend}"
            );

            var projectDir = Path.Combine(Directory.GetCurrentDirectory(), name);

            if (Directory.GetFileSystemEntries(Directory.GetCurrentDirectory()).Length > 0)
            {
                if (!Directory.Exists(projectDir))
                {
                    Directory.CreateDirectory(projectDir);
                    Console.WriteLine($"Created directory '{projectDir}' for project.");
                }

                Directory.SetCurrentDirectory(projectDir);
            }
            else
            {
                Console.WriteLine("Current directory is empty, initializing project here.");
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

    private static Command CreateAuthCommand()
    {
        var authCommand = new Command("auth", "Initialize authentication scaffolding");

        authCommand.SetAction(_ =>
        {
            Console.WriteLine("Initializing auth scaffolding");
            return 0;
        });

        return authCommand;
    }
}
