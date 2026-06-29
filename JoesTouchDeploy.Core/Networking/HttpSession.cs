using System.Diagnostics;
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

    public HttpSession(DebugLogger logger, string? baseUrl = null)
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

        if (!string.IsNullOrWhiteSpace(baseUrl))
        {
            _httpClient.BaseAddress = new Uri(baseUrl);
        }
    }

    public CookieCollection GetCookies(Uri uri)
    {
        return _cookieContainer.GetCookies(uri);
    }

    public string CrestXsrfToken => _crestXsrfToken;

    public Task<HttpResponseMessage> GetAsync(string relativeUrl)
    {
        return SendAsync(new HttpRequestMessage(HttpMethod.Get, CreateUri(relativeUrl)));
    }

    public Task<HttpResponseMessage> PostAsync(string relativeUrl, HttpContent content)
    {
        return SendAsync(new HttpRequestMessage(HttpMethod.Post, CreateUri(relativeUrl))
        {
            Content = content
        });
    }

    public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
    {
        return SendRequestAsync(request, ensureSuccessStatusCode: true);
    }

    public Task<HttpSessionResponse> GetSessionResponseAsync(string url)
    {
        return SendSessionResponseAsync(HttpMethod.Get, CreateUri(url), null, null);
    }

    public Task<HttpSessionResponse> PostFormSessionResponseAsync(
        string url,
        Dictionary<string, string> formValues,
        Dictionary<string, string>? headers = null)
    {
        return SendSessionResponseAsync(
            HttpMethod.Post,
            CreateUri(url),
            () => new FormUrlEncodedContent(formValues),
            headers);
    }

    public Task<HttpSessionResponse> PostMultipartSessionResponseAsync(
        string url,
        Func<MultipartFormDataContent> contentFactory,
        Dictionary<string, string>? headers = null)
    {
        return SendSessionResponseAsync(
            HttpMethod.Post,
            CreateUri(url),
            contentFactory,
            headers);
    }

    private async Task<HttpResponseMessage> SendRequestAsync(
        HttpRequestMessage request,
        bool ensureSuccessStatusCode)
    {
        var currentRequest = request;

        for (var redirectCount = 0; redirectCount <= MaxRedirects; redirectCount++)
        {
            EnsureAbsoluteRequestUri(currentRequest);
            ApplyCommonHeaders(currentRequest);

            var requestUri = currentRequest.RequestUri ?? throw new InvalidOperationException("Request URI is required.");
            _logger.Log($"Sending {currentRequest.Method} {requestUri}");
            _logger.Log($"Request headers: {FormatHeaders(currentRequest)}");

            var stopwatch = Stopwatch.StartNew();
            var response = await _httpClient.SendAsync(currentRequest);
            stopwatch.Stop();

            CaptureCrestXsrfToken(response);

            var content = await response.Content.ReadAsStringAsync();
            var responseUri = response.RequestMessage?.RequestUri ?? requestUri;
            var redirectTarget = response.Headers.Location?.ToString();
            var cookies = GetCookies(responseUri);

            _logger.Log($"Elapsed time: {stopwatch.ElapsedMilliseconds} ms");
            _logger.Log($"Response headers: {FormatHeaders(response)}");

            await _logger.SaveResponseAsync(
                responseUri,
                response,
                content,
                redirectTarget,
                cookies);

            if (!IsRedirect(response.StatusCode) || response.Headers.Location == null)
            {
                if (ensureSuccessStatusCode && !response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException(
                        $"HTTP request failed: {(int)response.StatusCode} {response.StatusCode} for {requestUri}",
                        null,
                        response.StatusCode);
                }

                return response;
            }

            var redirectUrl = ResolveRedirectUrl(responseUri, response.Headers.Location);

            if (response.StatusCode is HttpStatusCode.Moved or HttpStatusCode.Redirect or HttpStatusCode.SeeOther)
            {
                currentRequest = new HttpRequestMessage(HttpMethod.Get, redirectUrl);
                continue;
            }

            throw new InvalidOperationException($"Redirect preserving request content is not supported for {requestUri}.");
        }

        throw new InvalidOperationException($"Maximum redirect count exceeded for {request.RequestUri}.");
    }

    private async Task<HttpSessionResponse> SendSessionResponseAsync(
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

            using var response = await SendRequestAsync(request, ensureSuccessStatusCode: false);
            var content = await response.Content.ReadAsStringAsync();
            var redirectTarget = response.Headers.Location?.ToString();

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

    private Uri CreateUri(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var absoluteUri))
        {
            return absoluteUri;
        }

        if (_httpClient.BaseAddress == null)
        {
            throw new InvalidOperationException("A base address is required for relative URLs.");
        }

        return new Uri(_httpClient.BaseAddress, url);
    }

    private void EnsureAbsoluteRequestUri(HttpRequestMessage request)
    {
        if (request.RequestUri == null)
        {
            throw new InvalidOperationException("Request URI is required.");
        }

        if (request.RequestUri.IsAbsoluteUri)
        {
            return;
        }

        if (_httpClient.BaseAddress == null)
        {
            throw new InvalidOperationException("A base address is required for relative URLs.");
        }

        request.RequestUri = new Uri(_httpClient.BaseAddress, request.RequestUri);
    }

    private void ApplyCommonHeaders(HttpRequestMessage request)
    {
        if (!string.IsNullOrWhiteSpace(_crestXsrfToken) && !request.Headers.Contains("X-CREST-XSRF-TOKEN"))
        {
            request.Headers.TryAddWithoutValidation("X-CREST-XSRF-TOKEN", _crestXsrfToken);
        }
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

    private static string FormatHeaders(HttpResponseMessage response)
    {
        var headers = response.Headers
            .Select(header => $"{header.Key}: {string.Join(", ", header.Value)}")
            .ToList();

        headers.AddRange(response.Content.Headers
            .Select(header => $"{header.Key}: {string.Join(", ", header.Value)}"));

        return headers.Count == 0
            ? "none"
            : string.Join("; ", headers);
    }
}
