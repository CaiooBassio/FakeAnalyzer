namespace InstaFakeAnalyzer.Models
{
    public class Noticia
    {
        public int? Id { get; set; }
        public string? Conteudo { get; set; }
        public string? Justificativa { get; set; }
        public bool? snFalsa { get; set; }
        public bool? snAnalizada { get; set; }
        public DateTime? DataCadastro { get; set; }
    }
}