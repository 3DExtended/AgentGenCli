using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentGenCli.Cli.Scaffolding;

internal static class ProjectManifest
{
    public const string FileName = ".agentGenCli.json";

    public static ProjectManifestDocument Load(string root)
    {
        var path = Path.Combine(root, FileName);
        if (!File.Exists(path))
        {
            throw new InvalidOperationException(
                $"Manifest not found at '{path}'. Run 'agentGenCli init project' first."
            );
        }

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize(json, ProjectManifestJsonContext.Default.ProjectManifestDocument)
            ?? throw new InvalidOperationException($"Failed to parse '{path}'.");
    }

    public static void Save(string root, ProjectManifestDocument document)
    {
        var path = Path.Combine(root, FileName);
        var json = JsonSerializer.Serialize(
            document,
            ProjectManifestJsonContext.Default.ProjectManifestDocument
        );
        File.WriteAllText(path, json);
    }

    public static ProjectManifestDocument CreateInitial(string projectName)
    {
        return new ProjectManifestDocument
        {
            ProjectName = projectName,
            InitializedAt = DateTimeOffset.UtcNow,
            Commands = [],
            Features = [],
        };
    }

    public static void RecordCommand(string root, ManifestCommandEntry entry)
    {
        var document = Load(root);
        document.Commands.Add(entry);
        Save(root, document);
    }

    public static void RecordFeature(string root, string featurePascalName, ManifestCommandEntry entry)
    {
        var document = Load(root);
        document.Commands.Add(entry);

        if (!document.Features.Contains(featurePascalName, StringComparer.Ordinal))
        {
            document.Features.Add(featurePascalName);
        }

        Save(root, document);
    }
}

internal sealed class ProjectManifestDocument
{
    public required string ProjectName { get; set; }

    public DateTimeOffset InitializedAt { get; set; }

    public List<ManifestCommandEntry> Commands { get; set; } = [];

    public List<string> Features { get; set; } = [];
}

internal sealed class ManifestCommandEntry
{
    public required string Command { get; set; }

    public DateTimeOffset At { get; set; } = DateTimeOffset.UtcNow;

    public Dictionary<string, JsonElement>? Args { get; set; }
}

[JsonSerializable(typeof(ProjectManifestDocument))]
[JsonSerializable(typeof(ManifestCommandEntry))]
[JsonSerializable(typeof(Dictionary<string, JsonElement>))]
internal partial class ProjectManifestJsonContext : JsonSerializerContext;
