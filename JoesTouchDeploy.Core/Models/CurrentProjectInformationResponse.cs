using System.Text.Json.Serialization;

namespace JoesTouchDeploy.Core.Models;

public class CurrentProjectInformationResponse
{
    [JsonPropertyName("Device")]
    public CurrentProjectInformationDeviceResponse? Device { get; init; }
}

public class CurrentProjectInformationDeviceResponse
{
    [JsonPropertyName("UiUserProject")]
    public CurrentProjectInformationDetailsResponse? UiUserProject { get; init; }
}

public class CurrentProjectInformationDetailsResponse
{
    [JsonPropertyName("ProjectName")]
    public string ProjectName { get; init; } = string.Empty;

    [JsonPropertyName("CompiledOn")]
    public string CompiledOn { get; init; } = string.Empty;

    [JsonPropertyName("ProjectFileHash")]
    public string ProjectFileHash { get; init; } = string.Empty;

    [JsonPropertyName("Version")]
    public string Version { get; init; } = string.Empty;
}
