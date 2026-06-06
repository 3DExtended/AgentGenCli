namespace AgentGenCli.Cli.Scaffolding;

internal static class FlutterRouterPatcher
{
    private const string RoutesMarker = "// agentGenCli:routes";

    public static void AddFeatureRoute(ProjectContext context, FeatureNameInfo feature)
    {
        var path = context.FlutterRouterPath;
        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"Router file not found at '{path}'.");
        }

        var content = File.ReadAllText(path);
        var package = NameFormatting.ToSnakeCase(context.ProjectName);
        var importLine =
            $"import 'package:{package}/features/{feature.FolderName}/{feature.FolderName}_screen.dart';";

        if (!content.Contains(importLine, StringComparison.Ordinal))
        {
            var lastImportIndex = content.LastIndexOf("import ", StringComparison.Ordinal);
            if (lastImportIndex < 0)
            {
                throw new InvalidOperationException("No import statements found in app_router.dart.");
            }

            var endOfImportLine = content.IndexOf('\n', lastImportIndex);
            content = content.Insert(endOfImportLine + 1, $"{importLine}\n");
        }

        var routeEntry =
            $"    GoRoute(\r\n      path: '/{feature.FolderName}',\r\n      builder: (context, state) => const {feature.PascalName}Screen(),\r\n    ),\r\n    {RoutesMarker}";

        if (content.Contains($"path: '/{feature.FolderName}'", StringComparison.Ordinal))
        {
            return;
        }

        var markerIndex = content.IndexOf(RoutesMarker, StringComparison.Ordinal);
        if (markerIndex < 0)
        {
            throw new InvalidOperationException($"Marker '{RoutesMarker}' not found in app_router.dart.");
        }

        content = content.Replace(RoutesMarker, routeEntry, StringComparison.Ordinal);
        File.WriteAllText(path, content);
    }
}
