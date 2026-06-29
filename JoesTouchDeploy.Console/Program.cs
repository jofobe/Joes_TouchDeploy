using JoesTouchDeploy.Core.Models;
using JoesTouchDeploy.Core.Logging;
using JoesTouchDeploy.Core.Models;
using JoesTouchDeploy.Core.Networking;
using JoesTouchDeploy.Core.Parsing;
using JoesTouchDeploy.Core.Services;

Console.WriteLine("=================================");
Console.WriteLine("Joe's TouchDeploy");
Console.WriteLine("=================================");
Console.WriteLine();

Console.Write("Panel IP: ");
var ip = Console.ReadLine()!;

Console.Write("Username: ");
var username = Console.ReadLine()!;

Console.Write("Password: ");
var password = Console.ReadLine()!;

var connection = new PanelConnection
{
    IpAddress = ip,
    Username = username,
    Password = password
};

var debugOutputDirectory =
    Path.Combine(Directory.GetCurrentDirectory(), "DebugOutput");

var logger = new DebugLogger(debugOutputDirectory);
var httpSession = new HttpSession(logger);
var loginService = new LoginService(httpSession, logger);
var discoveryService = new DiscoveryService(
    httpSession,
    new HtmlParser(),
    logger);
var deviceSnapshotService = new DeviceSnapshotService(
    httpSession,
    logger);
var projectUploadService = new ProjectUploadService(
    httpSession,
    logger,
    connection.IpAddress);

Console.WriteLine();
Console.WriteLine("Authenticating...");
Console.WriteLine($"Debug output : {debugOutputDirectory}");
Console.WriteLine();

try
{
    var result =
        await loginService.LoginAsync(connection);

    Console.WriteLine($"Success : {result.Success}");
    Console.WriteLine($"Message : {result.Message}");
    Console.WriteLine($"Login final URL : {result.FinalUrl}");
    Console.WriteLine($"Landing page URL : {result.LandingPageUrl}");

    if (!result.Success)
    {
        return;
    }

    Console.WriteLine();
    Console.WriteLine("Discovering authenticated pages and endpoints...");
    Console.WriteLine();

    var discoveryResult =
        await discoveryService.DiscoverAsync(new Uri(result.LandingPageUrl));

    Console.WriteLine();
    Console.WriteLine("Discovery complete.");
    Console.WriteLine($"Visited URLs : {discoveryResult.VisitedUrls.Count}");
    Console.WriteLine($"Links : {discoveryResult.Links.Count}");
    Console.WriteLine($"Forms : {discoveryResult.Forms.Count}");
    Console.WriteLine($"Scripts : {discoveryResult.Scripts.Count}");
    Console.WriteLine($"Iframes : {discoveryResult.Iframes.Count}");
    Console.WriteLine($"Resources : {discoveryResult.Resources.Count}");
    Console.WriteLine($"Targeted probes : {discoveryResult.TargetedProbes.Count}");
    Console.WriteLine($"Configuration candidates : {discoveryResult.ConfigurationCandidates.Count}");

    Console.WriteLine();
    Console.WriteLine("Successful targeted probes:");

    foreach (var probe in discoveryResult.TargetedProbes.Where(probe => probe.IsSuccess))
    {
        Console.WriteLine($"{probe.StatusCode} {probe.ResponseSize} bytes {probe.Url}");
    }

    Console.WriteLine();
    Console.WriteLine("Discovered links/endpoints:");

    foreach (var endpoint in discoveryResult.Links
        .Concat(discoveryResult.Forms.Select(form => $"{form.Method} {form.Action}"))
        .Concat(discoveryResult.Scripts)
        .Concat(discoveryResult.Iframes)
        .Concat(discoveryResult.Resources)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Order())
    {
        Console.WriteLine(endpoint);
    }

    Console.WriteLine("Discovery summary saved to DebugOutput\\discovery_summary.txt");

    Console.WriteLine();
    Console.WriteLine("Creating device snapshot...");

    var snapshot =
        await deviceSnapshotService.CreateSnapshotAsync(new Uri(result.LandingPageUrl));

    Console.WriteLine("Device snapshot complete.");
    Console.WriteLine($"Model : {snapshot.Model}");
    Console.WriteLine($"Serial : {snapshot.SerialNumber}");
    Console.WriteLine($"Firmware : {snapshot.FirmwareVersion}");
    Console.WriteLine($"Web UI : {snapshot.WebUiVersion}");
    Console.WriteLine($"Project : {snapshot.ProjectName}");
    Console.WriteLine("Device snapshot saved to DebugOutput\\device_snapshot.json and DebugOutput\\device_snapshot.txt");

    Console.WriteLine();
    Console.Write("VTZ project file path to upload (leave blank to skip): ");
    var projectFilePath = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(projectFilePath))
    {
        Console.WriteLine("Project upload skipped.");
        return;
    }

    projectFilePath = projectFilePath.Trim().Trim('"');

    if (!File.Exists(projectFilePath))
    {
        Console.WriteLine($"Project file not found: {projectFilePath}");
        return;
    }

    Console.WriteLine("Upload started...");

    var uploadResult =
        await projectUploadService.UploadProjectAsync(projectFilePath);

    Console.WriteLine("Upload completed.");
    Console.WriteLine($"HTTP status : {uploadResult.HttpStatusCode}");
    Console.WriteLine($"Parsed server status : {uploadResult.ServerStatusInfo}");
    Console.WriteLine($"Upload success : {uploadResult.Success}");
    Console.WriteLine("Upload response saved to DebugOutput\\project_upload_response.*");
}
catch (Exception exception)
{
    logger.Log($"ERROR: {exception.GetType().Name}: {exception.Message}");
    Console.WriteLine();
    Console.WriteLine(exception);
}
