using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using InstaFakeAnalyzer.Models;
using Microsoft.Extensions.Hosting;

namespace InstaFakeAnalyzer.Services;

public sealed record InstagramMessageJob(string? DeduplicationKey, string SenderId, string Text);

public sealed class InstagramMessageQueue : BackgroundService
{
    private readonly Channel<InstagramMessageJob> _channel =
        Channel.CreateBounded<InstagramMessageJob>(new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly InstagramMessageDeduplicator _deduplicator;

    public InstagramMessageQueue(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        InstagramMessageDeduplicator deduplicator)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _deduplicator = deduplicator;
    }

    public bool TryEnqueue(InstagramMessageJob job) => _channel.Writer.TryWrite(job);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var job in _channel.Reader.ReadAllAsync(stoppingToken))
            await ProcessMessageAsync(job, stoppingToken);
    }

    private async Task ProcessMessageAsync(InstagramMessageJob job, CancellationToken stoppingToken)
    {
        try
        {
            var apiClient = _httpClientFactory.CreateClient("InstagramInternalApi");
            using var response = await apiClient.PostAsJsonAsync(
                "noticia",
                new Noticia { Conteudo = job.Text },
                stoppingToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(stoppingToken);
                Console.WriteLine($"Erro ao chamar API interna: {(int)response.StatusCode} {errorBody}");
                await CompleteWithFailureMessageAsync(job);
                return;
            }

            var result = await response.Content.ReadFromJsonAsync<Noticia>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                stoppingToken);

            if (result == null || string.IsNullOrWhiteSpace(result.Justificativa))
            {
                Console.WriteLine("Resposta da API interna vazia ou inválida.");
                await CompleteWithFailureMessageAsync(job);
                return;
            }

            if (await SendInstagramMessageAsync(job.SenderId, result.Justificativa))
            {
                Console.WriteLine("Resposta da análise enviada.");
                MarkCompleted(job);
            }
            else
            {
                Release(job);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            Release(job);
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Falha no processamento em segundo plano: {ex.Message}");
            await CompleteWithFailureMessageAsync(job);
        }
    }

    private async Task CompleteWithFailureMessageAsync(InstagramMessageJob job)
    {
        const string message =
            "Não foi possível concluir a análise agora. O serviço de IA pode estar temporariamente ocupado. Tente novamente mais tarde.";

        if (await SendInstagramMessageAsync(job.SenderId, message))
            MarkCompleted(job);
        else
            Release(job);
    }

    private async Task<bool> SendInstagramMessageAsync(string senderId, string text)
    {
        var rawToken = _configuration["Facebook:AccessToken"];
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            Console.WriteLine("Token do Facebook não configurado.");
            return false;
        }

        var body = new
        {
            recipient = new { id = senderId },
            message = new { text }
        };

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://graph.facebook.com/v26.0/me/messages?platform=instagram")
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer",
            rawToken.Replace("\"", "").Replace("'", "").Trim());

        using var client = _httpClientFactory.CreateClient("InstagramGraphApi");
        using var response = await client.SendAsync(request);
        if (response.IsSuccessStatusCode)
            return true;

        var error = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Erro ao enviar mensagem ({(int)response.StatusCode}): {error}");
        return false;
    }

    private void MarkCompleted(InstagramMessageJob job)
    {
        if (job.DeduplicationKey != null)
            _deduplicator.MarkCompleted(job.DeduplicationKey);
    }

    private void Release(InstagramMessageJob job)
    {
        if (job.DeduplicationKey != null)
            _deduplicator.Release(job.DeduplicationKey);
    }
}
