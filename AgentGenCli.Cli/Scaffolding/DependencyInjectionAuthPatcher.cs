namespace AgentGenCli.Cli.Scaffolding;

internal static class DependencyInjectionAuthPatcher
{
    private const string Marker = "// agentGenCli:auth-di";

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

        var encryptionUsing = $"using {context.ProjectName}.Common.Options;\r\nusing {context.ProjectName}.Common.Services;\r\n";
        var optionsUsing = $"using {context.ProjectName}.Common.Options;";
        if (!content.Contains("ConfigureEncryption", StringComparison.Ordinal))
        {
            content = content.Replace(
                optionsUsing,
                encryptionUsing,
                StringComparison.Ordinal
            );
        }

        const string anchor = "        AddCqrs(services, assemblies);";
        var insert = $"        ConfigureEncryption(services, configuration);\r\n        {Marker}\r\n";
        content = content.Replace(anchor, $"{insert}{anchor}", StringComparison.Ordinal);

        if (!content.Contains("private static void ConfigureEncryption", StringComparison.Ordinal))
        {
            const string method =
                """

                    private static void ConfigureEncryption(IServiceCollection services, IConfiguration configuration)
                    {
                        var encryptionOptions = configuration.GetSection("Encryption").Get<EncryptionOptions>();
                        if (encryptionOptions != null)
                        {
                            services.AddSingleton(encryptionOptions);
                        }
                    }

                """;

            content = content.Replace("    private static void AddCqrs(", method + "    private static void AddCqrs(", StringComparison.Ordinal);
        }

        File.WriteAllText(path, content, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }
}
