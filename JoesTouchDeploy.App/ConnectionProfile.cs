namespace JoesTouchDeploy.App;

public class ConnectionProfile
{
    public string? FriendlyName { get; set; }

    public string IpAddress { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public override string ToString()
    {
        return string.IsNullOrWhiteSpace(FriendlyName)
            ? IpAddress
            : $"{FriendlyName} ({IpAddress})";
    }
}
