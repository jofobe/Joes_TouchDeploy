namespace JoesTouchDeploy.Core.Models;

public class HtmlDiscoveryResult
{
    public List<string> Links { get; } = [];

    public List<HtmlFormInfo> Forms { get; } = [];

    public List<string> Scripts { get; } = [];

    public List<string> Iframes { get; } = [];

    public List<string> Resources { get; } = [];
}
