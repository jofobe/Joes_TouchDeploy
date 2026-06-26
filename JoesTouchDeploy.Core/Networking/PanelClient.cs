using System.Net;

namespace JoesTouchDeploy.Core.Networking;

public class PanelClient
{
    private readonly CookieContainer _cookieContainer = new();
    private readonly HttpClient _httpClient;

    public PanelClient()
    {
        var handler = new HttpClientHandler
        {
            CookieContainer = _cookieContainer,
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };

        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(15)
        };
    }

    public async Task<string> GetLoginPageAsync(string ipAddress)
    {
        var response = await _httpClient.GetAsync($"https://{ipAddress}/userlogin.html");

        Console.WriteLine($"GET Status: {(int)response.StatusCode}");

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }

    public async Task<HttpResponseMessage> PostFormAsync(
        string ipAddress,
        string username,
        string password)
    {
        _httpClient.DefaultRequestHeaders.Clear();

        _httpClient.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        _httpClient.DefaultRequestHeaders.Add("Origin", $"https://{ipAddress}");
        _httpClient.DefaultRequestHeaders.Referrer =
            new Uri($"https://{ipAddress}/userlogin.html");

        var form = new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                { "login", username },
                { "passwd", password }
            });

        return await _httpClient.PostAsync(
            $"https://{ipAddress}/userlogin.html",
            form);
    }

    public CookieCollection GetCookies(string ipAddress)
    {
        return _cookieContainer.GetCookies(new Uri($"https://{ipAddress}"));
    }
}