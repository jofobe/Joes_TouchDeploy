using JoesTouchDeploy.Core.Models;
using JoesTouchDeploy.Core;
using JoesTouchDeploy.Core.Logging;
using JoesTouchDeploy.Core.Models;
using JoesTouchDeploy.Core.Services;
using JoesTouchDeploy.Core.Utilities;

internal class Program
{
    [STAThread]
    private static async Task Main()
    {
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
        var videoTecClient = new VideoTecClient(connection, logger);
        var deploymentService = new DeploymentService(videoTecClient, logger);
        var fileDialogService = new JoesTouchDeploy.Console.FileDialogService();
        var vtzAnalyzer = new VtzAnalyzer(debugOutputDirectory);
        var vtzProjectReader = new VtzProjectReader(debugOutputDirectory);

        Console.WriteLine();
        Console.WriteLine("Authenticating...");
        Console.WriteLine($"Debug output : {debugOutputDirectory}");
        Console.WriteLine();

        try
        {
            await videoTecClient.LoginAsync();

            Console.WriteLine("Login successful.");

            var deviceInformationResult =
                await videoTecClient.GetDeviceInformationAsync();

            Console.WriteLine();
            Console.WriteLine($"Device information success : {deviceInformationResult.Success}");
            Console.WriteLine($"Device information message : {deviceInformationResult.Message}");

            if (!deviceInformationResult.Success || deviceInformationResult.Data == null)
            {
                return;
            }

            var deviceInformation = deviceInformationResult.Data;

            Console.WriteLine("Connected panel information:");
            Console.WriteLine($"Model : {deviceInformation.Model}");
            Console.WriteLine($"Serial : {deviceInformation.SerialNumber}");
            Console.WriteLine($"Host name : {deviceInformation.HostName}");
            Console.WriteLine($"Firmware : {deviceInformation.FirmwareVersion}");
            Console.WriteLine($"MAC address : {deviceInformation.MacAddress}");

            Console.WriteLine();
            Console.WriteLine("Select the VTZ project file to upload.");

            var projectFilePath = fileDialogService.SelectVtzProjectFile();

            if (string.IsNullOrWhiteSpace(projectFilePath))
            {
                Console.WriteLine("Project upload canceled. No file was selected.");
                return;
            }

            if (!File.Exists(projectFilePath))
            {
                Console.WriteLine($"Project file not found: {projectFilePath}");
                return;
            }

            Console.WriteLine($"Selected project file: {projectFilePath}");

            Console.Write("Analyze the selected VTZ file before upload? (y/N): ");
            var analyzeResponse = Console.ReadLine();

            if (analyzeResponse?.Equals("y", StringComparison.OrdinalIgnoreCase) == true ||
                analyzeResponse?.Equals("yes", StringComparison.OrdinalIgnoreCase) == true)
            {
                var analysisResult = await vtzAnalyzer.AnalyzeAsync(projectFilePath);

                Console.WriteLine("VTZ analysis complete.");
                Console.WriteLine($"Detected format : {analysisResult.DetectedFormat}");
                Console.WriteLine($"Recognized archive : {analysisResult.IsRecognizedArchive}");
                Console.WriteLine($"Entry count : {analysisResult.EntryCount}");
                Console.WriteLine($"Analysis report : {analysisResult.ReportPath}");

                if (analysisResult.DetectedFormat == "ZIP archive")
                {
                    var projectSummary = await vtzProjectReader.ReadAsync(projectFilePath);

                    Console.WriteLine("VTZ project summary complete.");
                    Console.WriteLine($"Environment.xml found : {projectSummary.EnvironmentXmlFound}");
                    Console.WriteLine($"SmartGraphics.xml found : {projectSummary.SmartGraphicsXmlFound}");
                    Console.WriteLine($".vtx file found : {projectSummary.VtxFileFound}");
                    Console.WriteLine($"Project summary report : {projectSummary.ReportPath}");
                }
            }

            Console.WriteLine("Upload started...");

            var deploymentResult =
                await deploymentService.DeployProjectAsync(projectFilePath);

            Console.WriteLine();
            Console.WriteLine("Deployment summary");
            Console.WriteLine("------------------");
            Console.WriteLine($"Deployment success : {deploymentResult.Success}");
            Console.WriteLine($"Message : {deploymentResult.Message}");
            Console.WriteLine($"Upload succeeded : {deploymentResult.UploadSucceeded}");
            Console.WriteLine($"UI responsive : {deploymentResult.UiResponsive}");

            if (deploymentResult.UploadResult != null)
            {
                Console.WriteLine($"HTTP status : {deploymentResult.UploadResult.HttpStatusCode}");
                Console.WriteLine($"Parsed server status : {deploymentResult.UploadResult.ServerStatusInfo}");
            }

            if (deploymentResult.CurrentProjectInformation != null)
            {
                Console.WriteLine("Current project information:");
                Console.WriteLine($"Project name : {deploymentResult.CurrentProjectInformation.ProjectName}");
                Console.WriteLine($"Compiled on : {deploymentResult.CurrentProjectInformation.CompiledOn}");
                Console.WriteLine($"Project file hash : {deploymentResult.CurrentProjectInformation.ProjectFileHash}");
                Console.WriteLine($"Version : {deploymentResult.CurrentProjectInformation.Version}");
            }

            Console.WriteLine("Upload response saved to DebugOutput\\project_upload_response.*");
        }
        catch (Exception exception)
        {
            logger.Log($"ERROR: {exception.GetType().Name}: {exception.Message}");
            Console.WriteLine();
            Console.WriteLine(exception);
        }
    }
}
