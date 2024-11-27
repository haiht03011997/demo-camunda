using System.Text.Json;
using DemoCamunda.Interfaces;
using Zeebe.Client;

namespace DemoCamunda.Workers
{
    public class ZeebeWorkerService : IHostedService
    {
        private readonly IZeebeClient _zeebeClient;
        private readonly IEmailSender _emailSender;
        // Dictionary để lưu trạng thái các job (hoặc thay bằng cơ sở dữ liệu)
        private static readonly HashSet<string> ProcessedJobs = new();
        public ZeebeWorkerService(IZeebeClient zeebeClient, IEmailSender emailSender)
        {
            _zeebeClient = zeebeClient;
            _emailSender = emailSender;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("Starting Zeebe workers...");
            // Worker for "send-email"
            _zeebeClient.NewWorker()
               .JobType("send-email") // Match BPMN job type
                .Handler(async (jobClient, job) =>
                {
                    Console.WriteLine("Worker for 'send-email' triggered.");

                    try
                    {
                        // Kiểm tra nếu job đã được xử lý
                        if (ProcessedJobs.Contains(job.Key.ToString()))
                        {
                            return; // Không xử lý lại job
                        }
                        // Deserialize the job variables
                        var variables = JsonSerializer.Deserialize<Dictionary<string, object>>(job.Variables);
                        var email = variables?["email"]?.ToString();

                        // Log email sending attempt
                        Console.WriteLine($"Preparing to send email to: {email}");

                        if (!string.IsNullOrEmpty(email))
                        {
                            await _emailSender.SendEmailAsync(email, "Phê duyệt tài liệu", "Kiểm tra và phê duyệt tài liệu");
                            Console.WriteLine("Email sent successfully.");
                        }
                    }
                    finally
                    {

                        await jobClient.NewCompleteJobCommand(job.Key).Send();
                        // Đánh dấu job là đã xử lý
                        ProcessedJobs.Add(job.Key.ToString());
                    }
                })
                .MaxJobsActive(5)
                .Name("send email")
                .PollInterval(TimeSpan.FromSeconds(1))
                .Timeout(TimeSpan.FromSeconds(60))
                .Open();

            // Worker for "send-approval-email"
            _zeebeClient.NewWorker()
                .JobType("send-approval-email")
                .Handler(async (jobClient, job) =>
                {
                    try
                    {
                        // Kiểm tra nếu job đã được xử lý
                        if (ProcessedJobs.Contains(job.Key.ToString()))
                        {
                            return; // Không xử lý lại job
                        }
                        var variables = JsonSerializer.Deserialize<Dictionary<string, object>>(job.Variables);
                        var email = variables?["email"]?.ToString();
                        await _emailSender.SendEmailAsync(email, "Approval Approved", "Your request has been approved.");
                    }
                    finally
                    {
                        await jobClient.NewCompleteJobCommand(job.Key).Send();
                        // Đánh dấu job là đã xử lý
                        ProcessedJobs.Add(job.Key.ToString());
                    }
                
                })

            .MaxJobsActive(5)
            .Name("Send mail confirm")
            .PollInterval(TimeSpan.FromSeconds(1))
            .Timeout(TimeSpan.FromSeconds(60))
                .Open();

            // Worker for "send-rejection-email"
            _zeebeClient.NewWorker()
                .JobType("send-rejection-email")
                .Handler(async (jobClient, job) =>
                {
                    try
                    {
                        // Kiểm tra nếu job đã được xử lý
                        if (ProcessedJobs.Contains(job.Key.ToString()))
                        {
                            return; // Không xử lý lại job
                        }
                        var variables = JsonSerializer.Deserialize<Dictionary<string, object>>(job.Variables);
                        var email = variables?["email"]?.ToString();
                        await _emailSender.SendEmailAsync(email, "Approval Rejected", "Your request has been rejected.");
                    }
                    finally
                    {
                        await jobClient.NewCompleteJobCommand(job.Key).Send();
                        // Đánh dấu job là đã xử lý
                        ProcessedJobs.Add(job.Key.ToString());
                    }
                })

            .MaxJobsActive(5)
            .Name("send email reject")
            .PollInterval(TimeSpan.FromSeconds(1))
            .Timeout(TimeSpan.FromSeconds(60))
                .Open();

            Console.WriteLine("Zeebe workers started.");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("Stopping Zeebe workers...");
            return Task.CompletedTask;
        }
    }
}
