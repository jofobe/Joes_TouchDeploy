namespace JoesTouchDeploy.Core.Models;

public class CurrentProjectInformation
{
    public string ProjectName { get; init; } = string.Empty;

    public string CompiledOn { get; init; } = string.Empty;

    public string ProjectFileHash { get; init; } = string.Empty;

    public string Version { get; init; } = string.Empty;
}
