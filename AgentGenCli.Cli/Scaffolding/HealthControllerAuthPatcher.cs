namespace AgentGenCli.Cli.Scaffolding;

internal static class HealthControllerAuthPatcher
{
    public static void Apply(ProjectContext context)
    {
        var path = Path.Combine(
            context.Root,
            "applications",
            $"{context.ProjectName}.Api",
            "Controllers",
            "HealthController.cs"
        );
        if (!File.Exists(path))
        {
            return;
        }

        var content = File.ReadAllText(path);
        if (content.Contains("[AllowAnonymous]", StringComparison.Ordinal))
        {
            return;
        }

        if (!content.Contains("Microsoft.AspNetCore.Authorization", StringComparison.Ordinal))
        {
            content = content.Replace(
                "using Microsoft.AspNetCore.Mvc;",
                "using Microsoft.AspNetCore.Authorization;\nusing Microsoft.AspNetCore.Mvc;",
                StringComparison.Ordinal
            );
        }

        const string classDeclaration = "public class HealthController";
        var index = content.IndexOf(classDeclaration, StringComparison.Ordinal);
        if (index < 0)
        {
            return;
        }

        content = content.Insert(index, "[AllowAnonymous]\n");
        File.WriteAllText(path, content, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }
}
