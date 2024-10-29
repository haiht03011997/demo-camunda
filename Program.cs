
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

            // Add services to the container.

            // Configure Zeebe Client for local Docker Zeebe instance
            builder.Services.AddSingleton<IZeebeClient>(_ =>
            {
                return ZeebeClient.Builder()
                    .UseGatewayAddress(builder.Configuration["Zeebe:GatewayAddress"])
                    .UsePlainText()  // Use PlainText because TLS is not needed for local development
                    .Build();
            });

            // Cấu hình dịch vụ gửi email
            builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
            builder.Services.AddHostedService<ZeebeWorkerService>();
            builder.Services.AddTransient<IEmailSender, EmailSender>();

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // config worker
            var zeebeClient = app.Services.GetRequiredService<IZeebeClient>();
            var emailSender = app.Services.GetRequiredService<IEmailSender>();

            // Worker xử lý nhiệm vụ gửi email phê duyệt
            var workerSendApprovalRequestEmail = zeebeClient.NewWorker()
            .JobType("send-approval-request-email")
            .Handler(async (jobClient, job) =>
            {
                try
                {
                    var variables = job.Variables;
                    var deserializedVariables = JsonConvert.DeserializeObject<Dictionary<string, object>>(variables);
                    var email = deserializedVariables["approverEmail"].ToString().Trim();
                    var documentName = deserializedVariables["documentName"].ToString();

                    if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                    {
                        Console.WriteLine("Invalid email format: " + email);
                        return;
                    }

                    var emailSender = app.Services.GetRequiredService<IEmailSender>();
                    var body = $"Bạn có một tài liệu cần phê duyệt: {documentName}.";
                    //await emailSender.SendEmailAsync(email, "Yêu cầu phê duyệt tài liệu", body);

                    //await jobClient.NewCompleteJobCommand(job.Key).Send();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing job: {ex.Message}");
                    // Consider failing the job here if necessary
                }
            })
            .MaxJobsActive(5)
            .Name("send-email-request-worker")
            .PollInterval(TimeSpan.FromSeconds(1))
            .Timeout(TimeSpan.FromSeconds(60))
            .Open();

            var worker = zeebeClient.NewWorker()
                .JobType("call-api-approve")
                .Handler(async (jobClient, job) =>
                {
                    // Retrieve information from job
                    var variables = job.Variables;
                    var deserializedVariables = JsonConvert.DeserializeObject<Dictionary<string, object>>(variables);
                    var email = deserializedVariables["approverEmail"].ToString().Trim();
                    var comment = deserializedVariables["approvalComment"].ToString();

                    // Validate email address
                    if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                    {
                        Console.WriteLine("Invalid email format: " + email);
                        return; // Optionally, you could fail the job here
                    }

                    var body = $"Yêu cầu của bạn đã được phê duyệt. Ý kiến: {comment}";
                    await emailSender.SendEmailAsync(email, "Phê duyệt thành công", body);

            //        await jobClient.NewCompleteJobCommand(job.Key)
            //.Variables($"{{\"approved\": true}}").Send();
                })
                .MaxJobsActive(5)
                .Name("approved-email-worker-1")
                .PollInterval(TimeSpan.FromSeconds(1))
                .Timeout(TimeSpan.FromSeconds(10))
                .Open();

            var workerApproved = zeebeClient.NewWorker()
                .JobType("send-approval-email")
                .Handler(async (jobClient, job) =>
                {
                    // Retrieve information from job
                    var variables = job.Variables;
                    var deserializedVariables = JsonConvert.DeserializeObject<Dictionary<string, object>>(variables);
                    var email = deserializedVariables["approverEmail"].ToString().Trim();
                    var comment = deserializedVariables["approvalComment"].ToString();

                    // Validate email address
                    if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                    {
                        Console.WriteLine("Invalid email format: " + email);
                        return; // Optionally, you could fail the job here
                    }

                    var body = $"Yêu cầu của bạn đã được phê duyệt. Ý kiến: {comment}";
                    await emailSender.SendEmailAsync(email, "Phê duyệt thành công", body);

                    await jobClient.NewCompleteJobCommand(job.Key).Send();
                })
                .MaxJobsActive(5)
                .Name("approved-email-worker")
                .PollInterval(TimeSpan.FromSeconds(1))
                .Timeout(TimeSpan.FromSeconds(10))
                .Open();

            // Worker xử lý nhiệm vụ gửi email từ chối
            var workerRejected = zeebeClient.NewWorker()
                .JobType("send-rejection-email")
                .Handler(async (jobClient, job) =>
                {
                    // Retrieve information from job
                    var variables = job.Variables;
                    var deserializedVariables = JsonConvert.DeserializeObject<Dictionary<string, object>>(variables);
                    var email = deserializedVariables["approverEmail"].ToString().Trim();
                    var comment = deserializedVariables["approvalComment"].ToString();

                    // Validate email address
                    if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                    {
                        Console.WriteLine("Invalid email format: " + email);
                        return; // Optionally, you could fail the job here
                    }
                    var body = $"Yêu cầu của bạn đã bị từ chối. Ý kiến: {comment}";
                    await emailSender.SendEmailAsync(email, "Yêu cầu bị từ chối", body);

                    await jobClient.NewCompleteJobCommand(job.Key).Send();
                })
                .MaxJobsActive(5)
                .Name("rejected-email-worker")
                .PollInterval(TimeSpan.FromSeconds(1))
                .Timeout(TimeSpan.FromSeconds(10))
                .Open();

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
