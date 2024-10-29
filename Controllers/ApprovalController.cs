using DemoCamunda.Interfaces;
using DemoCamunda.Request;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Zeebe.Client;
using Zeebe.Client.Api.Responses;

namespace DemoCamunda.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ApprovalController : ControllerBase
    {
        private readonly IZeebeClient _zeebeClient;
        private readonly IEmailSender _emailSender;

        public ApprovalController(IZeebeClient zeebeClient, IEmailSender emailSender)
        {
            _zeebeClient = zeebeClient;
            _emailSender = emailSender;
        }
        [HttpPost("start-approval")]
        public async Task<IActionResult> StartApproval([FromBody] Dictionary<string, string> variables)
        {
            try
            {
                // Serialize the dictionary to a JSON string
                var variablesJson = JsonConvert.SerializeObject(variables);

                // Pass the JSON string to the Variables method
                IProcessInstanceResponse processInstance = await _zeebeClient.NewCreateProcessInstanceCommand()
                    .BpmnProcessId("document-approval-process")
                    .LatestVersion()
                    .Variables(variablesJson)
                    .Send();

                return Ok(new { processInstance.ProcessInstanceKey });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error starting approval workflow: {ex.Message}");
            }
        }

        [HttpPost("send-email")]
        public async Task<IActionResult> SendApprovalEmail([FromBody] EmailRequest emailRequest)
        {
            try
            {
                await _emailSender.SendEmailAsync(emailRequest.ToEmail, "Phê duyệt thành công", "Yêu cầu của bạn đã được phê duyệt.");
                return Ok("Email đã được gửi.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error sending email: {ex.Message}");
            }
        }

        // Endpoint for user to submit the approval result
        [HttpPost("approve")]
        public IActionResult CompleteUserTask([FromBody] bool isApproved)
        {
            return Ok(new { approved = isApproved });
        }
    }
}
