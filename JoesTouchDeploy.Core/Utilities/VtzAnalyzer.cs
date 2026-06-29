using System.IO.Compression;
using System.Text;
using JoesTouchDeploy.Core.Models;

namespace JoesTouchDeploy.Core.Utilities;

public class VtzAnalyzer
{
    private readonly string _outputDirectory;

    public VtzAnalyzer(string outputDirectory)
    {
        _outputDirectory = outputDirectory;
        Directory.CreateDirectory(_outputDirectory);
    }

    public async Task<VtzAnalysisResult> AnalyzeAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("VTZ file path is required.", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("VTZ file was not found.", filePath);
        }

        var signature = await ReadSignatureAsync(filePath);
        var format = DetectFormat(signature);
        var report = new StringBuilder();
        var result = new VtzAnalysisResult
        {
            FilePath = filePath,
            DetectedFormat = format,
            IsRecognizedArchive = IsRecognizedArchive(format)
        };

        AppendHeader(report, filePath, format);

        if (format == "ZIP archive")
        {
            result.EntryCount = AnalyzeZip(filePath, report);
        }
        else
        {
            await AnalyzeUnknownOrUnsupportedAsync(filePath, signature, report);
        }

        result.ReportPath = await SaveReportAsync(filePath, report.ToString());

        return result;
    }

    private static async Task<byte[]> ReadSignatureAsync(string filePath)
    {
        var buffer = new byte[512];

        await using var stream = File.OpenRead(filePath);
        var bytesRead = await stream.ReadAsync(buffer);

        return buffer[..bytesRead];
    }

    private static string DetectFormat(byte[] signature)
    {
        if (StartsWith(signature, [0x50, 0x4B, 0x03, 0x04]) ||
            StartsWith(signature, [0x50, 0x4B, 0x05, 0x06]) ||
            StartsWith(signature, [0x50, 0x4B, 0x07, 0x08]))
        {
            return "ZIP archive";
        }

        if (StartsWith(signature, [0x1F, 0x8B]))
        {
            return "GZip archive";
        }

        if (StartsWith(signature, [0x37, 0x7A, 0xBC, 0xAF, 0x27, 0x1C]))
        {
            return "7z archive";
        }

        if (HasTarSignature(signature))
        {
            return "TAR archive";
        }

        return "Unrecognized format";
    }

    private static int AnalyzeZip(string filePath, StringBuilder report)
    {
        var entryCount = 0;

        report.AppendLine("ZIP contents");
        report.AppendLine("------------");

        using var archive = ZipFile.OpenRead(filePath);

        foreach (var entry in archive.Entries.OrderBy(entry => entry.FullName, StringComparer.OrdinalIgnoreCase))
        {
            entryCount++;

            var type = entry.FullName.EndsWith('/') || string.IsNullOrEmpty(entry.Name)
                ? "Folder"
                : "File";

            report.AppendLine($"{type}: {entry.FullName}");
            report.AppendLine($"  Compressed size: {entry.CompressedLength} bytes");
            report.AppendLine($"  Uncompressed size: {entry.Length} bytes");
            report.AppendLine();
        }

        if (entryCount == 0)
        {
            report.AppendLine("No entries found.");
        }

        return entryCount;
    }

    private static async Task AnalyzeUnknownOrUnsupportedAsync(
        string filePath,
        byte[] signature,
        StringBuilder report)
    {
        report.AppendLine("Raw signature information");
        report.AppendLine("-------------------------");
        report.AppendLine("First 64 bytes:");
        report.AppendLine(ToHex(signature.Take(64).ToArray()));
        report.AppendLine();
        report.AppendLine("Recognizable text strings near beginning of file:");

        var strings = await ReadLeadingStringsAsync(filePath);

        if (strings.Count == 0)
        {
            report.AppendLine("none");
        }
        else
        {
            foreach (var value in strings)
            {
                report.AppendLine(value);
            }
        }
    }

    private async Task<string> SaveReportAsync(string filePath, string report)
    {
        var reportFileName = $"vtz_analysis_{SanitizeFileName(Path.GetFileNameWithoutExtension(filePath))}_{DateTimeOffset.Now:yyyyMMddHHmmss}.txt";
        var reportPath = Path.Combine(_outputDirectory, reportFileName);

        await File.WriteAllTextAsync(reportPath, report);

        return reportPath;
    }

    private static void AppendHeader(StringBuilder report, string filePath, string format)
    {
        var fileInfo = new FileInfo(filePath);

        report.AppendLine("VTZ File Analysis");
        report.AppendLine("=================");
        report.AppendLine($"Generated: {DateTimeOffset.Now:O}");
        report.AppendLine($"File: {filePath}");
        report.AppendLine($"File name: {fileInfo.Name}");
        report.AppendLine($"File size: {fileInfo.Length} bytes");
        report.AppendLine($"Detected format: {format}");
        report.AppendLine();
    }

    private static async Task<List<string>> ReadLeadingStringsAsync(string filePath)
    {
        var buffer = new byte[4096];

        await using var stream = File.OpenRead(filePath);
        var bytesRead = await stream.ReadAsync(buffer);

        return ExtractPrintableStrings(buffer[..bytesRead]);
    }

    private static List<string> ExtractPrintableStrings(byte[] bytes)
    {
        var values = new List<string>();
        var current = new StringBuilder();

        foreach (var value in bytes)
        {
            if (value is >= 32 and <= 126)
            {
                current.Append((char)value);
                continue;
            }

            AddCurrentString(values, current);
        }

        AddCurrentString(values, current);

        return values;
    }

    private static void AddCurrentString(List<string> values, StringBuilder current)
    {
        if (current.Length >= 4)
        {
            values.Add(current.ToString());
        }

        current.Clear();
    }

    private static bool StartsWith(byte[] bytes, byte[] signature)
    {
        return bytes.Length >= signature.Length &&
            signature.Where((value, index) => bytes[index] == value).Count() == signature.Length;
    }

    private static bool HasTarSignature(byte[] signature)
    {
        if (signature.Length < 262)
        {
            return false;
        }

        var marker = Encoding.ASCII.GetString(signature, 257, 5);

        return marker == "ustar";
    }

    private static bool IsRecognizedArchive(string format)
    {
        return format is "ZIP archive" or "GZip archive" or "TAR archive" or "7z archive";
    }

    private static string ToHex(byte[] bytes)
    {
        return string.Join(' ', bytes.Select(value => value.ToString("X2")));
    }

    private static string SanitizeFileName(string value)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars();
        var sanitized = new string(value.Select(character => invalidCharacters.Contains(character) ? '_' : character).ToArray());

        return string.IsNullOrWhiteSpace(sanitized) ? "vtz" : sanitized;
    }
}
