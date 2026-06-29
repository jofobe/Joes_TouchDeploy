namespace JoesTouchDeploy.Core.Models;

public class VtzProjectSummaryResult
{
    public string FilePath { get; set; } = string.Empty;

    public string ReportPath { get; set; } = string.Empty;

    public bool EnvironmentXmlFound { get; set; }

    public bool SmartGraphicsXmlFound { get; set; }

    public bool VtxFileFound { get; set; }

    public int FilesRead { get; set; }
}
