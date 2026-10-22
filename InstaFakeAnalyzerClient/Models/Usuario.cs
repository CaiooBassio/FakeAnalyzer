namespace InstaFakeAnalyzerClient.Models
{
    public class Usuario
    {
        public enum TipoUsuario
        {
            Administrador = 1,
            Verificador = 2,
            Comum = 3
        }

        public int Id { get; set; }
        public string Nome { get; set; }
        public string NomeUsuario { get; set; }
        public string Senha { get; set; }
        public DateTime DataCadastro { get; set; }
        public TipoUsuario tipoUsuario { get; set; }
        public bool? snAtivo { get; set; }
    }
}
