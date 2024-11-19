using DemoCamunda.Interfaces;
using DemoCamunda.Request;
using Microsoft.AspNetCore.Mvc;

namespace DemoCamunda.Controllers
{
    [Route("api")]
    [ApiController]
    public class CamundaController : ControllerBase
    {
        private readonly ICamundaService _camundaService;

        public CamundaController(ICamundaService camundaService)
        {
            _camundaService = camundaService;
        }

        [HttpPost("start-process")]
        public async Task<IActionResult> StartProcess([FromBody] StartProcessRequest request)
        {
            var variables = new
            {
                recipient = new { value = request.Recipient, type = "String" },
                subject = new { value = request.Subject, type = "String" },
                content = new { value = request.Content, type = "String" }
            };

            var result = await _camundaService.StartProcessAsync("email_approval_process", variables);
            return Ok(result);
        }

        [HttpPost("complete-task/{taskId}")]
        public async Task<IActionResult> CompleteTask(string taskId, [FromBody] ApproveRequest request)
        {
            await _camundaService.CompleteTaskAsync(taskId, request.Approved);
            return Ok(new { message = "Task completed successfully" });
        }

        [HttpPost("email/send-approval")]
        public IActionResult SendApprovalEmail([FromBody] EmailRequest request)
        {
            return Ok(new { message = "Approval email sent successfully" });
        }

        [HttpPost("email/send-rejection")]
        public IActionResult SendRejectionEmail([FromBody] EmailRequest request)
        {
            return Ok(new { message = "Rejection email sent successfully" });
        }
    }
}
