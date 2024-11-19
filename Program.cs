
using Zeebe.Client;
using DemoCamunda.Config;
using DemoCamunda.Interfaces;
using DemoCamunda.Services;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace DemoCamunda
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            // Cấu hình dịch vụ
            builder.Services.Configure<CamundaConfig>(builder.Configuration.GetSection("Camunda"));
            builder.Services.AddHttpClient<ITokenService, TokenService>();

            builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
            builder.Services.AddTransient<IEmailSender, EmailSender>();
            builder.Services.AddTransient<ICamundaService, CamundaService>();

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
    }
}
