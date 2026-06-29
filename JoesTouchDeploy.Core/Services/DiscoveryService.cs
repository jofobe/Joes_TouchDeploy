using System.Text;
using JoesTouchDeploy.Core.Logging;
using JoesTouchDeploy.Core.Models;
using JoesTouchDeploy.Core.Networking;
using JoesTouchDeploy.Core.Parsing;

namespace JoesTouchDeploy.Core.Services;

public class DiscoveryService
{
    private const int MaxPages = 40;

    private static readonly string[] ConfigurationTerms =
    [
        "config",
        "configuration",
        "settings",
        "setup",
        "system",
        "device",
        "project",
        "vtz",
        "upload",
        "download",
        "backup",
        "restore",
        "file"
    ];

    private static readonly string[] TargetedProbePaths =
    [
        "/Device",
        "/Device/FilePaths",
        "/Device/FilePaths/Project",
        "/Device/UiUserProject",
        "/Device/DeviceInfo",
        "/Device/DeviceCapabilities",
        "/Device/SystemVersions",
        "/Device/UserInterfaceConfig",
        "/Device/CloudSettings",
        "/Device/ThirdPartyApplications",
        "/Device/DeviceConfiguration",
        "/Device/DeviceOperations",
        "/Device/Authentication",
        "/Device/CertificateStore",
        "/Device/CertificateStore/",
        "/Device/AudioVideoInputOutput/Inputs/0/Ports/Edid/",
        "/assets/data/deviceFamily.json",
        "/assets/data/version.json",
        "/assets/data/device-family-config/app.settings.",
        "/data/web/tmp/projectload/userproject",
        "/data/web/tmp/projectload/html5",
        "/data/web/tmp/firmware",
        "/realtimedata"
    ];

    private readonly HttpSession _httpSession;
    private readonly HtmlParser _htmlParser;
    private readonly DebugLogger _logger;

    public DiscoveryService(
        HttpSession httpSession,
        HtmlParser htmlParser,
        DebugLogger logger)
    {
        _httpSession = httpSession;
        _htmlParser = htmlParser;
        _logger = logger;
    }

    public async Task<DiscoveryResult> DiscoverAsync(Uri landingPageUrl)
    {
        var result = new DiscoveryResult
        {
            FinalLandingUrl = landingPageUrl
        };

        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var queued = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var queue = new Queue<Uri>();

        Enqueue(landingPageUrl, queue, queued, landingPageUrl.Host);

        foreach (var probeUrl in GetTargetedProbeUrls(landingPageUrl))
        {
            Enqueue(probeUrl, queue, queued, landingPageUrl.Host);
        }

        while (queue.Count > 0 && visited.Count < MaxPages)
        {
            var url = queue.Dequeue();
            var normalizedUrl = NormalizeUrl(url);

            if (!visited.Add(normalizedUrl))
            {
                continue;
            }

            try
            {
                var response = await _httpSession.GetSessionResponseAsync(normalizedUrl);
                result.VisitedUrls.Add(response.FinalUrl);

                AddTargetedProbeResult(result, response);

                if (!response.IsHtml)
                {
                    var references = _htmlParser.ParseReferences(response.Content, response.FinalUrl);

                    AddValues(references, result.Resources, result, landingPageUrl.Host);

                    foreach (var discoveredUrl in references.Select(reference => new Uri(reference)))
                    {
                        Enqueue(discoveredUrl, queue, queued, landingPageUrl.Host);
                    }

                    AddConfigurationCandidate(result, response.FinalUrl.ToString(), landingPageUrl.Host);
                    continue;
                }

                var parsed = _htmlParser.Parse(response.Content, response.FinalUrl);

                AddValues(parsed.Links, result.Links, result, landingPageUrl.Host);
                AddValues(parsed.Scripts, result.Scripts, result, landingPageUrl.Host);
                AddValues(parsed.Iframes, result.Iframes, result, landingPageUrl.Host);
                AddValues(parsed.Resources, result.Resources, result, landingPageUrl.Host);
                AddForms(parsed.Forms, result, landingPageUrl.Host);

                foreach (var discoveredUrl in GetCrawlableUrls(response.FinalUrl, parsed))
                {
                    Enqueue(discoveredUrl, queue, queued, landingPageUrl.Host);
                }
            }
            catch (HttpRequestException exception)
            {
                _logger.Log($"Discovery request failed: {url} - {exception.Message}");
            }
            catch (TaskCanceledException exception)
            {
                _logger.Log($"Discovery request timed out: {url} - {exception.Message}");
            }
        }

        await _logger.SaveDiscoverySummaryAsync(BuildSummary(result));

        return result;
    }

    private static IEnumerable<Uri> GetCrawlableUrls(Uri pageUrl, HtmlDiscoveryResult parsed)
    {
        foreach (var value in parsed.Links.Concat(parsed.Scripts).Concat(parsed.Iframes).Concat(parsed.Resources))
        {
            if (Uri.TryCreate(value, UriKind.Absolute, out var url) && IsHttpUrl(url))
            {
                yield return url;
            }
        }

        foreach (var form in parsed.Forms)
        {
            if (Uri.TryCreate(pageUrl, form.Action, out var url) && IsHttpUrl(url))
            {
                yield return url;
            }
        }
    }

    private static void Enqueue(Uri url, Queue<Uri> queue, HashSet<string> queued, string host)
    {
        if (!IsHttpUrl(url) ||
            !string.Equals(url.Host, host, StringComparison.OrdinalIgnoreCase) ||
            !IsUsefulLocalReference(url))
        {
            return;
        }

        var normalizedUrl = NormalizeUrl(url);

        if (queued.Add(normalizedUrl))
        {
            queue.Enqueue(new Uri(normalizedUrl));
        }
    }

