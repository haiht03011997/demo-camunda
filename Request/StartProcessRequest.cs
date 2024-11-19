namespace DemoCamunda.Request
{
    public class StartProcessRequest
    {
        public string Recipient { get; set; }
        public string Subject { get; set; }
        public string Content { get; set; }
    }
}
