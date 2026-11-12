namespace InstaFakeAnalyzer.Services.Interfaces;

public interface IClaimResearcher
{
    Task<GroundedResearchResult> ResearchAsync(string claim);
}

public sealed record GroundedResearchResult(string Summary, IReadOnlyList<ResearchSource> Sources);

public sealed record ResearchSource(string Title, string Url);
