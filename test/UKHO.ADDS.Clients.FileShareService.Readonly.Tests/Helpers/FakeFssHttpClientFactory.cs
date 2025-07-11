﻿using System.Net;
using System.Text;
using UKHO.ADDS.Clients.Common.Constants;
using UKHO.ADDS.Infrastructure.Serialization.Json;

namespace UKHO.ADDS.Clients.FileShareService.ReadOnly.Tests.Helpers
{
    public class FakeFssHttpClientFactory : DelegatingHandler, IHttpClientFactory
    {
        private readonly Func<HttpRequestMessage, (HttpStatusCode, object)> _httpMessageHandler;
        private HttpClient _httpClient;

        public FakeFssHttpClientFactory(Func<HttpRequestMessage, (HttpStatusCode, object)> httpMessageHandler) => _httpMessageHandler = httpMessageHandler;

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
            var (httpStatusCode, responseValue) = _httpMessageHandler(request);
            var response = new HttpResponseMessage { StatusCode = httpStatusCode };

            switch (responseValue)
            {
                case null:
                    break;
                case Stream stream:
                    response.Content = new StreamContent(stream);
                    break;
                default:
                    response.Content = new StringContent(JsonCodec.Encode(responseValue), Encoding.UTF8, ApiHeaderKeys.ContentTypeJson);
                    break;
            }

            return Task.FromResult(response);
        }
    }
}
