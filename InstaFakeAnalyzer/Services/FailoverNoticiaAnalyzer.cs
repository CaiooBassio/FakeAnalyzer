using System.Net;
using System.Text.Json;
using InstaFakeAnalyzer.DTOs;
using InstaFakeAnalyzer.Services.Interfaces;

namespace InstaFakeAnalyzer.Services;

public sealed class FailoverNoticiaAnalyzer : INoticiaAnalyzer
{
    private readonly (string Name, INoticiaAnalyzer Analyzer)[] _providers;
    private readonly ILogger<FailoverNoticiaAnalyzer> _logger;

    public FailoverNoticiaAnalyzer(
        IEnumerable<(string Name, INoticiaAnalyzer Analyzer)> providers,
        ILogger<FailoverNoticiaAnalyzer> logger)
    {
        _providers = providers.ToArray();
        _logger = logger;

        if (_providers.Length == 0)
            throw new InvalidOperationException("Nenhum provedor de IA foi configurado.");
    }

    public async Task<AnaliseResultado> AnalisarAsync(string texto)
    {
        for (var index = 0; index < _providers.Length; index++)
        {
            var (name, analyzer) = _providers[index];
            try
            {
                var result = await analyzer.AnalisarAsync(texto);
                if (result == null || string.IsNullOrWhiteSpace(result.Text) || result.Fake == null)
                    throw new InvalidOperationException(
                        $"O provedor {name} retornou uma análise incompleta.");

                if (index > 0)
                    _logger.LogInformation("Análise concluída pelo provedor alternativo {Provider}.", name);

                return result;
            }
            catch (Exception ex) when (IsProviderFailure(ex))
            {
                if (index == _providers.Length - 1)
                {
                    _logger.LogError(ex, "Todos os provedores de IA configurados falharam.");
                    throw;
                }

                _logger.LogWarning(
                    "Provedor de IA {Provider} indisponível ({Reason}); tentando {FallbackProvider}.",
                    name,
                    ex.GetType().Name,
                    _providers[index + 1].Name);
            }
        }

        throw new InvalidOperationException("Nenhum provedor de IA conseguiu concluir a análise.");
    }

    private static bool IsProviderFailure(Exception exception) =>
        exception switch
        {
            HttpRequestException { StatusCode: null } => true,
            HttpRequestException { StatusCode: HttpStatusCode.TooManyRequests } => true,
            HttpRequestException { StatusCode: { } statusCode } =>
                (int)statusCode >= 500,
            TaskCanceledException => true,
            JsonException => true,
            _ => false
        };
}
