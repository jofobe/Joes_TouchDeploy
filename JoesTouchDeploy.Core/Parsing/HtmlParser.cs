using System.Net;
using System.Text.RegularExpressions;
using JoesTouchDeploy.Core.Models;

namespace JoesTouchDeploy.Core.Parsing;

public class HtmlParser
{
    private static readonly Regex LinkRegex = new(
        @"<a\b[^>]*?\bhref\s*=\s*(?:(?<quote>['""`])(?<value>.*?)(\k<quote>)|(?<value>[^\s>]+))",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline);

    private static readonly Regex FormRegex = new(
        @"<form\b(?<attributes>[^>]*)>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline);

    private static readonly Regex AttributeRegex = new(
        @"\b(?<name>[a-zA-Z_:][-a-zA-Z0-9_:.]*)\s*=\s*(?:(?<quote>['""`])(?<value>.*?)(\k<quote>)|(?<value>[^\s>]+))",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline);

    private static readonly Regex ScriptRegex = new(
        @"<script\b[^>]*?\bsrc\s*=\s*(?:(?<quote>['""`])(?<value>.*?)(\k<quote>)|(?<value>[^\s>]+))",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline);

    private static readonly Regex IframeRegex = new(
        @"<iframe\b[^>]*?\bsrc\s*=\s*(?:(?<quote>['""`])(?<value>.*?)(\k<quote>)|(?<value>[^\s>]+))",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline);

    private static readonly Regex ResourceRegex = new(
        @"<(?:link|img|source|embed|object)\b[^>]*?\b(?:href|src|data)\s*=\s*(?:(?<quote>['""`])(?<value>.*?)(\k<quote>)|(?<value>[^\s>]+))",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline);

    private static readonly Regex QuotedReferenceRegex = new(
        @"(?<quote>['""`])(?<value>(?:https?://|/|\.\.?/)[^'""`\s<>]+)(\k<quote>)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline);

    public HtmlDiscoveryResult Parse(string html, Uri pageUrl)
    {
        var result = new HtmlDiscoveryResult();

        AddMatches(LinkRegex, html, pageUrl, result.Links);
        AddMatches(ScriptRegex, html, pageUrl, result.Scripts);
        AddMatches(IframeRegex, html, pageUrl, result.Iframes);
        AddMatches(ResourceRegex, html, pageUrl, result.Resources);
        AddMatches(QuotedReferenceRegex, html, pageUrl, result.Resources);
        AddForms(html, pageUrl, result.Forms);

        return result;
    }

    public IReadOnlyList<string> ParseReferences(string content, Uri pageUrl)
    {
        var references = new List<string>();

        AddMatches(QuotedReferenceRegex, content, pageUrl, references);

        return references;
    }

    private static void AddForms(string html, Uri pageUrl, List<HtmlFormInfo> forms)
    {
        foreach (Match match in FormRegex.Matches(html))
        {
            var attributes = GetAttributes(match.Groups["attributes"].Value);
            var method = attributes.TryGetValue("method", out var formMethod)
                ? formMethod.ToUpperInvariant()
                : "GET";

            var action = attributes.TryGetValue("action", out var formAction)
                ? ResolveUrl(pageUrl, formAction)
                : pageUrl.ToString();

            if (!string.IsNullOrWhiteSpace(action))
            {
                forms.Add(new HtmlFormInfo
                {
                    Action = action,
                    Method = method
                });
            }
        }
    }

    private static Dictionary<string, string> GetAttributes(string attributes)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in AttributeRegex.Matches(attributes))
        {
            values[match.Groups["name"].Value] = WebUtility.HtmlDecode(match.Groups["value"].Value);
        }

        return values;
    }

    private static void AddMatches(Regex regex, string html, Uri pageUrl, List<string> values)
    {
        foreach (Match match in regex.Matches(html))
        {
            var value = ResolveUrl(pageUrl, WebUtility.HtmlDecode(match.Groups["value"].Value));

            if (!string.IsNullOrWhiteSpace(value) && !values.Contains(value, StringComparer.OrdinalIgnoreCase))
            {
                values.Add(value);
            }
        }
    }

    private static string ResolveUrl(Uri pageUrl, string value)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            value.StartsWith("#", StringComparison.Ordinal) ||
            value.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        return Uri.TryCreate(pageUrl, value, out var resolved)
            ? resolved.ToString()
            : string.Empty;
    }
}
