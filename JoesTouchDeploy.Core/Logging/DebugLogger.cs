using System.Net;
using System.Text;
using System.Text.Json;

namespace JoesTouchDeploy.Core.Logging;

public class DebugLogger
{
    private readonly string _outputDirectory;
    private int _sequence;

    public DebugLogger(string outputDirectory)
    {
        _outputDirectory = outputDirectory;
        Directory.CreateDirectory(_outputDirectory);

        foreach (var file in Directory.GetFiles(_outputDirectory))
        {
            File.Delete(file);
        }
    }

    public string OutputDirectory => _outputDirectory;

    public void Log(string message)
    {
        var line = $"[{DateTimeOffset.Now:O}] {message}";

        Console.WriteLine(message);
        File.AppendAllText(
            Path.Combine(_outputDirectory, "session.log"),
            line + Environment.NewLine);
    }

    public async Task SaveResponseAsync(
        Uri requestUri,
        HttpResponseMessage response,
        string content,
        string? redirectTarget,
        CookieCollection cookies)
    {
        var sequence = Interlocked.Increment(ref _sequence);
        var prefix = $"{sequence:000}_{SanitizeFileName(requestUri.Host + requestUri.AbsolutePath)}";
        var contentType = response.Content.Headers.ContentType?.ToString() ?? string.Empty;
        var responseSize = Encoding.UTF8.GetByteCount(content);

        Log($"Request URL: {requestUri}");
        Log($"Status code: {(int)response.StatusCode} {response.StatusCode}");
        Log($"Redirect target: {redirectTarget ?? "none"}");
        Log($"Content-Type: {(string.IsNullOrWhiteSpace(contentType) ? "none" : contentType)}");
        Log($"Response size: {responseSize} bytes");

        await File.WriteAllTextAsync(
            Path.Combine(_outputDirectory, $"{prefix}_headers.txt"),
            BuildHeadersText(requestUri, response, redirectTarget, responseSize));

        await SaveCookiesAsync(prefix, cookies);

        if (IsTextContent(contentType) || LooksLikeText(content))
        {
            await File.WriteAllTextAsync(
                Path.Combine(_outputDirectory, $"{prefix}{GetFileExtension(contentType, content)}"),
                FormatContent(content));
        }
    }

    public async Task SaveDiscoverySummaryAsync(string content)
    {
        await File.WriteAllTextAsync(
            Path.Combine(_outputDirectory, "discovery_summary.txt"),
            content);
    }

    private async Task SaveCookiesAsync(string prefix, CookieCollection cookies)
    {
        var cookieText = BuildCookieText(cookies);

        await File.WriteAllTextAsync(
            Path.Combine(_outputDirectory, $"{prefix}_cookies.txt"),
            cookieText);

        await File.WriteAllTextAsync(
            Path.Combine(_outputDirectory, "latest_cookies.txt"),
            cookieText);
    }

    private static string BuildHeadersText(
        Uri requestUri,
        HttpResponseMessage response,
        string? redirectTarget,
        int responseSize)
    {
        var builder = new StringBuilder();

        builder.AppendLine($"RequestUrl: {requestUri}");
        builder.AppendLine($"StatusCode: {(int)response.StatusCode} {response.StatusCode}");
        builder.AppendLine($"RedirectTarget: {redirectTarget ?? "none"}");
        builder.AppendLine($"ContentType: {response.Content.Headers.ContentType?.ToString() ?? "none"}");
        builder.AppendLine($"ResponseSize: {responseSize} bytes");
        builder.AppendLine();
        builder.AppendLine("Response Headers:");

        foreach (var header in response.Headers)
        {
            builder.AppendLine($"{header.Key}: {string.Join(", ", header.Value)}");
        }

        builder.AppendLine();
        builder.AppendLine("Content Headers:");

        foreach (var header in response.Content.Headers)
        {
            builder.AppendLine($"{header.Key}: {string.Join(", ", header.Value)}");
        }

        return builder.ToString();
    }

    private static string BuildCookieText(CookieCollection cookies)
    {
        var builder = new StringBuilder();

        if (cookies.Count == 0)
        {
            builder.AppendLine("No cookies.");
            return builder.ToString();
        }

        foreach (Cookie cookie in cookies)
        {
            builder.AppendLine($"{cookie.Name}={cookie.Value}; Domain={cookie.Domain}; Path={cookie.Path}; Expires={cookie.Expires:O}; Secure={cookie.Secure}; HttpOnly={cookie.HttpOnly}");
        }

        return builder.ToString();
    }

    private static bool IsTextContent(string contentType)
    {
        return contentType.Contains("text/html", StringComparison.OrdinalIgnoreCase) ||
            contentType.Contains("application/xhtml", StringComparison.OrdinalIgnoreCase) ||
            contentType.Contains("text/", StringComparison.OrdinalIgnoreCase) ||
            contentType.Contains("javascript", StringComparison.OrdinalIgnoreCase) ||
            contentType.Contains("json", StringComparison.OrdinalIgnoreCase) ||
            contentType.Contains("xml", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetFileExtension(string contentType, string content)
    {
        if (LooksLikeJson(content))
        {
            return ".json";
        }

        if (contentType.Contains("html", StringComparison.OrdinalIgnoreCase) ||
            contentType.Contains("xhtml", StringComparison.OrdinalIgnoreCase))
        {
            return ".html";
        }

        if (contentType.Contains("javascript", StringComparison.OrdinalIgnoreCase))
        {
            return ".js";
        }

        if (contentType.Contains("json", StringComparison.OrdinalIgnoreCase))
        {
            return ".json";
        }

        if (contentType.Contains("xml", StringComparison.OrdinalIgnoreCase))
        {
            return ".xml";
        }

        if (contentType.Contains("css", StringComparison.OrdinalIgnoreCase))
        {
            return ".css";
        }

        return ".txt";
    }

    private static string FormatContent(string content)
    {
        if (!LooksLikeJson(content))
        {
            return content;
        }

        try
        {
            using var document = JsonDocument.Parse(content);

            return JsonSerializer.Serialize(
                document.RootElement,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });
        }
        catch (JsonException)
        {
            return content;
        }
    }

    private static bool LooksLikeJson(string content)
    {
        var trimmedContent = content.TrimStart();

        return trimmedContent.StartsWith('{') || trimmedContent.StartsWith('[');
    }

    private static bool LooksLikeText(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return false;
        }

        if (LooksLikeJson(content))
        {
            return true;
        }

        var sample = content.Length > 512 ? content[..512] : content;

        return !sample.Any(character => char.IsControl(character) && character != '\r' && character != '\n' && character != '\t');
    }

    private static string SanitizeFileName(string value)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars();
        var sanitized = new string(value.Select(character => invalidCharacters.Contains(character) ? '_' : character).ToArray());

        sanitized = sanitized
            .Replace('/', '_')
            .Replace('\\', '_')
            .Replace(':', '_');

        return string.IsNullOrWhiteSpace(sanitized) ? "response" : sanitized;
    }
}
