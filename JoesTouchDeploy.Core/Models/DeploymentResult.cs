namespace JoesTouchDeploy.Core.Models;

public class DeploymentResult
{
    public bool Success { get; init; }

    public string Message { get; init; } = string.Empty;

    public bool UploadSucceeded { get; init; }

    public bool UiResponsive { get; init; }

    public CurrentProjectInformation? CurrentProjectInformation { get; init; }

    public ProjectUploadResult? UploadResult { get; init; }
}
