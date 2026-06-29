using JoesTouchDeploy.Core.Logging;
using JoesTouchDeploy.Core.Models;

namespace JoesTouchDeploy.Core.Services;

public class DeploymentService
{
    private static readonly TimeSpan InitialUiReloadWait = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan MaximumWaitTime = TimeSpan.FromSeconds(60);

    private readonly VideoTecClient _videoTecClient;
    private readonly DebugLogger _logger;

    public DeploymentService(VideoTecClient videoTecClient, DebugLogger logger)
    {
        _videoTecClient = videoTecClient;
        _logger = logger;
    }

    public async Task<DeploymentResult> DeployProjectAsync(string filePath)
    {
        var uploadResult = await _videoTecClient.UploadProjectAsync(filePath);

        _logger.Log("Upload complete.");

        if (!uploadResult.Success || uploadResult.Data == null)
        {
            return new DeploymentResult
            {
                Success = false,
                Message = $"Upload failed: {uploadResult.Message}",
                UploadSucceeded = false,
                UploadResult = uploadResult.Data
            };
        }

        _logger.Log("Waiting for UI reload.");
        Console.WriteLine("Waiting for panel UI to reload...");

        await Task.Delay(InitialUiReloadWait);

        var projectInformationResult = await WaitForUiResponsiveAsync();

        if (!projectInformationResult.Success || projectInformationResult.Data == null)
        {
            return new DeploymentResult
            {
                Success = false,
                Message = projectInformationResult.Message ?? "Timed out waiting for panel UI to respond.",
                UploadSucceeded = true,
                UiResponsive = false,
                UploadResult = uploadResult.Data
            };
        }

        var currentProject = projectInformationResult.Data;

        _logger.Log("UI responsive.");
        _logger.Log($"Current project information: ProjectName={currentProject.ProjectName}; CompiledOn={currentProject.CompiledOn}; ProjectFileHash={currentProject.ProjectFileHash}; Version={currentProject.Version}");
        _logger.Log("Deployment complete.");

        return new DeploymentResult
        {
            Success = true,
            Message = "Deployment completed successfully.",
            UploadSucceeded = true,
            UiResponsive = true,
            CurrentProjectInformation = currentProject,
            UploadResult = uploadResult.Data
        };
    }

    private async Task<OperationResult<CurrentProjectInformation>> WaitForUiResponsiveAsync()
    {
        var startTime = DateTimeOffset.UtcNow;

        while (!IsTimedOut(startTime))
        {
            var projectInformationResult = await _videoTecClient.GetCurrentProjectInformationAsync();

            if (projectInformationResult.Success && projectInformationResult.Data != null)
            {
                return projectInformationResult;
            }

            _logger.Log($"UI not responsive yet: {projectInformationResult.Message}");
            await Task.Delay(PollInterval);
        }

        return new OperationResult<CurrentProjectInformation>
        {
            Success = false,
            Message = "Timeout waiting for panel UI to respond."
        };
    }

    private static bool IsTimedOut(DateTimeOffset startTime)
    {
        return DateTimeOffset.UtcNow - startTime >= MaximumWaitTime;
    }
}
