using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;
using ToolPool.Client.Models;

namespace ToolPool.Client.Services

{
    public class AuthService
    {

        // checks if user is logged in
        public async Task<bool> AsyncCheckAuth(NavigationManager nav)
        {
            HttpClient http = new HttpClient 
            { 
                BaseAddress = new Uri(nav.BaseUri)
            };
            var authStatus = await http.GetFromJsonAsync<bool>("api/auth/status");
            return authStatus;
        }
        
    }
}
