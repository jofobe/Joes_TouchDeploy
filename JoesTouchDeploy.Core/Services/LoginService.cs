using System.Net;
using JoesTouchDeploy.Core.Logging;
using JoesTouchDeploy.Core.Models;
using JoesTouchDeploy.Core.Networking;

namespace JoesTouchDeploy.Core.Services;

public class LoginService
{
    private const string LoginPath = "/userlogin.html";

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
        var loginUrl = $"{baseUrl}{LoginPath}";

        await _httpSession.GetAsync(LoginPath);

        using var request = new HttpRequestMessage(HttpMethod.Post, LoginPath)
        {
            Content = new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    { "login", connection.Username },
                    { "passwd", connection.Password }
                })
        };

        request.Headers.TryAddWithoutValidation("X-Requested-With", "XMLHttpRequest");
        request.Headers.TryAddWithoutValidation("Origin", baseUrl);
        request.Headers.Referrer = new Uri(loginUrl);

        var postResponse = await _httpSession.SendAsync(request);
        var cookies = _httpSession.GetCookies(new Uri(baseUrl));
        var success = cookies["userstr"] != null;
        var landingResponse = success
            ? await DetermineLandingPageAsync(postResponse)
            : postResponse;

        return new AuthenticationResult
        {
            Success = success,
            Message = postResponse.StatusCode.ToString(),
            FinalUrl = GetResponseUrl(postResponse).ToString(),
            LandingPageUrl = GetResponseUrl(landingResponse).ToString()
        };
    }

    private async Task<HttpResponseMessage> DetermineLandingPageAsync(
        HttpResponseMessage postResponse)
    {
        if (!IsLoginPage(GetResponseUrl(postResponse)) && IsHtml(postResponse))
        {
            return postResponse;
        }

        var candidates = new[]
        {
            "/index.html",
            "/index_device.html",
            "/"
        };

        foreach (var candidate in candidates)
        {
            try
            {
                var response = await _httpSession.GetAsync(candidate);

                if (response.StatusCode == HttpStatusCode.OK && IsHtml(response) && !IsLoginPage(GetResponseUrl(response)))
                {
                    _logger.Log($"Landing page candidate accepted: {GetResponseUrl(response)}");
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

        _logger.Log($"Using login POST final URL as landing page: {GetResponseUrl(postResponse)}");
        return postResponse;
    }

    private static Uri GetResponseUrl(HttpResponseMessage response)
    {
        return response.RequestMessage?.RequestUri ?? new Uri(LoginPath, UriKind.Relative);
    }

    private static bool IsHtml(HttpResponseMessage response)
    {
        var contentType = response.Content.Headers.ContentType?.ToString() ?? string.Empty;

        return contentType.Contains("text/html", StringComparison.OrdinalIgnoreCase) ||
            contentType.Contains("application/xhtml", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsLoginPage(Uri url)
    {
        return url.ToString().Contains("userlogin", StringComparison.OrdinalIgnoreCase);
    }
}
