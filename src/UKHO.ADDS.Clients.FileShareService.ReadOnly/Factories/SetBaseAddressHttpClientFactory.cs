namespace UKHO.ADDS.Clients.FileShareService.ReadOnly.Factories
{
    internal class SetBaseAddressHttpClientFactory : IHttpClientFactory
    {
        private readonly Uri _baseAddress;
        private readonly IHttpClientFactory _httpClientFactoryImplementation;

        public SetBaseAddressHttpClientFactory(IHttpClientFactory httpClientFactoryImplementation, Uri baseAddress)
        {
            _httpClientFactoryImplementation = httpClientFactoryImplementation;
            _baseAddress = baseAddress;
        }

        public HttpClient CreateClient(string name)
        {
            var httpClient = _httpClientFactoryImplementation.CreateClient(name);
            httpClient.BaseAddress = _baseAddress;
            return httpClient;
        }
    }
}
