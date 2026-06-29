using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using JoesTouchDeploy.Core.Logging;
using JoesTouchDeploy.Core.Models;
using JoesTouchDeploy.Core.Networking;

namespace JoesTouchDeploy.Core.Services;

public class DeviceSnapshotService
{
    private readonly HttpSession _httpSession;
    private readonly DebugLogger _logger;

    public DeviceSnapshotService(HttpSession httpSession, DebugLogger logger)
    {
        _httpSession = httpSession;
        _logger = logger;
    }

    public async Task<DeviceSnapshot> CreateSnapshotAsync(Uri landingPageUrl)
    {
        var baseUrl = $"{landingPageUrl.Scheme}://{landingPageUrl.Host}";
        var snapshot = new DeviceSnapshot
        {
            GeneratedAt = DateTimeOffset.Now.ToString("O"),
            PanelUrl = baseUrl
        };

        var deviceInfo = await GetJsonAsync($"{baseUrl}/Device/DeviceInfo");
        var uiUserProject = await GetJsonAsync($"{baseUrl}/Device/UiUserProject");
        var filePaths = await GetJsonAsync($"{baseUrl}/Device/FilePaths");
        var deviceCapabilities = await GetJsonAsync($"{baseUrl}/Device/DeviceCapabilities");
        var systemVersions = await GetJsonAsync($"{baseUrl}/Device/SystemVersions");
        var authentication = await GetJsonAsync($"{baseUrl}/Device/Authentication");
        var webUiVersion = await GetJsonAsync($"{baseUrl}/assets/data/version.json");

        PopulateDeviceInfo(snapshot, deviceInfo);
        PopulateProject(snapshot, uiUserProject);
        PopulateFilePaths(snapshot, filePaths);
        PopulateCapabilities(snapshot, deviceCapabilities);
        PopulateAuthentication(snapshot, authentication);
        PopulateVersions(snapshot, systemVersions, webUiVersion);
        PopulateRecommendedNextProbes(snapshot);

        await SaveSnapshotAsync(snapshot);

        return snapshot;
    }

    private async Task<JsonNode?> GetJsonAsync(string url)
    {
        var response = await _httpSession.GetAsync(url);

        try
        {
            return JsonNode.Parse(response.Content);
        }
        catch (JsonException exception)
        {
            _logger.Log($"Snapshot JSON parse failed: {url} - {exception.Message}");
            return null;
        }
    }

    private static void PopulateDeviceInfo(DeviceSnapshot snapshot, JsonNode? json)
    {
        var node = json?["Device"]?["DeviceInfo"];

        snapshot.Model = GetString(node, "Model");
        snapshot.SerialNumber = GetString(node, "SerialNumber");
        snapshot.DeviceName = GetString(node, "Name");
        snapshot.DeviceId = GetString(node, "DeviceId");
        snapshot.MacAddress = GetString(node, "MacAddress");
        snapshot.FirmwareVersion = GetString(node, "DeviceVersion");
    }

    private static void PopulateProject(DeviceSnapshot snapshot, JsonNode? json)
    {
        var node = json?["Device"]?["UiUserProject"];

        snapshot.ProjectName = GetString(node, "ProjectName");
        snapshot.ProjectCompiledOn = GetString(node, "CompiledOn");
        snapshot.ProjectFileHash = GetString(node, "ProjectFileHash");
        snapshot.ProjectApiVersion = GetString(node, "Version");
    }

    private static void PopulateFilePaths(DeviceSnapshot snapshot, JsonNode? json)
    {
        var filePaths = json?["Device"]?["FilePaths"];
        var project = filePaths?["Project"];
        var firmware = filePaths?["Firmware"];

        snapshot.ProjectHtml5Path = GetString(project, "Html5");
        snapshot.ProjectUserProjectPath = GetString(project, "UserProject");
        snapshot.FirmwareUploadPath = GetString(firmware, "FirmwareFile");
    }

    private static void PopulateCapabilities(DeviceSnapshot snapshot, JsonNode? json)
    {
        var node = json?["Device"]?["DeviceCapabilities"];

        snapshot.IsConfigFileUploadSupported = GetBool(node, "IsConfigFileUploadSupported");
        snapshot.IsLogFileUploadSupported = GetBool(node, "IsLogFileUploadSupported");
    }

    private static void PopulateAuthentication(DeviceSnapshot snapshot, JsonNode? json)
    {
        var currentUser = json?["Device"]?["Authentication"]?["CurrentUser"];

        snapshot.CurrentUserName = GetString(currentUser, "Name");
        snapshot.CurrentUserAccessLevel = GetString(currentUser, "AccessLevel");

        if (currentUser?["LocalGroups"] is JsonArray groups)
        {
            snapshot.LocalGroups = groups
                .Select(group => group?.GetValue<string>() ?? string.Empty)
                .Where(group => !string.IsNullOrWhiteSpace(group))
                .ToList();
        }
    }

