namespace AgentGenCli.Cli.Scaffolding;

internal static class ExecutableResolver
{
    public static string? Find(string command)
    {
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (var directory in pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = Path.Combine(directory, command);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        var pubCacheCandidate = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".pub-cache",
            "bin",
            command
        );
        if (File.Exists(pubCacheCandidate))
        {
            return pubCacheCandidate;
        }

        return null;
    }
}
