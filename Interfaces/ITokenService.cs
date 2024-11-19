namespace DemoCamunda.Interfaces
{
    public interface ITokenService
    {
        Task<string> GetAccessTokenAsync();
    }
}
