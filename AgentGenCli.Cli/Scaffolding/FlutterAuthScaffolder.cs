namespace AgentGenCli.Cli.Scaffolding;

internal static class FlutterAuthScaffolder
{
    public static int Apply(ProjectContext context, TemplateTokens tokens)
    {
        var authDir = Path.Combine(context.FlutterAppDir, "lib", "features", "auth");
        if (Directory.Exists(authDir))
        {
            Console.Error.WriteLine($"Flutter auth feature already exists at '{authDir}'.");
            return 1;
        }

        TemplateEngine.CopyTemplateTree("Flutter/lib/features/auth", authDir, tokens, templateRootKind: "Auth");

        var testDir = Path.Combine(context.FlutterAppDir, "test", "features", "auth");
        Directory.CreateDirectory(testDir);
        TemplateEngine.CopyTemplateTree("Flutter/test/features/auth", testDir, tokens, templateRootKind: "Auth");

        FlutterAuthRouterPatcher.Apply(context, tokens);
        FlutterMainAuthPatcher.Apply(context, tokens);
        FlutterPubspecAuthPatcher.Apply(context);

        return 0;
    }
}

internal static class FlutterAuthRouterPatcher
{
    public static void Apply(ProjectContext context, TemplateTokens tokens)
    {
        var source = Path.Combine(TemplateEngine.GetAuthTemplateRoot(), "Flutter", "lib", "core", "router", "app_router.dart.template");
        var destination = context.FlutterRouterPath;
        var content = File.ReadAllText(source);
        content = ReplaceTokens(content, tokens.ToDictionary());
        File.WriteAllText(destination, content);
    }

    private static string ReplaceTokens(string input, IReadOnlyDictionary<string, string> replacements)
    {
        var result = input;
        foreach (var (token, value) in replacements)
        {
            result = result.Replace(token, value, StringComparison.Ordinal);
        }

        return result;
    }
}

internal static class FlutterMainAuthPatcher
{
    public static void Apply(ProjectContext context, TemplateTokens tokens)
    {
        var path = Path.Combine(context.FlutterAppDir, "lib", "main.dart");
        var content = File.ReadAllText(path);
        if (content.Contains("routerProvider", StringComparison.Ordinal))
        {
            return;
        }

        content = content.Replace(
            "import 'package:{{ProjectNameSnake}}/core/router/app_router.dart';",
            "import 'package:{{ProjectNameSnake}}/core/router/app_router.dart';\nimport 'package:{{ProjectNameSnake}}/features/auth/auth_provider.dart';",
            StringComparison.Ordinal
        );
        content = content.Replace(
            "routerConfig: appRouter,",
            "routerConfig: ref.watch(routerProvider),",
            StringComparison.Ordinal
        );
        content = content.Replace(
            "class {{ProjectName}}App extends StatelessWidget {",
            "class {{ProjectName}}App extends ConsumerWidget {",
            StringComparison.Ordinal
        );
        content = content.Replace(
            "  Widget build(BuildContext context) {",
            "  Widget build(BuildContext context, WidgetRef ref) {",
            StringComparison.Ordinal
        );

        content = ReplaceProjectTokens(content, tokens);
        File.WriteAllText(path, content);
    }

    private static string ReplaceProjectTokens(string input, TemplateTokens tokens)
    {
        var result = input;
        foreach (var (token, value) in tokens.ToDictionary())
        {
            result = result.Replace(token, value, StringComparison.Ordinal);
        }

        result = result.Replace("{{ProjectName}}", tokens.ProjectName, StringComparison.Ordinal);
        return result;
    }
}

internal static class FlutterPubspecAuthPatcher
{
    public static void Apply(ProjectContext context)
    {
        var path = Path.Combine(context.FlutterAppDir, "pubspec.yaml");
        var content = File.ReadAllText(path);
        if (content.Contains("google_sign_in:", StringComparison.Ordinal))
        {
            return;
        }

        const string anchor = "  shared_preferences:";
        var packages =
            """
              google_sign_in: ^6.2.2
              sign_in_with_apple: ^6.1.4
              shared_preferences:
            """;

        content = content.Replace(anchor, packages, StringComparison.Ordinal);
        File.WriteAllText(path, content);
    }
}
