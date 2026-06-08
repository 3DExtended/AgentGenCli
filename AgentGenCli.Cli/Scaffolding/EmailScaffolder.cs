using System.Text.Json;

namespace AgentGenCli.Cli.Scaffolding;

internal sealed class EmailScaffoldRequest
{
    public string? ProjectFlag { get; init; }

    public bool Yes { get; init; }
}

internal static class EmailScaffolder
{
    public static int Scaffold(EmailScaffoldRequest request)
    {
        try
        {
            var context = ProjectContext.Resolve(projectFlag: request.ProjectFlag);
            ProjectManifest.EnsureEmailNotInitialized(context.Root);

            var sendGridDir = Path.Combine(
                context.Root,
                "common",
                $"{context.ProjectName}.Common.SendGrid"
            );
            if (Directory.Exists(sendGridDir))
            {
                Console.Error.WriteLine($"SendGrid project already exists at '{sendGridDir}'.");
                return 1;
            }

            Console.WriteLine("Email scaffold summary:");
            Console.WriteLine($"  Project: {context.ProjectName}");
            Console.WriteLine($"  SendGrid: common/{context.ProjectName}.Common.SendGrid/");

            if (!request.Yes && !Confirm())
            {
                Console.WriteLine("Aborted.");
                return 1;
            }

            var tokens = new TemplateTokens { ProjectName = context.ProjectName };
            Directory.CreateDirectory(sendGridDir);

            TemplateEngine.CopyTemplateTree(
                string.Empty,
                sendGridDir,
                tokens,
                includeFile: path => !path.EndsWith(".csproj.template", StringComparison.OrdinalIgnoreCase),
                templateRootKind: "Email"
            );

            var csprojDest = Path.Combine(sendGridDir, $"{context.ProjectName}.Common.SendGrid.csproj");
            TemplateEngine.CopyFileFrom(
                "ProjectName.Common.SendGrid.csproj.template",
                csprojDest,
                tokens,
                TemplateEngine.GetEmailTemplateRoot()
            );

            if (
                ProcessRunner.Run(
                    "dotnet",
                    $"sln add \"{Path.GetRelativePath(context.Root, csprojDest)}\""
                ) != 0
            )
            {
                Console.Error.WriteLine("Error adding SendGrid project to solution.");
                return 1;
            }

            if (
                !RunDotnetAddReference(context.CommonProjectPath, csprojDest)
                || !RunDotnetAddReference(context.ApiProjectPath, csprojDest)
            )
            {
                Console.Error.WriteLine("Error adding SendGrid project references.");
                return 1;
            }

            AppsettingsSendGridPatcher.Apply(context);
            DependencyInjectionSendGridPatcher.Apply(context);

            if (
                ProcessRunner.Run(
                    "dotnet",
                    "format analyzers --include common/ --include applications/"
                ) != 0
            )
            {
                Console.Error.WriteLine("Error formatting email scaffold output.");
                return 1;
            }

            if (ProcessRunner.Run("dotnet", "build") != 0)
            {
                Console.Error.WriteLine("Error building solution after email scaffold.");
                return 1;
            }

            ProjectManifest.MarkEmailInitialized(
                context.Root,
                new ManifestCommandEntry { Command = "init email" }
            );

            AgentDocsGenerator.RefreshState(context.Root);
            Console.WriteLine("Email scaffolding initialized successfully.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static bool Confirm()
    {
        Console.Write("Proceed? [y/N]: ");
        var response = Console.ReadLine();
        return string.Equals(response, "y", StringComparison.OrdinalIgnoreCase)
            || string.Equals(response, "yes", StringComparison.OrdinalIgnoreCase);
    }

    private static bool RunDotnetAddReference(string project, string reference) =>
        ProcessRunner.Run("dotnet", $"add \"{project}\" reference \"{reference}\"") == 0;
}

internal static class AppsettingsSendGridPatcher
{
    public static void Apply(ProjectContext context)
    {
        var path = Path.Combine(
            context.Root,
            "applications",
            $"{context.ProjectName}.Api",
            "appsettings.Development.json"
        );
        if (!File.Exists(path))
        {
            return;
        }

        var content = File.ReadAllText(path);
        if (content.Contains("\"SendGrid\"", StringComparison.Ordinal))
        {
            return;
        }

        const string anchor = "  \"Sql\":";
        var sendGridSection =
            """
              "SendGrid": {
                "ApiKey": "SG.replace-me",
                "FromEmailAddress": "noreply@example.com",
                "FromSenderName": "{{ProjectName}}",
                "IsDisabled": true
              },
            """.Replace("{{ProjectName}}", context.ProjectName, StringComparison.Ordinal);

        content = content.Replace(anchor, sendGridSection + anchor, StringComparison.Ordinal);
        File.WriteAllText(path, content);
    }
}
