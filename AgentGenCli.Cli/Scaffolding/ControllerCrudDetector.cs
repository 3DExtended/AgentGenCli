namespace AgentGenCli.Cli.Scaffolding;

internal sealed class ControllerCrudCapabilities
{
    public bool HasHandle { get; init; }

    public bool HasCreate { get; init; }

    public bool HasGetById { get; init; }

    public bool HasList { get; init; }

    public bool HasUpdate { get; init; }

    public bool HasDelete { get; init; }

    public bool HasAnyCrud => HasCreate || HasGetById || HasList || HasUpdate || HasDelete;
}

internal static class ControllerCrudDetector
{
    public static ControllerCrudCapabilities Detect(string controllerContent)
    {
        return new ControllerCrudCapabilities
        {
            HasHandle = controllerContent.Contains("[HttpGet(\"handle\")]", StringComparison.Ordinal),
            HasCreate = controllerContent.Contains("Task<IActionResult> Create(", StringComparison.Ordinal),
            HasGetById = controllerContent.Contains("Task<IActionResult> Get(Guid id", StringComparison.Ordinal),
            HasList = controllerContent.Contains("Task<IActionResult> List(", StringComparison.Ordinal),
            HasUpdate = controllerContent.Contains("Task<IActionResult> Update(", StringComparison.Ordinal),
            HasDelete = controllerContent.Contains("Task<IActionResult> Delete(Guid id", StringComparison.Ordinal),
        };
    }
}
