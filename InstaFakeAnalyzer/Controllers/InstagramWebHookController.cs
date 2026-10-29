using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using InstaFakeAnalyzer.DTOs;
using InstaFakeAnalyzer.Models;
using InstaFakeAnalyzer.Services;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/webhook/instagram")]

public class InstagramWebhookController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly InstagramMessageDeduplicator _messageDeduplicator;
    private readonly InstagramMessageQueue _messageQueue;
    private readonly IHttpClientFactory _httpClientFactory;

    public InstagramWebhookController(
        IConfiguration configuration,
        InstagramMessageDeduplicator messageDeduplicator,
        InstagramMessageQueue messageQueue,
        IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _messageDeduplicator = messageDeduplicator;
        _messageQueue = messageQueue;
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet]
    public IActionResult Get([FromQuery(Name = "hub.mode")] string? mode,
        [FromQuery(Name = "hub.verify_token")] string? token,
        [FromQuery(Name = "hub.challenge")] string? challenge)
    {
        var verifyToken = _configuration["Facebook:VerifyToken"];
        if (!string.IsNullOrWhiteSpace(verifyToken) &&
            mode == "subscribe" &&
            token == verifyToken &&
            !string.IsNullOrWhiteSpace(challenge))
            return Content(challenge, "text/plain");

        return Unauthorized();
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] InstagramWebhookDTOs body)
    {
        var entries = body?.Entry;
        if (entries == null || entries.Count == 0)
            return Ok();

        try
        {
            foreach (var entry in entries)
            {
                if (entry == null)
                    continue;

                if (entry.Messaging != null)
                    foreach (var messaging in entry.Messaging)
                        await ProcessMessageOnceAsync(
                            entry.Id,
                            messaging?.Message?.Mid,
                            messaging?.Sender?.Id,
                            messaging?.Message?.Text);

                if (entry.Changes != null)
                    foreach (var change in entry.Changes)
                        if (string.Equals(change?.Field, "messages", StringComparison.OrdinalIgnoreCase))
                            await ProcessMessageOnceAsync(
                                entry.Id,
                                change?.Value?.Message?.Mid,
                                change?.Value?.Sender?.Id,
                                change?.Value?.Message?.Text);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao processar a mensagem: {ex.Message}");
            return BadRequest(new { erro = ex.Message });
        }

        return Ok();
    }

    private async Task ProcessMessageOnceAsync(
        string? entryId,
        string? messageId,
        string? senderId,
        string? messageText)
    {
        if (string.IsNullOrWhiteSpace(senderId))
            return;

        if (entryId == senderId)
        {
            Console.WriteLine("Mensagem enviada pela própria conta; ignorada.");
            return;
        }

        if (string.IsNullOrWhiteSpace(messageText))
            return;

        if (string.IsNullOrWhiteSpace(messageId))
        {
            Console.WriteLine("Evento sem ID de mensagem; não é possível deduplicá-lo.");
            await QueueMessageAsync(null, senderId, messageText);
            return;
        }

        var deduplicationKey = $"{entryId ?? "unknown"}:{messageId}";
        if (!_messageDeduplicator.TryBegin(deduplicationKey))
        {
            Console.WriteLine($"Evento duplicado {messageId}; ignorado.");
            return;
        }

        try
        {
            await QueueMessageAsync(deduplicationKey, senderId, messageText);
        }
        catch
        {
            _messageDeduplicator.Release(deduplicationKey);
            throw;
        }
    }

    private async Task QueueMessageAsync(string? deduplicationKey, string senderId, string messageText)
    {
        Console.WriteLine($"Mensagem recebida de {senderId}.");

        await EnviarRespostaInstagramAsync(senderId, "Sua notícia foi recebida e está sendo analisada...");

        if (!_messageQueue.TryEnqueue(new InstagramMessageJob(deduplicationKey, senderId, messageText)))
        {
            if (deduplicationKey != null)
                _messageDeduplicator.Release(deduplicationKey);

            Console.WriteLine("Fila de análise cheia; mensagem não foi enfileirada.");
            await EnviarRespostaInstagramAsync(
                senderId,
                "O serviço está ocupado no momento. Envie sua mensagem novamente daqui a pouco.");
        }
    }

    public async Task<bool> EnviarRespostaInstagramAsync(string senderId, string resposta)
    {
        var rawToken = _configuration["Facebook:AccessToken"];

        if (string.IsNullOrWhiteSpace(rawToken))
        {
            Console.WriteLine("Token do Facebook não configurado.");
            return false;
        }

        var pageAccessToken = rawToken.Replace("\"", "").Replace("'", "").Trim();

        using var client = _httpClientFactory.CreateClient("InstagramGraphApi");

        var jsonBody = new
        {
            recipient = new { id = senderId },
            message = new { text = resposta }
        };

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://graph.facebook.com/v26.0/me/messages?platform=instagram")

        {
            Content = new StringContent(
                JsonSerializer.Serialize(jsonBody),
                Encoding.UTF8,
                "application/json")
        };

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", pageAccessToken);

        var response = await client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var erro = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Erro ao enviar mensagem ({(int)response.StatusCode}): {erro}");
            return false;
        }

        return true;
    }
}