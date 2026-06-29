using System.Net;

namespace JoesTouchDeploy.Core.Models;

public class HttpSessionResponse
{
    public Uri RequestUrl { get; set; } = null!;

    public Uri FinalUrl { get; set; } = null!;

    public HttpStatusCode StatusCode { get; set; }

    public string? RedirectTarget { get; set; }

    public string ContentType { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public int ResponseSize { get; set; }

    public bool IsHtml =>
        ContentType.Contains("text/html", StringComparison.OrdinalIgnoreCase) ||
        ContentType.Contains("application/xhtml", StringComparison.OrdinalIgnoreCase);
}
