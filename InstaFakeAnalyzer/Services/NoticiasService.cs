using InstaFakeAnalyzer.Models;
using InstaFakeAnalyzer.Services.Interfaces;

namespace InstaFakeAnalyzer.Services;

public class NoticiasService
{
    private readonly INoticiaAnalyzer _noticiaAnalyzer;

    public NoticiasService(INoticiaAnalyzer noticiaAnalyzer)
    {
        _noticiaAnalyzer = noticiaAnalyzer;
    }

    public async Task<Noticia> ProcessaNoticia(Noticia noticia)
    {
        if (noticia == null || string.IsNullOrWhiteSpace(noticia.Conteudo))
            throw new ArgumentException("Conteúdo da notícia não pode ser vazio.");

        var analise = await _noticiaAnalyzer.AnalisarAsync(noticia.Conteudo);

        return new Noticia
        {
            Conteudo = noticia.Conteudo,
            Justificativa = analise.Text ?? "Sem justificativa.",
            snFalsa = analise.Fake,
            snAnalizada = true,
            DataCadastro = DateTime.UtcNow
        };
    }
}