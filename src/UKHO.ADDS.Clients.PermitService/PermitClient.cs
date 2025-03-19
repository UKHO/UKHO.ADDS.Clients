using System.Text.Json;
using System.Text;
using UKHO.ADDS.Clients.Common.Authentication;
using UKHO.ADDS.Clients.Common.Extensions;
using UKHO.ADDS.Clients.Common.Factories;
using UKHO.ADDS.Clients.PermitService.Models;
using UKHO.ADDS.Infrastructure.Results;
using UKHO.ADDS.Clients.Common.Constants;

namespace UKHO.ADDS.Clients.PermitService
{
    public class PermitClient : IPermitClient
    {
        private readonly IAuthenticationTokenProvider _authTokenProvider;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly HttpClient _httpClient;

        public PermitClient(IHttpClientFactory httpClientFactory, string baseAddress, IAuthenticationTokenProvider authTokenProvider)
        {
            _httpClientFactory = new SetBaseAddressHttpClientFactory(httpClientFactory, new Uri(baseAddress));
            _authTokenProvider = authTokenProvider;
        }

        public PermitClient(IHttpClientFactory httpClientFactory, string baseAddress, string accessToken) :
            this(httpClientFactory, baseAddress, new DefaultAuthenticationTokenProvider(accessToken))
        {
        }

        public PermitClient(HttpClient httpClient, string accessToken)
        {
            _httpClient= httpClient;
            _authTokenProvider = new DefaultAuthenticationTokenProvider(accessToken);
        }

        public async Task<IResult<Stream>> GetPermitAsync(string apiVersion, string productType, PermitRequest requestBody,
            string correlationId)
        {
            var uri = $"/{apiVersion}/permits/{productType}"; 

            await _httpClient.SetAuthenticationHeaderAsync(_authTokenProvider);
            _httpClient.SetCorrelationIdHeaderAsync(correlationId);

            try
            {
                using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri))
                {
                    httpRequestMessage.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

                    var httpResponse = await _httpClient.SendAsync(httpRequestMessage);

                    return await httpResponse.CreateResultAsync<Stream>(Applications.PermitService);                    
                }
            }
            catch (Exception ex)
            {
                return Result.Failure<Stream>(ex);
            }
        }
    }
}
