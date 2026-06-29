using System.Text.Json;
using JoesTouchDeploy.Core.Models;
using JoesTouchDeploy.Core.Networking;

namespace JoesTouchDeploy.Core.Services;

public class DeviceInformationService
{
    private readonly HttpSession _httpSession;

    public DeviceInformationService(HttpSession httpSession, string ipAddress)
    {
        _httpSession = httpSession;
    }

    public async Task<DeviceInformation> GetDeviceInformationAsync()
    {
        var response = await _httpSession.GetAsync("/Device/DeviceInfo");
        var content = await response.Content.ReadAsStringAsync();
        var deviceInformationResponse = JsonSerializer.Deserialize<DeviceInformationResponse>(content);
        var deviceInfo = deviceInformationResponse?.Device?.DeviceInfo ??
            throw new InvalidOperationException("Device information response did not contain Device.DeviceInfo.");

        return new DeviceInformation
        {
            BuildDate = deviceInfo.BuildDate,
            Category = deviceInfo.Category,
            DeviceId = deviceInfo.DeviceId,
            FirmwareVersion = deviceInfo.DeviceVersion,
            HostName = deviceInfo.Name,
            MacAddress = deviceInfo.MacAddress,
            Manufacturer = deviceInfo.Manufacturer,
            Model = deviceInfo.Model,
            ModelId = deviceInfo.ModelId,
            PufVersion = deviceInfo.PufVersion,
            RebootReason = deviceInfo.RebootReason,
            SerialNumber = deviceInfo.SerialNumber,
            ApiVersion = deviceInfo.Version
        };
    }
}
