namespace DemoCamunda.Config
{
    public class ZeebeOptions
    {
        public string ClusterId { get; set; } = default!;
        public string ClientId { get; set; } = default!;
        public string ClientSecret { get; set; } = default!;
        public string Region { get; set; } = default!;
    }

}
