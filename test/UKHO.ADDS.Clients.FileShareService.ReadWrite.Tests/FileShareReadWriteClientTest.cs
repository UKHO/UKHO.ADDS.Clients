using System.Net;
using System.Text;
using System.Text.Json;
using FakeItEasy;
using NUnit.Framework;
using UKHO.ADDS.Clients.Common.Authentication;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models;

namespace UKHO.ADDS.Clients.FileShareService.ReadWrite.Tests
{
    [TestFixture]
    internal class FileShareReadWriteClientTest
    {
        private IHttpClientFactory _httpClientFactory;
        private IAuthenticationTokenProvider _authTokenProvider;
        private FileShareReadWriteClient _client;
        private BatchModel _batchModel;

        [SetUp]
        public void SetUp()
        {
            _httpClientFactory = A.Fake<IHttpClientFactory>();
            _authTokenProvider = A.Fake<IAuthenticationTokenProvider>();
            _client = new FileShareReadWriteClient(_httpClientFactory, "http://test.com", _authTokenProvider);
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _batchModel = new BatchModel
            {
                BusinessUnit = "TestUnit",
                Acl = new Acl { ReadUsers = new[] { "user1" } },
                Attributes = new List<KeyValuePair<string, string>> { new("key", "value") },
                ExpiryDate = DateTime.UtcNow.AddDays(1)
            };
        }

        [Test]
        public async Task WhenCreateBatchAsyncWithValidBatchModel_ThenReturnsSuccessResult()
        {
            var httpClient = A.Fake<HttpClient>();
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new BatchHandle("batchId")), Encoding.UTF8, "application/json")
            };

            A.CallTo(() => _httpClientFactory.CreateClient(A<string>._)).Returns(httpClient);
            A.CallTo(() => httpClient.SendAsync(A<HttpRequestMessage>._, A<CancellationToken>._)).Returns(Task.FromResult(responseMessage));

            var result = await _client.CreateBatchAsync(_batchModel);

            result.IsSuccess(out var value, out var error);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(value?.BatchId, Is.EqualTo("batchId"));
        }

        [Test]
        public async Task WhenCreateBatchAsyncWithInvalidResponse_ThenReturnsFailureResult()
        {
            var httpClient = A.Fake<HttpClient>();
            var responseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest);

            A.CallTo(() => _httpClientFactory.CreateClient(A<string>._)).Returns(httpClient);
            A.CallTo(() => httpClient.SendAsync(A<HttpRequestMessage>._, A<CancellationToken>._)).Returns(Task.FromResult(responseMessage));

            var result = await _client.CreateBatchAsync(_batchModel);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Errors.FirstOrDefault()?.Message, Is.EqualTo("Failed to create batch. Status code: BadRequest"));
        }

        [Test]
        public async Task WhenCreateBatchAsyncWithException_ThenReturnsFailureResult()
        {
            var httpClient = A.Fake<HttpClient>();

            A.CallTo(() => _httpClientFactory.CreateClient(A<string>._)).Returns(httpClient);
            A.CallTo(() => httpClient.SendAsync(A<HttpRequestMessage>._, A<CancellationToken>._)).Throws(new Exception("Test exception"));

            var result = await _client.CreateBatchAsync(_batchModel);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Errors.FirstOrDefault()?.Message, Is.EqualTo("Test exception"));
        }
    }
}
