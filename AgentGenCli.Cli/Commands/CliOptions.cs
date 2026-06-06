using System.CommandLine;

namespace AgentGenCli.Cli.Commands;

internal static class CliOptions
{
    public static Option<bool> List { get; } = new("--list")
    {
        Description = "List all valid templates for this command group",
    };
}
