using System.Net;
using UKHO.ADDS.Clients.Common.Authentication;
using UKHO.ADDS.Clients.Common.Extensions;
using UKHO.ADDS.Clients.Common.Factories;
using UKHO.ADDS.Clients.SalesCatalogueService.Models;
using UKHO.ADDS.Infrastructure.Results;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.Clients.SalesCatalogueService
{
    public class SalesCatalogueClient : ISalesCatalogueClient
    {
        private readonly IAuthenticationTokenProvider _authTokenProvider;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly HttpClient _httpClient;

        public SalesCatalogueClient(IHttpClientFactory httpClientFactory, string baseAddress, IAuthenticationTokenProvider authTokenProvider)
        {
            _httpClientFactory = new SetBaseAddressHttpClientFactory(httpClientFactory, new Uri(baseAddress));
            _authTokenProvider = authTokenProvider;
            _httpClient = _httpClientFactory.CreateClient();
        }

        public SalesCatalogueClient(IHttpClientFactory httpClientFactory, string baseAddress, string accessToken) :
            this(httpClientFactory, baseAddress, new DefaultAuthenticationTokenProvider(accessToken))
        {
        }

        public SalesCatalogueClient(HttpClient httpClient, string accessToken)
        {
            _httpClient = httpClient;
            _authTokenProvider = new DefaultAuthenticationTokenProvider(accessToken);
            _httpClientFactory = null;
        }

        /// <summary>
        /// Retrieves S100 products from a specific date asynchronously.
        /// </summary>
        /// <param name="apiVersion">The API version to use.</param>
        /// <param name="productType">The type of product to retrieve.</param>
        /// <param name="sinceDateTime">The date and time since when the products are to be retrieved.</param>
        /// <param name="correlationId">The correlation ID for the request.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the S100 sales catalogue response.</returns>
        public async Task<IResult<S100SalesCatalogueResponse>> GetS100ProductsFromSpecificDateAsync(string apiVersion, string productType, string sinceDateTime, string correlationId)
        {
            var uri = $"/{apiVersion}/catalogues/{productType}/basic";

            await _httpClient.SetAuthenticationHeaderAsync(_authTokenProvider);
            _httpClient.SetCorrelationIdHeaderAsync(correlationId);

            try
            {
                using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri))
                {
                    //set "If-Modified-Since" header value in request header if value is passed
                    if (!string.IsNullOrEmpty(sinceDateTime))
                    {
                        httpRequestMessage.Headers.Add("If-Modified-Since", sinceDateTime);
                    }

                    var response = await _httpClient.SendAsync(httpRequestMessage);

                    //create response result as per expected output by reading additional values from httpresponse object 
                    return await CreateS100ProductsFromSpecificDateResponse(response);
                }
            }
            catch (Exception ex)
            {
                return Result.Failure<S100SalesCatalogueResponse>(ex);
            }
        }

        public async Task<IResult<SalesCatalogueResponse>> GetProductsFromSpecificDateAsync(string sinceDateTime, string correlationId)
        {
            // Implementation for GetProductsFromSpecificDateAsync
            return Result.Success(new SalesCatalogueResponse());
        }

        public async Task<IResult<SalesCatalogueResponse>> PostProductIdentifiersAsync(List<string> productIdentifiers, string correlationId)
        {
            var code = HttpStatusCode.BadRequest;
            return await Task.FromResult(Result.Failure<SalesCatalogueResponse>(ErrorFactory.CreateError(code)));
        }

        public async Task<IResult<SalesCatalogueResponse>> PostProductVersionsAsync(List<ProductVersionRequest> productVersions, string correlationId)
        {
            try
            {
                return await Task.FromResult(Result.Success(new SalesCatalogueResponse()));
            }
            catch (Exception ex)
            {
                return await Task.FromResult(Result.Failure<SalesCatalogueResponse>(ex));
            }
        }

        public async Task<IResult<SalesCatalogueDataResponse>> GetSalesCatalogueDataResponse(string batchId, string correlationId)
        {
            return await Task.FromResult(Result.Success(new SalesCatalogueDataResponse()));
        }

        /// <summary>
        /// Creates a response for S100 products retrieved from a specific date.
        /// </summary>
        /// <param name="httpResponseMessage">The HTTP response message.</param>
        /// <returns>The result contains the S100 sales catalogue response.</returns>
        private async Task<IResult<S100SalesCatalogueResponse>> CreateS100ProductsFromSpecificDateResponse(HttpResponseMessage httpResponseMessage)
        {
            var response = new S100SalesCatalogueResponse();

            if (httpResponseMessage.StatusCode != HttpStatusCode.OK && httpResponseMessage.StatusCode != HttpStatusCode.NotModified)
            {
                return Result.Failure<S100SalesCatalogueResponse>(ErrorFactory.CreateError(httpResponseMessage.StatusCode));
            }
            else
            {
                response.ResponseCode = httpResponseMessage.StatusCode;

                if (httpResponseMessage.StatusCode == HttpStatusCode.OK)
                {
                    var bodyJson = await httpResponseMessage.Content.ReadAsStringAsync();

                    //Deserialize Json response body to S100Products
                    var products = JsonCodec.Decode<List<S100Products>>(bodyJson);

                    response.ResponseBody = products ?? new List<S100Products>();
                }

                var lastModified = httpResponseMessage.Content.Headers.LastModified;
                if (lastModified != null)
                {
                    response.LastModified = ((DateTimeOffset)lastModified).UtcDateTime;
                }
            }
            return Result.Success(response);
        }
    }
}
