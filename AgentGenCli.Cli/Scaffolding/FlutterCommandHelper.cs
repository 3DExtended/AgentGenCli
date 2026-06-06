namespace AgentGenCli.Cli.Scaffolding;

internal static class FlutterCommandHelper
{
    public static int RunDart(ProjectContext context, string arguments) =>
        Run(context, "dart", arguments);

    public static int RunFlutter(ProjectContext context, string arguments) =>
        Run(context, "flutter", arguments);

    public static int Run(ProjectContext context, string command, string arguments)
    {
        var workingDirectory = context.FlutterAppDir;
        if (!Directory.Exists(workingDirectory))
        {
            Console.Error.WriteLine($"Flutter app directory not found: '{workingDirectory}'.");
            return 1;
        }

        var fvmConfig = Path.Combine(workingDirectory, ".fvm", "fvm_config.json");
        if (File.Exists(fvmConfig))
        {
            var fvm = ExecutableResolver.Find("fvm") ?? "fvm";
            return ProcessRunner.Run(fvm, $"{command} {arguments}", workingDirectory);
        }

        var executable = ExecutableResolver.Find(command) ?? command;
        return ProcessRunner.Run(executable, arguments, workingDirectory);
    }
}
