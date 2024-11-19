using DemoCamunda.Interfaces;
using System.Net.Http.Headers;

namespace DemoCamunda.Services
{
    public class CamundaService : ICamundaService
    {
        private readonly HttpClient _httpClient;
        private readonly ITokenService _tokenService;

        public CamundaService(HttpClient httpClient, ITokenService tokenService)
        {
            _httpClient = httpClient;
            _tokenService = tokenService;
        }

        private async Task SetAuthorizationHeaderAsync()
        {
            var token = await _tokenService.GetAccessTokenAsync();
            _httpClient.BaseAddress = new Uri("https://8be965f4-d048-42b8-88e4-516cc1ff2bdf.sin-1.zeebe.camunda.io");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        public async Task<object> StartProcessAsync(string processKey, object variables)
        {
            await SetAuthorizationHeaderAsync();

            var response = await _httpClient.PostAsJsonAsync(
                $"/process-definition/key/{processKey}/start",
                new { variables }
            );

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<object>();
        }

        public async Task CompleteTaskAsync(string taskId, bool approved)
        {
            var payload = new
            {
                variables = new
                {
                    approved = new { value = approved, type = "Boolean" }
                }
            };

            var response = await _httpClient.PostAsJsonAsync($"/task/{taskId}/complete", payload);

            response.EnsureSuccessStatusCode();
        }

    }
}
