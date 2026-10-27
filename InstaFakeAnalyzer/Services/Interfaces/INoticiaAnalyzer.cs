using InstaFakeAnalyzer.DTOs;

namespace InstaFakeAnalyzer.Services.Interfaces;

public interface INoticiaAnalyzer
{
    Task<AnaliseResultado> AnalisarAsync(string texto);
}