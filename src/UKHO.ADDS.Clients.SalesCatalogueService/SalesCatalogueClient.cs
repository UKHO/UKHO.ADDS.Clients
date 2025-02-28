using System.Net;
using UKHO.ADDS.Clients.Common.Authentication;
using UKHO.ADDS.Clients.Common.Factories;
using UKHO.ADDS.Clients.SalesCatalogueService.Models;
using UKHO.ADDS.Infrastructure.Results;

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

        public async Task<IResult<SalesCatalogueResponse>> GetProductsFromSpecificDateAsync(string sinceDateTime, string correlationId)
        {
            return Result.Success(new SalesCatalogueResponse());
        }

        public async Task<IResult<SalesCatalogueResponse>> PostProductIdentifiersAsync(List<string> productIdentifiers, string correlationId)
        {
            var code = HttpStatusCode.BadRequest;
            return Result.Failure<SalesCatalogueResponse>(ErrorFactory.CreateError(code));
        }

        public async Task<IResult<SalesCatalogueResponse>> PostProductVersionsAsync(List<ProductVersionRequest> productVersions, string correlationId)
        {
            try
            {
                return Result.Success(new SalesCatalogueResponse());
            }
            catch (Exception ex)
            {
                return Result.Failure<SalesCatalogueResponse>(ex);
            }
        }

        public async Task<IResult<SalesCatalogueDataResponse>> GetSalesCatalogueDataResponse(string batchId, string correlationId)
        {
            return Result.Success(new SalesCatalogueDataResponse());
        }
    }
}
