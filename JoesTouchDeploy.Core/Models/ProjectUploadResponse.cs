using System.Text.Json.Serialization;

namespace JoesTouchDeploy.Core.Models;

public class ProjectUploadResponse
{
    [JsonPropertyName("Device")]
    public ProjectUploadDeviceResponse? Device { get; set; }

    [JsonPropertyName("Actions")]
    public List<ProjectUploadActionResponse> Actions { get; set; } = [];
}

public class ProjectUploadDeviceResponse
{
    [JsonPropertyName("DeviceOperations")]
    public ProjectUploadDeviceOperationsResponse? DeviceOperations { get; set; }
}

public class ProjectUploadDeviceOperationsResponse
{
    [JsonPropertyName("UploadProject")]
    public ProjectUploadStatusResponse? UploadProject { get; set; }
}

public class ProjectUploadStatusResponse
{
    [JsonPropertyName("StatusInfo")]
    public string StatusInfo { get; set; } = string.Empty;
}

public class ProjectUploadActionResponse
{
    [JsonPropertyName("Operation")]
    public string Operation { get; set; } = string.Empty;

    [JsonPropertyName("TargetObject")]
    public string TargetObject { get; set; } = string.Empty;

    [JsonPropertyName("Results")]
    public List<ProjectUploadActionResultResponse> Results { get; set; } = [];
}

public class ProjectUploadActionResultResponse
{
    [JsonPropertyName("Path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("Property")]
    public string Property { get; set; } = string.Empty;

    [JsonPropertyName("StatusId")]
    public int StatusId { get; set; }

    [JsonPropertyName("StatusInfo")]
    public string StatusInfo { get; set; } = string.Empty;
}
