using System.Diagnostics;

namespace AgentGenCli.Cli.Scaffolding;

internal static class ProcessRunner
{
    public static int Run(
        string fileName,
        string arguments,
        string? workingDirectory = null,
        IReadOnlyDictionary<string, string>? environment = null
    )
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory ?? Directory.GetCurrentDirectory(),
            },
        };

        if (environment != null)
        {
            foreach (var (key, value) in environment)
            {
                process.StartInfo.Environment[key] = value;
            }
        }

        process.Start();

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        process.WaitForExit();

        var output = outputTask.GetAwaiter().GetResult();
        var error = errorTask.GetAwaiter().GetResult();

        if (!string.IsNullOrWhiteSpace(output))
        {
            Console.WriteLine(output.TrimEnd());
        }

        if (process.ExitCode != 0 && !string.IsNullOrWhiteSpace(error))
        {
            Console.Error.WriteLine(error.TrimEnd());
        }

        return process.ExitCode;
    }

    public static int RunWithInheritedConsole(
        string fileName,
        string arguments,
        string? workingDirectory = null
    )
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                WorkingDirectory = workingDirectory ?? Directory.GetCurrentDirectory(),
            },
        };

        process.Start();
        process.WaitForExit();
        return process.ExitCode;
    }

    public static bool IsCommandAvailable(string command)
    {
        var exitCode = Run(command, "--version");
        return exitCode == 0;
    }
}
