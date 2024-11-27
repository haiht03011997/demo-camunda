using DemoCamunda.Request;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Zeebe.Client;

namespace DemoCamunda.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WorkflowController : ControllerBase
{
    private readonly IZeebeClient _zeebeClient;

    public WorkflowController(IZeebeClient zeebeClient)
    {
        _zeebeClient = zeebeClient;
    }

    [HttpPost("start")]
    public async Task<IActionResult> StartWorkflow([FromBody] StartWorkflowRequest request)
    {
        // Serialize the variables into JSON
        var variables = JsonSerializer.Serialize(new
        {
            email = request.Email,
        });
        // Create a new workflow instance
        var workflowInstance = await _zeebeClient
            .NewCreateProcessInstanceCommand()
            .BpmnProcessId("approval-process") // Use the process ID defined in your BPMN
            .LatestVersion()
            .Variables(variables)
            .Send();

        return Ok(new { workflowInstance.ProcessInstanceKey });
    }

    [HttpPost("complete")]
    public async Task<IActionResult> CompleteWorkflow([FromBody] CompleteWorkflowRequest request)
    {
        // Serialize the variables into JSON
        var variables = JsonSerializer.Serialize(new
        {
            email = request.Email,
            approved = request.Approved // Default value for the approval status
        });
        await _zeebeClient.NewPublishMessageCommand()
            .MessageName("ApprovalMessage")
            .CorrelationKey(request.Email)
            .Variables(variables)
            .Send();

        return Ok("Workflow completed.");
    }
}
