namespace DemoCamunda.Request
{
    public class CompleteWorkflowRequest
    {
        public bool Approved { get; set; }
        public string Email { get; set; }
        public long WorkflowInstanceKey { get; set; }
    }
}
