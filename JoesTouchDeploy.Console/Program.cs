using JoesTouchDeploy.Core.Models;
using JoesTouchDeploy.Core.Networking;
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

var panelClient = new PanelClient();

var authenticationService =
    new AuthenticationService(panelClient);

Console.WriteLine();
Console.WriteLine("Authenticating...");
Console.WriteLine();

var result =
    await authenticationService.AuthenticateAsync(connection);

Console.WriteLine($"Success : {result.Success}");
Console.WriteLine($"Message : {result.Message}");