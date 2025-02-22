namespace UKHO.ADDS.Clients.FileShareService.Authentication
{
    public interface IAuthenticationTokenProvider
    {
        Task<string> GetTokenAsync();
    }
}
