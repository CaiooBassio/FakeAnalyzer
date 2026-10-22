using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using InstaFakeAnalyzer.DTOs;
using InstaFakeAnalyzer.Models;
using Microsoft.Extensions.Configuration;

namespace InstaFakeAnalyzer.Services
{
    public class DeepSeekService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public DeepSeekService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<DeepSeekResponse> AnalisarNoticiaAsync(string prompt)
        {
            var apiKey = _configuration["DEEPSEEK_APIKEY"];
            var baseUrl = _configuration["DEEPSEEK_BASE_URL"];

#if DEBUG
            apiKey = _configuration["DeepSeek:ApiKey"];
            baseUrl = _configuration["DeepSeek:BaseUrl"];
#endif
            if (string.IsNullOrEmpty(apiKey))
                throw new InvalidOperationException("API Key do DeepSeek não configurada.");

            var requestBody = new
            {
                model = "deepseek/deepseek-r1-zero:free",
                messages = new[]
                {
                    new { role = "user", content = prompt }
                }
            };

            var requestJson = JsonSerializer.Serialize(requestBody);

            var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/chat/completions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Headers.Add("HTTP-Referer", "http://localhost"); // ajuste se necessário
            request.Headers.Add("X-Title", "FakeNewsAnalyzer");
            request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Erro ao chamar DeepSeek: {error}");
                }

                var responseBody = await response.Content.ReadAsStringAsync();
            

                using var document = JsonDocument.Parse(responseBody);

                var contentRaw = document.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                if (string.IsNullOrWhiteSpace(contentRaw))
                    throw new Exception("Conteúdo da resposta da IA está vazio.");

                var jsonMatch = Regex.Match(contentRaw!, @"\{[^{}]*""text""\s*:\s*""[^""]+""[^{}]*""fake""\s*:\s*(true|false)[^{}]*\}", RegexOptions.Singleline);

                if (!jsonMatch.Success)
                {
                    throw new Exception("Não foi possível extrair JSON da resposta da IA.");
                }

                var jsonClean = jsonMatch.Value;

                try
                {
                    var resultado = JsonSerializer.Deserialize<DeepSeekResponse>(jsonClean);
                    if (resultado == null)
                        throw new Exception("Falha ao desserializar JSON extraído.");
                    Console.WriteLine(jsonClean);
                    return resultado;
                }
                catch (JsonException ex)
                {
                    throw new Exception($"Erro ao desserializar JSON extraído: {ex.Message}\nConteúdo extraído: {jsonClean}");
                }
                finally{
                    Console.WriteLine("Tratada");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception" + ex);
            }
            finally{
                Console.WriteLine("Exception tratada");
            }
            return new DeepSeekResponse() { text = null, fake = null };
        }
        
    }
}
