using System.Net;
using System.Net;
using System.Text;
using JoesTouchDeploy.Core.Logging;
using JoesTouchDeploy.Core.Models;

namespace JoesTouchDeploy.Core.Networking;

public class HttpSession
{
    private const int MaxRedirects = 10;

    private readonly CookieContainer _cookieContainer = new();
    private readonly HttpClient _httpClient;
    private readonly DebugLogger _logger;
    private string _crestXsrfToken = string.Empty;

    public HttpSession(DebugLogger logger)
    {
        _logger = logger;

        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = false,
            CookieContainer = _cookieContainer,
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };

        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromMinutes(5)
        };
    }

    public CookieCollection GetCookies(Uri uri)
    {
        return _cookieContainer.GetCookies(uri);
    }

    public string CrestXsrfToken => _crestXsrfToken;

    public Task<HttpSessionResponse> GetAsync(string url)
    {
        return SendAsync(HttpMethod.Get, new Uri(url), null, null);
    }

    public Task<HttpSessionResponse> PostFormAsync(
        string url,
        Dictionary<string, string> formValues,
        Dictionary<string, string>? headers = null)
    {
        return SendAsync(
            HttpMethod.Post,
            new Uri(url),
            () => new FormUrlEncodedContent(formValues),
            headers);
    }

    public Task<HttpSessionResponse> PostMultipartAsync(
        string url,
        Func<MultipartFormDataContent> contentFactory,
        Dictionary<string, string>? headers = null)
    {
        return SendAsync(
            HttpMethod.Post,
            new Uri(url),
            contentFactory,
            headers);
    }

    private async Task<HttpSessionResponse> SendAsync(
        HttpMethod method,
        Uri url,
        Func<HttpContent>? contentFactory,
        Dictionary<string, string>? headers)
    {
        var currentUrl = url;
        var currentMethod = method;
        var currentContentFactory = contentFactory;

        for (var redirectCount = 0; redirectCount <= MaxRedirects; redirectCount++)
        {
            using var request = new HttpRequestMessage(currentMethod, currentUrl);

            if (currentContentFactory != null)
            {
                request.Content = currentContentFactory();
            }

            if (headers != null)
            {
                foreach (var header in headers)
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            _logger.Log($"Sending {currentMethod} {currentUrl}");
            _logger.Log($"Request headers: {FormatHeaders(request)}");

            using var response = await _httpClient.SendAsync(request);
            CaptureCrestXsrfToken(response);

            var content = await response.Content.ReadAsStringAsync();
            var redirectTarget = response.Headers.Location?.ToString();
            var cookies = GetCookies(currentUrl);

            await _logger.SaveResponseAsync(
                currentUrl,
                response,
                content,
                redirectTarget,
                cookies);

            if (!IsRedirect(response.StatusCode) || response.Headers.Location == null)
            {
                return CreateSessionResponse(url, currentUrl, response, content, redirectTarget);
            }

            currentUrl = ResolveRedirectUrl(currentUrl, response.Headers.Location);

            if (response.StatusCode is HttpStatusCode.Moved or HttpStatusCode.Redirect or HttpStatusCode.SeeOther)
            {
                currentMethod = HttpMethod.Get;
                currentContentFactory = null;
                headers = null;
            }
        }

        throw new InvalidOperationException($"Maximum redirect count exceeded for {url}.");
    }

    private static HttpSessionResponse CreateSessionResponse(
        Uri requestUrl,
        Uri finalUrl,
        HttpResponseMessage response,
        string content,
        string? redirectTarget)
    {
        return new HttpSessionResponse
        {
            RequestUrl = requestUrl,
            FinalUrl = finalUrl,
            StatusCode = response.StatusCode,
            RedirectTarget = redirectTarget,
            ContentType = response.Content.Headers.ContentType?.ToString() ?? string.Empty,
            Content = content,
            ResponseSize = Encoding.UTF8.GetByteCount(content)
        };
    }

    private static bool IsRedirect(HttpStatusCode statusCode)
    {
        return statusCode is HttpStatusCode.Moved
            or HttpStatusCode.Redirect
            or HttpStatusCode.RedirectMethod
            or HttpStatusCode.SeeOther
            or HttpStatusCode.TemporaryRedirect
            or HttpStatusCode.PermanentRedirect;
    }

    private static Uri ResolveRedirectUrl(Uri currentUrl, Uri redirectUrl)
    {
        return redirectUrl.IsAbsoluteUri
            ? redirectUrl
            : new Uri(currentUrl, redirectUrl);
    }

    private void CaptureCrestXsrfToken(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues("CREST-XSRF-TOKEN", out var values))
        {
            _crestXsrfToken = values.FirstOrDefault() ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(_crestXsrfToken))
            {
                _logger.Log("Captured CREST-XSRF-TOKEN response header for authenticated requests.");
            }
        }
    }

    private static string FormatHeaders(HttpRequestMessage request)
    {
        var headers = request.Headers
            .Select(header => $"{header.Key}: {string.Join(", ", header.Value)}")
            .ToList();

        if (request.Content != null)
        {
            headers.AddRange(request.Content.Headers
                .Select(header => $"{header.Key}: {string.Join(", ", header.Value)}"));
        }

        return headers.Count == 0
            ? "none"
            : string.Join("; ", headers);
    }
}
