using InstaFakeAnalyzerClient.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace InstaFakeAnalyzerClient.ApiServices
{
    public class UsuarioService(HttpClient client)
    {
        public readonly HttpClient _httpClient = client;

        public async Task<Usuario?> ObterUsuarioLogadoAsync()
        {
            Usuario? usuario = await _httpClient.GetFromJsonAsync<Usuario>("usuario");
            if (usuario != null)
            {
                return usuario;
            }
            else
            {
                throw new HttpRequestException("Erro ao obter usuário logado. Login já foi efetuado?");
            }
        }

        public async Task CadastrarUsuario(Usuario usuario)
        {
            try
            {
                await _httpClient.PostAsJsonAsync("usuario/cadastrar", usuario);
            }
            catch(Exception ex)
            {
                throw new HttpRequestException("Erro ao cadastrar usuário. Verifique os dados e tente novamente.", ex);
            }
        }
    }
}
