using System.Net;

namespace JoesTouchDeploy.Core.Networking;

public class CrestronHttpClient
{
    private readonly HttpClient _httpClient;

    public CrestronHttpClient()
    {
        var cookieContainer = new CookieContainer();

        var handler = new HttpClientHandler
        {
            CookieContainer = cookieContainer,

            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };

        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(15)
        };
    }

    public async Task<HttpResponseMessage> GetAsync(string url)
    {
        return await _httpClient.GetAsync(url);
    }

    public async Task<string> GetLoginPageAsync(string ipAddress)
    {
        var response = await _httpClient.GetAsync($"https://{ipAddress}/userlogin.html");

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }
}