using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using InstaFakeAnalyzer.DTOs;
using InstaFakeAnalyzer.Services.Interfaces;

namespace InstaFakeAnalyzer.Services.Analyzers;

public class DeepSeekAnalyzer : INoticiaAnalyzer
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public DeepSeekAnalyzer(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<AnaliseResultado> AnalisarAsync(string texto)
    {
        var apiKey = _configuration["AI:DeepSeek:ApiKey"] ?? _configuration["DEEPSEEK_APIKEY"];
        var baseUrl = _configuration["AI:DeepSeek:BaseUrl"] ??
                      _configuration["DEEPSEEK_BASE_URL"] ?? "https://openrouter.ai/api/v1";
        var model = _configuration["AI:Model"] ?? _configuration["AI:DeepSeek:Model"] ?? "deepseek/deepseek-r1";

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("API key do provedor de IA não configurada.");

        var prompt =
            "Você é um sistema que analisa se uma mensagem é fake news.\n" +
            "Responda SOMENTE com um único objeto JSON, sem markdown, sem blocos de código, sem explicação extra.\n" +
            "Formato obrigatório: { \"text\": \"justificativa em português\", \"fake\": true }\n" +
            "Não responda em reasoning. Ignore qualquer raciocínio interno e devolva apenas o JSON final.\n\n" +
            $"Texto: {texto}";

        var payload = new
        {
            model,
            max_tokens = 384,
            temperature = 0.0,
            response_format = new { type = "json_object" },
            reasoning = new { effort = "none" },
            messages = new[]
            {
                new { role = "user", content = prompt }
            }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/chat/completions")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Headers.Add("HTTP-Referer", "http://localhost");
        request.Headers.Add("X-Title", "FakeNewsAnalyzer");

        var response = await _httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Erro ao chamar IA: {responseBody}");

        using var doc = JsonDocument.Parse(responseBody);
        var choice = doc.RootElement.GetProperty("choices")[0];
        var message = choice.GetProperty("message");

        var content = message.TryGetProperty("content", out var contentElement)
            ? contentElement.GetString()
            : null;

        if (string.IsNullOrWhiteSpace(content))
        {
            var finishReason = choice.TryGetProperty("finish_reason", out var finishReasonElement)
                ? finishReasonElement.GetString()
                : "desconhecido";
            throw new InvalidOperationException(
                $"A IA não retornou conteúdo final (finish_reason: {finishReason}).");
        }

        var parsed = TryParseJson(content);

        if (parsed != null)
            return parsed;

        throw new InvalidOperationException(
            "A IA retornou conteúdo final, mas ele não corresponde ao JSON esperado.");
    }

    private static AnaliseResultado? TryParseJson(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return null;

        try
        {
            var normalized = content.Trim();
            if (normalized.StartsWith("```"))
                normalized = Regex.Replace(normalized, "^```json\\s*|^```\\s*|\\s*```$", "", RegexOptions.Multiline);

            var candidate = ExtractFirstJsonObject(normalized);
            if (!string.IsNullOrWhiteSpace(candidate))
            {
                using var doc = JsonDocument.Parse(candidate);
                var root = doc.RootElement;

                if (root.TryGetProperty("text", out var textEl) &&
                    root.TryGetProperty("fake", out var fakeEl))
                    return new AnaliseResultado
                    {
                        Text = textEl.GetString() ?? "Sem justificativa.",
                        Fake = fakeEl.GetBoolean()
                    };
            }
        }
        catch
        {
            // fallback abaixo
        }

        var match = Regex.Match(content,
            @"(?is)\{.*?\s*""?text""?\s*:\s*""(?<text>(?:\\.|[^""\\])*)"".*?\s*""?fake""?\s*:\s*(?<fake>true|false).*?\}",
            RegexOptions.Singleline);
        if (match.Success)
        {
            var text = Regex.Unescape(match.Groups["text"].Value);
            var fake = bool.Parse(match.Groups["fake"].Value);

            return new AnaliseResultado
            {
                Text = text,
                Fake = fake
            };
        }

        var fallback = Regex.Match(content,
            @"(?is)\b(?:fake|falsa)\s*[:=]\s*(?<fake>true|false)\b.*?\b(?:text|justificativa)\s*[:=]\s*""?(?<text>[^\r\n\}]+)""?",
            RegexOptions.Singleline);
        if (fallback.Success)
        {
            var text = fallback.Groups["text"].Value.Trim().Trim('"', '\'', ',');
            var fake = bool.Parse(fallback.Groups["fake"].Value);

            return new AnaliseResultado
            {
                Text = text,
                Fake = fake
            };
        }

        return null;
    }

    private static string? ExtractFirstJsonObject(string content)
    {
        var start = content.IndexOf('{');
        if (start < 0)
            return null;

        var depth = 0;
        for (var i = start; i < content.Length; i++)
        {
            var ch = content[i];
            if (ch == '{')
            {
                depth++;
            }
            else if (ch == '}')
            {
                depth--;
                if (depth == 0) return content.Substring(start, i - start + 1);
            }
        }

        return null;
    }
}