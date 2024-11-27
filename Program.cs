
using Zeebe.Client;
using DemoCamunda.Config;
using Microsoft.Extensions.Options;
using DemoCamunda.Services;
using DemoCamunda.Interfaces;
using DemoCamunda.Workers;

namespace DemoCamunda
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            // Bind ZeebeOptions from appsettings.json
            builder.Services.Configure<ZeebeOptions>(builder.Configuration.GetSection("Zeebe"));
            builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

            // Register Zeebe client
            builder.Services.AddSingleton(serviceProvider =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<ZeebeOptions>>().Value;

                // Fetch OAuth token manually
                var accessToken = GetOAuthToken(options.ClientId, options.ClientSecret).Result;

                return ZeebeClient.Builder()
                    .UseGatewayAddress($"{options.ClusterId}.{options.Region}.zeebe.camunda.io:443")
                    .UseTransportEncryption() // Use TLS encryption for secure communication
                    .UseAccessToken(accessToken) // Set the access token for authentication
                    .Build();
            });

            builder.Services.AddTransient<IEmailSender, EmailSender>();
            // Đăng ký Worker Service
            builder.Services.AddHostedService<ZeebeWorkerService>();
            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }

        // Method to retrieve an OAuth token
        static async Task<string> GetOAuthToken(string clientId, string clientSecret)
        {
            using var httpClient = new HttpClient();
            var tokenEndpoint = "https://login.cloud.camunda.io/oauth/token";

            var requestBody = new Dictionary<string, string>
            {
                { "grant_type", "client_credentials" },
                { "client_id", clientId },
                { "client_secret", clientSecret },
                { "audience", "zeebe.camunda.io" }
            };

            var response = await httpClient.PostAsync(tokenEndpoint, new FormUrlEncodedContent(requestBody));
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var tokenResponse = System.Text.Json.JsonSerializer.Deserialize<TokenResponse>(content)
                                ?? throw new InvalidOperationException("Invalid OAuth response");

            return tokenResponse.access_token;
        }
        // Class to deserialize the OAuth token response
        public class TokenResponse
        {
            public string access_token { get; set; } = default!;
            public string token_type { get; set; } = default!;
            public int expires_in { get; set; }
        }
    }
}
