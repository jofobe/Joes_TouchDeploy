using System.Net;
using System.Net.Http.Headers;
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

    public async Task SaveDiagnosticResponseAsync(
        HttpRequestMessage request,
        HttpResponseMessage response,
        byte[] responseBytes,
        TimeSpan elapsed,
        string? redirectTarget,
        CookieCollection cookies)
    {
        var sequence = Interlocked.Increment(ref _sequence);
        var requestUri = response.RequestMessage?.RequestUri ?? request.RequestUri ?? new Uri("about:blank");
        var prefix = $"{sequence:000}_{SanitizeFileName(requestUri.Host + requestUri.AbsolutePath)}";
        var contentType = response.Content.Headers.ContentType?.ToString() ?? string.Empty;
        var savedFileName = await SaveResponseBodyAsync(prefix, requestUri, response, responseBytes);

        Log($"Request URL: {requestUri}");
        Log($"Status code: {(int)response.StatusCode} {response.StatusCode}");
        Log($"Redirect target: {redirectTarget ?? "none"}");
        Log($"Content-Type: {(string.IsNullOrWhiteSpace(contentType) ? "none" : contentType)}");
        Log($"Response size: {responseBytes.Length} bytes");

        await File.WriteAllTextAsync(
            Path.Combine(_outputDirectory, $"{prefix}_headers.txt"),
            BuildHeadersText(request, response, requestUri, redirectTarget, responseBytes.Length, elapsed));

        await SaveCookiesAsync(prefix, cookies);
        await AppendRequestIndexAsync(request, response, requestUri, contentType, responseBytes.Length, savedFileName, elapsed, redirectTarget);
    }

    public async Task SaveResponseAsync(
        Uri requestUri,
        HttpResponseMessage response,
        string content,
        string? redirectTarget,
        CookieCollection cookies)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

        await SaveDiagnosticResponseAsync(
            request,
            response,
            Encoding.UTF8.GetBytes(content),
            TimeSpan.Zero,
            redirectTarget,
            cookies);
    }

    public async Task SaveDiscoverySummaryAsync(string content)
    {
        await File.WriteAllTextAsync(
            Path.Combine(_outputDirectory, "discovery_summary.txt"),
            content);
    }

    private async Task<string> SaveResponseBodyAsync(
        string prefix,
        Uri requestUri,
        HttpResponseMessage response,
        byte[] responseBytes)
    {
        if (responseBytes.Length == 0)
        {
            return string.Empty;
        }

        var contentType = response.Content.Headers.ContentType?.ToString() ?? string.Empty;

        if (IsTextContent(contentType) || LooksLikeText(responseBytes))
        {
            var content = Encoding.UTF8.GetString(responseBytes);
            var fileName = $"{prefix}{GetFileExtension(contentType, content)}";

            await File.WriteAllTextAsync(
                Path.Combine(_outputDirectory, fileName),
                FormatContent(content));

            return fileName;
        }

        var binaryFileName = GetBinaryFileName(prefix, requestUri, response.Content.Headers, contentType);

        await File.WriteAllBytesAsync(
            Path.Combine(_outputDirectory, binaryFileName),
            responseBytes);

        return binaryFileName;
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

    private async Task AppendRequestIndexAsync(
        HttpRequestMessage request,
        HttpResponseMessage response,
        Uri requestUri,
        string contentType,
        int responseSize,
        string savedFileName,
        TimeSpan elapsed,
        string? redirectTarget)
    {
        var indexPath = Path.Combine(_outputDirectory, "requests.csv");

        if (!File.Exists(indexPath))
        {
            await File.WriteAllTextAsync(
                indexPath,
                "Timestamp,Method,URL,Status,Content-Type,Response size,Saved filename,Elapsed milliseconds,Redirect target" + Environment.NewLine);
        }

        var row = string.Join(",",
            Csv(DateTimeOffset.Now.ToString("O")),
            Csv(request.Method.Method),
            Csv(requestUri.ToString()),
            Csv($"{(int)response.StatusCode} {response.StatusCode}"),
            Csv(string.IsNullOrWhiteSpace(contentType) ? "none" : contentType),
            Csv(responseSize.ToString()),
            Csv(savedFileName),
            Csv(elapsed.TotalMilliseconds.ToString("F0")),
            Csv(redirectTarget ?? string.Empty));

        await File.AppendAllTextAsync(indexPath, row + Environment.NewLine);
    }

    private static string BuildHeadersText(
        HttpRequestMessage request,
        HttpResponseMessage response,
        Uri requestUri,
        string? redirectTarget,
        int responseSize,
        TimeSpan elapsed)
    {
        var builder = new StringBuilder();

        builder.AppendLine($"Timestamp: {DateTimeOffset.Now:O}");
        builder.AppendLine($"Method: {request.Method}");
        builder.AppendLine($"RequestUrl: {requestUri}");
        builder.AppendLine($"StatusCode: {(int)response.StatusCode} {response.StatusCode}");
        builder.AppendLine($"RedirectTarget: {redirectTarget ?? "none"}");
        builder.AppendLine($"ContentType: {response.Content.Headers.ContentType?.ToString() ?? "none"}");
        builder.AppendLine($"ResponseSize: {responseSize} bytes");
        builder.AppendLine($"ElapsedMilliseconds: {elapsed.TotalMilliseconds:F0}");
        builder.AppendLine();
        builder.AppendLine("Request Headers:");

        foreach (var header in request.Headers)
        {
            builder.AppendLine($"{header.Key}: {string.Join(", ", header.Value)}");
        }

        if (request.Content != null)
        {
            foreach (var header in request.Content.Headers)
            {
                builder.AppendLine($"{header.Key}: {string.Join(", ", header.Value)}");
            }
        }

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

        if (LooksLikeXml(content) || contentType.Contains("xml", StringComparison.OrdinalIgnoreCase))
        {
            return ".xml";
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

        if (contentType.Contains("css", StringComparison.OrdinalIgnoreCase))
        {
            return ".css";
        }

        return ".txt";
    }

    private static string GetBinaryFileName(
        string prefix,
        Uri requestUri,
        HttpContentHeaders headers,
        string contentType)
    {
        var fileName = headers.ContentDisposition?.FileNameStar ?? headers.ContentDisposition?.FileName;

        if (!string.IsNullOrWhiteSpace(fileName))
        {
            return SanitizeFileName(fileName.Trim('"'));
        }

        var extension = GetBinaryFileExtension(contentType, requestUri);

        return $"{prefix}_{DateTimeOffset.Now:yyyyMMddHHmmssfff}{extension}";
    }

    private static string GetBinaryFileExtension(string contentType, Uri requestUri)
    {
        var pathExtension = Path.GetExtension(requestUri.AbsolutePath);

        if (!string.IsNullOrWhiteSpace(pathExtension))
        {
            return pathExtension;
        }

        if (contentType.Contains("zip", StringComparison.OrdinalIgnoreCase))
        {
            return ".zip";
        }

        if (contentType.Contains("octet-stream", StringComparison.OrdinalIgnoreCase))
        {
            return ".bin";
        }

        return ".bin";
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

    private static bool LooksLikeXml(string content)
    {
        return content.TrimStart().StartsWith('<');
    }

    private static bool LooksLikeText(byte[] content)
    {
        if (content.Length == 0)
        {
            return false;
        }

        var sampleLength = Math.Min(content.Length, 512);
        var controlCharacters = 0;

        for (var index = 0; index < sampleLength; index++)
        {
            var value = content[index];

            if (value == 0)
            {
                return false;
            }

            if (value < 32 && value != '\r' && value != '\n' && value != '\t')
            {
                controlCharacters++;
            }
        }

        return controlCharacters == 0;
    }

    private static string Csv(string value)
    {
        return $"\"{value.Replace("\"", "\"\"")}\"";
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
