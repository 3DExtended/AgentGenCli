namespace AgentGenCli.Cli.Scaffolding;

internal static class DependencyInjectionSendGridPatcher
{
    private const string Marker = "// agentGenCli:sendgrid-di";

    public static void Apply(ProjectContext context)
    {
        var path = Path.Combine(context.Root, "common", $"{context.ProjectName}.Common", "DependencyInjectionHelpers.cs");
        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"DependencyInjectionHelpers not found at '{path}'.");
        }

        var content = File.ReadAllText(path);
        if (content.Contains(Marker, StringComparison.Ordinal))
        {
            return;
        }

        var sendGridUsing = $"using {context.ProjectName}.Common.SendGrid;\r\n";
        var handlerUsing = $"using {context.ProjectName}.Common.SendGrid.QueryHandlers;\r\n";
        var optionsUsing = $"using {context.ProjectName}.Common.Options;";

        if (!content.Contains(sendGridUsing, StringComparison.Ordinal))
        {
            content = content.Replace(
                optionsUsing,
                $"{optionsUsing}\r\n{sendGridUsing}{handlerUsing}",
                StringComparison.Ordinal
            );
        }

        const string anchor = "        AddCqrs(services, assemblies);";
        var insert = $"        ConfigureSendGrid(services, configuration);\r\n        {Marker}\r\n";
        content = content.Replace(anchor, $"{insert}{anchor}", StringComparison.Ordinal);

        if (!content.Contains("private static void ConfigureSendGrid", StringComparison.Ordinal))
        {
            const string method =
                """

                    private static void ConfigureSendGrid(IServiceCollection services, IConfiguration configuration)
                    {
                        var sendGridOptions = configuration.GetSection("SendGrid").Get<SendGridOptions>();
                        if (sendGridOptions == null)
                        {
                            return;
                        }

                        services.AddSingleton(sendGridOptions);
                    }

                """;

            content = content.Replace("    private static void AddCqrs(", method + "    private static void AddCqrs(", StringComparison.Ordinal);
        }

        const string cqrsAnchor =
            "options.WithQueryHandlersFrom(typeof(SystemHealthCheckQueryHandler).Assembly);";
        const string cqrsInsert =
            "options.WithQueryHandlersFrom(typeof(SystemHealthCheckQueryHandler).Assembly);\r\n            options.WithQueryHandlersFrom(typeof(EmailSendQueryHandler).Assembly);";
        if (content.Contains(cqrsAnchor, StringComparison.Ordinal)
            && !content.Contains("EmailSendQueryHandler", StringComparison.Ordinal))
        {
            content = content.Replace(cqrsAnchor, cqrsInsert, StringComparison.Ordinal);
        }

        File.WriteAllText(path, content, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }
}
