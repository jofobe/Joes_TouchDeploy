using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using JoesTouchDeploy.Core.Models;

namespace JoesTouchDeploy.Core.Utilities;

public class VtzProjectReader
{
    private static readonly string[] ExactFileNames =
    [
        "Environment.xml",
        "SmartGraphics.xml",
        "Annotation.ini",
        "XPanel.ini"
    ];

    private static readonly Regex GuidLikeRegex = new(
        @"\b[0-9a-fA-F]{8}(?:-[0-9a-fA-F]{4}){3}-[0-9a-fA-F]{12}\b|\b[0-9a-fA-F]{32}\b",
        RegexOptions.Compiled);

    private readonly string _outputDirectory;

    public VtzProjectReader(string outputDirectory)
    {
        _outputDirectory = outputDirectory;
        Directory.CreateDirectory(_outputDirectory);
    }

    public async Task<VtzProjectSummaryResult> ReadAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("VTZ file path is required.", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("VTZ file was not found.", filePath);
        }

        var result = new VtzProjectSummaryResult
        {
            FilePath = filePath
        };

        var report = new StringBuilder();
        AppendHeader(report, filePath);

        using var archive = ZipFile.OpenRead(filePath);

        result.EnvironmentXmlFound = await AppendXmlSummaryAsync(
            archive,
            "Environment.xml",
            report,
            "Environment.xml",
            includeGuidLikeValues: false);

        result.SmartGraphicsXmlFound = await AppendXmlSummaryAsync(
            archive,
            "SmartGraphics.xml",
            report,
            "SmartGraphics.xml",
            includeGuidLikeValues: true);

        AppendTextFileSummary(archive, "Annotation.ini", report);
        AppendTextFileSummary(archive, "XPanel.ini", report);
        result.VtxFileFound = AppendVtxSummary(archive, report);

        result.FilesRead = CountFilesRead(result, archive);
        result.ReportPath = await SaveReportAsync(report.ToString());

