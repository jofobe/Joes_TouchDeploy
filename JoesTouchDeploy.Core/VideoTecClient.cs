using System.Text.Json;
using JoesTouchDeploy.Core.Logging;
using JoesTouchDeploy.Core.Logging;
using JoesTouchDeploy.Core.Models;
using JoesTouchDeploy.Core.Networking;
using JoesTouchDeploy.Core.Services;

namespace JoesTouchDeploy.Core;

public class VideoTecClient
{
    private readonly PanelConnection _connection;
    private readonly LoginService _loginService;
    private readonly DeviceInformationService _deviceInformationService;
    private readonly ProjectUploadService _projectUploadService;
    private AuthenticationResult? _authenticationResult;

    public VideoTecClient(PanelConnection connection, DebugLogger logger)
    {
        _connection = connection;

        var httpSession = new HttpSession(
            logger,
            $"https://{connection.IpAddress}");

        _loginService = new LoginService(httpSession, logger);
        _deviceInformationService = new DeviceInformationService(
            httpSession,
            connection.IpAddress);
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

    public async Task<OperationResult<ProjectUploadResult>> UploadProjectAsync(string filePath)
    {
        if (_authenticationResult?.Success != true)
        {
            throw new InvalidOperationException("Login must complete before uploading a project.");
        }

        try
        {
            var uploadResult = await _projectUploadService.UploadProjectAsync(filePath);

            return new OperationResult<ProjectUploadResult>
            {
                Success = uploadResult.Success,
                Message = uploadResult.ServerStatusInfo,
                Data = uploadResult
            };
        }
        catch (ArgumentException exception)
        {
            return CreateFailure<ProjectUploadResult>(exception.Message, exception);
        }
        catch (FileNotFoundException exception)
        {
            return CreateFailure<ProjectUploadResult>(exception.Message, exception);
        }
        catch (HttpRequestException exception)
        {
            return CreateFailure<ProjectUploadResult>(exception.Message, exception);
        }
        catch (TaskCanceledException exception)
        {
            return CreateFailure<ProjectUploadResult>("The project upload timed out.", exception);
        }
    }

    public async Task<OperationResult<DeviceInformation>> GetDeviceInformationAsync()
    {
        if (_authenticationResult?.Success != true)
        {
            throw new InvalidOperationException("Login must complete before retrieving device information.");
        }

        try
        {
            var deviceInformation = await _deviceInformationService.GetDeviceInformationAsync();

            return new OperationResult<DeviceInformation>
            {
                Success = true,
                Message = "Device information retrieved successfully.",
                Data = deviceInformation
            };
        }
        catch (HttpRequestException exception)
        {
            return CreateFailure<DeviceInformation>(exception.Message, exception);
        }
        catch (JsonException exception)
        {
            return CreateFailure<DeviceInformation>("Device information response could not be parsed.", exception);
        }
        catch (TaskCanceledException exception)
        {
            return CreateFailure<DeviceInformation>("The device information request timed out.", exception);
        }
    }

    private static OperationResult<T> CreateFailure<T>(string message, Exception exception)
    {
        return new OperationResult<T>
        {
            Success = false,
            Message = message,
            Exception = exception
        };
    }
}
