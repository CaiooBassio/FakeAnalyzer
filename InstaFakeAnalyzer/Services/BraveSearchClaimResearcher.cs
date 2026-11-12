using System.Text.Json;
using InstaFakeAnalyzer.Services.Interfaces;

namespace InstaFakeAnalyzer.Services;

public sealed class BraveSearchClaimResearcher : IClaimResearcher
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public BraveSearchClaimResearcher(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<GroundedResearchResult> ResearchAsync(string claim)
    {
        var apiKey = _configuration["AI:Research:ApiKey"];
        var baseUrl = _configuration["AI:Research:BaseUrl"] ??
                      "https://api.search.brave.com/res/v1";
        var country = _configuration["AI:Research:Country"] ?? "BR";
        var language = _configuration["AI:Research:Language"] ?? "pt-br";

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("A chave AI:Research:ApiKey não está configurada.");

        var requestUri =
            $"{baseUrl.TrimEnd('/')}/web/search" +
            $"?q={Uri.EscapeDataString(claim)}" +
            $"&count=5&country={Uri.EscapeDataString(country)}" +
            $"&search_lang={Uri.EscapeDataString(language)}&safesearch=moderate";

        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Add("X-Subscription-Token", apiKey);
        request.Headers.Accept.ParseAdd("application/json");

        using var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"Pesquisa externa Brave falhou (HTTP {(int)response.StatusCode}).",
                null,
                response.StatusCode);

        await using var responseStream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(responseStream);
        if (!document.RootElement.TryGetProperty("web", out var web) ||
            !web.TryGetProperty("results", out var results) ||
            results.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("A pesquisa externa não retornou resultados web.");

        var resultItems = results.EnumerateArray()
            .Select(result => new
            {
                Title = GetString(result, "title"),
                Url = GetString(result, "url"),
                Description = GetString(result, "description")
            })
            .Where(result =>
                !string.IsNullOrWhiteSpace(result.Title) &&
                !string.IsNullOrWhiteSpace(result.Description) &&
                Uri.TryCreate(result.Url, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp))
            .Take(5)
            .ToArray();

        if (resultItems.Length == 0)
            throw new InvalidOperationException("A pesquisa externa não retornou fontes utilizáveis.");

        var sources = resultItems
            .Select(result => new ResearchSource(result.Title!, result.Url!))
            .ToArray();
        var summary = string.Join(
            Environment.NewLine,
            resultItems.Select(result => $"- {result.Title}: {result.Description}"));

        return new GroundedResearchResult(summary, sources);
    }

    private static string? GetString(JsonElement element, string propertyName) =>
        element.ValueKind == JsonValueKind.Object &&
        element.TryGetProperty(propertyName, out var value) &&
        value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
