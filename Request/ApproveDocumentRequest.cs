namespace DemoCamunda.Request
{
    public class ApproveDocumentRequest
    {
        public string InstanceId { get; set; }
        public bool Decision { get; set; } // true for approved, false for rejected
    }
}
