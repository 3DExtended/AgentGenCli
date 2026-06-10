namespace AgentGenCli.Cli.Scaffolding;

internal static class ApiFeatureRegistrationPatcher
{
    private const string FeatureAssembliesMarker = "// agentGenCli:feature-assemblies";

    public static void RegisterFeature(ProjectContext context, FeatureNameInfo feature)
    {
        var path = context.FeatureRegistrationPath;
        var content = File.ReadAllText(path);
        var anchorType = ResolveAssemblyAnchorType(context, feature);
        var assemblyLine = $"        typeof(global::{anchorType}).Assembly,";
        if (content.Contains(assemblyLine, StringComparison.Ordinal))
        {
            return;
        }

        var markerIndex = content.IndexOf(FeatureAssembliesMarker, StringComparison.Ordinal);
        if (markerIndex < 0)
        {
            throw new InvalidOperationException(
                $"Marker '{FeatureAssembliesMarker}' not found in FeatureRegistration.cs."
            );
        }

        content = content.Insert(markerIndex, $"{assemblyLine}\r\n");
        WriteUtf8WithBom(path, content);
    }

    private static string ResolveAssemblyAnchorType(ProjectContext context, FeatureNameInfo feature)
    {
        var featureNamespace = $"{context.ProjectName}.Features.{feature.PascalName}";
        var mapsterConfigPath = Path.Combine(
            context.FeatureProjectDir(feature),
            $"{feature.PascalName}MapsterConfig.cs"
        );

        if (File.Exists(mapsterConfigPath))
        {
            return $"{featureNamespace}.{feature.PascalName}MapsterConfig";
        }

        if (string.Equals(feature.FolderName, "users", StringComparison.OrdinalIgnoreCase))
        {
            return $"{featureNamespace}.QueryHandlers.Users.UserRegisterQueryHandler";
        }

        return $"{featureNamespace}.QueryHandlers.Handle{feature.PascalName}QueryHandler";
    }

    private static void WriteUtf8WithBom(string path, string content)
    {
        File.WriteAllText(path, content, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }
}
