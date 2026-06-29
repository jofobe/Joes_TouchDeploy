namespace JoesTouchDeploy.Core.Models;

public class AuthenticationResult
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public string FinalUrl { get; set; } = string.Empty;

    public string LandingPageUrl { get; set; } = string.Empty;
}