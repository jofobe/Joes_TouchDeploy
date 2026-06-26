using JoesTouchDeploy.Core.Networking;

Console.WriteLine("=================================");
Console.WriteLine("Joe's TouchDeploy");
Console.WriteLine("=================================");
Console.WriteLine();

var client = new CrestronHttpClient();

Console.Write("Panel IP: ");

var ip = Console.ReadLine();

Console.WriteLine();
Console.WriteLine("Connecting...");
Console.WriteLine();

try
{
    var html = await client.GetLoginPageAsync(ip!);

    Console.WriteLine("Connection successful!");
    Console.WriteLine();
    Console.WriteLine($"Downloaded {html.Length:N0} characters.");
}
catch (Exception ex)
{
    Console.WriteLine($"Connection failed: {ex.Message}");
}