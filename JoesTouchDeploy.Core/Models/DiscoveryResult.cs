namespace JoesTouchDeploy.Core.Models;

public class DiscoveryResult
{
    public Uri? FinalLandingUrl { get; set; }

    public List<Uri> VisitedUrls { get; } = [];

    public SortedSet<string> Links { get; } = new(StringComparer.OrdinalIgnoreCase);

    public List<HtmlFormInfo> Forms { get; } = [];

    public SortedSet<string> Scripts { get; } = new(StringComparer.OrdinalIgnoreCase);

    public SortedSet<string> Iframes { get; } = new(StringComparer.OrdinalIgnoreCase);

    public SortedSet<string> Resources { get; } = new(StringComparer.OrdinalIgnoreCase);

    public SortedSet<string> ConfigurationCandidates { get; } = new(StringComparer.OrdinalIgnoreCase);

    public List<EndpointProbeResult> TargetedProbes { get; } = [];
}
