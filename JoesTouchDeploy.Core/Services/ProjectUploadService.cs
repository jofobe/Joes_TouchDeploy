using System.Net.Http.Headers;
using System.Text.Json;
using JoesTouchDeploy.Core.Logging;
using JoesTouchDeploy.Core.Models;
using JoesTouchDeploy.Core.Networking;

namespace JoesTouchDeploy.Core.Services;

public class ProjectUploadService
{
    private const string UploadEndpoint = "/Device/DeviceOperations";
    private const string SuccessStatusInfo = "Success. Restarting UI..";
    private const string BoundaryPrefix = "----WebKitFormBoundary";

    private readonly HttpSession _httpSession;
    private readonly DebugLogger _logger;
    private readonly Uri _baseUri;

    public ProjectUploadService(HttpSession httpSession, DebugLogger logger, string ipAddress)
    {
        _httpSession = httpSession;
        _logger = logger;
        _baseUri = new Uri($"https://{ipAddress}");
    }

    public async Task<ProjectUploadResult> UploadProjectAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("Project file path is required.", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Project file was not found.", filePath);
        }

        var uploadUri = new Uri(_baseUri, UploadEndpoint);
        var fileName = Path.GetFileName(filePath);

        LogRequest(uploadUri, fileName);

        var response = await _httpSession.PostMultipartAsync(
            uploadUri.ToString(),
            () => CreateMultipartContent(filePath, fileName),
            CreateUploadHeaders());

        await SaveUploadResponseAsync(response.Content);

        var uploadResponse = DeserializeResponse(response.Content);
        var statusInfo = GetUploadProjectStatusInfo(uploadResponse) ??
            GetFallbackStatusInfo(response);

        _logger.Log($"Upload response status info: {(string.IsNullOrWhiteSpace(statusInfo) ? "none" : statusInfo)}");

        return new ProjectUploadResult
        {
            HttpStatusCode = (int)response.StatusCode,
            ServerStatusInfo = statusInfo,
            Success = statusInfo.Equals(SuccessStatusInfo, StringComparison.Ordinal),
            ResponseJson = response.Content
        };
    }

    private static MultipartFormDataContent CreateMultipartContent(string filePath, string fileName)
    {
        var boundary = BoundaryPrefix + Guid.NewGuid().ToString("N")[..16];
        var multipartContent = new MultipartFormDataContent(boundary);
        var boundaryParameter = multipartContent.Headers.ContentType?.Parameters
            .FirstOrDefault(parameter => parameter.Name.Equals("boundary", StringComparison.OrdinalIgnoreCase));

        if (boundaryParameter != null)
        {
            boundaryParameter.Value = boundary;
        }

        var projectTypeContent = new StringContent("User");
        projectTypeContent.Headers.ContentType = null;
        projectTypeContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
        {
            Name = "\"ProjectType\""
        };
        multipartContent.Add(projectTypeContent);

        var fileStream = File.OpenRead(filePath);
        var fileContent = new KnownLengthStreamContent(fileStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
        {
            Name = "\"UploadProject\"",
            FileName = $"\"{fileName}\""
        };
        multipartContent.Add(fileContent);

        return multipartContent;
    }

    private void LogRequest(Uri uploadUri, string fileName)
    {
        _logger.Log($"Project upload request URL: {uploadUri}");
        _logger.Log("Project upload request headers: Accept, Origin, Referer, X-CREST-XSRF-TOKEN, X-File-Upload");
        _logger.Log("Project upload multipart fields: ProjectType, UploadProject");
        _logger.Log($"Project upload file name: {fileName}");
        _logger.Log("Project upload file content is binary and will not be logged.");
    }

    private Dictionary<string, string> CreateUploadHeaders()
    {
        var headers = new Dictionary<string, string>
        {
            { "Accept", "application/json, text/plain, */*" },
            { "Cache-Control", "no-cache" },
            { "Origin", _baseUri.ToString().TrimEnd('/') },
            { "Pragma", "no-cache" },
            { "Referer", _baseUri.ToString() },
            { "X-File-Upload", "true" }
        };

        if (!string.IsNullOrWhiteSpace(_httpSession.CrestXsrfToken))
        {
            headers.Add("X-CREST-XSRF-TOKEN", _httpSession.CrestXsrfToken);
        }
        else
        {
            _logger.Log("Warning: CREST-XSRF-TOKEN was not captured before upload.");
        }

        return headers;
    }

    private async Task SaveUploadResponseAsync(string responseJson)
    {
        await File.WriteAllTextAsync(
            Path.Combine(_logger.OutputDirectory, GetUploadResponseFileName(responseJson)),
            FormatResponse(responseJson));
    }

    private static ProjectUploadResponse? DeserializeResponse(string responseJson)
    {
        if (string.IsNullOrWhiteSpace(responseJson))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<ProjectUploadResponse>(responseJson);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string FormatResponse(string responseJson)
    {
        if (string.IsNullOrWhiteSpace(responseJson))
        {
            return responseJson;
        }

        try
        {
            using var document = JsonDocument.Parse(responseJson);

            return JsonSerializer.Serialize(
                document.RootElement,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });
        }
        catch (JsonException)
        {
            return responseJson;
        }
    }

    private static string GetUploadResponseFileName(string response)
    {
        return LooksLikeJson(response)
            ? "project_upload_response.json"
            : "project_upload_response.txt";
    }

    private static string GetFallbackStatusInfo(HttpSessionResponse response)
    {
        if (response.Content.Trim().Equals("null", StringComparison.OrdinalIgnoreCase))
        {
            return $"Server returned null: {(int)response.StatusCode} {response.StatusCode}";
        }

        if (!string.IsNullOrWhiteSpace(response.Content))
        {
            return $"Non-JSON response from server: {(int)response.StatusCode} {response.StatusCode}";
        }

        return $"Empty response from server: {(int)response.StatusCode} {response.StatusCode}";
    }

    private static string? GetUploadProjectStatusInfo(ProjectUploadResponse? response)
    {
        var deviceStatusInfo = response?.Device?.DeviceOperations?.UploadProject?.StatusInfo;

        if (!string.IsNullOrWhiteSpace(deviceStatusInfo))
        {
            return deviceStatusInfo;
        }

        return response?.Actions
            .SelectMany(action => action.Results)
            .FirstOrDefault(result => result.Property.Equals("UploadProject", StringComparison.OrdinalIgnoreCase))
            ?.StatusInfo ??
            response?.Actions
                .SelectMany(action => action.Results)
                .FirstOrDefault()
                ?.StatusInfo;
    }

    private static bool LooksLikeJson(string response)
    {
        var trimmedResponse = response.TrimStart();

        return trimmedResponse.StartsWith('{') || trimmedResponse.StartsWith('[');
    }

    private sealed class KnownLengthStreamContent : StreamContent
    {
        private readonly Stream _stream;

        public KnownLengthStreamContent(Stream stream)
            : base(stream)
        {
            _stream = stream;
        }

        protected override bool TryComputeLength(out long length)
        {
            if (_stream.CanSeek)
            {
                length = _stream.Length - _stream.Position;
                return true;
            }

            length = 0;
            return false;
        }
    }
}
