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
        private HttpClient _httpClient;
        private IHttpClientFactory _httpClientFactory;
        private IAuthenticationTokenProvider _authTokenProvider;
        private FileShareReadWriteClient _client;
        private BatchModel _batchModel;
        private const string CorrelationId = "TestCorrelationId";

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _httpClient = A.Fake<HttpClient>();
            _httpClientFactory = A.Fake<IHttpClientFactory>();
            _authTokenProvider = A.Fake<IAuthenticationTokenProvider>();
            _client = new FileShareReadWriteClient(_httpClientFactory, "http://test.com", _authTokenProvider);

            A.CallTo(() => _httpClientFactory.CreateClient(A<string>._)).Returns(_httpClient);

            _batchModel = new BatchModel
            {
                BusinessUnit = "TestUnit",
                Acl = new Acl { ReadUsers = new[] { "user1" } },
                Attributes = new List<KeyValuePair<string, string>> { new("key", "value") },
                ExpiryDate = DateTime.UtcNow.AddDays(1)
            };
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _httpClient.Dispose();
        }

        [Test]
        public async Task WhenCreateBatchAsyncIsCalledWithValidBatchModel_ThenReturnSuccessResult()
        {
            var batchHandle = new BatchHandle("batchId");

            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(batchHandle), Encoding.UTF8, "application/json")
            };

            A.CallTo(() => _httpClient.SendAsync(A<HttpRequestMessage>._, A<CancellationToken>._)).Returns(Task.FromResult(responseMessage));

            var result = await _client.CreateBatchAsync(_batchModel, CorrelationId, CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess(out var valueCt, out _), Is.True);
                Assert.That(valueCt?.BatchId, Is.EqualTo(batchHandle.BatchId));
            });
        }

        [Test]
        public async Task WhenCreateBatchAsyncIsCalledWithInvalidResponse_ThenReturnFailureResult()
        {
            var responseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest);

            A.CallTo(() => _httpClient.SendAsync(A<HttpRequestMessage>._, A<CancellationToken>._)).Returns(Task.FromResult(responseMessage));

            var result = await _client.CreateBatchAsync(_batchModel, CorrelationId, CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.False);
                Assert.That(result.Errors.FirstOrDefault()?.Message,
                    Is.EqualTo("Bad request"));
            });
        }

        [Test]
        public async Task WhenCreateBatchAsyncThrowsException_ThenReturnFailureResult()
        {
            A.CallTo(() => _httpClient.SendAsync(A<HttpRequestMessage>._, A<CancellationToken>._)).Throws(new Exception("Test exception"));

            var result = await _client.CreateBatchAsync(_batchModel, CorrelationId, CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.False);
                Assert.That(result.Errors.FirstOrDefault()?.Message, Is.EqualTo("Test exception"));
            });
        }
    }
}
