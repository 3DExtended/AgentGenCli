namespace AgentGenCli.Cli.Scaffolding;

internal static class ApiFeatureRegistrationPatcher
{
    private const string FeatureAssembliesMarker = "// agentGenCli:feature-assemblies";

    public static void RegisterFeature(ProjectContext context, FeatureNameInfo feature)
    {
        var path = context.FeatureRegistrationPath;
        var content = File.ReadAllText(path);
        var featureNamespace = $"{context.ProjectName}.Features.{feature.PascalName}";
        var usingLine = $"using {featureNamespace};";

        if (!content.Contains(usingLine, StringComparison.Ordinal))
        {
            content = content.Replace(
                "using System.Reflection;",
                $"using System.Reflection;\r\n\r\n{usingLine}",
                StringComparison.Ordinal
            );
        }

        var assemblyLine = $"        typeof({feature.PascalName}PipelineProfile).Assembly,";
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

    private static void WriteUtf8WithBom(string path, string content)
    {
        File.WriteAllText(path, content, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }
}
