using System.Text.Json;

namespace JoesTouchDeploy.App;

public class ProfileService
{
    private readonly string _profilesDirectory;
    private readonly string _profilesPath;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        WriteIndented = true
    };

    public ProfileService()
    {
        _profilesDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "JoesTouchDeploy");
        _profilesPath = Path.Combine(_profilesDirectory, "profiles.json");
    }

    public string ProfilesPath => _profilesPath;

    public async Task<List<ConnectionProfile>> LoadProfilesAsync()
    {
        if (!File.Exists(_profilesPath))
        {
            return [];
        }

        await using var stream = File.OpenRead(_profilesPath);
        var profiles = await JsonSerializer.DeserializeAsync<List<ConnectionProfile>>(stream) ?? [];

        return profiles
            .Where(profile => !string.IsNullOrWhiteSpace(profile.IpAddress))
            .OrderBy(profile => profile.FriendlyName ?? profile.IpAddress, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task SaveProfilesAsync(IEnumerable<ConnectionProfile> profiles)
    {
        Directory.CreateDirectory(_profilesDirectory);

        var cleanedProfiles = profiles
            .Where(profile => !string.IsNullOrWhiteSpace(profile.IpAddress))
            .GroupBy(profile => NormalizeIpAddress(profile.IpAddress), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.Last())
            .OrderBy(profile => profile.FriendlyName ?? profile.IpAddress, StringComparer.OrdinalIgnoreCase)
            .ToList();

        await using var stream = File.Create(_profilesPath);
        await JsonSerializer.SerializeAsync(stream, cleanedProfiles, _jsonSerializerOptions);
    }

    public static ConnectionProfile? FindByIpAddress(IEnumerable<ConnectionProfile> profiles, string ipAddress)
    {
        var normalizedIpAddress = NormalizeIpAddress(ipAddress);

        return profiles.FirstOrDefault(profile =>
            NormalizeIpAddress(profile.IpAddress).Equals(normalizedIpAddress, StringComparison.OrdinalIgnoreCase));
    }

    public static string NormalizeIpAddress(string ipAddress)
    {
        return ipAddress.Trim();
    }
}
