using UKHO.ADDS.Clients.Common.Authentication;
using UKHO.ADDS.Clients.Common.Factories;

namespace UKHO.ADDS.Clients.SalesCatalogueService
{
    public class SalesCatalogueClient : ISalesCatalogueClient
    {
        private readonly IAuthenticationTokenProvider _authTokenProvider;
        private readonly IHttpClientFactory _httpClientFactory;

        public SalesCatalogueClient(IHttpClientFactory httpClientFactory, string baseAddress, IAuthenticationTokenProvider authTokenProvider)
        {
            _httpClientFactory = new SetBaseAddressHttpClientFactory(httpClientFactory, new Uri(baseAddress));
            _authTokenProvider = authTokenProvider;
        }

        public SalesCatalogueClient(IHttpClientFactory httpClientFactory, string baseAddress, string accessToken) :
            this(httpClientFactory, baseAddress, new DefaultAuthenticationTokenProvider(accessToken))
        {
        }


    }
}
