namespace UKHO.ADDS.Clients.FileShareService.ReadOnly.Authentication
{
    public interface IAuthenticationTokenProvider
    {
        Task<string> GetTokenAsync();
    }
}