        return result;
    }

    private static async Task<bool> AppendXmlSummaryAsync(
        ZipArchive archive,
        string fileName,
        StringBuilder report,
        string title,
        bool includeGuidLikeValues)
    {
        var entry = FindEntry(archive, fileName);

        AppendSectionTitle(report, title);

        if (entry == null)
        {
            report.AppendLine("Not found.");
            report.AppendLine();
            return false;
        }

        await using var stream = entry.Open();
        var document = await XDocument.LoadAsync(stream, LoadOptions.None, CancellationToken.None);

        if (document.Root == null)
        {
            report.AppendLine("XML document has no root element.");
            report.AppendLine();
            return true;
        }

        var elements = document.Descendants().ToList();
        var attributes = elements.SelectMany(element => element.Attributes()).ToList();
        var namespaces = elements
            .Select(element => element.Name.NamespaceName)
            .Concat(attributes.Select(attribute => attribute.Name.NamespaceName))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();

        report.AppendLine($"Archive path: {entry.FullName}");
        report.AppendLine($"Root element: {document.Root.Name.LocalName}");
        report.AppendLine($"Root namespace: {FormatNamespace(document.Root.Name.NamespaceName)}");
        report.AppendLine($"XML namespaces: {FormatNamespaces(namespaces)}");
        report.AppendLine($"Total element count: {elements.Count}");
        report.AppendLine($"Attribute count: {attributes.Count}");
        report.AppendLine($"Maximum tree depth: {GetMaximumDepth(document.Root)}");
        report.AppendLine();

        AppendCounts(report, "Top element names", elements.Select(element => element.Name.LocalName));
        AppendCounts(report, "Top attribute names", attributes.Select(attribute => attribute.Name.LocalName));

        if (includeGuidLikeValues)
        {
            AppendGuidLikeValues(report, document);
        }

        return true;
    }

    private static void AppendTextFileSummary(ZipArchive archive, string fileName, StringBuilder report)
    {
        var entry = FindEntry(archive, fileName);

        AppendSectionTitle(report, fileName);

        if (entry == null)
        {
            report.AppendLine("Not found.");
            report.AppendLine();
            return;
        }

        using var stream = entry.Open();
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var content = reader.ReadToEnd();
        var lines = content.Split(["\r\n", "\n"], StringSplitOptions.None);

        report.AppendLine($"Archive path: {entry.FullName}");
        report.AppendLine($"Size: {entry.Length} bytes");
        report.AppendLine($"Line count: {lines.Length}");
        report.AppendLine("Preview:");

        foreach (var line in lines.Take(40))
        {
            report.AppendLine(line);
        }

        if (lines.Length > 40)
        {
            report.AppendLine("...preview truncated...");
        }

        report.AppendLine();
    }

    private static bool AppendVtxSummary(ZipArchive archive, StringBuilder report)
    {
        var entry = archive.Entries.FirstOrDefault(
            entry => Path.GetExtension(entry.FullName).Equals(".vtx", StringComparison.OrdinalIgnoreCase));

        AppendSectionTitle(report, ".vtx file");

        if (entry == null)
        {
            report.AppendLine("Not found.");
            report.AppendLine();
            return false;
        }

        using var stream = entry.Open();
        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        var bytes = memoryStream.ToArray();

        report.AppendLine($"Archive path: {entry.FullName}");
        report.AppendLine($"Size: {entry.Length} bytes");

        if (LooksLikeText(bytes))
        {
            report.AppendLine("Detected as text. Contents:");
            report.AppendLine(Encoding.UTF8.GetString(bytes));
        }
        else
        {
            report.AppendLine("Detected as binary. First 64 bytes:");
            report.AppendLine(ToHex(bytes.Take(64).ToArray()));
        }

        report.AppendLine();
        return true;
    }

    private static void AppendHeader(StringBuilder report, string filePath)
    {
        var fileInfo = new FileInfo(filePath);

        report.AppendLine("VTZ Project Summary");
        report.AppendLine("===================");
        report.AppendLine($"Generated: {DateTimeOffset.Now:O}");
        report.AppendLine($"File: {filePath}");
        report.AppendLine($"File name: {fileInfo.Name}");
        report.AppendLine($"File size: {fileInfo.Length} bytes");
        report.AppendLine();
    }

    private static void AppendSectionTitle(StringBuilder report, string title)
    {
        report.AppendLine(title);
        report.AppendLine(new string('-', title.Length));
    }

    private static void AppendCounts(StringBuilder report, string title, IEnumerable<string> names)
    {
        report.AppendLine(title);

        var counts = names
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .GroupBy(name => name, StringComparer.OrdinalIgnoreCase)
            .Select(group => new { Name = group.Key, Count = group.Count() })
            .OrderByDescending(item => item.Count)
            .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .Take(50)
            .ToList();

        if (counts.Count == 0)
        {
            report.AppendLine("none");
        }
        else
        {
            foreach (var item in counts)
            {
                report.AppendLine($"{item.Name}: {item.Count}");
            }
        }

        report.AppendLine();
    }

    private static void AppendGuidLikeValues(StringBuilder report, XDocument document)
    {
        var text = document.ToString(SaveOptions.DisableFormatting);
        var values = GuidLikeRegex.Matches(text)
            .Select(match => match.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(100)
            .ToList();

        report.AppendLine("GUID-like values");

        if (values.Count == 0)
        {
            report.AppendLine("none");
        }
        else
        {
            foreach (var value in values)
            {
                report.AppendLine(value);
            }
        }

        report.AppendLine();
    }

    private async Task<string> SaveReportAsync(string report)
    {
        var reportPath = Path.Combine(_outputDirectory, "vtz_project_summary.txt");

        await File.WriteAllTextAsync(reportPath, report);

        return reportPath;
    }

    private static ZipArchiveEntry? FindEntry(ZipArchive archive, string fileName)
    {
        return archive.Entries.FirstOrDefault(
            entry => Path.GetFileName(entry.FullName).Equals(fileName, StringComparison.OrdinalIgnoreCase));
    }

    private static int CountFilesRead(VtzProjectSummaryResult result, ZipArchive archive)
    {
        var count = 0;

        count += result.EnvironmentXmlFound ? 1 : 0;
        count += result.SmartGraphicsXmlFound ? 1 : 0;
        count += result.VtxFileFound ? 1 : 0;
        count += ExactFileNames.Count(fileName => FindEntry(archive, fileName) != null && !fileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase));

        return count;
    }

    private static int GetMaximumDepth(XElement element)
    {
        if (!element.Elements().Any())
        {
            return 1;
        }

        return 1 + element.Elements().Max(GetMaximumDepth);
    }

    private static string FormatNamespace(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "none" : value;
    }

    private static string FormatNamespaces(IReadOnlyCollection<string> namespaces)
    {
        return namespaces.Count == 0 ? "none" : string.Join(", ", namespaces);
    }

    private static bool LooksLikeText(byte[] content)
    {
        if (content.Length == 0)
        {
            return true;
        }

        var sampleLength = Math.Min(content.Length, 512);

        for (var index = 0; index < sampleLength; index++)
        {
            var value = content[index];

            if (value == 0 || value < 8)
            {
                return false;
            }
        }

        return true;
    }

    private static string ToHex(byte[] bytes)
    {
        return string.Join(' ', bytes.Select(value => value.ToString("X2")));
    }
}
