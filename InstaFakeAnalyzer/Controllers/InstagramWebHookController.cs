using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using InstaFakeAnalyzer.DTOs;
using InstaFakeAnalyzer.Models;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/webhook/instagram")]

public class InstagramWebhookController : ControllerBase
{
    private readonly HttpClient _client;
    private readonly IConfiguration _configuration;

    public InstagramWebhookController(IConfiguration configuration, HttpClient client)
    {
        _client = client;
        _configuration = configuration;

        var apiUrl = _configuration["ApiUrl"] ?? _configuration["Urls:ApiUrl"];
        if (!string.IsNullOrWhiteSpace(apiUrl)) _client.BaseAddress = new Uri(apiUrl);
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
                        await ProcessMessageAsync(
                            entry.Id,
                            messaging?.Sender?.Id,
                            messaging?.Message?.Text);

                if (entry.Changes != null)
                    foreach (var change in entry.Changes)
                        if (string.Equals(change?.Field, "messages", StringComparison.OrdinalIgnoreCase))
                            await ProcessMessageAsync(
                                entry.Id,
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

    private async Task ProcessMessageAsync(string? entryId, string? senderId, string? messageText)
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

        Console.WriteLine($"Mensagem recebida de {senderId}.");

        await EnviarRespostaInstagramAsync(senderId, "Sua notícia foi recebida e está sendo analisada...");

        var noticia = new Noticia { Conteudo = messageText };
        var response = await _client.PostAsJsonAsync("noticia", noticia);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Erro ao chamar API interna: {errorBody}");
            return;
        }

        var responseBody = await response.Content.ReadAsStringAsync();
        var resposta = JsonSerializer.Deserialize<Noticia>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (resposta == null || string.IsNullOrWhiteSpace(resposta.Justificativa))
        {
            Console.WriteLine("Resposta da API vazia ou inválida.");
            return;
        }

        if (await EnviarRespostaInstagramAsync(senderId, resposta.Justificativa))
            Console.WriteLine("Resposta da análise enviada.");
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

        using var client = new HttpClient();

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