    private static void AddValues(
        IEnumerable<string> values,
        SortedSet<string> destination,
        DiscoveryResult result,
        string host)
    {
        foreach (var value in values)
        {
            if (!IsUsefulCandidate(value, host))
            {
                continue;
            }

            destination.Add(value);
            AddConfigurationCandidate(result, value, host);
        }
    }

    private static void AddForms(IEnumerable<HtmlFormInfo> forms, DiscoveryResult result, string host)
    {
        foreach (var form in forms)
        {
            if (!IsUsefulCandidate(form.Action, host))
            {
                continue;
            }

            result.Forms.Add(form);
            AddConfigurationCandidate(result, form.Action, host);
        }
    }

    private static void AddConfigurationCandidate(DiscoveryResult result, string value, string host)
    {
        if (IsUsefulCandidate(value, host) && ConfigurationTerms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase)))
        {
            result.ConfigurationCandidates.Add(value);
        }
    }

    private static IEnumerable<Uri> GetTargetedProbeUrls(Uri landingPageUrl)
    {
        foreach (var path in TargetedProbePaths)
        {
            yield return new Uri(landingPageUrl, path);
        }
    }

    private static void AddTargetedProbeResult(DiscoveryResult result, HttpSessionResponse response)
    {
        if (!TargetedProbePaths.Any(path => response.FinalUrl.AbsolutePath.Equals(path, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        result.TargetedProbes.Add(new EndpointProbeResult
        {
            Url = response.FinalUrl.ToString(),
            StatusCode = (int)response.StatusCode,
            ContentType = string.IsNullOrWhiteSpace(response.ContentType) ? "none" : response.ContentType,
            ResponseSize = response.ResponseSize
        });

        if ((int)response.StatusCode >= 200 && (int)response.StatusCode <= 299)
        {
            result.ConfigurationCandidates.Add(response.FinalUrl.ToString());
        }
    }

    private static string BuildSummary(DiscoveryResult result)
    {
        var builder = new StringBuilder();

        builder.AppendLine("Joe's TouchDeploy Discovery Summary");
        builder.AppendLine($"Generated: {DateTimeOffset.Now:O}");
        builder.AppendLine($"Final landing page URL: {result.FinalLandingUrl}");
        builder.AppendLine();

        AppendSection(builder, "Visited URLs", result.VisitedUrls.Select(url => url.ToString()));
        AppendSection(builder, "Links", result.Links);
        AppendSection(builder, "Forms", result.Forms.Select(form => $"{form.Method} {form.Action}"));
        AppendSection(builder, "Scripts", result.Scripts);
        AppendSection(builder, "Iframes", result.Iframes);
        AppendSection(builder, "Referenced Resources", result.Resources);
        AppendSection(
            builder,
            "Targeted Probe Results",
            result.TargetedProbes.Select(probe => $"{probe.StatusCode} {probe.ContentType} {probe.ResponseSize} bytes {probe.Url}"));
        AppendSection(builder, "Configuration Candidates", result.ConfigurationCandidates);

        return builder.ToString();
    }

    private static void AppendSection(
        StringBuilder builder,
        string title,
        IEnumerable<string> values)
    {
        builder.AppendLine(title);
        builder.AppendLine(new string('-', title.Length));

        var count = 0;

        foreach (var value in values.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.OrdinalIgnoreCase).Order())
        {
            builder.AppendLine(value);
            count++;
        }

        if (count == 0)
        {
            builder.AppendLine("none");
        }

        builder.AppendLine();
    }

    private static bool IsHttpUrl(Uri url)
    {
        return url.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
            url.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsUsefulCandidate(string value, string host)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var url) &&
            string.Equals(url.Host, host, StringComparison.OrdinalIgnoreCase) &&
            IsUsefulLocalReference(url);
    }

    private static bool IsUsefulLocalReference(Uri url)
    {
        var path = Uri.UnescapeDataString(url.AbsolutePath);

        if (string.IsNullOrWhiteSpace(path) || path.Length < 2)
        {
            return false;
        }

        if (path.Contains("${", StringComparison.Ordinal) ||
            path.Contains('^', StringComparison.Ordinal) ||
            path.Contains('(') ||
            path.Contains(')') ||
            path.Contains(',') ||
            path.Contains('*') ||
            path.Contains(' '))
        {
            return false;
        }

        if (IsLikelyLibraryNoise(path))
        {
            return false;
        }

        return path.StartsWith("/Device", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/assets/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/data/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/resources/", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/index.html", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/index_device.html", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/banner.txt", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/bannercontents.txt", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/realtimedata", StringComparison.OrdinalIgnoreCase) ||
            IsUsefulScript(path) ||
            path.EndsWith(".css", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".html", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".txt", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsUsefulScript(string path)
    {
        if (!path.EndsWith(".js", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return path.StartsWith("/resources/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/include/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/runtime.", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/polyfills.", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/scripts.", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/main.", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsLikelyLibraryNoise(string path)
    {
        var trimmedPath = path.Trim('/');

        if (trimmedPath.Length <= 3 && !trimmedPath.Contains('.'))
        {
            return true;
        }

        if (trimmedPath.EndsWith(".js", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return trimmedPath.Contains("ns.adobe.com", StringComparison.OrdinalIgnoreCase) ||
            trimmedPath.Contains("www.w3.org", StringComparison.OrdinalIgnoreCase) ||
            trimmedPath.Contains("purl.org", StringComparison.OrdinalIgnoreCase) ||
            trimmedPath.Contains("jspdf", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeUrl(Uri url)
    {
        var builder = new UriBuilder(url)
        {
            Fragment = string.Empty
        };

        return builder.Uri.ToString();
    }
}
