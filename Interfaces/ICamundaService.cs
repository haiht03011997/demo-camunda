namespace DemoCamunda.Interfaces
{
    public interface ICamundaService
    {
        Task<object> StartProcessAsync(string processKey, object variables);
        Task CompleteTaskAsync(string taskId, bool approved);
    }
}
