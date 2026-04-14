using System.Net.Http.Json;
using ToolPool.Client.Models;

namespace ToolPool.Client.Services

{
    public class AuthService
    {
        // HttpClient for API calls
        private readonly HttpClient _http;

        public AuthService(HttpClient http)
        {
            _http = http; 
        }

        // checks if user is logged in
        public async Task<bool> AsyncCheckAuth()
        {
            var authStatus = await _http.GetFromJsonAsync<bool>("api/auth/status");
            return authStatus;
        }
        
    }
}
