using System.Net;
using JoesTouchDeploy.Core.Networking;

Console.WriteLine("=================================");
Console.WriteLine("Joe's TouchDeploy");
Console.WriteLine("=================================");
Console.WriteLine();

var client = new PanelClient();

Console.Write("Panel IP: ");
var ip = Console.ReadLine()!;

Console.Write("Username: ");
var username = Console.ReadLine()!;

Console.Write("Password: ");
var password = Console.ReadLine()!;

Console.WriteLine();
Console.WriteLine("Downloading login page...");

var html = await client.GetLoginPageAsync(ip);

Console.WriteLine($"Downloaded {html.Length:N0} characters.");
Console.WriteLine();

var cookies = client.GetCookies(ip);

Console.WriteLine($"Cookies received: {cookies.Count}");

foreach (Cookie cookie in cookies)
{
    Console.WriteLine($"{cookie.Name} = {cookie.Value}");
}

Console.WriteLine();
Console.WriteLine("Submitting login...");

var response = await client.PostFormAsync(
    ip,
    username,
    password);

Console.WriteLine();
Console.WriteLine($"HTTP Status: {(int)response.StatusCode}");
Console.WriteLine(response.StatusCode);

Console.WriteLine();
Console.WriteLine("Cookies after login:");

cookies = client.GetCookies(ip);

foreach (Cookie cookie in cookies)
{
    Console.WriteLine($"{cookie.Name} = {cookie.Value}");
}

Console.WriteLine();
Console.WriteLine("Requesting authenticated page...");

var devicePage = await client.GetDevicePageAsync(ip);

Console.WriteLine($"Downloaded {devicePage.Length:N0} characters.");