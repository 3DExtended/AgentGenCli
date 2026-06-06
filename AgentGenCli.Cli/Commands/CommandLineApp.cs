using System.CommandLine;
using AgentGenCli.Cli.Commands.Init;
using AgentGenCli.Cli.Commands.New;
using AgentGenCli.Cli.Commands.Project;

namespace AgentGenCli.Cli.Commands;

internal static class CommandLineApp
{
    public static RootCommand Build()
    {
        var rootCommand = new RootCommand(
            "Generate projects or features from templates for agentic coding workflows"
        );

        rootCommand.Subcommands.Add(InitCommands.Create());
        rootCommand.Subcommands.Add(NewCommands.Create());
        rootCommand.Subcommands.Add(ProjectCommands.Create());

        return rootCommand;
    }
}
