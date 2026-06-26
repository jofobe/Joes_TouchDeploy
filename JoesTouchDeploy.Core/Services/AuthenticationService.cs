using JoesTouchDeploy.Core.Models;
using JoesTouchDeploy.Core.Networking;

namespace JoesTouchDeploy.Core.Services;

public class AuthenticationService
{
    private readonly PanelClient _panelClient;

    public AuthenticationService(PanelClient panelClient)
    {
        _panelClient = panelClient;
    }

    public async Task<AuthenticationResult> AuthenticateAsync(
        PanelConnection connection)
    {
        await _panelClient.GetLoginPageAsync(connection.IpAddress);

        var response = await _panelClient.PostFormAsync(
            connection.IpAddress,
            connection.Username,
            connection.Password);

        var cookies =
            _panelClient.GetCookies(connection.IpAddress);

        return new AuthenticationResult
        {
            Success = cookies["userstr"] != null,
            Message = response.StatusCode.ToString()
        };
    }
}