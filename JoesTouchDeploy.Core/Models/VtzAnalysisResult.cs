namespace JoesTouchDeploy.Core.Models;

public class VtzAnalysisResult
{
    public string FilePath { get; set; } = string.Empty;

    public string DetectedFormat { get; set; } = string.Empty;

    public bool IsRecognizedArchive { get; set; }

    public int EntryCount { get; set; }

    public string ReportPath { get; set; } = string.Empty;
}
