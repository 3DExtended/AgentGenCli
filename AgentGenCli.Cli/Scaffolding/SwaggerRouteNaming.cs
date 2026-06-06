namespace AgentGenCli.Cli.Scaffolding;

internal static class SwaggerRouteNaming
{
    public static string ToMethodPrefix(string routeFolder)
    {
        if (string.IsNullOrWhiteSpace(routeFolder))
        {
            return routeFolder;
        }

        var parts = routeFolder.Split('-', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return routeFolder;
        }

        var result = parts[0].ToLowerInvariant();
        for (var i = 1; i < parts.Length; i++)
        {
            var part = parts[i];
            if (part.Length == 0)
            {
                continue;
            }

            result += char.ToUpperInvariant(part[0]) + part[1..].ToLowerInvariant();
        }

        return result;
    }
}
