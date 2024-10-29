using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Zeebe.Client;

namespace DemoCamunda.Controllers
{
    [Route("api")]
    [ApiController]
    public class CamundaController : ControllerBase
    {
        private readonly IZeebeClient _zeebeClient;

        public CamundaController(IZeebeClient zeebeClient)
        {
            _zeebeClient = zeebeClient;
        }

        // Endpoint to upload and deploy a BPMN file
        [HttpPost("deploy")]
        public async Task<IActionResult> DeployBpmnFile()
        {
            try
            {
                var bpmnFilePath = Path.Combine(Directory.GetCurrentDirectory(), "BpmnFiles", "approval-process.bpmn");
                if (!System.IO.File.Exists(bpmnFilePath))
                {
                    return BadRequest("BPMN file not found.");
                }
                // Deploy the BPMN file to Zeebe
                var deployment = await _zeebeClient.NewDeployCommand()
                    .AddResourceFile(bpmnFilePath) // Add the BPMN file
                    .Send();

                // Return the deployment details
                return Ok(new
                {
                    DeploymentKey = deployment.Key,
                    Processes = deployment.Processes.Select(p => new
                    {
                        p.BpmnProcessId,
                        p.Version,
                        p.ProcessDefinitionKey
                    })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Deployment failed: {ex.Message}");
            }
        }

        [HttpPost("process/start")]
        public async Task<IActionResult> StartProcess([FromBody] string approverEmail)
        {
            var processInstance = await _zeebeClient.NewCreateProcessInstanceCommand()
                .BpmnProcessId("document-approval-process") // ID của BPMN Process
                .LatestVersion()
                .Variables($"{{ \"approverEmail\": \"{approverEmail}\" }}") // Biến đầu vào
                .Send();

            return Ok(new { Message = "Process started", ProcessInstanceKey = processInstance.ProcessInstanceKey });
        }

        // Endpoint để hoàn thành user task sau khi chọn món
        [HttpPost("process/complete-task")]
        public async Task<IActionResult> CompleteTask([FromForm] string meal)
        {

            // Worker sẽ tự động nhận và xử lý công việc dựa trên dữ liệu món ăn đã chọn
            var jobWorker = _zeebeClient.NewWorker()
                .JobType("decide-dinner")  // Thay thế với JobType trong BPMN của bạn
                .Handler(async (jobClient, job) =>
                {
                    // Gửi biến "meal" đã chọn vào Zeebe
                    var variables = new Dictionary<string, object>
                    {
                        { "meal", meal }
                    };

                    // Hoàn thành công việc với biến đã chọn
                    await jobClient.NewCompleteJobCommand(job.Key)
                        .Variables(JsonConvert.SerializeObject( variables))
                        .Send();

                    Console.WriteLine($"User selected: {meal}");
                })
                .MaxJobsActive(5)
                .Name("user-task-worker")
                .PollInterval(TimeSpan.FromSeconds(1))
                .Timeout(TimeSpan.FromSeconds(10))
                .Open();

            return Ok($"User selected {meal} for dinner.");
        }
    }
}
