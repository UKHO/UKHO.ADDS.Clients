using UKHO.ADDS.Clients.Common.Authentication;
using UKHO.ADDS.Clients.Common.Factories;

namespace UKHO.ADDS.Clients.PermitService
{
    public class PermitClient : IPermitClient
    {
        private readonly IAuthenticationTokenProvider _authTokenProvider;
        private readonly IHttpClientFactory _httpClientFactory;

        public PermitClient(IHttpClientFactory httpClientFactory, string baseAddress, IAuthenticationTokenProvider authTokenProvider)
        {
            _httpClientFactory = new SetBaseAddressHttpClientFactory(httpClientFactory, new Uri(baseAddress));
            _authTokenProvider = authTokenProvider;
        }

        public PermitClient(IHttpClientFactory httpClientFactory, string baseAddress, string accessToken) :
            this(httpClientFactory, baseAddress, new DefaultAuthenticationTokenProvider(accessToken))
        {
        }


    }
}
