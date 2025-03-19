using UKHO.ADDS.Clients.Common.Authentication;

namespace UKHO.ADDS.Clients.PermitService
{
    public class PermitClientFactory : IPermitClientFactory
    {
        private readonly IHttpClientFactory _clientFactory;

        public PermitClientFactory(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public IPermitClient CreateClient(string baseAddress, string accessToken) => new PermitClient(_clientFactory, baseAddress, accessToken);

        public IPermitClient CreateClient(string baseAddress, IAuthenticationTokenProvider tokenProvider) => new PermitClient(_clientFactory, baseAddress, tokenProvider);
    }
}
