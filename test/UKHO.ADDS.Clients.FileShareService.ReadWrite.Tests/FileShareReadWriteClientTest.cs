using System.Net;
using System.Text;
using FakeItEasy;
using NUnit.Framework;
using UKHO.ADDS.Clients.Common.Authentication;
using UKHO.ADDS.Clients.FileShareService.ReadWrite.Models;
using UKHO.ADDS.Infrastructure.Serialization.Json;

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
        private const string AccessToken = "TestAccessToken";
        private const string BaseAddress = "http://test.com";
        private const int MaxFileBlockSize = 8192;

        [SetUp]
        public void SetUp()
        {
            _httpClientFactory = A.Fake<IHttpClientFactory>();
            _authTokenProvider = A.Fake<IAuthenticationTokenProvider>();
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _httpClient = A.Fake<HttpClient>();
            _httpClientFactory = A.Fake<IHttpClientFactory>();
            _authTokenProvider = A.Fake<IAuthenticationTokenProvider>();
            _client = new FileShareReadWriteClient(_httpClientFactory, BaseAddress, _authTokenProvider);

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
                Content = new StringContent(JsonCodec.Encode(batchHandle), Encoding.UTF8, "application/json")
            };

            A.CallTo(() => _httpClient.SendAsync(A<HttpRequestMessage>._, A<CancellationToken>._)).Returns(Task.FromResult(responseMessage));

            var result = await _client.CreateBatchAsync(_batchModel, CorrelationId, CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess(out var value, out _), Is.True);
                Assert.That(value?.BatchId, Is.EqualTo(batchHandle.BatchId));
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

        [Test]
        public void WhenCreateHttpRequestMessageIsCalled_ThenHttpRequestMessageIsConfiguredCorrectly()
        {
            var uri = new Uri($"batch", UriKind.Relative);
            var batchModel = new BatchModel
            {
                BusinessUnit = "TestUnit",
                Acl = new Acl { ReadUsers = new[] { "user1" } },
                Attributes = new List<KeyValuePair<string, string>> { new("key", "value") },
                ExpiryDate = DateTime.UtcNow.AddDays(1)
            };

            var method = typeof(FileShareReadWriteClient).GetMethod("CreateHttpRequestMessage",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var httpRequestMessage = (HttpRequestMessage)method.Invoke(_client, new object[] { uri, batchModel });

            Assert.Multiple(() =>
            {
                Assert.That(httpRequestMessage!.Method, Is.EqualTo(HttpMethod.Post));
                Assert.That(httpRequestMessage.RequestUri, Is.EqualTo(uri));
                Assert.That(httpRequestMessage.Content, Is.Not.Null);

                var content = httpRequestMessage.Content!.ReadAsStringAsync().Result;
                var serializedBatchModel = JsonCodec.Encode(batchModel);
                Assert.That(content, Is.EqualTo(serializedBatchModel));

                Assert.That(httpRequestMessage.Content.Headers.ContentType!.MediaType, Is.EqualTo("application/json"));
            });
        }

        [Test]
        public void WhenConstructorIsCalledWithHttpClientFactoryBaseAddressAndAccessToken_ThenHttpClientFactoryIsNotNull()
        {
            var client = new FileShareReadWriteClient(_httpClientFactory, BaseAddress, "TestAccessToken");

            Assert.Multiple(() =>
            {
                Assert.That(client, Is.Not.Null);
                Assert.That(client, Is.InstanceOf<FileShareReadWriteClient>());

                var baseAddressField = typeof(FileShareReadWriteClient)
                    .GetField("_httpClientFactory", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                var httpClientFactory = baseAddressField?.GetValue(client) as IHttpClientFactory;
                Assert.That(httpClientFactory, Is.Not.Null);
            });
        }

        [Test]
        public void WhenConstructorIsCalledWithHttpClientFactoryBaseAddressAndAuthTokenProvider_ThenInstanceIsCreated()
        {
            var client = new FileShareReadWriteClient(_httpClientFactory, BaseAddress, _authTokenProvider);

            Assert.Multiple(() =>
            {
                Assert.That(client, Is.Not.Null);
                Assert.That(client, Is.InstanceOf<FileShareReadWriteClient>());
            });
        }

        [Test]
        public void WhenConstructorIsCalledWithHttpClientFactoryBaseAddressAndAccessToken_ThenInstanceIsCreated()
        {
            var client = new FileShareReadWriteClient(_httpClientFactory, BaseAddress, AccessToken);

            Assert.Multiple(() =>
            {
                Assert.That(client, Is.Not.Null);
                Assert.That(client, Is.InstanceOf<FileShareReadWriteClient>());
            });
        }

        [Test]
        public void WhenConstructorIsCalledWithHttpClientFactoryBaseAddressAccessTokenAndMaxFileBlockSize_ThenInstanceIsCreated()
        {
            var client = new FileShareReadWriteClient(_httpClientFactory, BaseAddress, AccessToken, MaxFileBlockSize);

            Assert.Multiple(() =>
            {
                Assert.That(client, Is.Not.Null);
                Assert.That(client, Is.InstanceOf<FileShareReadWriteClient>());

                var maxFileBlockSizeField = typeof(FileShareReadWriteClient)
                    .GetField("_maxFileBlockSize", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                var maxFileBlockSize = (int)maxFileBlockSizeField?.GetValue(client);
                Assert.That(maxFileBlockSize, Is.EqualTo(MaxFileBlockSize));
            });
        }

        [Test]
        public void WhenConstructorIsCalledWithHttpClientFactoryBaseAddressAuthTokenProviderAndMaxFileBlockSize_ThenInstanceIsCreated()
        {
            var client = new FileShareReadWriteClient(_httpClientFactory, BaseAddress, _authTokenProvider, MaxFileBlockSize);

            Assert.Multiple(() =>
            {
                Assert.That(client, Is.Not.Null);
                Assert.That(client, Is.InstanceOf<FileShareReadWriteClient>());

                var maxFileBlockSizeField = typeof(FileShareReadWriteClient)
                    .GetField("_maxFileBlockSize", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                var maxFileBlockSize = (int)maxFileBlockSizeField?.GetValue(client);
                Assert.That(maxFileBlockSize, Is.EqualTo(MaxFileBlockSize));
            });
        }
    }
}
