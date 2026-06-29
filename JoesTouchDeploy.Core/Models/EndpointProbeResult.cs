namespace JoesTouchDeploy.Core.Models;

public class EndpointProbeResult
{
    public string Url { get; set; } = string.Empty;

    public int StatusCode { get; set; }

    public string ContentType { get; set; } = string.Empty;

    public int ResponseSize { get; set; }

    public bool IsSuccess => StatusCode >= 200 && StatusCode <= 299;
}
