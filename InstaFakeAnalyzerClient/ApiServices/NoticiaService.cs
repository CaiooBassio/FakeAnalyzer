using InstaFakeAnalyzerClient.Models;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;


namespace InstaFakeAnalyzerClient.ApiServices
{
    public class NoticiaService
    {
        private readonly HttpClient _httpClient;
        private string api = "noticia";

        public NoticiaService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<Noticia> EnviarNoticiaAsync(Noticia noticia)
        {
            var response = await _httpClient.PostAsJsonAsync(api, noticia);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<Noticia>();
                return result;
            }
            return null;
        }

        public async Task<bool> EnviarNoticiaDiretamenteAsync(Noticia noticia)
        {
            var response = await _httpClient.PostAsJsonAsync($"{api}/direto", noticia);
            return response.IsSuccessStatusCode;
               
        }

        public async Task<Noticia> ObterNoticiaAsync(int id)
        {
            var response = await _httpClient.GetAsync($"{api}/{id}");
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<Noticia>();
                return result;
            }
            return null;
        }

        public async Task<List<Noticia>> ObterNoticiasAsync()
        {
            var response = await _httpClient.GetAsync(api);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<List<Noticia>>();
                return result;
            }
            return null;
        }

        public async Task<List<Noticia>> ObterNoticiasParaVerificarAsync()
        {
            var response = await _httpClient.GetAsync($"{api}/verificar");
            if(response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<List<Noticia>>();
                return result;
            }
            return null;
        }

        public async Task<Noticia> AtualizarNoticiaAsync(Noticia noticia)
        {
            var response = await _httpClient.PutAsJsonAsync(api, noticia);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<Noticia>();
                return result;
            }
            return null;
        }
    }
}

