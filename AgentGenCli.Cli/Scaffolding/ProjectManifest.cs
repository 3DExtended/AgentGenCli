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
            EmailInitialized = false,
            AuthInitialized = false,
            Commands = [],
            Features = [],
            FrontendFeatures = [],
        };
    }

    public static void EnsureEmailNotInitialized(string root)
    {
        var document = Load(root);
        if (document.EmailInitialized)
        {
            throw new InvalidOperationException(
                "Email scaffolding is already initialized. See '.agentGenCli.json' (emailInitialized: true)."
            );
        }
    }

    public static void EnsureAuthNotInitialized(string root)
    {
        var document = Load(root);
        if (document.AuthInitialized)
        {
            throw new InvalidOperationException(
                "Auth scaffolding is already initialized. See '.agentGenCli.json' (authInitialized: true)."
            );
        }
    }

    public static void MarkEmailInitialized(string root, ManifestCommandEntry entry)
    {
        var document = Load(root);
        document.EmailInitialized = true;
        document.Commands.Add(entry);
        Save(root, document);
    }

    public static void MarkAuthInitialized(string root, ManifestCommandEntry entry)
    {
        var document = Load(root);
        document.AuthInitialized = true;
        document.Commands.Add(entry);

        if (!document.Features.Contains("Users", StringComparer.Ordinal))
        {
            document.Features.Add("Users");
        }

        if (!document.FrontendFeatures.Contains("Auth", StringComparer.Ordinal))
        {
            document.FrontendFeatures.Add("Auth");
        }

        Save(root, document);
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

    public static void RecordFrontendFeature(
        string root,
        string featurePascalName,
        ManifestCommandEntry entry
    )
    {
        var document = Load(root);
        document.Commands.Add(entry);

        if (!document.FrontendFeatures.Contains(featurePascalName, StringComparer.Ordinal))
        {
            document.FrontendFeatures.Add(featurePascalName);
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

    public List<string> FrontendFeatures { get; set; } = [];

    public bool EmailInitialized { get; set; }

    public bool AuthInitialized { get; set; }
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
