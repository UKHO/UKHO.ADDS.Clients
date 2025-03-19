using UKHO.ADDS.Clients.Common.Authentication;

namespace UKHO.ADDS.Clients.PermitService
{
    public interface IPermitClientFactory
    {
        IPermitClient CreateClient(string baseAddress, string accessToken);

        IPermitClient CreateClient(string baseAddress, IAuthenticationTokenProvider tokenProvider);
    }
}
