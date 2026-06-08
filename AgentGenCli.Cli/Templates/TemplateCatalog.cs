namespace AgentGenCli.Cli.Templates;

internal static class TemplateCatalog
{
    public static IReadOnlyList<string> InitTemplates { get; } = ["project", "email", "auth"];

    public static IReadOnlyList<string> NewTemplates { get; } =
    ["backend-feature", "frontend-feature", "efmigration"];

    public static void PrintInitTemplates()
    {
        Console.WriteLine("Available init templates:");
        foreach (var template in InitTemplates)
        {
            Console.WriteLine($"  {template}");
        }
    }

    public static void PrintNewTemplates()
    {
        Console.WriteLine("Available new templates:");
        foreach (var template in NewTemplates)
        {
            Console.WriteLine($"  {template}");
        }
    }
}
