using InstaFakeAnalyzer.Models;

namespace InstaFakeAnalyzer.Utils
{
    public class Sessao
    {
        public static Usuario usuario;

        public static void Iniciar(Usuario usuarioLogado)
        {
            usuario = usuarioLogado;
        }

        public static Usuario ObterUsuarioLogado()
        {
            return usuario;
        }

        public static void Encerrar()
        {
            usuario = null;
        }
    }
}
