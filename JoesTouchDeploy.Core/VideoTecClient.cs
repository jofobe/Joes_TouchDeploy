using JoesTouchDeploy.Core.Logging;
using JoesTouchDeploy.Core.Models;
using JoesTouchDeploy.Core.Networking;
using JoesTouchDeploy.Core.Services;

namespace JoesTouchDeploy.Core;

public class VideoTecClient
{
    private readonly PanelConnection _connection;
    private readonly LoginService _loginService;
    private readonly ProjectUploadService _projectUploadService;
    private AuthenticationResult? _authenticationResult;

    public VideoTecClient(PanelConnection connection, DebugLogger logger)
    {
        _connection = connection;

        var httpSession = new HttpSession(logger);

        _loginService = new LoginService(httpSession, logger);
        _projectUploadService = new ProjectUploadService(
            httpSession,
            logger,
            connection.IpAddress);
    }

    public async Task LoginAsync()
    {
        _authenticationResult = await _loginService.LoginAsync(_connection);

        if (!_authenticationResult.Success)
        {
            throw new InvalidOperationException($"Panel login failed: {_authenticationResult.Message}");
        }
    }

    public Task<ProjectUploadResult> UploadProjectAsync(string filePath)
    {
        if (_authenticationResult?.Success != true)
        {
            throw new InvalidOperationException("Login must complete before uploading a project.");
        }

        return _projectUploadService.UploadProjectAsync(filePath);
    }
}
