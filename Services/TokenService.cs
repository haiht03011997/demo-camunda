using DemoCamunda.Config;
using DemoCamunda.Interfaces;
using Google.Apis.Auth.OAuth2.Responses;
using Microsoft.Extensions.Options;

namespace DemoCamunda.Services
{
    public class TokenService : ITokenService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private string _accessToken;

        public TokenService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<string> GetAccessTokenAsync()
        {
            if (!string.IsNullOrEmpty(_accessToken)) return _accessToken;

            var payload = new
            {
                grant_type = "client_credentials",
                client_id = _configuration["Camunda:ClientId"],
                client_secret = _configuration["Camunda:ClientSecret"],
                audience = _configuration["Camunda:Audience"]
            };

            var response = await _httpClient.PostAsJsonAsync("https://login.cloud.camunda.io/oauth/token", payload);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<TokenResponse>();
            _accessToken = result.access_token;
            return _accessToken;
        }
    }

    public class TokenResponse
    {
        public string access_token { get; set; }
    }

}
