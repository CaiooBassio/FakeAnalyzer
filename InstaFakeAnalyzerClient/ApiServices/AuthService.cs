using InstaFakeAnalyzerClient.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace InstaFakeAnalyzerClient.ApiServices
{
    public class AuthService
    {
        public readonly HttpClient _httpClient;
        public AuthService(HttpClient client) 
        {
            _httpClient = client;
        }

        public async Task<bool> FazerLoginAsync(Usuario usuario)
        {
            var api = "usuario/login";
            var response = await _httpClient.PostAsJsonAsync(api, usuario);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> FazerLogoutAsync()
        {
            var response = await _httpClient.PostAsync("/usuario/logout", null);
            return response.IsSuccessStatusCode;
        }

    }
}
