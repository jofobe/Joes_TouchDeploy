using JoesTouchDeploy.Core.Models;
using JoesTouchDeploy.Core;
using JoesTouchDeploy.Core.Logging;
using JoesTouchDeploy.Core.Models;

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
        var fileDialogService = new JoesTouchDeploy.Console.FileDialogService();

        Console.WriteLine();
        Console.WriteLine("Authenticating...");
        Console.WriteLine($"Debug output : {debugOutputDirectory}");
        Console.WriteLine();

        try
        {
            await videoTecClient.LoginAsync();

            Console.WriteLine("Login successful.");

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
            Console.WriteLine("Upload started...");

            var uploadResult =
                await videoTecClient.UploadProjectAsync(projectFilePath);

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
    }
}
