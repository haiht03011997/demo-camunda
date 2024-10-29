namespace DemoCamunda.Request
{
    public class EmailRequest
    {
        public required string ToEmail { get; set; }
        public required string Body { get; set; }
    }
}
