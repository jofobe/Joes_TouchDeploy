using System.Text.Json;
using JoesTouchDeploy.Core.Models;
using JoesTouchDeploy.Core.Networking;

namespace JoesTouchDeploy.Core.Services;

public class CurrentProjectInformationService
{
    private readonly HttpSession _httpSession;

    public CurrentProjectInformationService(HttpSession httpSession)
    {
        _httpSession = httpSession;
    }

    public async Task<CurrentProjectInformation> GetCurrentProjectInformationAsync()
    {
        var response = await _httpSession.GetAsync("/Device/UiUserProject");
        var content = await response.Content.ReadAsStringAsync();
        var projectResponse = JsonSerializer.Deserialize<CurrentProjectInformationResponse>(content);
        var project = projectResponse?.Device?.UiUserProject ??
            throw new InvalidOperationException("Current project response did not contain Device.UiUserProject.");

        return new CurrentProjectInformation
        {
            ProjectName = project.ProjectName,
            CompiledOn = project.CompiledOn,
            ProjectFileHash = project.ProjectFileHash,
            Version = project.Version
        };
    }
}
