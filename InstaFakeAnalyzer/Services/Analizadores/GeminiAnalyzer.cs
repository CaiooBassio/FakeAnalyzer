using System.Text;
using System.Text.Json;
using System.Net;
using InstaFakeAnalyzer.DTOs;
using InstaFakeAnalyzer.Services.Interfaces;

namespace InstaFakeAnalyzer.Services.Analyzers;

public class GeminiAnalyzer : INoticiaAnalyzer
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public GeminiAnalyzer(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<AnaliseResultado> AnalisarAsync(string texto)
    {
        var apiKey = _configuration["AI:Gemini:ApiKey"];
        var baseUrl = _configuration["AI:Gemini:BaseUrl"] ??
                      "https://generativelanguage.googleapis.com/v1beta";
        var model = _configuration["AI:Gemini:Model"];

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("A chave AI:Gemini:ApiKey não está configurada.");
        if (string.IsNullOrWhiteSpace(model))
            throw new InvalidOperationException("O modelo AI:Gemini:Model não está configurado.");

        const string instructions =
            "Analise a alegação fornecida em português. Não afirme ter consultado fontes externas. " +
            "Se o texto não trouxer evidências suficientes para confirmar que é falsa, use fake=false " +
            "e explique que a alegação não foi confirmada, sem apresentá-la como verdadeira. " +
            "Responda somente com um objeto JSON contendo text (string, justificativa em português) " +
            "e fake (boolean).";

        const int maxAttempts = 2;
        var maxOutputTokens = 1024;
        string? lastFailure = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var payload = new
            {
                systemInstruction = new
                {
                    parts = new[] { new { text = instructions } }
                },
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[] { new { text = texto } }
                    }
                },
                generationConfig = new
                {
                    responseMimeType = "application/json",
                    temperature = 0.0,
                    maxOutputTokens
                }
            };

            var (response, responseBody) = await SendWithTransientRetryAsync(
                $"{baseUrl.TrimEnd('/')}/models/{Uri.EscapeDataString(model)}:generateContent",
                apiKey,
                JsonSerializer.Serialize(payload));
            using (response)
            {

                if (!response.IsSuccessStatusCode)
                    throw new HttpRequestException(
                        $"Erro ao chamar Gemini (HTTP {(int)response.StatusCode}): {responseBody}",
                        null,
                        response.StatusCode);

                using var document = JsonDocument.Parse(responseBody);
                var root = document.RootElement;
                if (!root.TryGetProperty("candidates", out var candidates) ||
                    candidates.ValueKind != JsonValueKind.Array ||
                    candidates.GetArrayLength() == 0)
                {
                    var blockReason = root.TryGetProperty("promptFeedback", out var feedback) &&
                                      feedback.TryGetProperty("blockReason", out var reason)
                        ? reason.GetString()
                        : null;
                    throw new InvalidOperationException(
                        string.IsNullOrWhiteSpace(blockReason)
                            ? "Gemini não retornou candidatos de resposta."
                            : $"Gemini bloqueou a solicitação (motivo: {blockReason}).");
                }

                var candidate = candidates[0];
                var finishReason = candidate.TryGetProperty("finishReason", out var finish)
                    ? finish.GetString() ?? "desconhecido"
                    : "desconhecido";

                if (candidate.ValueKind != JsonValueKind.Object ||
                    !candidate.TryGetProperty("content", out var content) ||
                    content.ValueKind != JsonValueKind.Object ||
                    !content.TryGetProperty("parts", out var parts) ||
                    parts.ValueKind != JsonValueKind.Array)
                    throw new InvalidOperationException(
                        $"Gemini retornou uma resposta sem conteúdo de texto (finishReason: {finishReason}).");

                var responseText = string.Concat(parts.EnumerateArray()
                    .Where(part => part.TryGetProperty("text", out _))
                    .Select(part => part.GetProperty("text").GetString()));

                if (string.IsNullOrWhiteSpace(responseText))
                {
                    lastFailure = $"Gemini não retornou texto final (finishReason: {finishReason}).";
                }
                else
                {
                    try
                    {
                        using var resultDocument = JsonDocument.Parse(responseText);
                        var result = resultDocument.RootElement;
                        if (result.ValueKind == JsonValueKind.Object &&
                            result.TryGetProperty("text", out var explanation) &&
                            explanation.ValueKind == JsonValueKind.String &&
                            result.TryGetProperty("fake", out var fake) &&
                            fake.ValueKind is JsonValueKind.True or JsonValueKind.False)
                        {
                            return new AnaliseResultado
                            {
                                Text = explanation.GetString(),
                                Fake = fake.GetBoolean()
                            };
                        }

                        lastFailure =
                            $"JSON do Gemini sem os campos esperados (finishReason: {finishReason}).";
                    }
                    catch (JsonException ex)
                    {
                        lastFailure =
                            $"JSON inválido do Gemini (finishReason: {finishReason}, tamanho: {responseText.Length} caracteres): {ex.Message}";
                    }
                }
            }

            if (attempt < maxAttempts)
            {
                maxOutputTokens *= 2;
                continue;
            }
        }

        throw new InvalidOperationException(
            $"Não foi possível obter uma análise JSON válida após {maxAttempts} tentativas. {lastFailure}");
    }

    private async Task<(HttpResponseMessage Response, string Body)> SendWithTransientRetryAsync(
        string requestUrl,
        string apiKey,
        string requestBody)
    {
        const int maxAttempts = 2;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
            {
                Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("x-goog-api-key", apiKey);

            var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (attempt == maxAttempts || !IsTransient(response.StatusCode))
                return (response, responseBody);

            var delay = GetRetryDelay(response, attempt);
            Console.WriteLine(
                $"Gemini respondeu HTTP {(int)response.StatusCode}; nova tentativa em {delay.TotalSeconds:0.#}s.");
            response.Dispose();
            await Task.Delay(delay);
        }

        throw new InvalidOperationException("A chamada ao Gemini terminou sem resposta.");
    }

    private static bool IsTransient(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.TooManyRequests or
            HttpStatusCode.InternalServerError or
            HttpStatusCode.BadGateway or
            HttpStatusCode.ServiceUnavailable or
            HttpStatusCode.GatewayTimeout;

    private static TimeSpan GetRetryDelay(HttpResponseMessage response, int attempt)
    {
        var retryAfter = response.Headers.RetryAfter;
        var requestedDelay = retryAfter?.Delta ??
                             (retryAfter?.Date is { } retryDate
                                 ? retryDate - DateTimeOffset.UtcNow
                                 : TimeSpan.FromSeconds(attempt));

        return TimeSpan.FromMilliseconds(Math.Clamp(requestedDelay.TotalMilliseconds, 250, 5000));
    }
}
