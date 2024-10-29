using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Zeebe.Client;

public class ZeebeWorkerService : BackgroundService
{
    private readonly IZeebeClient _zeebeClient;

    public ZeebeWorkerService(IZeebeClient zeebeClient)
    {
        _zeebeClient = zeebeClient;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Khởi tạo worker để xử lý user tasks
        var jobWorker = _zeebeClient.NewWorker()
            .JobType("decide-dinner") // Thay thế bằng JobType của bạn
            .Handler((jobClient, job) =>
                {
                // Parse the job.Variables (JSON) into a dictionary
                var variables = JsonSerializer.Deserialize<Dictionary<string, object>>(job.Variables);

                // Check if 'meal' exists in the dictionary
                if (variables.TryGetValue("meal", out var meal))
                {
                    Console.WriteLine($"Meal selected: {meal}");
                }
                else
                {
                    Console.WriteLine("Meal not specified.");
                }

                // Complete the job
                jobClient.NewCompleteJobCommand(job.Key).Send().Wait();
            })
            .MaxJobsActive(5)
            .Name("user-task-worker")
            .PollInterval(TimeSpan.FromSeconds(1))
            .Timeout(TimeSpan.FromSeconds(10))
            .Open();

        return Task.CompletedTask;
    }
}