    private static void PopulateVersions(DeviceSnapshot snapshot, JsonNode? systemVersions, JsonNode? webUiVersion)
    {
        snapshot.WebUiVersion = GetString(webUiVersion, "version");

        if (!string.IsNullOrWhiteSpace(snapshot.WebUiVersion))
        {
            return;
        }

        var components = systemVersions?["Device"]?["SystemVersions"]?["Components"]?.AsArray();
        var ccuiVersion = components?
            .FirstOrDefault(component => GetString(component, "Name").Equals("CCUI Version", StringComparison.OrdinalIgnoreCase));

        snapshot.WebUiVersion = GetString(ccuiVersion, "Version");
    }

    private static void PopulateRecommendedNextProbes(DeviceSnapshot snapshot)
    {
        AddProbe(snapshot, "/Device/UiUserProject");
        AddProbe(snapshot, "/Device/FilePaths/Project");
        AddProbe(snapshot, "/Device/DeviceOperations");

        if (!string.IsNullOrWhiteSpace(snapshot.ProjectHtml5Path))
        {
            AddProbe(snapshot, snapshot.ProjectHtml5Path);
        }

        if (!string.IsNullOrWhiteSpace(snapshot.ProjectUserProjectPath))
        {
            AddProbe(snapshot, snapshot.ProjectUserProjectPath);
        }

        AddProbe(snapshot, "/Device/DeviceOperations/ProjectType");
        AddProbe(snapshot, "/Device/DeviceOperations/UpgradeStatus");
    }

    private async Task SaveSnapshotAsync(DeviceSnapshot snapshot)
    {
        var json = JsonSerializer.Serialize(
            snapshot,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        await File.WriteAllTextAsync(
            Path.Combine(_logger.OutputDirectory, "device_snapshot.json"),
            json);

        await File.WriteAllTextAsync(
            Path.Combine(_logger.OutputDirectory, "device_snapshot.txt"),
            BuildTextSnapshot(snapshot));
    }

    private static string BuildTextSnapshot(DeviceSnapshot snapshot)
    {
        var builder = new StringBuilder();

        builder.AppendLine("Joe's TouchDeploy Device Snapshot");
        builder.AppendLine($"Generated: {snapshot.GeneratedAt}");
        builder.AppendLine($"Panel URL: {snapshot.PanelUrl}");
        builder.AppendLine();
        builder.AppendLine("Device");
        builder.AppendLine("------");
        builder.AppendLine($"Model: {snapshot.Model}");
        builder.AppendLine($"Serial Number: {snapshot.SerialNumber}");
        builder.AppendLine($"Device Name: {snapshot.DeviceName}");
        builder.AppendLine($"Device ID: {snapshot.DeviceId}");
        builder.AppendLine($"MAC Address: {snapshot.MacAddress}");
        builder.AppendLine($"Firmware Version: {snapshot.FirmwareVersion}");
        builder.AppendLine($"Web UI Version: {snapshot.WebUiVersion}");
        builder.AppendLine();
        builder.AppendLine("Project");
        builder.AppendLine("-------");
        builder.AppendLine($"Project Name: {snapshot.ProjectName}");
        builder.AppendLine($"Compiled On: {snapshot.ProjectCompiledOn}");
        builder.AppendLine($"Project File Hash: {snapshot.ProjectFileHash}");
        builder.AppendLine($"Project API Version: {snapshot.ProjectApiVersion}");
        builder.AppendLine($"HTML5 Path: {snapshot.ProjectHtml5Path}");
        builder.AppendLine($"User Project Path: {snapshot.ProjectUserProjectPath}");
        builder.AppendLine($"Firmware Upload Path: {snapshot.FirmwareUploadPath}");
        builder.AppendLine();
        builder.AppendLine("Capabilities");
        builder.AppendLine("------------");
        builder.AppendLine($"Config File Upload Supported: {snapshot.IsConfigFileUploadSupported}");
        builder.AppendLine($"Log File Upload Supported: {snapshot.IsLogFileUploadSupported}");
        builder.AppendLine();
        builder.AppendLine("Authentication");
        builder.AppendLine("--------------");
        builder.AppendLine($"Current User: {snapshot.CurrentUserName}");
        builder.AppendLine($"Access Level: {snapshot.CurrentUserAccessLevel}");
        builder.AppendLine($"Local Groups: {string.Join(", ", snapshot.LocalGroups)}");
        builder.AppendLine();
        builder.AppendLine("Recommended Next Probes");
        builder.AppendLine("-----------------------");

        foreach (var probe in snapshot.RecommendedNextProbes)
        {
            builder.AppendLine(probe);
        }

        return builder.ToString();
    }

    private static string GetString(JsonNode? node, string propertyName)
    {
        return node?[propertyName]?.GetValue<string>() ?? string.Empty;
    }

    private static bool? GetBool(JsonNode? node, string propertyName)
    {
        return node?[propertyName]?.GetValue<bool>();
    }

    private static void AddProbe(DeviceSnapshot snapshot, string value)
    {
        if (!snapshot.RecommendedNextProbes.Contains(value, StringComparer.OrdinalIgnoreCase))
        {
            snapshot.RecommendedNextProbes.Add(value);
        }
    }
}
