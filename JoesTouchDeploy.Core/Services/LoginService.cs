using JoesTouchDeploy.Core.Logging;
using JoesTouchDeploy.Core.Logging;
using JoesTouchDeploy.Core.Models;
using JoesTouchDeploy.Core.Networking;

namespace JoesTouchDeploy.Core.Services;

public class LoginService
{
    private readonly HttpSession _httpSession;
    private readonly DebugLogger _logger;

    public LoginService(HttpSession httpSession, DebugLogger logger)
    {
        _httpSession = httpSession;
        _logger = logger;
    }

    public async Task<AuthenticationResult> LoginAsync(PanelConnection connection)
    {
        var baseUrl = $"https://{connection.IpAddress}";
        var loginUrl = $"{baseUrl}/userlogin.html";

        await _httpSession.GetAsync(loginUrl);

        var postResponse = await _httpSession.PostFormAsync(
            loginUrl,
            new Dictionary<string, string>
            {
                { "login", connection.Username },
                { "passwd", connection.Password }
            },
            new Dictionary<string, string>
            {
                { "X-Requested-With", "XMLHttpRequest" },
                { "Origin", baseUrl },
                { "Referer", loginUrl }
            });

        var cookies = _httpSession.GetCookies(new Uri(baseUrl));
        var success = cookies["userstr"] != null;
        var landingResponse = success
            ? await DetermineLandingPageAsync(baseUrl, postResponse)
            : postResponse;

        return new AuthenticationResult
        {
            Success = success,
            Message = postResponse.StatusCode.ToString(),
            FinalUrl = postResponse.FinalUrl.ToString(),
            LandingPageUrl = landingResponse.FinalUrl.ToString()
        };
    }

    private async Task<HttpSessionResponse> DetermineLandingPageAsync(
        string baseUrl,
        HttpSessionResponse postResponse)
    {
        if (!IsLoginPage(postResponse.FinalUrl) && postResponse.IsHtml)
        {
            return postResponse;
        }

        var candidates = new[]
        {
            $"{baseUrl}/index.html",
            $"{baseUrl}/index_device.html",
            $"{baseUrl}/"
        };

        foreach (var candidate in candidates)
        {
            try
            {
                var response = await _httpSession.GetAsync(candidate);

                if (IsSuccessful(response) && response.IsHtml && !IsLoginPage(response.FinalUrl))
                {
                    _logger.Log($"Landing page candidate accepted: {response.FinalUrl}");
                    return response;
                }
            }
            catch (HttpRequestException exception)
            {
                _logger.Log($"Landing page candidate failed: {candidate} - {exception.Message}");
            }
            catch (TaskCanceledException exception)
            {
                _logger.Log($"Landing page candidate timed out: {candidate} - {exception.Message}");
            }
        }

        _logger.Log($"Using login POST final URL as landing page: {postResponse.FinalUrl}");
        return postResponse;
    }

    private static bool IsSuccessful(HttpSessionResponse response)
    {
        var statusCode = (int)response.StatusCode;
        return statusCode >= 200 && statusCode <= 299;
    }

    private static bool IsLoginPage(Uri url)
    {
        return url.AbsolutePath.Contains("userlogin", StringComparison.OrdinalIgnoreCase);
    }
}
