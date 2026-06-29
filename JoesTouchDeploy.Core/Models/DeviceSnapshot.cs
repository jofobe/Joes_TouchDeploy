namespace JoesTouchDeploy.Core.Models;

public class DeviceSnapshot
{
    public string GeneratedAt { get; set; } = string.Empty;

    public string PanelUrl { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public string SerialNumber { get; set; } = string.Empty;

    public string DeviceName { get; set; } = string.Empty;

    public string DeviceId { get; set; } = string.Empty;

    public string MacAddress { get; set; } = string.Empty;

    public string FirmwareVersion { get; set; } = string.Empty;

    public string WebUiVersion { get; set; } = string.Empty;

    public string ProjectName { get; set; } = string.Empty;

    public string ProjectCompiledOn { get; set; } = string.Empty;

    public string ProjectFileHash { get; set; } = string.Empty;

    public string ProjectApiVersion { get; set; } = string.Empty;

    public string ProjectHtml5Path { get; set; } = string.Empty;

    public string ProjectUserProjectPath { get; set; } = string.Empty;

    public string FirmwareUploadPath { get; set; } = string.Empty;

    public string CurrentUserName { get; set; } = string.Empty;

    public string CurrentUserAccessLevel { get; set; } = string.Empty;

    public bool? IsConfigFileUploadSupported { get; set; }

    public bool? IsLogFileUploadSupported { get; set; }

    public List<string> LocalGroups { get; set; } = [];

    public List<string> RecommendedNextProbes { get; set; } = [];
}
