using System.Text.Json.Serialization;

namespace JoesTouchDeploy.Core.Models;

public class DeviceInformationResponse
{
    [JsonPropertyName("Device")]
    public DeviceInformationDeviceResponse? Device { get; set; }
}

public class DeviceInformationDeviceResponse
{
    [JsonPropertyName("DeviceInfo")]
    public DeviceInformationDetailsResponse? DeviceInfo { get; set; }
}

public class DeviceInformationDetailsResponse
{
    [JsonPropertyName("BuildDate")]
    public string BuildDate { get; set; } = string.Empty;

    [JsonPropertyName("Category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("DeviceId")]
    public string DeviceId { get; set; } = string.Empty;

    [JsonPropertyName("DeviceVersion")]
    public string DeviceVersion { get; set; } = string.Empty;

    [JsonPropertyName("MacAddress")]
    public string MacAddress { get; set; } = string.Empty;

    [JsonPropertyName("Manufacturer")]
    public string Manufacturer { get; set; } = string.Empty;

    [JsonPropertyName("Model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("ModelId")]
    public string ModelId { get; set; } = string.Empty;

    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("PufVersion")]
    public string PufVersion { get; set; } = string.Empty;

    [JsonPropertyName("RebootReason")]
    public string RebootReason { get; set; } = string.Empty;

    [JsonPropertyName("SerialNumber")]
    public string SerialNumber { get; set; } = string.Empty;

    [JsonPropertyName("Version")]
    public string Version { get; set; } = string.Empty;
}
