using System.CommandLine;
using System.Diagnostics;
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

            // check if directory is empty and if not, create a new directory with the project name and cd into it
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
                projectDir = Directory.GetCurrentDirectory();
            }

            if (backend != null || frontend != null)
            {
                Console.WriteLine("Creating project directories...");
                Directory.CreateDirectory(Path.Combine("applications"));
                Directory.CreateDirectory(Path.Combine("features"));
                Directory.CreateDirectory(Path.Combine("common"));
                Directory.CreateDirectory(Path.Combine("extern"));
                Directory.CreateDirectory(Path.Combine("tests"));

                // TODO init git repository and add .gitignore (dotnet and flutter gitignore templates)
            }

            if (backend == "dotnet")
            {
                Console.WriteLine("Scaffolding .NET backend...");

                Console.WriteLine("Creating .NET Solution...");
                // run dotnet new to create new dotnet setup:
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "dotnet",
                        Arguments = $"new sln --name {name}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    },
                };
                process.StartInfo.Environment["DOTNET_CLI_CONTEXT_VERBOSE"] = "true";
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                Console.WriteLine(output);
                if (process.ExitCode != 0)
                {
                    Console.Error.WriteLine($"Error creating .NET solution: {error}");
                    return 1;
                }

                Console.WriteLine("Creating .NET Web API project...");

                process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "dotnet",
                        Arguments = $"new webapi --name {name}.Api -o applications/{name}.Api",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    },
                };
                process.StartInfo.Environment["DOTNET_CLI_CONTEXT_VERBOSE"] = "true";
                process.Start();
                output = process.StandardOutput.ReadToEnd();
                error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                Console.WriteLine(output);
                if (process.ExitCode != 0)
                {
                    Console.Error.WriteLine($"Error creating .NET Web API project: {error}");
                    return 1;
                }

                // add git submodule under extern for git@github.com:3DExtended/Prodot.Patterns.Cqrs.git
                Console.WriteLine("Adding new submodule for Prodot.Patterns.Cqrs...");
                process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments =
                            "submodule add git@github.com:3DExtended/Prodot.Patterns.Cqrs.git extern/Prodot.Patterns.Cqrs",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    },
                };
                process.Start();
                output = process.StandardOutput.ReadToEnd();
                error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                Console.WriteLine(output);
                if (process.ExitCode != 0)
                {
                    Console.Error.WriteLine($"Error adding git submodule: {error}");
                    return 1;
                }

                // TODO add all projects to the solution
                // TODO setup Dependency Injection in API project
                // TODO Create common project and setup database with efcore and add reference to API project
                // TODO database context must be a partial class so that it can be extended with feature-specific DbSets in the features projects without modifying the common project
                // TODO add references between projects (e.g. API project references base and extern, features reference base, etc.)
                // TODO setup Prodot.Patterns.Cqrs in the common project and add reference to API project
                // TODO add test project and add reference to API project
            }
            else
            {
                Console.WriteLine($"Unknown backend template '{backend}'");
            }

            return 0;
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
