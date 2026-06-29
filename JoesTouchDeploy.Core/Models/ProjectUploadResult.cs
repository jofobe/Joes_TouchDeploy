namespace JoesTouchDeploy.Core.Models;

public class ProjectUploadResult
{
    public int HttpStatusCode { get; set; }

    public string ServerStatusInfo { get; set; } = string.Empty;

    public bool Success { get; set; }

    public string ResponseJson { get; set; } = string.Empty;
}
