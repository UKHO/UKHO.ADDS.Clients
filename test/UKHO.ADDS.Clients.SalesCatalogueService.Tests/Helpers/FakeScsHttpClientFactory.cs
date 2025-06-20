using System.Net;
using System.Text;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.Clients.SalesCatalogueService.Tests.Helpers
{
    public class FakeScsHttpClientFactory : DelegatingHandler, IHttpClientFactory
    {
        private readonly Func<HttpRequestMessage, (HttpStatusCode, object, DateTime?, DateTime?)> _responseGenerator;

        public FakeScsHttpClientFactory(Func<HttpRequestMessage, (HttpStatusCode, object, DateTime?, DateTime?)> responseGenerator)
        {
            _responseGenerator = responseGenerator;
            HttpClient = new HttpClient(this);
        }

        // Backward compatibility constructor
        public FakeScsHttpClientFactory(Func<HttpRequestMessage, (HttpStatusCode, object, DateTime?)> simpleGenerator)
            : this(req =>
            {
                var (code, body, contentLastModified) = simpleGenerator(req);
                return (code, body, contentLastModified, null);
            })
        { }

        public HttpClient HttpClient { get; private set; }

        public HttpClient CreateClient(string name) => HttpClient;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var (statusCode, responseBody, contentLastModified, headerLastModified) = _responseGenerator(request);

            var response = new HttpResponseMessage(statusCode)
            {
                Content = responseBody switch
                {
                    null => null,
                    Stream stream => new StreamContent(stream),
                    HttpContent httpContent => httpContent,
                    _ => CreateJsonContent(responseBody, contentLastModified)
                }
            };

            // Set main response header if provided
            if (headerLastModified.HasValue)
                response.Headers.TryAddWithoutValidation("Last-Modified", headerLastModified.Value.ToString("R"));

            return Task.FromResult(response);
        }

        private static StringContent CreateJsonContent(object value, DateTime? contentLastModified)
        {
            var content = new StringContent(JsonCodec.Encode(value), Encoding.UTF8, "application/json");
            if (contentLastModified.HasValue)
                content.Headers.LastModified = contentLastModified;
            return content;
        }
    }
}
