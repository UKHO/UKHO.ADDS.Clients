﻿using System.Net;
using System.Text;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.Clients.PermitService.Tests.Helpers
{
    public class FakePermitHttpClientFactory(Func<HttpRequestMessage, (HttpStatusCode, object)> httpMessageHandler) : DelegatingHandler, IHttpClientFactory
    {
        private HttpClient _httpClient;

        public HttpClient HttpClient
        {
            get => _httpClient ?? CreateClient("");
            private set => _httpClient = value;
        }

        public HttpClient CreateClient(string name)
        {
            _httpClient = new HttpClient(this);
            return HttpClient;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var (httpStatusCode, responseValue) = httpMessageHandler(request);
            var response = new HttpResponseMessage { StatusCode = httpStatusCode };

            switch (responseValue)
            {
                case null:
                    break;
                case Stream stream:
                    response.Content = new StreamContent(stream);
                    break;
                default:
                    response.Content = new StringContent(JsonCodec.Encode(responseValue), Encoding.UTF8, "application/json");
                    break;
            }

            return Task.FromResult(response);
        }
    }
}
