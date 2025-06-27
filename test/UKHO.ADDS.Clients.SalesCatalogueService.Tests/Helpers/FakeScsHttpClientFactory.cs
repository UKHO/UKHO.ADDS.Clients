using System.Net;
using System.Text;
using UKHO.ADDS.Clients.Common.Constants;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.Clients.SalesCatalogueService.Tests.Helpers
{
    public class FakeScsHttpClientFactory : DelegatingHandler, IHttpClientFactory
    {
        private readonly Func<HttpRequestMessage, Task<(HttpStatusCode, object, DateTime?, DateTime?)>> _asyncResponseGenerator;
        private readonly Func<HttpRequestMessage, (HttpStatusCode, object, DateTime?, DateTime?)> _syncResponseGenerator;
        private readonly bool _isAsync;

        public FakeScsHttpClientFactory(Func<HttpRequestMessage, Task<(HttpStatusCode, object, DateTime?, DateTime?)>> asyncResponseGenerator)
        {
            _asyncResponseGenerator = asyncResponseGenerator;
            _isAsync = true;
            HttpClient = new HttpClient(this);
        }

        public FakeScsHttpClientFactory(Func<HttpRequestMessage, (HttpStatusCode, object, DateTime?, DateTime?)> responseGenerator)
        {
            _syncResponseGenerator = responseGenerator;
            _isAsync = false;
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

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var (statusCode, responseBody, contentLastModified, headerLastModified) = _isAsync 
                ? await _asyncResponseGenerator(request) 
                : _syncResponseGenerator(request);

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

            return response;
        }

        private static StringContent CreateJsonContent(object value, DateTime? contentLastModified)
        {
            var content = new StringContent(JsonCodec.Encode(value), Encoding.UTF8, ApiHeaderKeys.ContentTypeJson);
            if (contentLastModified.HasValue)
                content.Headers.LastModified = contentLastModified;
            return content;
        }
    }
}
